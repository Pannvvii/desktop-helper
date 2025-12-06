using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DesktopTaskAid.Models;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Newtonsoft.Json.Linq;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Util; // For utilities (if needed)

namespace DesktopTaskAid.Services
{
    public enum CredentialStatus
    {
        Missing,
        Valid,
        Invalid
    }

    public enum CalendarImportOutcome
    {
        Success,
        NoEvents,
        Cancelled,
        AccessBlocked,
        MissingCredentials,
        InvalidCredentials,
        Error
    }

    public class CredentialState
    {
        public CredentialStatus Status { get; set; }
        public string Message { get; set; }
    }

    public class CalendarImportResult
    {
        public CalendarImportOutcome Outcome { get; set; }
        public List<TaskItem> Tasks { get; set; } = new List<TaskItem>();
        public string ErrorMessage { get; set; }
    }

    public sealed class CalendarImportService : IDisposable
    {
        private const string CredentialFileName = "google-credentials.json";
        private static readonly string[] CredentialPatterns =
        {
            "google-credentials.json",
            "google-credentials.json*",
            "credentials.json",
            "client_secret*.json",
            "client_secret*apps.googleusercontent.com.json"
        };

        private const string EmbeddedCredentialsJson = "{\"installed\":{\"client_id\":\"216624332793-aaif0eugih39tmr0nn1a857nmaum2i8c.apps.googleusercontent.com\",\"project_id\":\"caramel-medley-475320-r8\",\"auth_uri\":\"https://accounts.google.com/o/oauth2/auth\",\"token_uri\":\"https://oauth2.googleapis.com/token\",\"auth_provider_x509_cert_url\":\"https://www.googleapis.com/oauth2/v1/certs\",\"client_secret\":\"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\",\"redirect_uris\":[\"http://localhost\"]}}";

        private const string ApplicationName = "DesktopTaskAid";
        private static readonly string[] Scopes = { CalendarService.Scope.CalendarReadonly };

        private readonly StorageService _storageService;
        private readonly string _appDirectory;
        private string _credentialsPath;
        private string _tokenDirectory;
        private readonly object _stateLock = new object();
        private FileSystemWatcher _watcher;
        private CredentialState _credentialState;

        public event EventHandler CredentialsChanged;

        public CalendarImportService(StorageService storageService)
        {
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            // Change credentials location to per-user AppData, writable under MSI installs
            var dataFolder = _storageService.GetDataFolderPath();
            _credentialsPath = Path.Combine(dataFolder, CredentialFileName);
            _tokenDirectory = Path.Combine(dataFolder, "GoogleOAuth");

            // Ensure paths are writable; never store under Program Files
            EnsureWritablePaths(ref _credentialsPath, ref _tokenDirectory);

            Directory.CreateDirectory(_tokenDirectory);

            // If credentials file is missing, materialize embedded JSON to disk once
            TryMaterializeEmbeddedCredentials();

            _credentialState = ValidateCredentialFile();

            InitializeWatcher();
        }

        private static bool IsUnderProtectedSystemFolder(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;
            try
            {
                var full = Path.GetFullPath(path);
                return full.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), StringComparison.OrdinalIgnoreCase)
                    || full.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }

