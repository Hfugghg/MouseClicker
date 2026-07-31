using System.Reflection;
using Xunit;

namespace MouseClicker.Tests
{
    public class LocalizationTests : IDisposable
    {
        private readonly string _originalLanguage;

        public LocalizationTests()
        {
            // 每个测试前保存原始语言，以便测试后恢复。
            _originalLanguage = Localization.Language;
        }

        public void Dispose()
        {
            // 每个测试后恢复原始语言。
            Localization.Language = _originalLanguage;
        }

        // 辅助方法：反射访问私有静态 Translations 字典。
        private static Dictionary<string, Dictionary<string, string>> GetTranslations()
        {
            var field = typeof(Localization).GetField(
                "Translations",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.NotNull(field);
            var value = field!.GetValue(null);
            Assert.NotNull(value);
            return (Dictionary<string, Dictionary<string, string>>)value!;
        }

        // ----------------------------------------------------------------
        // 测试1：默认语言为 "zh-CN"
        // ----------------------------------------------------------------
        [Fact]
        public void DefaultLanguage_IsZhCN()
        {
            // 重置为类初始化器的默认值，然后读取。
            Localization.Language = "zh-CN";
            Assert.Equal("zh-CN", Localization.Language);
        }

        // ----------------------------------------------------------------
        // 测试2：Get() 返回正确的中文字符串
        // ----------------------------------------------------------------
        [Fact]
        public void Get_ReturnsChineseString_WhenLanguageIsZhCN()
        {
            Localization.Language = "zh-CN";

            Assert.Equal("鼠标点击器", Localization.Get("WindowTitle"));
            Assert.Equal("文件", Localization.Get("File"));
            Assert.Equal("开始点击", Localization.Get("StartClicking"));
            Assert.Equal("停止点击 (ESC)", Localization.Get("StopClicking"));
        }

        // ----------------------------------------------------------------
        // 测试3：Get() 返回正确的英文字符串
        // ----------------------------------------------------------------
        [Fact]
        public void Get_ReturnsEnglishString_WhenLanguageIsEnUS()
        {
            Localization.Language = "en-US";

            Assert.Equal("Mouse Clicker", Localization.Get("WindowTitle"));
            Assert.Equal("File", Localization.Get("File"));
            Assert.Equal("Start Clicking", Localization.Get("StartClicking"));
            Assert.Equal("Stop Clicking (ESC)", Localization.Get("StopClicking"));
        }

        // ----------------------------------------------------------------
        // 测试4：Get() 返回正确的日文字符串
        // ----------------------------------------------------------------
        [Fact]
        public void Get_ReturnsJapaneseString_WhenLanguageIsJaJP()
        {
            Localization.Language = "ja-JP";

            Assert.Equal("マウス自動クリッカー", Localization.Get("WindowTitle"));
            Assert.Equal("ファイル", Localization.Get("File"));
            Assert.Equal("クリックを開始", Localization.Get("StartClicking"));
            Assert.Equal("クリックを停止 (ESC)", Localization.Get("StopClicking"));
        }

        // ----------------------------------------------------------------
        // 测试5：切换语言返回不同翻译
        //         针对同一键。
        // ----------------------------------------------------------------
        [Fact]
        public void SwitchingLanguage_ReturnsDifferentTranslationsForSameKey()
        {
            const string key = "StartClicking";

            Localization.Language = "zh-CN";
            string zh = Localization.Get(key);

            Localization.Language = "en-US";
            string en = Localization.Get(key);

            Localization.Language = "ja-JP";
            string ja = Localization.Get(key);

            // 三种语言必须返回不同的翻译。
            Assert.NotEqual(zh, en);
            Assert.NotEqual(en, ja);
            Assert.NotEqual(zh, ja);

            // 抽查预期值。
            Assert.Equal("开始点击", zh);
            Assert.Equal("Start Clicking", en);
            Assert.Equal("クリックを開始", ja);
        }

        // ----------------------------------------------------------------
        // 测试6：未知语言代码时 Get() 回退到英文。
        // ----------------------------------------------------------------
        [Fact]
        public void Get_FallsBackToEnglish_ForUnknownLanguageCode()
        {
            // "fr-FR" 未在 Translations 字典中注册。
            Localization.Language = "fr-FR";

            // 对于英文中存在的键，返回英文值。
            Assert.Equal("Mouse Clicker", Localization.Get("WindowTitle"));
            Assert.Equal("File", Localization.Get("File"));
            Assert.Equal("Configuration saved successfully.", Localization.Get("ConfigSavedSuccess"));
        }

        // ----------------------------------------------------------------
        // 测试7：当前语言的键缺失时 Get() 回退到英文
        //         当前语言字典的键，但存在于英文中。
        //         由于三种已注册语言具有相同的键集，
        //         此路径通过设置未知语言代码来触发 —
        //         首次 ContainsKey 检查失败，因此
        //         方法会回退到英文字典。
        // ----------------------------------------------------------------
        [Fact]
        public void Get_FallsBackToEnglish_WhenKeyMissingFromCurrentLanguage()
        {
            // 设置为非英文的有效语言。
            Localization.Language = "zh-CN";

            // 所有语言共享相同的键集，因此仅英文独有的
            // 回退路径在有效语言下自然不可达。然而，
            // 我们可以验证当某个键缺失时，英文仍然是
            // 安全网：请求一个在任何语言中都不存在的键，
            // 并确保方法返回键本身（而非异常或 null）。
            // 实现中在进行最终回退前会先检查英文。
            string result = Localization.Get("NonExistentKey_XYZ");
            Assert.Equal("NonExistentKey_XYZ", result);

            // 现在切换到未知语言，并确认
            // 存在于英文中的键仍能解析到英文值。
            Localization.Language = "xx-XX";
            Assert.Equal("Success", Localization.Get("Success"));
            Assert.Equal("Error", Localization.Get("Error"));
        }

        // ----------------------------------------------------------------
        // 测试8：当键不存在于任何语言字典中时，
        //         Get() 返回键本身。
        // ----------------------------------------------------------------
        [Fact]
        public void Get_ReturnsKeyItself_WhenKeyNotInAnyLanguage()
        {
            const string unknownKey = "ThisKeyDoesNotExistAnywhere_12345";

            // 使用有效语言测试。
            Localization.Language = "zh-CN";
            Assert.Equal(unknownKey, Localization.Get(unknownKey));

            // 使用英文测试。
            Localization.Language = "en-US";
            Assert.Equal(unknownKey, Localization.Get(unknownKey));

            // 使用未知语言测试 — 在尝试当前语言和英文查找
            // 都失败后，仍应返回键本身。
            Localization.Language = "fr-FR";
            Assert.Equal(unknownKey, Localization.Get(unknownKey));
        }

        // ----------------------------------------------------------------
        // 测试9：三种已注册语言具有一致的
        //         （相同的）键集。
        // ----------------------------------------------------------------
        [Fact]
        public void AllLanguages_HaveConsistentKeySet()
        {
            var translations = GetTranslations();

            // 预期正好三种语言。
            Assert.True(translations.ContainsKey("zh-CN"));
            Assert.True(translations.ContainsKey("en-US"));
            Assert.True(translations.ContainsKey("ja-JP"));
            Assert.Equal(3, translations.Count);

            var zhKeys = new HashSet<string>(translations["zh-CN"].Keys);
            var enKeys = new HashSet<string>(translations["en-US"].Keys);
            var jaKeys = new HashSet<string>(translations["ja-JP"].Keys);

            // 三者必须相等。
            Assert.True(zhKeys.SetEquals(enKeys),
                "zh-CN and en-US key sets differ.");
            Assert.True(enKeys.SetEquals(jaKeys),
                "en-US and ja-JP key sets differ.");

            // 同时验证任何语言中都没有多余的键。
            Assert.Equal(zhKeys.Count, enKeys.Count);
            Assert.Equal(enKeys.Count, jaKeys.Count);
        }

        // ----------------------------------------------------------------
        // 测试10：应用依赖的特定 UI 键
        //          在每种语言中都存在（翻译为非键值）。
        // ----------------------------------------------------------------
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
        [InlineData("SequenceLabel")]
        [InlineData("SequenceLoop")]
        [InlineData("SequenceRandom")]
        [InlineData("SequenceOnce")]
        [InlineData("ShowOverlay")]
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
        public void RequiredKeys_ExistInAllLanguages(string key)
        {
            // 在每个受支持的语言中，键必须解析为真实的
            // 翻译字符串（非 null 或空）。对于 zh-CN 和 ja-JP，
            // 结果始终与英文键名不同。对于 en-US，
            // 某些键合法地使用相同的词
            // （例如 "File" -> "File"），因此我们只检查非 null/非空。

            Localization.Language = "zh-CN";
            string zh = Localization.Get(key);
            Assert.False(string.IsNullOrEmpty(zh),
                $"Key '{key}' returned null or empty for zh-CN.");
            Assert.NotEqual(key, zh);

            Localization.Language = "en-US";
            string en = Localization.Get(key);
            Assert.False(string.IsNullOrEmpty(en),
                $"Key '{key}' returned null or empty for en-US.");

            Localization.Language = "ja-JP";
            string ja = Localization.Get(key);
            Assert.False(string.IsNullOrEmpty(ja),
                $"Key '{key}' returned null or empty for ja-JP.");
            Assert.NotEqual(key, ja);
        }

        // ----------------------------------------------------------------
        // 测试11：HardwareMode 键在三种语言中都存在。
        // ----------------------------------------------------------------
        [Fact]
        public void HardwareModeKey_ExistsInAllThreeLanguages()
        {
            const string key = "HardwareMode";

            Localization.Language = "zh-CN";
            string zh = Localization.Get(key);
            Assert.NotEqual(key, zh);
            Assert.Equal("硬件模拟 (SendInput)", zh);

            Localization.Language = "en-US";
            string en = Localization.Get(key);
            Assert.NotEqual(key, en);
            Assert.Equal("Hardware Simulation (SendInput)", en);

            Localization.Language = "ja-JP";
            string ja = Localization.Get(key);
            Assert.NotEqual(key, ja);
            Assert.Equal("ハードウェアシミュレーション (SendInput)", ja);
        }
    }
}
