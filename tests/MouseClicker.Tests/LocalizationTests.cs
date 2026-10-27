using System.Globalization;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using Xunit;

namespace MouseClicker.Tests
{
    public class LocalizationTests : IDisposable
    {
        private readonly string _originalLanguage = Localization.Language;

        public void Dispose() => Localization.Language = _originalLanguage;

        private static Dictionary<string, Dictionary<string, string>> GetTranslations()
        {
            var field = typeof(Localization).GetField("Translations", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(field);
            return Assert.IsType<Dictionary<string, Dictionary<string, string>>>(field.GetValue(null));
        }

        [Theory]
        [InlineData("zh-CN", "鼠标点击器", "文件", "开始点击")]
        [InlineData("en-US", "Mouse Clicker", "File", "Start Clicking")]
        [InlineData("ja-JP", "マウス自動クリッカー", "ファイル", "クリックを開始")]
        [InlineData("ko-KR", "마우스 자동 클릭기", "파일", "클릭 시작")]
        [InlineData("zh-TW", "滑鼠自動點擊器", "檔案", "開始點擊")]
        [InlineData("fr-FR", "Cliqueur automatique", "Fichier", "Démarrer les clics")]
        [InlineData("de-DE", "Automatischer Mausklicker", "Datei", "Klicken starten")]
        [InlineData("es-ES", "Clic automático", "Archivo", "Iniciar clics")]
        [InlineData("ru-RU", "Автокликер мыши", "Файл", "Начать нажатия")]
        [InlineData("pt-BR", "Clicador automático", "Arquivo", "Iniciar cliques")]
        public void Get_ReturnsNativeUiText(string language, string title, string file, string start)
        {
            Localization.Language = language;
            Assert.Equal(title, Localization.Get("WindowTitle"));
            Assert.Equal(file, Localization.Get("File"));
            Assert.Equal(start, Localization.Get("StartClicking"));
        }

        [Fact]
        public void Get_FallsBackToEnglish_ForUnknownLanguageCode()
        {
            Localization.Language = "xx-XX";
            Assert.Equal("Mouse Clicker", Localization.Get("WindowTitle"));
            Assert.Equal("Configuration saved successfully.", Localization.Get("ConfigSavedSuccess"));
        }

        [Fact]
        public void Get_ReturnsKeyItself_WhenNoTranslationExists()
        {
            const string unknownKey = "NonExistentKey_XYZ";
            foreach (var language in Localization.SupportedLanguages.Keys.Append("xx-XX"))
            {
                Localization.Language = language;
                Assert.Equal(unknownKey, Localization.Get(unknownKey));
            }
        }

        [Fact]
        public void SupportedLanguages_AndTranslationsMatchAllInstallerLanguages()
        {
            string[] expected = { "en-US", "zh-CN", "ja-JP", "ko-KR", "zh-TW", "fr-FR", "de-DE", "es-ES", "ru-RU", "pt-BR" };
            var translations = GetTranslations();
            Assert.Equal(expected.OrderBy(code => code), Localization.SupportedLanguages.Keys.OrderBy(code => code));
            Assert.Equal(expected.OrderBy(code => code), translations.Keys.OrderBy(code => code));

            var englishKeys = translations["en-US"].Keys.ToHashSet();
            foreach (var language in expected)
            {
                Assert.False(string.IsNullOrWhiteSpace(Localization.SupportedLanguages[language]));
                Assert.True(englishKeys.SetEquals(translations[language].Keys), $"Translation keys differ for {language}.");
                Assert.Equal(language, Localization.ResolveLanguage(CultureInfo.GetCultureInfo(language)));
                if (language != "en-US")
                {
                    Assert.NotEqual(translations["en-US"]["HelpTutorialContent"], translations[language]["HelpTutorialContent"]);
                }
            }
        }

        [Theory]
        [InlineData("File")]
        [InlineData("SaveConfig")]
        [InlineData("LoadConfig")]
        [InlineData("Language")]
        [InlineData("Help")]
        [InlineData("ViewHelp")]
        [InlineData("About")]
        [InlineData("Add")]
        [InlineData("Remove")]
        [InlineData("MoveUp")]
        [InlineData("MoveDown")]
        [InlineData("IntervalLabel")]
        [InlineData("JitterLabel")]
        [InlineData("JitterExceedsIntervalHint")]
        [InlineData("SequenceLabel")]
        [InlineData("SequenceLoop")]
        [InlineData("SequenceRandom")]
        [InlineData("SequenceOnce")]
        [InlineData("ShowOverlay")]
        [InlineData("HighlightCurrentArea")]
        [InlineData("HardwareMode")]
        [InlineData("StartClicking")]
        [InlineData("StopClicking")]
        [InlineData("WindowTitle")]
        [InlineData("SaveConfigTitle")]
        [InlineData("JsonFilter")]
        [InlineData("ConfigSavedSuccess")]
        [InlineData("Success")]
        [InlineData("SaveConfigError")]
        [InlineData("Error")]
        [InlineData("LoadConfigTitle")]
        [InlineData("ConfigLoadedSuccess")]
        [InlineData("LoadConfigError")]
        [InlineData("AboutMessage")]
        [InlineData("SetLabelPrompt")]
        [InlineData("SetLabelTitle")]
        [InlineData("AreaLabelDefault")]
        [InlineData("AddAreaFirst")]
        [InlineData("HelpTutorialTitle")]
        [InlineData("HelpTutorialContent")]
        [InlineData("CloseButton")]
        [InlineData("ProjectAddress")]
        [InlineData("CheckingForUpdates")]
        [InlineData("UpdateAvailable")]
        [InlineData("UpdateAvailableVersion")]
        [InlineData("NoUpdateAvailable")]
        [InlineData("UpdateCheckUnavailable")]
        [InlineData("ViewUpdate")]
        [InlineData("SkipThisVersion")]
        [InlineData("UpdateSkipped")]
        [InlineData("UpdatePreferencesError")]
        [InlineData("OpenLinkError")]
        [InlineData("HotkeySettings")]
        [InlineData("HotkeyLabel")]
        [InlineData("HotkeyHint")]
        [InlineData("HotkeyApply")]
        [InlineData("HotkeyInvalidKey")]
        [InlineData("HotkeyUnavailable")]
        [InlineData("LeftMouseButton")]
        [InlineData("RightMouseButton")]
        [InlineData("MouseInputError")]
        public void RequiredKeys_ExistInAllTenLanguages(string key)
        {
            var translations = GetTranslations();
            foreach (var language in Localization.SupportedLanguages.Keys)
            {
                Assert.True(translations[language].TryGetValue(key, out var value), $"{language} is missing {key}.");
                Assert.False(string.IsNullOrWhiteSpace(value), $"{language}/{key} is empty.");
                Localization.Language = language;
                Assert.Equal(value, Localization.Get(key));
            }
        }

        [Fact]
        public void FormattedMessages_HaveExactlyTheArgumentsRequiredByTheUi()
        {
            var formattedKeys = new HashSet<string>
            {
                "SaveConfigError", "LoadConfigError", "AboutMessage", "UpdateAvailableVersion",
                "UpdateSkipped", "UpdatePreferencesError", "OpenLinkError", "HotkeyUnavailable",
                "MouseInputError"
            };
            foreach (var language in GetTranslations())
            {
                foreach (var entry in language.Value)
                {
                    var format = CompositeFormat.Parse(entry.Value);
                    bool requiresArgument = formattedKeys.Contains(entry.Key);
                    Assert.Equal(requiresArgument ? 1 : 0, format.MinimumArgumentCount);
                    if (requiresArgument)
                    {
                        Assert.Contains("test-value", string.Format(CultureInfo.InvariantCulture, entry.Value, "test-value"));
                    }
                }
            }
        }

        [Theory]
        [InlineData("en", "en-US")]
        [InlineData("en-GB", "en-US")]
        [InlineData("en-AU", "en-US")]
        [InlineData("zh", "zh-CN")]
        [InlineData("zh-Hans", "zh-CN")]
        [InlineData("zh-Hans-SG", "zh-CN")]
        [InlineData("zh-SG", "zh-CN")]
        [InlineData("zh-Hant", "zh-TW")]
        [InlineData("zh-Hant-HK", "zh-TW")]
        [InlineData("zh-HK", "zh-TW")]
        [InlineData("zh-MO", "zh-TW")]
        [InlineData("zh-Hans-HK", "zh-CN")]
        [InlineData("zh-Hant-CN", "zh-TW")]
        [InlineData("zh-CHS", "zh-CN")]
        [InlineData("zh-CHT", "zh-TW")]
        [InlineData("ja", "ja-JP")]
        [InlineData("ko", "ko-KR")]
        [InlineData("fr", "fr-FR")]
        [InlineData("fr-CA", "fr-FR")]
        [InlineData("fr-CH", "fr-FR")]
        [InlineData("de", "de-DE")]
        [InlineData("de-AT", "de-DE")]
        [InlineData("es", "es-ES")]
        [InlineData("es-MX", "es-ES")]
        [InlineData("es-AR", "es-ES")]
        [InlineData("ru", "ru-RU")]
        [InlineData("ru-BY", "ru-RU")]
        [InlineData("pt", "pt-BR")]
        [InlineData("pt-PT", "pt-BR")]
        [InlineData("pt-AO", "pt-BR")]
        [InlineData("ar-EG", "en-US")]
        [InlineData("hi-IN", "en-US")]
        [InlineData("", "en-US")]
        public void ResolveLanguage_MatchesCultureFamilyAndChineseScript(string cultureName, string expected)
        {
            Localization.Language = "ja-JP";
            Assert.Equal(expected, Localization.ResolveLanguage(CultureInfo.GetCultureInfo(cultureName)));
            Assert.Equal("ja-JP", Localization.Language);
        }

        [Fact]
        public void ResolveLanguage_RejectsNullCulture()
        {
            Assert.Throws<ArgumentNullException>(() => Localization.ResolveLanguage(null!));
        }

        [Theory]
        [InlineData("fr-CA", "fr-FR")]
        [InlineData("zh-HK", "zh-TW")]
        [InlineData("zh-SG", "zh-CN")]
        [InlineData("pt-PT", "pt-BR")]
        [InlineData("ar-EG", "en-US")]
        [InlineData("", "en-US")]
        public void Language_DefaultsToDeviceUiCultureOnFirstInitialization(string cultureName, string expected)
        {
            var originalCulture = CultureInfo.CurrentUICulture;
            var context = new AssemblyLoadContext("localization-device-default", isCollectible: true);
            try
            {
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);
                // 全新的上下文会执行生产代码的静态初始化器，而测试本身不会给 Language 赋值。
                var assembly = context.LoadFromAssemblyPath(typeof(Localization).Assembly.Location);
                var type = assembly.GetType("MouseClicker.Localization", throwOnError: true)!;
                var field = type.GetField("Language", BindingFlags.Public | BindingFlags.Static)!;
                Assert.Equal(expected, Assert.IsType<string>(field.GetValue(null)));
            }
            finally
            {
                CultureInfo.CurrentUICulture = originalCulture;
                context.Unload();
            }
        }
    }
}
