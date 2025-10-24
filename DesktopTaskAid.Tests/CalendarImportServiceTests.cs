using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DesktopTaskAid.Models;
using DesktopTaskAid.Services;
using Google;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Calendar.v3.Data;
using Newtonsoft.Json;
using NUnit.Framework;

namespace DesktopTaskAid.Tests
{
    [TestFixture]
    public class CalendarImportServiceTests
    {
        private string _tempDirectory;
        private string _dataDirectory;
        private StorageService _storageService;
        private FakeCalendarClient _calendarClient;
        private CalendarImportService _service;

        [SetUp]
        public void SetUp()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDirectory);

            _dataDirectory = Path.Combine(_tempDirectory, "data");
            Directory.CreateDirectory(_dataDirectory);

            _storageService = new StorageService(_dataDirectory);
            _calendarClient = new FakeCalendarClient();
            _service = new CalendarImportService(_storageService, _calendarClient, _tempDirectory, enableWatcher: false);
        }

        [TearDown]
        public void TearDown()
        {
            _service?.Dispose();

            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }

        [Test]
        public async Task MissingCredentials_ReturnsMissingCredentialsOutcome()
        {
            var result = await _service.RunImportAsync();

            Assert.AreEqual(CalendarImportOutcome.MissingCredentials, result.Outcome);
            Assert.IsFalse(_calendarClient.WasInvoked);
        }

        [Test]
        public async Task InvalidCredentials_ReturnsInvalidCredentialsOutcome()
        {
            var credentialPath = Path.Combine(_tempDirectory, "google-credentials.json");
            File.WriteAllText(credentialPath, "not json");

            await _service.ImportCredentialsAsync(credentialPath);

            var result = await _service.RunImportAsync();

            Assert.AreEqual(CalendarImportOutcome.InvalidCredentials, result.Outcome);
            Assert.IsFalse(_calendarClient.WasInvoked);
        }

        [Test]
        public async Task UserCancellation_ReturnsCancelledOutcome()
        {
            await WriteValidCredentialsAsync();
            _calendarClient.EventsToReturn = null;

            var result = await _service.RunImportAsync();

            Assert.AreEqual(CalendarImportOutcome.Cancelled, result.Outcome);
        }

        [Test]
        public async Task NoEvents_ReturnsNoEventsOutcome()
        {
            await WriteValidCredentialsAsync();
            _calendarClient.EventsToReturn = new List<Event>();

            var result = await _service.RunImportAsync();

            Assert.AreEqual(CalendarImportOutcome.NoEvents, result.Outcome);
        }

        [Test]
        public async Task Success_ReturnsTasksForUpcomingEvents()
        {
            await WriteValidCredentialsAsync();

            var calendarEvent = new Event
            {
                Id = "abc123",
                Summary = "Team Sync",
                Start = new EventDateTime
                {
                    DateTime = DateTime.UtcNow.AddDays(35)
                }
            };

            _calendarClient.EventsToReturn = new List<Event> { calendarEvent };

            var result = await _service.RunImportAsync();

            Assert.AreEqual(CalendarImportOutcome.Success, result.Outcome);
            Assert.AreEqual(1, result.Tasks.Count);
            Assert.AreEqual("abc123", result.Tasks[0].ExternalId);
            Assert.AreEqual("Team Sync", result.Tasks[0].Name);
        }

        [Test]
        public async Task AccessBlocked_ReturnsAccessBlockedOutcome()
        {
            await WriteValidCredentialsAsync();

            var apiException = new GoogleApiException("Calendar", "Forbidden")
            {
                HttpStatusCode = HttpStatusCode.Forbidden
            };

            _calendarClient.ExceptionToThrow = apiException;

            var result = await _service.RunImportAsync();

            Assert.AreEqual(CalendarImportOutcome.AccessBlocked, result.Outcome);
        }

        [Test]
        public async Task AccessDeniedDuringTokenExchange_ReturnsCancelledOutcome()
        {
            await WriteValidCredentialsAsync();

            var tokenException = new TokenResponseException(new TokenErrorResponse
            {
                Error = "access_denied"
            });

            _calendarClient.ExceptionToThrow = tokenException;

            var result = await _service.RunImportAsync();

            Assert.AreEqual(CalendarImportOutcome.Cancelled, result.Outcome);
        }

        [Test]
        public async Task UnexpectedFailure_ReturnsErrorOutcome()
        {
            await WriteValidCredentialsAsync();

            _calendarClient.ExceptionToThrow = new InvalidOperationException("boom");

            var result = await _service.RunImportAsync();

            Assert.AreEqual(CalendarImportOutcome.Error, result.Outcome);
            Assert.AreEqual("boom", result.ErrorMessage);
        }

        [Test]
        public async Task ImportCredentials_FileNotFound_ReturnsMissing()
        {
            var bogusPath = Path.Combine(_tempDirectory, "nope.json");
            var state = await _service.ImportCredentialsAsync(bogusPath);
            Assert.AreEqual(CredentialStatus.Missing, state.Status);
            StringAssert.Contains("couldn't find", state.Message);
        }

        [Test]
        public async Task ImportCredentials_MissingClientFields_ReturnsInvalid()
        {
            var credentialPath = Path.Combine(_tempDirectory, "google-credentials.json");
            var json = "{\"installed\":{\"client_id\":\"id\",\"redirect_uris\":[\"urn:ietf:wg:oauth:2.0:oob\"]}}";
            File.WriteAllText(credentialPath, json);

            var state = await _service.ImportCredentialsAsync(credentialPath);

            Assert.AreEqual(CredentialStatus.Invalid, state.Status);
            StringAssert.Contains("client ID or secret", state.Message);
        }

        [Test]
        public async Task ImportCredentials_MissingRedirectUris_ReturnsInvalid()
        {
            var credentialPath = Path.Combine(_tempDirectory, "google-credentials.json");
            var json = "{\"installed\":{\"client_id\":\"id\",\"client_secret\":\"secret\"}}";
            File.WriteAllText(credentialPath, json);

            var state = await _service.ImportCredentialsAsync(credentialPath);

            Assert.AreEqual(CredentialStatus.Invalid, state.Status);
            StringAssert.Contains("missing redirect URIs", state.Message);
        }

        [Test]
        public async Task ImportCredentials_Valid_SavesAndClearsTokens()
        {
            var tokensDir = Path.Combine(_dataDirectory, "GoogleOAuth");
            Directory.CreateDirectory(tokensDir);
            File.WriteAllText(Path.Combine(tokensDir, "old.token"), "x");

            var credentialPath = Path.Combine(_tempDirectory, "google-credentials.json");
            var json = "{\"installed\":{\"client_id\":\"id\",\"client_secret\":\"secret\",\"redirect_uris\":[\"urn:ietf:wg:oauth:2.0:oob\"]}}";
            File.WriteAllText(credentialPath, json);

            var state = await _service.ImportCredentialsAsync(credentialPath);

            Assert.AreEqual(CredentialStatus.Valid, state.Status);
            Assert.IsTrue(File.Exists(Path.Combine(_tempDirectory, "google-credentials.json")));
            Assert.IsTrue(Directory.Exists(tokensDir));
            Assert.IsFalse(Directory.EnumerateFileSystemEntries(tokensDir).Any());
        }

        [Test]
        public void AutoImport_FindsAlternateCredentialFile()
        {
            var altPath = Path.Combine(_tempDirectory, "google-credentials.json.backup");
            var json = "{\"installed\":{\"client_id\":\"id\",\"client_secret\":\"secret\",\"redirect_uris\":[\"urn:ietf:wg:oauth:2.0:oob\"]}}";
            File.WriteAllText(altPath, json);

            var storage = new StorageService(_dataDirectory);
            var client = new FakeCalendarClient();

            using (var service = new CalendarImportService(storage, client, _tempDirectory, enableWatcher: false))
            {
                var state = service.GetCredentialState();
                Assert.AreEqual(CredentialStatus.Valid, state.Status);
                var savedPath = Path.Combine(_tempDirectory, "google-credentials.json");
                Assert.IsTrue(File.Exists(savedPath));
                Assert.AreEqual(json, File.ReadAllText(savedPath));
            }
        }

        [Test]
        public async Task Watcher_ReactsToDeleteAndChange()
        {
            _service.Dispose();
            _service = new CalendarImportService(_storageService, _calendarClient, _tempDirectory, enableWatcher: true);

            await WriteValidCredentialsAsync();

            var path = Path.Combine(_tempDirectory, "google-credentials.json");
            File.Delete(path);
            await Task.Delay(500);
            var afterDelete = _service.GetCredentialState();
            Assert.AreEqual(CredentialStatus.Missing, afterDelete.Status);

            File.WriteAllText(path, "not json");
            await Task.Delay(500);
            var afterInvalid = _service.GetCredentialState();
            Assert.AreEqual(CredentialStatus.Invalid, afterInvalid.Status);
        }

        [Test]
        public async Task RunImportAsync_TaskCanceled_ReturnsCancelled()
        {
            await WriteValidCredentialsAsync();
            _calendarClient.ExceptionToThrow = new TaskCanceledException();

            var result = await _service.RunImportAsync(new CancellationTokenSource().Token);

            Assert.AreEqual(CalendarImportOutcome.Cancelled, result.Outcome);
        }

        [Test]
        public async Task ConvertToTaskItem_RemindersAndLabels()
        {
            await WriteValidCredentialsAsync();

            var nextMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(1);
            var baseDate = nextMonthStart.AddDays(2).AddHours(9);

            var events = new List<Event>
            {
                new Event
                {
                    Id = "e0",
                    Summary = "StartNow",
                    Start = new EventDateTime { DateTime = baseDate },
                    Reminders = new Event.RemindersData
                    {
                        Overrides = new List<EventReminder>{ new EventReminder{ Method = "popup", Minutes = 0 } }
                    }
                },
                new Event
                {
                    Id = "e150",
                    Summary = "TwoAndHalfHours",
                    Start = new EventDateTime { DateTime = baseDate.AddDays(1) },
                    Reminders = new Event.RemindersData
                    {
                        Overrides = new List<EventReminder>{ new EventReminder{ Method = "email", Minutes = 150 } }
                    }
                },
                new Event
                {
                    Id = "edef",
                    Summary = "DefaultRem",
                    Start = new EventDateTime { DateTime = baseDate.AddDays(2) },
                    Reminders = new Event.RemindersData { UseDefault = true }
                },
                new Event
                {
                    Id = "enone",
                    Summary = "NoReminders",
                    Start = new EventDateTime { DateTime = baseDate.AddDays(3) }
                },
                new Event
                {
                    Id = "eday",
                    Summary = "AllDay",
                    Start = new EventDateTime { Date = baseDate.AddDays(4).ToString("yyyy-MM-dd") }
                },
                new Event
                {
                    Id = "enull",
                    Summary = null,
                    Start = null
                }
            };

            _calendarClient.EventsToReturn = events;

            var result = await _service.RunImportAsync();

            Assert.AreEqual(CalendarImportOutcome.Success, result.Outcome);
            Assert.AreEqual(events.Count, result.Tasks.Count);

            var t0 = result.Tasks.Single(t => t.ExternalId == "e0");
            Assert.AreEqual("active", t0.ReminderStatus);
            Assert.AreEqual("At start time", t0.ReminderLabel);

            var t150 = result.Tasks.Single(t => t.ExternalId == "e150");
            Assert.AreEqual("active", t150.ReminderStatus);
            Assert.AreEqual("2 hours 30 minutes before", t150.ReminderLabel);

            var tdef = result.Tasks.Single(t => t.ExternalId == "edef");
            Assert.AreEqual("active", tdef.ReminderStatus);
            Assert.AreEqual("Default reminder", tdef.ReminderLabel);

            var tnone = result.Tasks.Single(t => t.ExternalId == "enone");
            Assert.AreEqual("active", tnone.ReminderStatus);
            StringAssert.Contains(
                tnone.DueDate.Value.ToString("ddd", CultureInfo.InvariantCulture),
                tnone.ReminderLabel);

            var tday = result.Tasks.Single(t => t.ExternalId == "eday");
            Assert.IsTrue(tday.DueDate.HasValue);
            Assert.IsNull(tday.DueTime);
            StringAssert.Contains(tday.DueDate.Value.ToString("MMM dd", CultureInfo.InvariantCulture), tday.ReminderLabel);

            var tnull = result.Tasks.Single(t => t.ExternalId == "enull");
            Assert.AreEqual("Untitled event", tnull.Name);
            Assert.IsNull(tnull.DueDate);
            Assert.AreEqual("none", tnull.ReminderStatus);
            Assert.AreEqual("Not set", tnull.ReminderLabel);
        }

        [Test]
        public async Task ConvertToTaskItem_UsesDateTimeRaw_WhenPresent()
        {
            await WriteValidCredentialsAsync();

            var nextMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(1);
            var utcRaw = new DateTime(nextMonthStart.Year, nextMonthStart.Month, Math.Min(5, DateTime.DaysInMonth(nextMonthStart.Year, nextMonthStart.Month)), 14, 0, 0, DateTimeKind.Utc);
            var raw = utcRaw.ToString("o", CultureInfo.InvariantCulture);

            var ev = new Event
            {
                Id = "raw1",
                Summary = "RawTime",
                Start = new EventDateTime { DateTimeRaw = raw }
            };

            _calendarClient.EventsToReturn = new List<Event> { ev };

            var result = await _service.RunImportAsync();
            Assert.AreEqual(CalendarImportOutcome.Success, result.Outcome);
            var task = result.Tasks[0];
            Assert.IsTrue(task.DueDate.HasValue);
            Assert.IsTrue(task.DueTime.HasValue);
            var expectedLocal = DateTimeOffset.Parse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind).ToLocalTime();
            Assert.AreEqual(expectedLocal.Date, task.DueDate.Value.Date);
        }

        [Test]
        public async Task ImportCredentials_WebRoot_Succeeds()
        {
            var credentialPath = Path.Combine(_tempDirectory, "google-credentials.json");
            var json = "{\"web\":{\"client_id\":\"id\",\"client_secret\":\"secret\",\"redirect_uris\":[\"http://localhost\"]}}";
            File.WriteAllText(credentialPath, json);

            var state = await _service.ImportCredentialsAsync(credentialPath);

            Assert.AreEqual(CredentialStatus.Valid, state.Status);
        }

        [Test]
        public async Task StartInfo_TimeZone_Valid_LocalId()
        {
            await WriteValidCredentialsAsync();
            var nextMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(1);
            var dt = new DateTime(nextMonthStart.Year, nextMonthStart.Month, Math.Min(10, DateTime.DaysInMonth(nextMonthStart.Year, nextMonthStart.Month)), 9, 0, 0);
            var ev = new Event
            {
                Id = "tzLocal",
                Summary = "WithTZ",
                Start = new EventDateTime { DateTime = dt, TimeZone = TimeZoneInfo.Local.Id }
            };

            _calendarClient.EventsToReturn = new List<Event> { ev };
            var result = await _service.RunImportAsync();

            Assert.AreEqual(CalendarImportOutcome.Success, result.Outcome);
            Assert.IsTrue(result.Tasks[0].DueDate.HasValue);
            Assert.IsTrue(result.Tasks[0].DueTime.HasValue);
        }

        [Test]
        public async Task StartInfo_TimeZone_Invalid_Fallbacks()
        {
            await WriteValidCredentialsAsync();
            var nextMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(1);
            var dt = new DateTime(nextMonthStart.Year, nextMonthStart.Month, Math.Min(12, DateTime.DaysInMonth(nextMonthStart.Year, nextMonthStart.Month)), 11, 30, 0);
            var ev = new Event
            {
                Id = "tzBad",
                Summary = "BadTZ",
                Start = new EventDateTime { DateTime = dt, TimeZone = "Invalid/Zone" }
            };

            _calendarClient.EventsToReturn = new List<Event> { ev };
            var result = await _service.RunImportAsync();

            Assert.AreEqual(CalendarImportOutcome.Success, result.Outcome);
            Assert.IsTrue(result.Tasks[0].DueDate.HasValue);
            Assert.IsTrue(result.Tasks[0].DueTime.HasValue);
        }

        [Test]
        public async Task OverrideReminder_SelectsSmallestMinutes()
        {
            await WriteValidCredentialsAsync();
            var nextMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(1);
            var ev = new Event
            {
                Id = "minPick",
                Summary = "PickSmallest",
                Start = new EventDateTime { DateTime = nextMonthStart.AddDays(3).AddHours(8) },
                Reminders = new Event.RemindersData
                {
                    Overrides = new List<EventReminder>
                    {
                        new EventReminder { Minutes = 30 },
                        new EventReminder { Minutes = 10 }
                    }
                }
            };

            _calendarClient.EventsToReturn = new List<Event> { ev };
            var result = await _service.RunImportAsync();

            Assert.AreEqual(CalendarImportOutcome.Success, result.Outcome);
            Assert.AreEqual("10 minutes before", result.Tasks[0].ReminderLabel);
        }

        [Test]
        public async Task ReminderLabel_CommonCases()
        {
            await WriteValidCredentialsAsync();
            var baseDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(1).AddDays(1).AddHours(10);
            var events = new List<Event>
            {
                new Event
                {
                    Id = "m1",
                    Summary = "1m",
                    Start = new EventDateTime{ DateTime = baseDate },
                    Reminders = new Event.RemindersData{ Overrides = new List<EventReminder>{ new EventReminder{ Minutes = 1 } } }
                },
                new Event
                {
                    Id = "m30",
                    Summary = "30m",
                    Start = new EventDateTime{ DateTime = baseDate.AddDays(1) },
                    Reminders = new Event.RemindersData{ Overrides = new List<EventReminder>{ new EventReminder{ Minutes = 30 } } }
                },
                new Event
                {
                    Id = "h1",
                    Summary = "1h",
                    Start = new EventDateTime{ DateTime = baseDate.AddDays(2) },
                    Reminders = new Event.RemindersData{ Overrides = new List<EventReminder>{ new EventReminder{ Minutes = 60 } } }
                },
                new Event
                {
                    Id = "h2",
                    Summary = "2h",
                    Start = new EventDateTime{ DateTime = baseDate.AddDays(3) },
                    Reminders = new Event.RemindersData{ Overrides = new List<EventReminder>{ new EventReminder{ Minutes = 120 } } }
                },
                new Event
                {
                    Id = "d1",
                    Summary = "1d",
                    Start = new EventDateTime{ DateTime = baseDate.AddDays(4) },
                    Reminders = new Event.RemindersData{ Overrides = new List<EventReminder>{ new EventReminder{ Minutes = 1440 } } }
                },
                new Event
                {
                    Id = "d2",
                    Summary = "2d",
                    Start = new EventDateTime{ DateTime = baseDate.AddDays(5) },
                    Reminders = new Event.RemindersData{ Overrides = new List<EventReminder>{ new EventReminder{ Minutes = 2880 } } }
                }
            };

            _calendarClient.EventsToReturn = events;
            var result = await _service.RunImportAsync();
            var tasks = result.Tasks.ToDictionary(t => t.ExternalId);

            Assert.AreEqual("1 minute before", tasks["m1"].ReminderLabel);
            Assert.AreEqual("30 minutes before", tasks["m30"].ReminderLabel);
            Assert.AreEqual("1 hour before", tasks["h1"].ReminderLabel);
            Assert.AreEqual("2 hours before", tasks["h2"].ReminderLabel);
            Assert.AreEqual("1 day before", tasks["d1"].ReminderLabel);
            Assert.AreEqual("2 days before", tasks["d2"].ReminderLabel);
        }

        [Test]
        public async Task WhitespaceSummary_BecomesUntitled()
        {
            await WriteValidCredentialsAsync();
            var nextMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(1);
            var ev = new Event
            {
                Id = "blank",
                Summary = "   ",
                Start = new EventDateTime{ DateTime = nextMonthStart.AddDays(7).AddHours(12) }
            };

            _calendarClient.EventsToReturn = new List<Event>{ ev };
            var result = await _service.RunImportAsync();

            Assert.AreEqual("Untitled event", result.Tasks[0].Name);
        }

        [Test]
        public async Task CredentialsChanged_Event_Raised_OnImport()
        {
            int eventCount = 0;
            _service.CredentialsChanged += (s, e) => eventCount++;

            var credentialPath = Path.Combine(_tempDirectory, "google-credentials.json");
            var json = "{\"installed\":{\"client_id\":\"id\",\"client_secret\":\"secret\",\"redirect_uris\":[\"urn:ietf:wg:oauth:2.0:oob\"]}}";
            File.WriteAllText(credentialPath, json);

            await _service.ImportCredentialsAsync(credentialPath);

            Assert.GreaterOrEqual(eventCount, 1);
        }

        [Test]
        public async Task Watcher_ValidFile_ClearsTokens()
        {
            _service.Dispose();
            _service = new CalendarImportService(_storageService, _calendarClient, _tempDirectory, enableWatcher: true);

            var tokensDir = Path.Combine(_dataDirectory, "GoogleOAuth");
            Directory.CreateDirectory(tokensDir);
            File.WriteAllText(Path.Combine(tokensDir, "old.token"), "x");

            var credentialPath = Path.Combine(_tempDirectory, "google-credentials.json");
            var json = "{\"installed\":{\"client_id\":\"id\",\"client_secret\":\"secret\",\"redirect_uris\":[\"urn:ietf:wg:oauth:2.0:oob\"]}}";
            File.WriteAllText(credentialPath, json);

            await Task.Delay(600);

            Assert.IsTrue(Directory.Exists(tokensDir));
            Assert.IsFalse(Directory.EnumerateFileSystemEntries(tokensDir).Any());
            Assert.AreEqual(CredentialStatus.Valid, _service.GetCredentialState().Status);
        }

        [Test]
        public void AutoSearch_FindsInParentDirectory()
        {
            var nestedAppDir = Path.Combine(_tempDirectory, "bin");
            Directory.CreateDirectory(nestedAppDir);

            var parentCredPath = Path.Combine(_tempDirectory, "google-credentials.json");
            var json = "{\"installed\":{\"client_id\":\"id\",\"client_secret\":\"secret\",\"redirect_uris\":[\"urn:ietf:wg:oauth:2.0:oob\"]}}";
            File.WriteAllText(parentCredPath, json);

            using (var service = new CalendarImportService(_storageService, _calendarClient, nestedAppDir, enableWatcher: false))
            {
                var state = service.GetCredentialState();
                Assert.AreEqual(CredentialStatus.Valid, state.Status);
                Assert.IsTrue(File.Exists(Path.Combine(nestedAppDir, "google-credentials.json")));
            }
        }

        [Test]
        public void StartWithInvalidCredentials_FilePresent_StateInvalid()
        {
            var path = Path.Combine(_tempDirectory, "google-credentials.json");
            File.WriteAllText(path, "not json");

            using (var service = new CalendarImportService(_storageService, _calendarClient, _tempDirectory, enableWatcher: false))
            {
                var state = service.GetCredentialState();
                Assert.AreEqual(CredentialStatus.Invalid, state.Status);
                StringAssert.Contains("valid JSON", state.Message);
            }
        }

        [Test]
        public void AutoImport_InvalidAlternate_DoesNotSaveMain()
        {
            var altPath = Path.Combine(_tempDirectory, "google-credentials.json.bad");
            File.WriteAllText(altPath, "not json");

            using (var service = new CalendarImportService(_storageService, _calendarClient, _tempDirectory, enableWatcher: false))
            {
                var state = service.GetCredentialState();
                Assert.AreEqual(CredentialStatus.Invalid, state.Status);
                Assert.IsFalse(File.Exists(Path.Combine(_tempDirectory, "google-credentials.json")));
            }
        }

        [Test]
        public async Task ValidateCredentialJson_NoInstalledOrWeb_ReturnsInvalid()
        {
            var path = Path.Combine(_tempDirectory, "google-credentials.json");
            File.WriteAllText(path, "{}");

            var state = await _service.ImportCredentialsAsync(path);
            Assert.AreEqual(CredentialStatus.Invalid, state.Status);
            StringAssert.Contains("installed\" client configuration", state.Message);
        }

        [Test]
        public async Task OverridesWithoutMinutes_KeepNoneReminder()
        {
            await WriteValidCredentialsAsync();
            var nextMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(1);
            var ev = new Event
            {
                Id = "noMin",
                Summary = "NoMinutes",
                Start = new EventDateTime { DateTime = nextMonthStart.AddDays(2).AddHours(10) },
                Reminders = new Event.RemindersData
                {
                    Overrides = new List<EventReminder> { new EventReminder { Minutes = null } }
                }
            };

            _calendarClient.EventsToReturn = new List<Event> { ev };
            var result = await _service.RunImportAsync();

            Assert.AreEqual(CalendarImportOutcome.Success, result.Outcome);
            Assert.AreEqual("none", result.Tasks[0].ReminderStatus);
            Assert.AreEqual("Not set", result.Tasks[0].ReminderLabel);
        }

        [Test]
        public async Task NullEventInList_IsFilteredOut()
        {
            await WriteValidCredentialsAsync();
            var nextMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(1);
            var ev = new Event
            {
                Id = "real",
                Summary = "RealEvent",
                Start = new EventDateTime { DateTime = nextMonthStart.AddDays(3).AddHours(9) }
            };

            _calendarClient.EventsToReturn = new List<Event> { null, ev };
            var result = await _service.RunImportAsync();

            Assert.AreEqual(CalendarImportOutcome.Success, result.Outcome);
            Assert.AreEqual(1, result.Tasks.Count);
            Assert.AreEqual("real", result.Tasks[0].ExternalId);
        }

        [Test]
        public async Task TokenResponseException_OtherError_ReturnsError()
        {
            await WriteValidCredentialsAsync();

            var tokenException = new TokenResponseException(new TokenErrorResponse
            {
                Error = "invalid_client"
            });
            _calendarClient.ExceptionToThrow = tokenException;

            var result = await _service.RunImportAsync();

            Assert.AreEqual(CalendarImportOutcome.Error, result.Outcome);
            StringAssert.Contains("invalid_client", result.ErrorMessage);
        }

        [Test]
        public async Task DefaultReminderLabel_ExactFormat_ForTimedEvent()
        {
            await WriteValidCredentialsAsync();
            var nextMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(1);
            var date = nextMonthStart.AddDays(6).AddHours(13).AddMinutes(15);
            var ev = new Event
            {
                Id = "fmt",
                Summary = "FormatLabel",
                Start = new EventDateTime { DateTime = date }
            };

            _calendarClient.EventsToReturn = new List<Event> { ev };
            var result = await _service.RunImportAsync();
            Assert.AreEqual(CalendarImportOutcome.Success, result.Outcome);

            var t = result.Tasks[0];
            var expected = string.Format(
                CultureInfo.InvariantCulture,
                "{0}, {1} - {2}",
                t.DueDate.Value.ToString("dddd", CultureInfo.InvariantCulture),
                t.DueDate.Value.ToString("MMM dd", CultureInfo.InvariantCulture),
                DateTime.Today.Add(t.DueTime.Value).ToString("h:mm tt", CultureInfo.InvariantCulture));

            Assert.AreEqual(expected, t.ReminderLabel);
            Assert.AreEqual("active", t.ReminderStatus);
        }

        [Test]
        public async Task FormatReminderMinutes_CompositeDaysAndMinutes()
        {
            await WriteValidCredentialsAsync();
            var baseDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(1).AddDays(2).AddHours(9);
            var ev = new Event
            {
                Id = "mixd",
                Summary = "Composite",
                Start = new EventDateTime{ DateTime = baseDate },
                Reminders = new Event.RemindersData
                {
                    Overrides = new List<EventReminder>{ new EventReminder{ Minutes = 2885 } }
                }
            };

            _calendarClient.EventsToReturn = new List<Event>{ ev };
            var result = await _service.RunImportAsync();

            Assert.AreEqual(CalendarImportOutcome.Success, result.Outcome);
            Assert.AreEqual("2 days 5 minutes before", result.Tasks[0].ReminderLabel);
        }

        [Test]
        public async Task GetStartInfo_UtcDateTime_NoTimeZone_ConvertsToLocal()
        {
            await WriteValidCredentialsAsync();

            var nextMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(1);
            var utc = new DateTime(nextMonthStart.Year, nextMonthStart.Month, Math.Min(8, DateTime.DaysInMonth(nextMonthStart.Year, nextMonthStart.Month)), 15, 0, 0, DateTimeKind.Utc);

            var ev = new Event
            {
                Id = "utc",
                Summary = "Utc",
                Start = new EventDateTime{ DateTime = utc }
            };

            _calendarClient.EventsToReturn = new List<Event>{ ev };
            var result = await _service.RunImportAsync();

            var local = utc.ToLocalTime();
            Assert.AreEqual(local.Date, result.Tasks[0].DueDate.Value.Date);
            Assert.AreEqual(local.TimeOfDay, result.Tasks[0].DueTime.Value);
        }

        [Test]
        public async Task GetStartInfo_Unspecified_NoTimeZone_TreatedAsLocal()
        {
            await WriteValidCredentialsAsync();

            var nextMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(1);
            var unspecified = new DateTime(nextMonthStart.Year, nextMonthStart.Month, Math.Min(9, DateTime.DaysInMonth(nextMonthStart.Year, nextMonthStart.Month)), 8, 20, 0, DateTimeKind.Unspecified);

            var ev = new Event
            {
                Id = "unspec",
                Summary = "Unspec",
                Start = new EventDateTime{ DateTime = unspecified }
            };

            _calendarClient.EventsToReturn = new List<Event>{ ev };
            var result = await _service.RunImportAsync();

            Assert.AreEqual(unspecified.Date, result.Tasks[0].DueDate.Value.Date);
            Assert.AreEqual(new TimeSpan(8, 20, 0), result.Tasks[0].DueTime.Value);
        }

        [Test]
        public void ValidateCredentialFile_ExistingValidFile_YieldsValidState()
        {
            var credPath = Path.Combine(_tempDirectory, "google-credentials.json");
            var json = "{\"installed\":{\"client_id\":\"id\",\"client_secret\":\"secret\",\"redirect_uris\":[\"urn:ietf:wg:oauth:2.0:oob\"]}}";
            File.WriteAllText(credPath, json);

            using (var service = new CalendarImportService(_storageService, _calendarClient, _tempDirectory, enableWatcher: false))
            {
                var state = service.GetCredentialState();
                Assert.AreEqual(CredentialStatus.Valid, state.Status);
            }
        }

        [Test]
        public async Task CredentialsChanged_Event_Raised_OnWatcherChanges()
        {
            int fired = 0;
            _service.Dispose();
            _service = new CalendarImportService(_storageService, _calendarClient, _tempDirectory, enableWatcher: true);
            _service.CredentialsChanged += (s, e) => Interlocked.Increment(ref fired);

            var path = Path.Combine(_tempDirectory, "google-credentials.json");
            var valid = "{\"installed\":{\"client_id\":\"id\",\"client_secret\":\"secret\",\"redirect_uris\":[\"urn:ietf:wg:oauth:2.0:oob\"]}}";

            File.WriteAllText(path, valid);
            await Task.Delay(400);

            File.WriteAllText(path, "not json");
            await Task.Delay(400);

            if (File.Exists(path)) File.Delete(path);
            await Task.Delay(400);

            Assert.GreaterOrEqual(fired, 3);
        }

        [Test]
        public async Task ImportCredentials_Cancellation_ReturnsInvalid()
        {
            var credentialPath = Path.Combine(_tempDirectory, "google-credentials.json");
            var json = "{\"installed\":{\"client_id\":\"id\",\"client_secret\":\"secret\",\"redirect_uris\":[\"urn:ietf:wg:oauth:2.0:oob\"]}}";
            File.WriteAllText(credentialPath, json);
            var cts = new CancellationTokenSource();
            cts.Cancel();

            var state = await _service.ImportCredentialsAsync(credentialPath, cts.Token);

            Assert.AreEqual(CredentialStatus.Invalid, state.Status);
            StringAssert.Contains("save google-credentials.json", state.Message);
        }

        [Test]
        public async Task InvalidAllDayDate_String_ResultsInNoDueAndNoReminder()
        {
            await WriteValidCredentialsAsync();

            var ev = new Event
            {
                Id = "baddate",
                Summary = "InvalidDate",
                Start = new EventDateTime { Date = "2025-14-99" }
            };

            _calendarClient.EventsToReturn = new List<Event> { ev };
            var result = await _service.RunImportAsync();

            Assert.AreEqual(CalendarImportOutcome.Success, result.Outcome);
            var task = result.Tasks[0];
            Assert.IsNull(task.DueDate);
            Assert.IsNull(task.DueTime);
            Assert.AreEqual("none", task.ReminderStatus);
            Assert.AreEqual("Not set", task.ReminderLabel);
        }

        [Test]
        public async Task AllDay_DefaultReminderLabel_ExactFormat()
        {
            await WriteValidCredentialsAsync();
            var nextMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(1);
            var date = nextMonthStart.AddDays(10).Date;

            var ev = new Event
            {
                Id = "alldayfmt",
                Summary = "AllDayExact",
                Start = new EventDateTime { Date = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) }
            };

            _calendarClient.EventsToReturn = new List<Event> { ev };
            var result = await _service.RunImportAsync();

            var task = result.Tasks[0];
            var expected = date.ToString("dddd, MMM dd", CultureInfo.InvariantCulture);
            Assert.AreEqual(expected, task.ReminderLabel);
        }

        [Test]
        public async Task FormatReminderMinutes_OneHourOneMinute()
        {
            await WriteValidCredentialsAsync();
            var baseDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(1).AddDays(3).AddHours(10);
            var ev = new Event
            {
                Id = "h1m1",
                Summary = "1h1m",
                Start = new EventDateTime { DateTime = baseDate },
                Reminders = new Event.RemindersData
                {
                    Overrides = new List<EventReminder> { new EventReminder { Minutes = 61 } }
                }
            };

            _calendarClient.EventsToReturn = new List<Event> { ev };
            var result = await _service.RunImportAsync();

            Assert.AreEqual(CalendarImportOutcome.Success, result.Outcome);
            Assert.AreEqual("1 hour 1 minute before", result.Tasks[0].ReminderLabel);
        }

        [Test]
        public void ValidateCredentialFile_ReadError_ResultsInvalid()
        {
            var path = Path.Combine(_tempDirectory, "google-credentials.json");
            File.WriteAllText(path, "{not-json}");

            using (var locked = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                using (var service = new CalendarImportService(_storageService, _calendarClient, _tempDirectory, enableWatcher: false))
                {
                    var state = service.GetCredentialState();
                    Assert.AreEqual(CredentialStatus.Invalid, state.Status);
                    StringAssert.Contains("couldn't read google-credentials.json", state.Message.ToLowerInvariant());
                }
            }
        }

        [Test]
        public async Task VerifyFetchWindow_NextMonthBounds()
        {
            await WriteValidCredentialsAsync();
            _calendarClient.EventsToReturn = new List<Event>();

            await _service.RunImportAsync();

            var now = DateTime.Now;
            var expectedStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Local).AddMonths(1);
            var expectedEnd = expectedStart.AddMonths(1);

            Assert.AreEqual(expectedStart, _calendarClient.CapturedTimeMin);
            Assert.AreEqual(expectedEnd, _calendarClient.CapturedTimeMax);
        }

        [Test]
        public async Task ImportCredentials_WhitespacePath_ReturnsMissing()
        {
            var state = await _service.ImportCredentialsAsync("   ");
            Assert.AreEqual(CredentialStatus.Missing, state.Status);
        }

        [Test]
        public async Task ImportCredentials_EmptyRedirectUris_ReturnsInvalid()
        {
            var credentialPath = Path.Combine(_tempDirectory, "google-credentials.json");
            var json = "{\"installed\":{\"client_id\":\"id\",\"client_secret\":\"secret\",\"redirect_uris\":[]}}";
            File.WriteAllText(credentialPath, json);

            var state = await _service.ImportCredentialsAsync(credentialPath);
            Assert.AreEqual(CredentialStatus.Invalid, state.Status);
            StringAssert.Contains("missing redirect URIs", state.Message);
        }

        [Test]
        public async Task NameIsTrimmed()
        {
            await WriteValidCredentialsAsync();
            var nextMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(1);
            var ev = new Event
            {
                Id = "trim",
                Summary = "  Hello World  ",
                Start = new EventDateTime { DateTime = nextMonthStart.AddDays(2).AddHours(10) }
            };

            _calendarClient.EventsToReturn = new List<Event> { ev };
            var result = await _service.RunImportAsync();

            Assert.AreEqual("Hello World", result.Tasks[0].Name);
        }

        [Test]
        public void AutoImport_ReadError_FromAlternateFile_SetsInvalid()
        {
            var altPath = Path.Combine(_tempDirectory, "google-credentials.json.backup");
            var json = "{\"installed\":{\"client_id\":\"id\",\"client_secret\":\"secret\",\"redirect_uris\":[\"urn:ietf:wg:oauth:2.0:oob\"]}}";
            File.WriteAllText(altPath, json);

            using (var locked = new FileStream(altPath, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                using (var service = new CalendarImportService(_storageService, _calendarClient, _tempDirectory, enableWatcher: false))
                {
                    var state = service.GetCredentialState();
                    Assert.AreEqual(CredentialStatus.Invalid, state.Status);
                    StringAssert.Contains("couldn't read google-credentials.json", state.Message.ToLowerInvariant());
                }
            }
        }

        private async Task WriteValidCredentialsAsync()
        {
            var credentialPath = Path.Combine(_tempDirectory, "google-credentials.json");
            var json = "{\"installed\":{\"client_id\":\"id\",\"client_secret\":\"secret\",\"redirect_uris\":[\"urn:ietf:wg:oauth:2.0:oob\"]}}";
            File.WriteAllText(credentialPath, json);

            await _service.ImportCredentialsAsync(credentialPath);
        }

        private sealed class FakeCalendarClient : IGoogleCalendarClient
        {
            public IList<Event> EventsToReturn { get; set; } = new List<Event>();
            public Exception ExceptionToThrow { get; set; }
            public bool WasInvoked { get; private set; }
            public DateTime CapturedTimeMin { get; private set; }
            public DateTime CapturedTimeMax { get; private set; }

            public Task<IList<Event>> FetchEventsAsync(Stream credentialStream, string tokenDirectory, DateTime timeMin, DateTime timeMax, CancellationToken cancellationToken)
            {
                WasInvoked = true;
                CapturedTimeMin = timeMin;
                CapturedTimeMax = timeMax;

                if (ExceptionToThrow != null)
                {
                    throw ExceptionToThrow;
                }

                return Task.FromResult(EventsToReturn);
            }
        }
    }

    [TestFixture]
    public class GoogleCalendarClientTests
    {
        private Type _clientType;

        [SetUp]
        public void Setup()
        {
            _clientType = typeof(CalendarImportService).GetNestedType("GoogleCalendarClient", BindingFlags.NonPublic);
            Assert.IsNotNull(_clientType, "Internal GoogleCalendarClient type not found");
        }

        private IGoogleCalendarClient CreateClient()
        {
            var instance = Activator.CreateInstance(_clientType, nonPublic: true);
            return (IGoogleCalendarClient)instance;
        }

        [Test]
        public void FetchEventsAsync_NullStream_Throws()
        {
            var client = CreateClient();
            var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

            try
            {
                Assert.ThrowsAsync<ArgumentNullException>(async () =>
                    await client.FetchEventsAsync(null, tmp, DateTime.Now, DateTime.Now.AddMonths(1), CancellationToken.None));
            }
            finally
            {
                if (Directory.Exists(tmp)) Directory.Delete(tmp, true);
            }
        }

        [Test]
        public void FetchEventsAsync_InvalidJson_Throws()
        {
            var client = CreateClient();
            var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tmp);

            using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("not json")))
            {
                Assert.ThrowsAsync<JsonReaderException>(async () =>
                    await client.FetchEventsAsync(stream, tmp, DateTime.Now, DateTime.Now.AddMonths(1), CancellationToken.None));
            }

            Directory.Delete(tmp, true);
        }

        [Test]
        public void FetchEventsAsync_DisposedStream_Throws()
        {
            var client = CreateClient();
            var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tmp);

            var stream = new MemoryStream();
            stream.Dispose();

            Assert.ThrowsAsync<ArgumentException>(async () =>
                await client.FetchEventsAsync(stream, tmp, DateTime.Now, DateTime.Now.AddMonths(1), CancellationToken.None));

            Directory.Delete(tmp, true);
        }

        [Test]
        public void FetchEventsAsync_ValidJson_CanceledToken_ThrowsOperationCanceled()
        {
            var client = CreateClient();
            var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tmp);

            var json = "{\"installed\":{\"client_id\":\"id\",\"client_secret\":\"secret\",\"redirect_uris\":[\"urn:ietf:wg:oauth:2.0:oob\"]}}";
            using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)))
            {
                var cts = new CancellationTokenSource();
                cts.Cancel();

                Assert.ThrowsAsync<OperationCanceledException>(async () =>
                    await client.FetchEventsAsync(stream, tmp, DateTime.Now, DateTime.Now.AddMonths(1), cts.Token));
            }

            Directory.Delete(tmp, true);
        }
    }
}
