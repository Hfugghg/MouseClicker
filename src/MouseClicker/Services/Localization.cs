using System.Collections.Generic;

namespace MouseClicker
{
    // 处理应用程序的本地化和多语言支持。
    public static class Localization
    {
        // 当前选择的语言代码, 例如 “zh-CN” 或 “en-US”。
        public static string Language = "zh-CN";

        // 存储所有翻译文本的字典。
        private static readonly Dictionary<string, Dictionary<string, string>> Translations = new Dictionary<string, Dictionary<string, string>>
        {
            // 中文翻译
            {
                "zh-CN", new Dictionary<string, string>
                {
                    { "File", "文件" },
                    { "SaveConfig", "保存配置" },
                    { "LoadConfig", "读取配置" },
                    { "Language", "语言" },
                    { "Help", "帮助" },
                    { "ViewHelp", "查看教程" },
                    { "About", "关于" },
                    { "Add", "添加" },
                    { "Remove", "移除" },
                    { "MoveUp", "上移" },
                    { "MoveDown", "下移" },
                    { "IntervalLabel", "间隔(ms):" },
                    { "JitterLabel", "抖动(ms):" },
                    { "JitterExceedsIntervalHint", "抖动大于间隔时，会延长平均等待时间。" },
                    { "SequenceLabel", "点击顺序:" },
                    { "SequenceLoop", "自定义顺序循环" },
                    { "SequenceRandom", "随机顺序循环" },
                    { "SequenceOnce", "单次顺序点击" },
                    { "ShowOverlay", "显示浮窗" },
                    { "HardwareMode", "硬件模拟 (SendInput)" },
                    { "StartClicking", "开始点击" },
                    { "StopClicking", "停止点击 (ESC)" },
                    { "WindowTitle", "鼠标点击器" },
                    { "SaveConfigTitle", "保存配置文件" },
                    { "JsonFilter", "JSON 文件|*.json" },
                    { "ConfigSavedSuccess", "配置已成功保存。" },
                    { "Success", "成功" },
                    { "SaveConfigError", "保存配置失败: {0}" },
                    { "Error", "错误" },
                    { "LoadConfigTitle", "读取配置文件" },
                    { "ConfigLoadedSuccess", "配置已成功加载。" },
                    { "LoadConfigError", "读取配置失败: {0}" },
                    { "AboutMessage", "鼠标点击器 v1.2.1\n" },
                    { "SetLabelPrompt", "请输入此区域的标注字符:" },
                    { "SetLabelTitle", "设置标注" },
                    { "AreaLabelDefault", "区域" },
                    { "AddAreaFirst", "请至少添加一个点击区域" },
                    { "HelpTutorialTitle", "软件使用教程" },
                    { "HelpTutorialContent", """
欢迎使用鼠标点击器！

1. 添加点击区域:
   - 点击“添加”按钮。
   - 屏幕会变暗，此时您可以按住鼠标左键拖动，以选择一个矩形区域。
   - 松开鼠标后，您可以为此区域设置一个名称。

2. 管理点击区域:
   - “移除”: 删除列表中选中的区域。
   - “上移”/“下移”: 调整列表中区域的顺序，这会影响“自定义顺序循环”模式下的点击次序。

3. 设置点击参数:
   - “间隔(ms)”: 每次点击之间的基础等待时间（毫秒）。
   - “抖动(ms)”: 在基础间隔上增加或减去一个随机值，使点击间隔更自然。例如，间隔1000，抖动100，则实际间隔在900到1100之间。

4. 设置点击顺序:
   - “自定义顺序循环”: 按照列表中从上到下的顺序循环点击。
   - “随机顺序循环”: 在所有区域中随机选择一个进行点击，无限循环。
   - “单次顺序点击”: 按照列表顺序点击一遍，然后自动停止。

5. 开始和停止:
   - 点击“开始点击”按钮启动。
   - 在点击过程中，按钮会变为“停止点击 (ESC)”。您可以随时点击此按钮或按键盘上的 ESC 键来停止。

6. 配置文件:
   - “文件” -> “保存配置”: 将当前所有的点击区域和设置保存到一个文件中。
   - “文件” -> “读取配置”: 从文件中加载之前保存的配置。

7. 语言切换:
   - “语言”菜单允许您在中文和英文以及日语之间切换界面语言。

感谢您的使用！
""" },
                    { "CloseButton", "关闭" }
                }
            },
            // 英文翻译
            {
                "en-US", new Dictionary<string, string>
                {
                    { "File", "File" },
                    { "SaveConfig", "Save Config" },
                    { "LoadConfig", "Load Config" },
                    { "Language", "Language" },
                    { "Help", "Help" },
                    { "ViewHelp", "View Tutorial" },
                    { "About", "About" },
                    { "Add", "Add" },
                    { "Remove", "Remove" },
                    { "MoveUp", "Move Up" },
                    { "MoveDown", "Move Down" },
                    { "IntervalLabel", "Interval (ms):" },
                    { "JitterLabel", "Jitter (ms):" },
                    { "JitterExceedsIntervalHint", "When jitter exceeds the interval, the average wait time increases." },
                    { "SequenceLabel", "Click Sequence:" },
                    { "SequenceLoop", "Loop in Order" },
                    { "SequenceRandom", "Loop in Random Order" },
                    { "SequenceOnce", "Click Once in Order" },
                    { "ShowOverlay", "Show Overlay" },
                    { "HardwareMode", "Hardware Simulation (SendInput)" },
                    { "StartClicking", "Start Clicking" },
                    { "StopClicking", "Stop Clicking (ESC)" },
                    { "WindowTitle", "Mouse Clicker" },
                    { "SaveConfigTitle", "Save Configuration File" },
                    { "JsonFilter", "JSON Files|*.json" },
                    { "ConfigSavedSuccess", "Configuration saved successfully." },
                    { "Success", "Success" },
                    { "SaveConfigError", "Failed to save configuration: {0}" },
                    { "Error", "Error" },
                    { "LoadConfigTitle", "Load Configuration File" },
                    { "ConfigLoadedSuccess", "Configuration loaded successfully." },
                    { "LoadConfigError", "Failed to load configuration: {0}" },
                    { "AboutMessage", "Mouse Clicker v1.2.1\n" },
                    { "SetLabelPrompt", "Please enter a label for this area:" },
                    { "SetLabelTitle", "Set Label" },
                    { "AreaLabelDefault", "Area" },
                    { "AddAreaFirst", "Please add at least one click area." },
                    { "HelpTutorialTitle", "Software Tutorial" },
                    { "HelpTutorialContent", """
Welcome to Mouse Clicker!

1. Add a Click Area:
   - Click the "Add" button.
   - The screen will dim. Press and drag the left mouse button to select a rectangular area.
   - After releasing the mouse, you can set a name for this area.

2. Manage Click Areas:
   - "Remove": Deletes the selected area from the list.
   - "Move Up"/"Move Down": Adjusts the order of areas in the list, which affects the click sequence in "Loop in Order" mode.

3. Set Click Parameters:
   - "Interval (ms)": The base waiting time between each click (in milliseconds).
   - "Jitter (ms)": Adds or subtracts a random value to the base interval to make the clicks more natural. E.g., with an interval of 1000 and jitter of 100, the actual interval will be between 900 and 1100.

4. Set Click Sequence:
   - "Loop in Order": Clicks through the areas sequentially from top to bottom in the list, and repeats.
   - "Loop in Random Order": Randomly selects an area to click from all defined areas, repeating indefinitely.
   - "Click Once in Order": Clicks through the areas sequentially once, then stops automatically.

5. Start and Stop:
   - Click the "Start Clicking" button to begin.
   - During clicking, the button will change to "Stop Clicking (ESC)". You can click this button or press the ESC key on your keyboard to stop at any time.

6. Configuration Files:
   - "File" -> "Save Config": Saves all current click areas and settings to a file.
   - "File" -> "Load Config": Loads a previously saved configuration from a file.

7. Language Switching:
   - The "Language" menu allows you to switch the interface language between Chinese、English and Japanese.

Thank you for using the application!
""" },
                    { "CloseButton", "Close" }
                }
            },
            // 日本語翻訳
            {
                "ja-JP", new Dictionary<string, string>
                {
                    { "File", "ファイル" },
                    { "SaveConfig", "設定を保存" },
                    { "LoadConfig", "設定を読み込む" },
                    { "Language", "言語" },
                    { "Help", "ヘルプ" },
                    { "ViewHelp", "チュートリアルを見る" },
                    { "About", "バージョン情報" },
                    { "Add", "追加" },
                    { "Remove", "削除" },
                    { "MoveUp", "上へ" },
                    { "MoveDown", "下へ" },
                    { "IntervalLabel", "間隔 (ms):" },
                    { "JitterLabel", "ジッター (ms):" },
                    { "JitterExceedsIntervalHint", "ジッターが間隔を上回ると、平均待ち時間が長くなります。" },
                    { "SequenceLabel", "クリックシーケンス:" },
                    { "SequenceLoop", "順番にループ" },
                    { "SequenceRandom", "ランダムにループ" },
                    { "SequenceOnce", "順番に1回クリック" },
                    { "ShowOverlay", "オーバーレイを表示" },
                    { "HardwareMode", "ハードウェアシミュレーション (SendInput)" },
                    { "StartClicking", "クリックを開始" },
                    { "StopClicking", "クリックを停止 (ESC)" },
                    { "WindowTitle", "マウス自動クリッカー" },
                    { "SaveConfigTitle", "設定ファイルを保存" },
                    { "JsonFilter", "JSON ファイル|*.json" },
                    { "ConfigSavedSuccess", "設定が正常に保存されました。" },
                    { "Success", "成功" },
                    { "SaveConfigError", "設定の保存に失敗しました: {0}" },
                    { "Error", "エラー" },
                    { "LoadConfigTitle", "設定ファイルを読み込む" },
                    { "ConfigLoadedSuccess", "設定が正常に読み込まれました。" },
                    { "LoadConfigError", "設定の読み込みに失敗しました: {0}" },
                    { "AboutMessage", "マウス自動クリッカー v1.2.1\n" },
                    { "SetLabelPrompt", "このエリアのラベルを入力してください:" },
                    { "SetLabelTitle", "ラベルを設定" },
                    { "AreaLabelDefault", "エリア" },
                    { "AddAreaFirst", "少なくとも1つのクリックエリアを追加してください。" },
                    { "HelpTutorialTitle", "ソフトウェアチュートリアル" },
                    { "HelpTutorialContent", """
マウス自動クリッカーへようこそ！

1. クリックエリアの追加:
   - 「追加」ボタンをクリックします。
   - 画面が暗くなります。マウスの左ボタンを押しながらドラッグして、長方形のエリアを選択します。
   - マウスを離した後、このエリアに名前を設定できます。

2. クリックエリアの管理:
   - 「削除」: リストで選択したエリアを削除します。
   - 「上へ」/「下へ」: リスト内のエリアの順序を調整します。これは「順番にループ」モードでのクリック順序に影響します。

3. クリックパラメータの設定:
   - 「間隔 (ms)」: 各クリック間の基本待機時間（ミリ秒）。
   - 「ジッター (ms)」: クリック間隔をより自然にするために、基本間隔にランダムな値を追加または減算します。例：間隔1000、ジッター100の場合、実際の間隔は900から1100の間になります。

4. クリックシーケンスの設定:
   - 「順番にループ」: リストの上から下へ順番にエリアをクリックし、それを繰り返します。
   - 「ランダムにループ」: 定義されたすべてのエリアからランダムに1つを選択してクリックし、無限に繰り返します。
   - 「順番に1回クリック」: エリアを順番に1回ずつクリックした後、自動的に停止します。

5. 開始と停止:
   - 「クリックを開始」ボタンをクリックして開始します。
   - クリック中は、ボタンが「クリックを停止 (ESC)」に変わります。このボタンをクリックするか、キーボードのESCキーを押すといつでも停止できます。

6. 設定ファイル:
   - 「ファイル」 -> 「設定を保存」: 現在のすべてのクリックエリアと設定をファイルに保存します。
   - 「ファイル」 -> 「設定を読み込む」: 以前に保存した設定をファイルから読み込みます。

7. 言語の切り替え:
   - 「言語」メニューで、インターフェースの言語を中国語、英語、日本語の間で切り替えることができます。

ご利用いただきありがとうございます！
""" },
                    { "CloseButton", "閉じる" }
                }
            }
        };

        // 根据键获取当前语言的翻译文本。
        public static string Get(string key)
        {
            if (Translations.ContainsKey(Language) && Translations[Language].ContainsKey(key))
            {
                return Translations[Language][key];
            }
            if (Translations["en-US"].ContainsKey(key))
            {
                return Translations["en-US"][key];
            }
            return key;
        }
    }
}