        private void EnsureWritablePaths(ref string credentialsPath, ref string tokenDir)
        {
            try
            {
                // If somehow resolving to a protected location, force to AppData
                var shouldRelocate = IsUnderProtectedSystemFolder(credentialsPath) || IsUnderProtectedSystemFolder(tokenDir);
                if (shouldRelocate)
                {
                    var appData = _storageService.GetDataFolderPath();
                    credentialsPath = Path.Combine(appData, CredentialFileName);
                    tokenDir = Path.Combine(appData, "GoogleOAuth");
                    LoggingService.Log("Relocated credentials and token paths to AppData to avoid write-denied errors.");
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Failed to ensure writable paths for credentials/token cache", ex);
            }
        }

        private void TryMaterializeEmbeddedCredentials()
        {
            try
            {
                if (!File.Exists(_credentialsPath) && !string.IsNullOrWhiteSpace(EmbeddedCredentialsJson))
                {
                    // Validate before writing
                    if (ValidateCredentialJson(EmbeddedCredentialsJson, out var _))
                    {
                        File.WriteAllText(_credentialsPath, EmbeddedCredentialsJson);
                        // Do NOT clear tokens here; let existing sign-in persist
                        LoggingService.Log("Embedded google-credentials.json written to AppData without clearing tokens.");
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Failed to materialize embedded credentials", ex);
            }
        }

        public CredentialState GetCredentialState()
        {
            lock (_stateLock)
            {
                return new CredentialState
                {
                    Status = _credentialState.Status,
                    Message = _credentialState.Message
                };
            }
        }

        // NEW: Public method to force a rescan of common locations and import automatically if found
        public CredentialState TryAutoImportNow()
        {
            try
            {
                var state = TryAutoImportCredentials();
                if (state != null)
                {
                    // Update cached state and notify listeners
                    return UpdateCredentialState(state);
                }

                return GetCredentialState();
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Auto-import rescan failed", ex);
                return GetCredentialState();
            }
        }

        public async Task<CredentialState> ImportCredentialsAsync(string sourcePath, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
            {
                return UpdateCredentialState(new CredentialState
                {
                    Status = CredentialStatus.Missing,
                    Message = "We couldn't find that google-credentials.json file."
                });
            }

            try
            {
                var json = await ReadAllTextAsync(sourcePath, cancellationToken).ConfigureAwait(false);
                if (!ValidateCredentialJson(json, out var validationMessage))
                {
                    return UpdateCredentialState(new CredentialState
                    {
                        Status = CredentialStatus.Invalid,
                        Message = validationMessage
                    });
                }

                await WriteAllTextAsync(_credentialsPath, json, cancellationToken).ConfigureAwait(false);
                ClearCachedTokens();

                var message = "Credentials saved. Click Import Next Month to sign in with Google.";
                return UpdateCredentialState(new CredentialState
                {
                    Status = CredentialStatus.Valid,
                    Message = message
                });
            }
            catch (UnauthorizedAccessException uae)
            {
                LoggingService.LogError("Permission denied saving google-credentials.json; switching to AppData.", uae);
                try
                {
                    var appData = _storageService.GetDataFolderPath();
                    _credentialsPath = Path.Combine(appData, CredentialFileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(_credentialsPath) ?? appData);
                    await WriteAllTextAsync(_credentialsPath, await ReadAllTextAsync(sourcePath, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
                    ClearCachedTokens();
                    return UpdateCredentialState(new CredentialState
                    {
                        Status = CredentialStatus.Valid,
                        Message = "Credentials saved to AppData. Click Import Next Month to sign in with Google."
                    });
                }
                catch (Exception ex)
                {
                    LoggingService.LogError("Failed fallback write to AppData", ex);
                    return UpdateCredentialState(new CredentialState
                    {
                        Status = CredentialStatus.Invalid,
                        Message = "We couldn't save google-credentials.json. Try picking the file again."
                    });
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Failed to import google-credentials.json", ex);
                return UpdateCredentialState(new CredentialState
                {
                    Status = CredentialStatus.Invalid,
                    Message = "We couldn't save google-credentials.json. Try picking the file again."
                });
            }
        }

        public async Task<CalendarImportResult> RunImportAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            // Ensure modern TLS
            try
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            }
            catch { }

            CredentialState credentialState;
            lock (_stateLock)
            {
                credentialState = _credentialState;
            }

            if (credentialState.Status == CredentialStatus.Missing)
            {
                return new CalendarImportResult
                {
                    Outcome = CalendarImportOutcome.MissingCredentials,
                    ErrorMessage = credentialState.Message
                };
            }

            if (credentialState.Status == CredentialStatus.Invalid)
            {
                return new CalendarImportResult
                {
                    Outcome = CalendarImportOutcome.InvalidCredentials,
                    ErrorMessage = credentialState.Message
                };
            }

            try
            {
                if (!File.Exists(_credentialsPath))
                {
                    LoggingService.LogError("Credentials file reported valid but missing on disk: " + _credentialsPath);
                    return new CalendarImportResult
                    {
                        Outcome = CalendarImportOutcome.MissingCredentials,
                        ErrorMessage = "google-credentials.json not found at runtime. Place it next to the .exe and try again."
                    };
                }

                var size = new FileInfo(_credentialsPath).Length;
                LoggingService.Log($"Loading Google credentials (size: {size} bytes) from {_credentialsPath}");

                using (var stream = new FileStream(_credentialsPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    GoogleClientSecrets secrets;
                    try
                    {
                        secrets = await GoogleClientSecrets.FromStreamAsync(stream, cancellationToken).ConfigureAwait(false);
                        LoggingService.Log("Client secrets parsed successfully");
                        LoggingService.Log($"ClientId: {secrets?.Secrets?.ClientId}");
                    }
                    catch (Exception ex)
                    {
                        LoggingService.LogError("Failed parsing Google client secrets", ex);
                        return new CalendarImportResult
                        {
                            Outcome = CalendarImportOutcome.InvalidCredentials,
                            ErrorMessage = string.IsNullOrWhiteSpace(ex.Message) ? "Unable to parse google-credentials.json." : ex.Message
                        };
                    }

                    if (secrets?.Secrets == null)
                    {
                        LoggingService.LogError("Parsed secrets object is null or missing inner Secrets");
                        return new CalendarImportResult
                        {
                            Outcome = CalendarImportOutcome.InvalidCredentials,
                            ErrorMessage = "Client secrets missing required fields. Download a fresh credentials file."
                        };
                    }

                    // Prepare data store (token cache)
                    LoggingService.Log($"Using token directory: {_tokenDirectory}");
                    FileDataStore dataStore;
                    try
                    {
                        dataStore = new FileDataStore(_tokenDirectory, true);
                    }
                    catch (Exception ex)
                    {
                        LoggingService.LogError("Failed to initialize token data store", ex);
                        return new CalendarImportResult
                        {
                            Outcome = CalendarImportOutcome.Error,
                            ErrorMessage = "Unable to initialize token cache directory. Run as a user with write permissions." + (ex.Message ?? string.Empty)
                        };
                    }

                    UserCredential credential = null;

                    // SILENT AUTH: if token already stored, attempt reuse via GoogleWebAuthorizationBroker (should not prompt)
                    if (HasStoredToken())
                    {
                        LoggingService.Log("Attempting silent OAuth using existing stored token");
                        try
                        {
                            credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                                secrets.Secrets,
                                Scopes,
                                "desktop-user",
                                cancellationToken,
                                dataStore).ConfigureAwait(false);
                            LoggingService.Log("Silent OAuth completed. Prompt displayed? " + (credential?.Token?.RefreshToken == null && credential?.Token?.AccessToken == null));
                        }
                        catch (Exception ex)
                        {
                            LoggingService.LogError("Silent OAuth reuse failed; falling back to interactive", ex);
                        }
                    }

                    if (credential == null)
                    {
                        // INTERACTIVE FLOW: only if silent failed / no token present
                        LoggingService.Log("Starting interactive OAuth authorization (AuthorizationCodeInstalledApp)");
                        try
                        {
                            var flowInitializer = new GoogleAuthorizationCodeFlow.Initializer
                            {
                                ClientSecrets = secrets.Secrets,
                                Scopes = Scopes,
                                DataStore = dataStore
                            };
                            var flow = new GoogleAuthorizationCodeFlow(flowInitializer);
                            var codeReceiver = new LocalServerCodeReceiver();
                            credential = await new AuthorizationCodeInstalledApp(flow, codeReceiver)
                                .AuthorizeAsync("desktop-user", cancellationToken).ConfigureAwait(false);
                            LoggingService.Log("Interactive OAuth flow completed. Credential obtained: " + (credential != null));
                        }
                        catch (TaskCanceledException)
                        {
                            LoggingService.Log("OAuth flow canceled by user");
                            return new CalendarImportResult
                            {
                                Outcome = CalendarImportOutcome.Cancelled,
                                ErrorMessage = "Sign-in was canceled before completion."
                            };
                        }
                        catch (TokenResponseException trex)
                        {
                            LoggingService.LogError("Token response error during interactive OAuth", trex);
                            var detail = trex.Error != null ? trex.Error.ErrorDescription ?? trex.Error.Error : trex.Message;
                            return new CalendarImportResult
                            {
                                Outcome = CalendarImportOutcome.InvalidCredentials,
                                ErrorMessage = string.IsNullOrWhiteSpace(detail) ? "OAuth token response error." : detail
                            };
                        }
                        catch (Exception ex)
                        {
                            LoggingService.LogError("Unexpected exception during interactive OAuth authorization", ex);
                            return new CalendarImportResult
                            {
                                Outcome = CalendarImportOutcome.Error,
                                ErrorMessage = "Unexpected OAuth error: " + ex.Message
                            };
                        }
                    }

                    if (credential == null)
                    {
                        LoggingService.Log("Authorization yielded null credential after silent + interactive attempts");
                        return new CalendarImportResult
                        {
                            Outcome = CalendarImportOutcome.Cancelled,
                            ErrorMessage = "Authorization did not complete successfully."
                        };
                    }

                    // Build service
                    LoggingService.Log("Building CalendarService initializer");
                    var service = new CalendarService(new BaseClientService.Initializer
                    {
                        HttpClientInitializer = credential,
                        ApplicationName = ApplicationName
                    });

                    var now = DateTime.Now;
                    var rangeStart = now;
                    var rangeEnd = now.AddDays(31);
                    LoggingService.Log($"Event query range: {rangeStart:u} to {rangeEnd:u}");

                    var request = service.Events.List("primary");
                    request.TimeMinDateTimeOffset = rangeStart;
                    request.TimeMaxDateTimeOffset = rangeEnd;
                    request.SingleEvents = true;
                    request.ShowDeleted = false;
                    request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;
                    request.MaxResults = 2500;
                    request.TimeZone = TimeZoneInfo.Local.Id;

                    Events response;
                    try
                    {
                        response = await request.ExecuteAsync(cancellationToken).ConfigureAwait(false);
                        LoggingService.Log("Events API call succeeded");
                    }
                    catch (Google.GoogleApiException apiEx)
                    {
                        LoggingService.LogError("Google API exception during event list", apiEx);
                        return MapGoogleApiException(apiEx);
                    }
                    catch (Exception ex)
                    {
                        LoggingService.LogError("Non-Google exception during event list", ex);
                        return new CalendarImportResult
                        {
                            Outcome = CalendarImportOutcome.Error,
                            ErrorMessage = "Failed to query events: " + ex.Message
                        };
                    }

                    var events = response?.Items ?? new List<Event>();
                    LoggingService.Log($"Events retrieved: {events.Count}");

                    if (events.Count == 0)
                    {
                        return new CalendarImportResult { Outcome = CalendarImportOutcome.NoEvents, ErrorMessage = "No events found in the next 31 days." };
                    }

                    var tasks = events.Select(ConvertToTaskItem).Where(t => t != null).ToList();
                    LoggingService.Log($"Converted tasks: {tasks.Count}");

                    return new CalendarImportResult
                    {
                        Outcome = CalendarImportOutcome.Success,
                        Tasks = tasks
                    };
                }
            }
            catch (TaskCanceledException)
            {
                LoggingService.Log("Import cancelled by user or system");
                return new CalendarImportResult
                {
                    Outcome = CalendarImportOutcome.Cancelled,
                    ErrorMessage = "Authorization was canceled."
                };
            }
            catch (TokenResponseException ex) when (string.Equals(ex.Error?.Error, "access_denied", StringComparison.OrdinalIgnoreCase))
            {
                LoggingService.Log("User denied access in OAuth dialog", "WARN");
                return new CalendarImportResult
                {
                    Outcome = CalendarImportOutcome.Cancelled,
                    ErrorMessage = "Access was denied during sign-in."
                };
            }
            catch (Exception ex)
            {
                string offlineHint = string.Empty;
                if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
                {
                    offlineHint = " (No network connection detected)";
                }
                LoggingService.LogError("Unexpected error importing Google Calendar events.", ex);
                var message = string.IsNullOrWhiteSpace(ex.Message) ? $"Unexpected error: {ex.GetType().Name}{offlineHint}" : ex.Message + offlineHint;
                return new CalendarImportResult
                {
                    Outcome = CalendarImportOutcome.Error,
                    ErrorMessage = message
                };
            }
        }

        private CalendarImportResult MapGoogleApiException(Google.GoogleApiException ex)
        {
            var code = (int)ex.HttpStatusCode;
            string msg = ex.Message;

            if (ex.HttpStatusCode == HttpStatusCode.Forbidden)
            {
                // Could be missing API enabled or not test user
                if (msg != null && msg.IndexOf("has not been used", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return new CalendarImportResult
                    {
                        Outcome = CalendarImportOutcome.AccessBlocked,
                        ErrorMessage = "Google Calendar API not enabled for this project. Enable it in Google Cloud Console and try again."
                    };
                }
                return new CalendarImportResult
                {
                    Outcome = CalendarImportOutcome.AccessBlocked,
                    ErrorMessage = "Access denied by Google Calendar API. Add the account as a test user or enable the API."
                };
            }

            if (ex.HttpStatusCode == HttpStatusCode.Unauthorized)
            {
                return new CalendarImportResult
                {
                    Outcome = CalendarImportOutcome.InvalidCredentials,
                    ErrorMessage = "Unauthorized. Credentials may be invalid or revoked. Re-download the credentials file and retry."
                };
            }

            if (ex.HttpStatusCode == HttpStatusCode.NotFound)
            {
                return new CalendarImportResult
                {
                    Outcome = CalendarImportOutcome.Error,
                    ErrorMessage = "Calendar not found or inaccessible." + (string.IsNullOrWhiteSpace(msg) ? string.Empty : " Details: " + msg)
                };
            }

            if (ex.HttpStatusCode == HttpStatusCode.BadRequest)
            {
                return new CalendarImportResult
                {
                    Outcome = CalendarImportOutcome.Error,
                    ErrorMessage = "Bad request sent to Google Calendar API." + (string.IsNullOrWhiteSpace(msg) ? string.Empty : " Details: " + msg)
                };
            }

            return new CalendarImportResult
            {
                Outcome = CalendarImportOutcome.Error,
                ErrorMessage = string.IsNullOrWhiteSpace(msg) ? $"Google API error (HTTP {(int)ex.HttpStatusCode})." : msg
            };
        }

        private static Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return File.ReadAllText(path);
            }, cancellationToken);
        }

        private static Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                File.WriteAllText(path, contents);
            }, cancellationToken);
        }

        private TaskItem ConvertToTaskItem(Event calendarEvent)
        {
            if (calendarEvent == null)
            {
                return null;
            }

            var startInfo = GetStartInfo(calendarEvent.Start);
            var task = new TaskItem
            {
                Name = string.IsNullOrWhiteSpace(calendarEvent.Summary) ? "Untitled event" : calendarEvent.Summary.Trim(),
                DueDate = startInfo.Date,
                DueTime = startInfo.Time,
                ReminderStatus = "none",
                ReminderLabel = "Disabled",
                ExternalId = calendarEvent.Id
            };

            if (calendarEvent.Reminders?.Overrides != null && calendarEvent.Reminders.Overrides.Count > 0)
            {
                var overrideReminder = calendarEvent.Reminders.Overrides
                    .Where(r => r.Minutes.HasValue)
                    .OrderBy(r => r.Minutes.Value)
                    .FirstOrDefault();

                if (overrideReminder?.Minutes != null)
                {
                    task.ReminderStatus = "active";
                    task.ReminderLabel = FormatReminderMinutes(overrideReminder.Minutes.Value);
                }
            }
            else if (calendarEvent.Reminders?.UseDefault == true)
            {
                task.ReminderStatus = "active";
                task.ReminderLabel = "Default reminder";
            }
            else if (startInfo.Date.HasValue)
            {
                task.ReminderStatus = "active";
                task.ReminderLabel = BuildDefaultReminderLabel(startInfo);
            }

            return task;
        }

        private EventStartInfo GetStartInfo(EventDateTime start)
        {
            if (start == null)
            {
                return new EventStartInfo(null, null, false);
            }

            if (!string.IsNullOrEmpty(start.DateTimeRaw))
            {
                if (DateTimeOffset.TryParse(start.DateTimeRaw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dto))
                {
                    var local = dto.ToLocalTime();
                    return new EventStartInfo(local.Date, local.TimeOfDay, false);
                }
            }

            if (start.DateTime.HasValue)
            {
                var dateTime = start.DateTime.Value;

                if (!string.IsNullOrEmpty(start.TimeZone))
                {
                    try
                    {
                        var sourceZone = TimeZoneInfo.FindSystemTimeZoneById(start.TimeZone);
                        var unspecified = DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified);
                        var converted = TimeZoneInfo.ConvertTime(unspecified, sourceZone, TimeZoneInfo.Local);
                        return new EventStartInfo(converted.Date, converted.TimeOfDay, false);
                    }
                    catch (TimeZoneNotFoundException)
                    {
                    }
                    catch (InvalidTimeZoneException)
                    {
                    }
                }

                if (dateTime.Kind == DateTimeKind.Utc)
                {
                    dateTime = dateTime.ToLocalTime();
                }
                else if (dateTime.Kind == DateTimeKind.Unspecified)
                {
                    dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Local);
                }

                return new EventStartInfo(dateTime.Date, dateTime.TimeOfDay, false);
            }

            if (!string.IsNullOrEmpty(start.Date))
            {
                if (DateTime.TryParseExact(start.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var date))
                {
                    return new EventStartInfo(date.Date, null, true);
                }
            }

            return new EventStartInfo(null, null, false);
        }

        private string BuildDefaultReminderLabel(EventStartInfo startInfo)
        {
            if (startInfo.Date.HasValue && startInfo.Time.HasValue)
            {
                var dayOfWeek = startInfo.Date.Value.ToString("dddd", CultureInfo.InvariantCulture);
                var monthDay = startInfo.Date.Value.ToString("MMM dd", CultureInfo.InvariantCulture);
                var time = DateTime.Today.Add(startInfo.Time.Value).ToString("h:mm tt", CultureInfo.InvariantCulture);
                return $"{dayOfWeek}, {monthDay} - {time}";
            }

            if (startInfo.Date.HasValue)
            {
                return startInfo.Date.Value.ToString("dddd, MMM dd", CultureInfo.InvariantCulture);
            }

            return "Active";
        }

        private string FormatReminderMinutes(int minutes)
        {
            if (minutes == 0)
            {
                return "At start time";
            }

            if (minutes == 1)
            {
                return "1 minute before";
            }

            if (minutes < 60)
            {
                return $"{minutes} minutes before";
            }

            if (minutes == 60)
            {
                return "1 hour before";
            }

            if (minutes < 1440 && minutes % 60 == 0)
            {
                var hours = minutes / 60;
                return $"{hours} hours before";
            }

            if (minutes == 1440)
            {
                return "1 day before";
            }

            if (minutes % 1440 == 0)
            {
                var days = minutes / 1440;
                return $"{days} days before";
            }

            var timeSpan = TimeSpan.FromMinutes(minutes);
            var parts = new List<string>();
            if (timeSpan.Days > 0)
            {
                parts.Add($"{timeSpan.Days} day{(timeSpan.Days == 1 ? string.Empty : "s")}");
            }

            if (timeSpan.Hours > 0)
            {
                parts.Add($"{timeSpan.Hours} hour{(timeSpan.Hours == 1 ? string.Empty : "s")}");
            }

            if (timeSpan.Minutes > 0)
            {
                parts.Add($"{timeSpan.Minutes} minute{(timeSpan.Minutes == 1 ? string.Empty : "s")}");
            }

            if (parts.Count == 0)
            {
                parts.Add($"{minutes} minutes");
            }

            return string.Join(" ", parts) + " before";
        }

        private bool HasStoredToken()
        {
            try
            {
                if (!Directory.Exists(_tokenDirectory)) return false;
                var files = Directory.GetFiles(_tokenDirectory, "*desktop-user*", SearchOption.TopDirectoryOnly);
                return files.Any(f => new FileInfo(f).Length > 0);
            }
            catch { return false; }
        }

        private CredentialState ValidateCredentialFile()
        {
            if (!File.Exists(_credentialsPath))
            {
                var autoImportState = TryAutoImportCredentials();
                if (autoImportState != null)
                {
                    return autoImportState;
                }

                return new CredentialState
                {
                    Status = CredentialStatus.Missing,
                    Message = "Add google-credentials.json next to the app to import upcoming events."
                };
            }

            try
            {
                var json = File.ReadAllText(_credentialsPath);
                if (ValidateCredentialJson(json, out var message))
                {
                    return new CredentialState
                    {
                        Status = CredentialStatus.Valid,
                        Message = HasStoredToken() ? "Credentials and token found. Click Import Next Month to sync." : "google-credentials.json looks good. Click Import Next Month to continue."
                    };
                }

                return new CredentialState
                {
                    Status = CredentialStatus.Invalid,
                    Message = message
                };
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Failed to validate google-credentials.json", ex);
                // Fallback: attempt to rewrite embedded credentials if available
                try
                {
                    if (!string.IsNullOrWhiteSpace(EmbeddedCredentialsJson))
                    {
                        File.WriteAllText(_credentialsPath, EmbeddedCredentialsJson);
                        var json2 = File.ReadAllText(_credentialsPath);
                        if (ValidateCredentialJson(json2, out var msg2))
                        {
                            return new CredentialState
                            {
                                Status = CredentialStatus.Valid,
                                Message = "Embedded credentials restored. Click Import Next Month." + (HasStoredToken() ? " Token cached." : string.Empty)
                            };
                        }
                    }
                }
                catch (Exception inner)
                {
                    LoggingService.LogError("Embedded credentials restoration failed", inner);
                }

                return new CredentialState
                {
                    Status = CredentialStatus.Invalid,
                    Message = "We couldn't read google-credentials.json. Choose the file again."
                };
            }
        }

        private CredentialState TryAutoImportCredentials()
        {
            var candidatePath = FindCredentialFileCandidate();
            if (string.IsNullOrEmpty(candidatePath))
            {
                return null;
            }

            try
            {
                var json = File.ReadAllText(candidatePath);
                if (!ValidateCredentialJson(json, out var validationMessage))
                {
                    return new CredentialState
                    {
                        Status = CredentialStatus.Invalid,
                        Message = validationMessage
                    };
                }

                // Always write to AppData location to avoid Program Files access issues
                EnsureWritablePaths(ref _credentialsPath, ref _tokenDirectory);

                // Only overwrite and clear tokens if content actually changed
                var existing = File.Exists(_credentialsPath) ? File.ReadAllText(_credentialsPath) : null;
                var contentChanged = existing == null || !string.Equals(existing, json, StringComparison.Ordinal);
                File.WriteAllText(_credentialsPath, json);
                if (contentChanged)
                {
                    LoggingService.Log("Credentials content changed; clearing token cache to avoid mismatch.");
                    ClearCachedTokens();
                }
                else
                {
                    LoggingService.Log("Credentials unchanged; preserving existing token cache.");
                }

                return new CredentialState
                {
                    Status = CredentialStatus.Valid,
                    Message = "google-credentials.json found. Click Import Next Month to continue."
                };
            }
            catch (UnauthorizedAccessException uae)
            {
                LoggingService.LogError("Auto-import write denied; relocating to AppData", uae);
                try
                {
                    var appData = _storageService.GetDataFolderPath();
                    _credentialsPath = Path.Combine(appData, CredentialFileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(_credentialsPath) ?? appData);

                    var json = File.ReadAllText(candidatePath);
                    var existing = File.Exists(_credentialsPath) ? File.ReadAllText(_credentialsPath) : null;
                    var contentChanged = existing == null || !string.Equals(existing, json, StringComparison.Ordinal);
                    File.WriteAllText(_credentialsPath, json);
                    if (contentChanged)
                    {
                        LoggingService.Log("Credentials content changed (AppData fallback); clearing token cache.");
                        ClearCachedTokens();
                    }
                    else
                    {
                        LoggingService.Log("Credentials unchanged (AppData fallback); preserving token cache.");
                    }

                    return new CredentialState
                    {
                        Status = CredentialStatus.Valid,
                        Message = "google-credentials.json imported to AppData. Click Import Next Month to continue."
                    };
                }
                catch (Exception ex)
                {
                    LoggingService.LogError("Auto-import fallback to AppData failed", ex);
                    return new CredentialState
                    {
                        Status = CredentialStatus.Invalid,
                        Message = "We couldn't read google-credentials.json. Choose the file again."
                    };
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Failed to automatically import google-credentials.json", ex);
                return new CredentialState
                {
                    Status = CredentialStatus.Invalid,
                    Message = "We couldn't read google-credentials.json. Choose the file again."
                };
            }
        }

        private string FindCredentialFileCandidate()
        {
            try
            {
                // Existing parent traversal
                var directory = new DirectoryInfo(_appDirectory);
                for (var depth = 0; depth < 5 && directory != null; depth++)
                {
                    var directHit = TryExactFiles(directory.FullName);
                    if (!string.IsNullOrEmpty(directHit))
                    {
                        return directHit;
                    }
                    directory = directory.Parent;
                }

                // Search common user folders (limited depth)
                foreach (var root in GetCommonSearchDirectories())
                {
                    var found = SearchDirectoryForCredentials(root, maxDepth: 3);
                    if (!string.IsNullOrEmpty(found))
                    {
                        return found;
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Failed to search for google-credentials.json automatically", ex);
            }
            return null;
        }

        private string TryExactFiles(string dir)
        {
            try
            {
                foreach (var pattern in CredentialPatterns)
                {
                    var files = Directory.GetFiles(dir, pattern, SearchOption.TopDirectoryOnly);
                    var match = files.OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault();
                    if (!string.IsNullOrEmpty(match))
                    {
                        return match;
                    }
                }
            }
            catch { }
            return null;
        }

        private string SearchDirectoryForCredentials(string root, int maxDepth)
        {
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root)) return null;
            try
            {
                var queue = new Queue<(string path,int depth)>();
                queue.Enqueue((root,0));
                string best = null;
                DateTime bestTime = DateTime.MinValue;
                while (queue.Count > 0)
                {
                    var (path, depth) = queue.Dequeue();
                    // Check files at this level
                    foreach (var pattern in CredentialPatterns)
                    {
                        string[] files;
                        try { files = Directory.GetFiles(path, pattern, SearchOption.TopDirectoryOnly); } catch { continue; }
                        foreach (var f in files)
                        {
                            try
                            {
                                var t = File.GetLastWriteTimeUtc(f);
                                if (t > bestTime)
                                {
                                    best = f; bestTime = t;
                                }
                            }
                            catch { }
                        }
                    }
                    // Queue children if depth allowed
                    if (depth < maxDepth)
                    {
                        string[] dirs;
                        try { dirs = Directory.GetDirectories(path); } catch { continue; }
                        foreach (var d in dirs)
                        {
                            // Skip node_modules / bin / obj to reduce noise
                            var name = Path.GetFileName(d);
                            if (name.Equals("node_modules", StringComparison.OrdinalIgnoreCase) ||
                                name.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
                                name.Equals("obj", StringComparison.OrdinalIgnoreCase))
                                continue;
                            queue.Enqueue((d, depth+1));
                        }
                    }
                }
                return best;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Recursive search failed in '{root}'", ex);
            }
            return null;
        }

        // NEW: Common directories where users typically place downloads
        private IEnumerable<string> GetCommonSearchDirectories()
        {
            var dirs = new List<string>();
            try { dirs.Add(_storageService.GetDataFolderPath()); } catch { }

            try { dirs.Add(Environment.GetFolderPath(Environment.SpecialFolder.Desktop)); } catch { }
            try { dirs.Add(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)); } catch { }

            // There is no SpecialFolder for Downloads on .NET Framework; fall back to %USERPROFILE%\Downloads
            try
            {
                var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                if (!string.IsNullOrEmpty(userProfile))
                {
                    var downloads = Path.Combine(userProfile, "Downloads");
                    dirs.Add(downloads);
                }
            }
            catch { }

            // Distinct and non-empty
            return dirs.Where(d => !string.IsNullOrWhiteSpace(d)).Distinct(StringComparer.OrdinalIgnoreCase);
        }

        private static string GetAlternateCredentialPath(string directory)
        {
            try
            {
                var files = Directory.GetFiles(directory, CredentialFileName + "*", SearchOption.TopDirectoryOnly);
                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file);
                    if (fileName != null && fileName.StartsWith(CredentialFileName, StringComparison.OrdinalIgnoreCase))
                    {
                        return file;
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Failed to enumerate potential credential files", ex);
            }

            return null;
        }

        private bool ValidateCredentialJson(string json, out string message)
        {
            try
            {
                var jObject = JObject.Parse(json);
                var root = (JObject)jObject["installed"] ?? (JObject)jObject["web"];
                if (root == null)
                {
                    message = "The credential file must include an \"installed\" client configuration.";
                    return false;
                }

                var clientId = root.Value<string>("client_id");
                var clientSecret = root.Value<string>("client_secret");
                var redirectUris = root["redirect_uris"] as JArray;

                if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
                {
                    message = "The credential file is missing the client ID or secret.";
                    return false;
                }

                if (redirectUris == null || redirectUris.Count == 0)
                {
                    message = "The credential file is missing redirect URIs.";
                    return false;
                }

                // localhost redirect URIs are valid for installed apps; do not block them
                message = "Credentials look good. Continue with Import Next Month.";
                return true;
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Invalid google-credentials.json", ex);
                message = "google-credentials.json isn't valid JSON. Download a fresh file and try again.";
                return false;
            }
        }

        private CredentialState UpdateCredentialState(CredentialState newState)
        {
            lock (_stateLock)
            {
                _credentialState = new CredentialState
                {
                    Status = newState.Status,
                    Message = newState.Message
                };
            }

            CredentialsChanged?.Invoke(this, EventArgs.Empty);
            return GetCredentialState();
        }

        private void InitializeWatcher()
        {
            try
            {
                // Watch the AppData folder for credentials changes instead of the exe folder
                var watchFolder = Path.GetDirectoryName(_credentialsPath) ?? _appDirectory;
                _watcher = new FileSystemWatcher(watchFolder, CredentialFileName)
                {
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size
                };

                _watcher.Changed += HandleCredentialsFileChanged;
                _watcher.Created += HandleCredentialsFileChanged;
                _watcher.Renamed += HandleCredentialsFileChanged;
                _watcher.Deleted += HandleCredentialsFileChanged;
                _watcher.EnableRaisingEvents = true;
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Failed to initialize credentials watcher", ex);
            }
        }

        private void HandleCredentialsFileChanged(object sender, FileSystemEventArgs e)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(200).ConfigureAwait(false);

                try
                {
                    if (e.ChangeType == WatcherChangeTypes.Deleted)
                    {
                        UpdateCredentialState(new CredentialState
                        {
                            Status = CredentialStatus.Missing,
                            Message = "Add google-credentials.json next to the app to import upcoming events."
                        });
                        return;
                    }

                    var state = ValidateCredentialFile();
                    // Do NOT clear tokens on generic change; only when content changed during import
                    UpdateCredentialState(state);
                }
                catch (Exception ex)
                {
                    LoggingService.LogError("Error reacting to google-credentials.json changes", ex);
                }
            });
        }

        private void ClearCachedTokens()
        {
            try
            {
                if (Directory.Exists(_tokenDirectory))
                {
                    Directory.Delete(_tokenDirectory, true);
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Failed to clear Google OAuth token cache", ex);
            }
            finally
            {
                try
                {
                    Directory.CreateDirectory(_tokenDirectory);
                }
                catch (Exception ex)
                {
                    LoggingService.LogError("Failed to recreate Google OAuth token directory", ex);
                }
            }
        }

        public void Dispose()
        {
            _watcher?.Dispose();
        }

        private readonly struct EventStartInfo
        {
            public EventStartInfo(DateTime? date, TimeSpan? time, bool isAllDay)
            {
                Date = date;
                Time = time;
                IsAllDay = isAllDay;
            }

            public DateTime? Date { get; }
            public TimeSpan? Time { get; }
            public bool IsAllDay { get; }
        }
    }
}
