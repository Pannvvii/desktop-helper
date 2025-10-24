using System;
using System.Threading;
using System.Windows;
using DesktopTaskAid.Converters;
using NUnit.Framework;

namespace DesktopTaskAid.Tests
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class IconStrokeConverterCoverageTests
    {
        [SetUp]
        public void Setup()
        {
            if (Application.Current == null)
            {
                new Application();
            }
            Application.Current.Resources.MergedDictionaries.Clear();
        }

        [Test]
        public void Convert_IsDarkTheme_Bool_True_ReturnsWhite()
        {
            var conv = new IconStrokeConverter();
            Application.Current.Resources["IsDarkTheme"] = true;

            var result = conv.Convert(null, typeof(System.Windows.Media.Brush), null, null);

            Assert.IsNotNull(result);
            Assert.AreEqual(System.Windows.Media.Brushes.White, result);
        }

        [Test]
        public void Convert_IsDarkTheme_NullableBool_True_ReturnsWhite()
        {
            var conv = new IconStrokeConverter();
            Application.Current.Resources["IsDarkTheme"] = (bool?)true;

            var result = conv.Convert(null, typeof(System.Windows.Media.Brush), null, null);

            Assert.IsNotNull(result);
            Assert.AreEqual(System.Windows.Media.Brushes.White, result);
        }

        [Test]
        public void Convert_ThemeName_Dark_ReturnsWhite()
        {
            var conv = new IconStrokeConverter();
            // Remove IsDarkTheme to force ThemeName fallback
            Application.Current.Resources.Remove("IsDarkTheme");
            Application.Current.Resources["ThemeName"] = "dark";

            var result = conv.Convert(null, typeof(System.Windows.Media.Brush), null, null);

            Assert.IsNotNull(result);
            Assert.AreEqual(System.Windows.Media.Brushes.White, result);
        }

        [Test]
        public void Convert_NoFlags_NoMergedDictionaries_ReturnsLight()
        {
            var conv = new IconStrokeConverter();
            Application.Current.Resources.Remove("IsDarkTheme");
            Application.Current.Resources.Remove("ThemeName");
            Application.Current.Resources.MergedDictionaries.Clear();

            var result = conv.Convert(null, typeof(System.Windows.Media.Brush), null, null);

            Assert.IsNotNull(result);
            var brush = result as System.Windows.Media.SolidColorBrush;
            Assert.IsNotNull(brush);
            Assert.AreEqual(System.Windows.Media.Color.FromRgb(0x1A, 0x1A, 0x1A), brush.Color);
        }

        [Test]
        public void Convert_MergedDictionary_WithNullSource_DefaultsToLight()
        {
            var conv = new IconStrokeConverter();
            Application.Current.Resources.Remove("IsDarkTheme");
            Application.Current.Resources.Remove("ThemeName");

            // Add a merged dictionary with null source (won't match "dark")
            Application.Current.Resources.MergedDictionaries.Add(new System.Windows.ResourceDictionary());

            var result = conv.Convert(null, typeof(System.Windows.Media.Brush), null, null);

            Assert.IsNotNull(result);
            // Should default to light since no dark theme detected
            var brush = result as System.Windows.Media.SolidColorBrush;
            Assert.IsNotNull(brush);
            Assert.AreEqual(System.Windows.Media.Color.FromRgb(0x1A, 0x1A, 0x1A), brush.Color);
        }

        [Test]
        public void Convert_IsDarkTheme_False_ReturnsLight()
        {
            var conv = new IconStrokeConverter();
            Application.Current.Resources["IsDarkTheme"] = false;

            var result = conv.Convert(null, typeof(System.Windows.Media.Brush), null, null);

            Assert.IsNotNull(result);
            var brush = result as System.Windows.Media.SolidColorBrush;
            Assert.IsNotNull(brush);
            Assert.AreEqual(System.Windows.Media.Color.FromRgb(0x1A, 0x1A, 0x1A), brush.Color);
        }
    }
}
