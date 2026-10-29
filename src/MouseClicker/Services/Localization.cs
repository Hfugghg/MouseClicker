using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;

namespace MouseClicker
{
    public static class Localization
    {
        // 此列表需与安装程序的语言保持一致；值使用各语言的本地名称。
        public static IReadOnlyDictionary<string, string> SupportedLanguages { get; } =
            new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
            {
                { "en-US", "English" },
                { "zh-CN", "简体中文" },
                { "ja-JP", "日本語" },
                { "ko-KR", "한국어" },
                { "zh-TW", "繁體中文" },
                { "fr-FR", "Français" },
                { "de-DE", "Deutsch" },
                { "es-ES", "Español" },
                { "ru-RU", "Русский" },
                { "pt-BR", "Português (Brasil)" }
            });

        public static string Language = ResolveLanguage(CultureInfo.CurrentUICulture);

        public static string ResolveLanguage(CultureInfo culture)
        {
            ArgumentNullException.ThrowIfNull(culture);

            if (culture.TwoLetterISOLanguageName.Equals("zh", StringComparison.OrdinalIgnoreCase))
            {
                for (var current = culture; current.Name.Length > 0; current = current.Parent)
                {
                    var tags = current.Name.Split('-');
                    // 显式指定的文字系统比区域更具体，例如 zh-Hans-HK。
                    foreach (var tag in tags)
                    {
                        if (tag.Equals("Hant", StringComparison.OrdinalIgnoreCase) ||
                            tag.Equals("CHT", StringComparison.OrdinalIgnoreCase))
                            return "zh-TW";
                        if (tag.Equals("Hans", StringComparison.OrdinalIgnoreCase) ||
                            tag.Equals("CHS", StringComparison.OrdinalIgnoreCase))
                            return "zh-CN";
                    }
                    foreach (var tag in tags)
                    {
                        if (tag.Equals("TW", StringComparison.OrdinalIgnoreCase) ||
                            tag.Equals("HK", StringComparison.OrdinalIgnoreCase) ||
                            tag.Equals("MO", StringComparison.OrdinalIgnoreCase))
                            return "zh-TW";
                        if (tag.Equals("CN", StringComparison.OrdinalIgnoreCase) ||
                            tag.Equals("SG", StringComparison.OrdinalIgnoreCase))
                            return "zh-CN";
                    }
                }
                return "zh-CN";
            }

            for (var current = culture; current.Name.Length > 0; current = current.Parent)
            {
                foreach (var supported in SupportedLanguages.Keys)
                {
                    if (supported.Equals(current.Name, StringComparison.OrdinalIgnoreCase))
                        return supported;
                }

                string? family = current.TwoLetterISOLanguageName.ToLowerInvariant() switch
                {
                    "en" => "en-US",
                    "ja" => "ja-JP",
                    "ko" => "ko-KR",
                    "fr" => "fr-FR",
                    "de" => "de-DE",
                    "es" => "es-ES",
                    "ru" => "ru-RU",
                    "pt" => "pt-BR",
                    _ => null
                };
                if (family != null)
                    return family;
            }
            return "en-US";
        }

        private static readonly Dictionary<string, Dictionary<string, string>> Translations =
            new Dictionary<string, Dictionary<string, string>>
            {
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
                        { "HighlightCurrentArea", "Highlight current click area" },
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
                        { "AboutMessage", "Mouse Clicker v{0}\n" },
                        { "SetLabelPrompt", "Please enter a label for this area:" },
                        { "SetLabelTitle", "Set Label" },
                        { "AreaLabelDefault", "Area" },
                        { "AddAreaFirst", "Please add at least one click area." },
                        { "HelpTutorialTitle", "Software Tutorial" },
                        { "HelpTutorialContent", """
Welcome to Mouse Clicker!

1. Add a Click Area:
   - Click "Add".
   - When the screen dims, hold the left mouse button and drag to select a rectangle.
   - Release the button, then give the area a name.

2. Manage Click Areas:
   - "Remove" deletes the selected area.
   - "Move Up" and "Move Down" change the list order and the order used by sequential clicking.

3. Set the Interval and Jitter:
   - "Interval (ms)" is the base wait between clicks.
   - "Jitter (ms)" is the range of the random offset added to or subtracted from the interval.
   - If the total is negative, its absolute value is used. The final wait is at least 10 ms.
   - When jitter exceeds the interval, the average wait increases. For example, interval 1000 and jitter 100 produce waits from 900 to 1100 ms.

4. Choose the Click Sequence:
   - "Loop in Order" clicks from the top to the bottom of the list, then repeats.
   - "Loop in Random Order" chooses a random area for each click.
   - "Click Once in Order" clicks each area once, then stops.

5. Start and Stop:
   - Click "Start Clicking".
   - During execution, use the stop button or press ESC to stop at any time.
   - The SendInput option sends synthetic input events through the Windows API.

6. Configuration Files:
   - "File" → "Save Config" saves areas and settings to a JSON file.
   - "File" → "Load Config" opens a previously saved file.

7. Languages:
   - On first launch, the app matches the device display language, falling back to English if it is unsupported.
   - The "Language" menu offers English, Simplified Chinese, Japanese, Korean, Traditional Chinese, French, German, Spanish, Russian, and Brazilian Portuguese.

8. About and Updates:
   - "About" provides the project page and information about new versions.
   - "View update" opens the release page. "Skip this version" hides notifications for that version.

9. Locate and Highlight Areas:
   - Hover over or select an item in the area list to locate it with a blue border.
   - During automatic clicking, the current click area is marked with a green border.
   - "Highlight current click area" is on by default and is saved with the configuration. Turn it off to disable the green highlight.
   - These highlights are visible only while "Show Overlay" is enabled.

10. Hotkey and Mouse Button Settings:
   - "Settings" → "Hotkey Settings": click the input box, then press the desired key. Supported keys are F6–F12, Insert, Home, End, PageUp, PageDown and Pause. Do not hold Ctrl/Alt/Shift/Win. ESC remains the emergency stop while clicking.
   - Right-click an area in the list to choose the left or right mouse button. The list shows [L] or [R] for each area.

Thank you for using the application!
""" },
                        { "CloseButton", "Close" },
                        { "ProjectAddress", "Project page" },
                        { "CheckingForUpdates", "Checking for updates…" },
                        { "UpdateAvailable", "Update available" },
                        { "UpdateAvailableVersion", "Version {0} is available." },
                        { "NoUpdateAvailable", "No updates available." },
                        { "UpdateCheckUnavailable", "Unable to check for updates right now." },
                        { "ViewUpdate", "View update" },
                        { "SkipThisVersion", "Skip this version" },
                        { "UpdateSkipped", "Version {0} was skipped." },
                        { "UpdatePreferencesError", "Unable to save the skipped-version preference: {0}" },
                        { "OpenLinkError", "Unable to open the link: {0}" },
                        { "HotkeySettings", "Hotkey Settings" },
                        { "HotkeyLabel", "Start/stop hotkey:" },
                        { "HotkeyHint", "Click the box above, then press a key. ESC still stops clicking." },
                        { "HotkeyApply", "Apply" },
                        { "HotkeyInvalidKey", "Unsupported key or modifier. Use F6–F12, Insert, Home, End, PageUp, PageDown or Pause without Ctrl/Alt/Shift/Win." },
                        { "HotkeyUnavailable", "Global hotkey is unavailable: {0}" },
                        { "LeftMouseButton", "Left mouse button" },
                        { "RightMouseButton", "Right mouse button" },
                        { "MouseInputError", "Mouse input failed. Automatic clicking has stopped.\n{0}" }
                    }
                },
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
                        { "SequenceLoop", "按顺序循环" },
                        { "SequenceRandom", "随机顺序循环" },
                        { "SequenceOnce", "单次顺序点击" },
                        { "ShowOverlay", "显示区域框" },
                        { "HighlightCurrentArea", "点击时高亮区域" },
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
                        { "AboutMessage", "鼠标点击器 v{0}\n" },
                        { "SetLabelPrompt", "请输入此区域的名称：" },
                        { "SetLabelTitle", "设置名称" },
                        { "AreaLabelDefault", "区域" },
                        { "AddAreaFirst", "请至少添加一个点击区域" },
                        { "HelpTutorialTitle", "软件使用教程" },
                        { "HelpTutorialContent", """
欢迎使用鼠标点击器！

1. 添加点击区域：
   - 点击“添加”按钮。
   - 屏幕变暗后，按住鼠标左键拖动，选择矩形区域。
   - 松开鼠标后，为此区域设置名称。

2. 管理点击区域：
   - “移除”会删除列表中选中的区域。
   - “上移”和“下移”会调整列表顺序，影响顺序点击模式的次序。

3. 设置间隔与抖动：
   - “间隔(ms)”是两次点击之间的基础等待时间。
   - “抖动(ms)”是加到或减去基础间隔的随机偏移量范围。
   - 总等待时间为负数时取绝对值，最后保留最短 10ms 等待。
   - 抖动大于间隔时，平均等待时间会变长。例如间隔 1000、抖动 100，等待范围为 900～1100ms。

4. 设置点击顺序：
   - “按顺序循环”：从上到下依次点击列表中的区域并重复。
   - “随机顺序循环”：每次随机选择一个区域。
   - “单次顺序点击”：每个区域点击一次后自动停止。

5. 开始和停止：
   - 点击“开始点击”启动。
   - 运行时可随时点击停止按钮或按 ESC 键停止。
   - SendInput 选项通过 Windows API 发送合成输入。

6. 配置文件：
   - “文件”→“保存配置”可将区域和设置保存为 JSON 文件。
   - “文件”→“读取配置”可加载先前保存的文件。

7. 语言：
   - 首次启动自动匹配设备显示语言；不支持的语言使用英文。
   - “语言”菜单支持英语、简体中文、日语、韩语、繁体中文、法语、德语、西班牙语、俄语和巴西葡萄牙语。

8. 关于与更新：
   - “关于”提供项目地址和新版本信息。
   - 可点击“查看更新”打开发行页面，或“跳过此版本”隐藏该版本的通知。

9. 区域定位与高亮：
   - 鼠标悬停或选中区域列表中的项目时，会用蓝色边框标出对应区域。
   - 自动点击时，当前点击区域会以绿色边框高亮。
   - “点击时高亮区域”默认开启，并随配置保存；关闭后不再显示绿色高亮。
   - 这些高亮仅在开启“显示区域框”时可见。

10. 热键与鼠标按键设置：
   - “设置”→“热键设置”：点击输入框后按下所需按键。支持的按键为 F6–F12、Insert、Home、End、PageUp、PageDown 和 Pause；请勿同时按住 Ctrl/Alt/Shift/Win。点击过程中 ESC 仍可紧急停止。
   - 右键点击列表中的区域，可为该区域选择鼠标左键或右键；列表中以 [L] 或 [R] 显示当前按键。

感谢您的使用！
""" },
                        { "CloseButton", "关闭" },
                        { "ProjectAddress", "项目地址" },
                        { "CheckingForUpdates", "正在检查更新…" },
                        { "UpdateAvailable", "有更新" },
                        { "UpdateAvailableVersion", "发现新版本 {0}" },
                        { "NoUpdateAvailable", "未发现可用更新。" },
                        { "UpdateCheckUnavailable", "暂时无法检查更新。" },
                        { "ViewUpdate", "查看更新" },
                        { "SkipThisVersion", "跳过此版本" },
                        { "UpdateSkipped", "已跳过版本 {0}" },
                        { "UpdatePreferencesError", "无法保存跳过设置：{0}" },
                        { "OpenLinkError", "无法打开链接：{0}" },
                        { "HotkeySettings", "热键设置" },
                        { "HotkeyLabel", "启停热键：" },
                        { "HotkeyHint", "点击上方输入框后按下按键。点击过程中 ESC 仍可紧急停止。" },
                        { "HotkeyApply", "应用" },
                        { "HotkeyInvalidKey", "不支持的按键或组合键。请使用 F6–F12、Insert、Home、End、PageUp、PageDown 或 Pause，且不要同时按 Ctrl/Alt/Shift/Win。" },
                        { "HotkeyUnavailable", "全局热键不可用：{0}" },
                        { "LeftMouseButton", "鼠标左键" },
                        { "RightMouseButton", "鼠标右键" },
                        { "MouseInputError", "鼠标输入失败，自动点击已停止。\n{0}" }
                    }
                },
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
                        { "HighlightCurrentArea", "クリック中のエリアを強調" },
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
                        { "AboutMessage", "マウス自動クリッカー v{0}\n" },
                        { "SetLabelPrompt", "このエリアのラベルを入力してください:" },
                        { "SetLabelTitle", "ラベルを設定" },
                        { "AreaLabelDefault", "エリア" },
                        { "AddAreaFirst", "少なくとも1つのクリックエリアを追加してください。" },
                        { "HelpTutorialTitle", "ソフトウェアチュートリアル" },
                        { "HelpTutorialContent", """
マウス自動クリッカーへようこそ！

1. クリックエリアの追加：
   - 「追加」をクリックします。
   - 画面が暗くなったら、マウスの左ボタンを押したままドラッグして長方形のエリアを選択します。
   - ボタンを離してから、エリアの名前を設定します。

2. クリックエリアの管理：
   - 「削除」は選択したエリアを削除します。
   - 「上へ」と「下へ」はリストの順序を変更し、順番にクリックするモードに反映されます。

3. 間隔とジッターの設定：
   - 「間隔 (ms)」はクリック間の基本待機時間です。
   - 「ジッター (ms)」は基本間隔に加算または減算するランダムな値の範囲です。
   - 合計が負の場合は絶対値を使い、最終待機時間は最低 10ms にします。
   - ジッターが間隔を上回ると平均待機時間が長くなります。例えば間隔 1000、ジッター 100 なら、待機時間は 900～1100ms です。

4. クリックシーケンス：
   - 「順番にループ」はリストを上から下へクリックし、繰り返します。
   - 「ランダムにループ」はクリックのたびにエリアをランダムに選びます。
   - 「順番に1回クリック」は各エリアを1回ずつクリックして停止します。

5. 開始と停止：
   - 「クリックを開始」をクリックします。
   - 実行中は停止ボタンか ESC キーでいつでも停止できます。
   - SendInput は Windows API を通じて合成入力を送信します。

6. 設定ファイル：
   - 「ファイル」→「設定を保存」でエリアと設定を JSON ファイルに保存します。
   - 「ファイル」→「設定を読み込む」で保存したファイルを開きます。

7. 言語：
   - 初回起動時はデバイスの表示言語を自動選択し、未対応の言語では英語を使用します。
   - 「言語」メニューで英語、簡体字中国語、日本語、韓国語、繁体字中国語、フランス語、ドイツ語、スペイン語、ロシア語、ブラジルポルトガル語を選べます。

8. バージョン情報と更新：
   - 「バージョン情報」でプロジェクトページや新しいバージョンの情報を確認できます。
   - 「更新を見る」はリリースページを開きます。「このバージョンをスキップ」はそのバージョンの通知を非表示にします。

9. エリアの位置確認と強調表示：
   - リストの項目にマウスを合わせるか選択すると、対応するエリアを青い枠線で表示します。
   - 自動クリック中は、現在クリックしているエリアを緑の枠線で強調表示します。
   - 「クリック中のエリアを強調」は既定でオンになっており、設定ファイルに保存されます。オフにすると緑色の強調表示を停止します。
   - これらの強調表示は「オーバーレイを表示」がオンのときだけ見えます。

10. ホットキーとマウスボタンの設定：
   - 「設定」→「ホットキー設定」：入力欄をクリックしてから、目的のキーを押します。使用できるキーは F6～F12、Insert、Home、End、PageUp、PageDown、Pause です。Ctrl/Alt/Shift/Win は押さないでください。クリック中は ESC で緊急停止できます。
   - リスト内のエリアを右クリックすると、マウスの左ボタンまたは右ボタンを選択できます。リストには各エリアのボタンが [L] または [R] で表示されます。

ご利用いただきありがとうございます！
""" },
                        { "CloseButton", "閉じる" },
                        { "ProjectAddress", "プロジェクトページ" },
                        { "CheckingForUpdates", "更新を確認中…" },
                        { "UpdateAvailable", "更新があります" },
                        { "UpdateAvailableVersion", "バージョン {0} が利用できます。" },
                        { "NoUpdateAvailable", "利用可能な更新はありません。" },
                        { "UpdateCheckUnavailable", "現在、更新を確認できません。" },
                        { "ViewUpdate", "更新を見る" },
                        { "SkipThisVersion", "このバージョンをスキップ" },
                        { "UpdateSkipped", "バージョン {0} をスキップしました。" },
                        { "UpdatePreferencesError", "スキップ設定を保存できません：{0}" },
                        { "OpenLinkError", "リンクを開けません：{0}" },
                        { "HotkeySettings", "ホットキー設定" },
                        { "HotkeyLabel", "開始/停止ホットキー：" },
                        { "HotkeyHint", "上の入力欄をクリックしてキーを押してください。クリック中は ESC で緊急停止できます。" },
                        { "HotkeyApply", "適用" },
                        { "HotkeyInvalidKey", "サポートされていないキーまたは修飾キーです。F6～F12、Insert、Home、End、PageUp、PageDown、Pause を Ctrl/Alt/Shift/Win なしで使用してください。" },
                        { "HotkeyUnavailable", "グローバルホットキーを使用できません：{0}" },
                        { "LeftMouseButton", "マウスの左ボタン" },
                        { "RightMouseButton", "マウスの右ボタン" },
                        { "MouseInputError", "マウス入力に失敗したため、自動クリックを停止しました。\n{0}" }
                    }
                },
                {
                    "ko-KR", new Dictionary<string, string>
                    {
                        { "File", "파일" },
                        { "SaveConfig", "설정 저장" },
                        { "LoadConfig", "설정 불러오기" },
                        { "Language", "언어" },
                        { "Help", "도움말" },
                        { "ViewHelp", "사용 방법" },
                        { "About", "프로그램 정보" },
                        { "Add", "추가" },
                        { "Remove", "제거" },
                        { "MoveUp", "위로" },
                        { "MoveDown", "아래로" },
                        { "IntervalLabel", "간격 (ms):" },
                        { "JitterLabel", "변동 폭 (ms):" },
                        { "JitterExceedsIntervalHint", "변동 폭이 간격보다 크면 평균 대기 시간이 길어집니다." },
                        { "SequenceLabel", "클릭 순서:" },
                        { "SequenceLoop", "순서대로 반복" },
                        { "SequenceRandom", "무작위로 반복" },
                        { "SequenceOnce", "순서대로 한 번" },
                        { "ShowOverlay", "영역 표시" },
                        { "HighlightCurrentArea", "클릭 중인 영역 강조" },
                        { "HardwareMode", "하드웨어 시뮬레이션 (SendInput)" },
                        { "StartClicking", "클릭 시작" },
                        { "StopClicking", "클릭 중지 (ESC)" },
                        { "WindowTitle", "마우스 자동 클릭기" },
                        { "SaveConfigTitle", "설정 파일 저장" },
                        { "JsonFilter", "JSON 파일|*.json" },
                        { "ConfigSavedSuccess", "설정을 저장했습니다." },
                        { "Success", "성공" },
                        { "SaveConfigError", "설정을 저장하지 못했습니다: {0}" },
                        { "Error", "오류" },
                        { "LoadConfigTitle", "설정 파일 불러오기" },
                        { "ConfigLoadedSuccess", "설정을 불러왔습니다." },
                        { "LoadConfigError", "설정을 불러오지 못했습니다: {0}" },
                        { "AboutMessage", "마우스 자동 클릭기 v{0}\n" },
                        { "SetLabelPrompt", "이 영역의 이름을 입력하세요:" },
                        { "SetLabelTitle", "영역 이름 설정" },
                        { "AreaLabelDefault", "영역" },
                        { "AddAreaFirst", "클릭할 영역을 하나 이상 추가하세요." },
                        { "HelpTutorialTitle", "사용 방법" },
                        { "HelpTutorialContent", """
마우스 자동 클릭기에 오신 것을 환영합니다!

1. 클릭 영역 추가:
   - "추가"를 누릅니다.
   - 화면이 어두워지면 마우스 왼쪽 버튼을 누른 채 드래그하여 사각형 영역을 선택합니다.
   - 버튼을 놓은 뒤 영역 이름을 지정합니다.

2. 영역 관리:
   - "제거"는 선택한 영역을 삭제합니다.
   - "위로"와 "아래로"는 목록의 순서를 바꾸며, 순서대로 클릭하는 모드에 적용됩니다.

3. 클릭 간격과 변동 폭:
   - "간격 (ms)"은 클릭 사이의 기본 대기 시간입니다.
   - "변동 폭 (ms)"은 기본 간격에 더하거나 빼는 무작위 값의 범위입니다.
   - 합계가 음수이면 절댓값을 사용하고, 최종 대기 시간은 최소 10ms로 유지합니다.
   - 변동 폭이 간격보다 크면 평균 대기 시간이 길어집니다. 예를 들어 간격 1000, 변동 폭 100이면 대기 시간은 900~1100ms입니다.

4. 클릭 순서:
   - "순서대로 반복": 목록 위에서 아래로 클릭한 뒤 반복합니다.
   - "무작위로 반복": 매번 임의의 영역을 선택합니다.
   - "순서대로 한 번": 각 영역을 한 번씩 클릭한 뒤 중지합니다.

5. 시작과 중지:
   - "클릭 시작"을 눌러 시작합니다.
   - 실행 중에는 중지 버튼이나 ESC 키로 언제든지 중지할 수 있습니다.
   - SendInput 옵션은 Windows API로 합성 입력을 보냅니다.

6. 설정 파일:
   - "파일" → "설정 저장"으로 영역과 설정을 JSON 파일에 저장합니다.
   - "파일" → "설정 불러오기"로 저장한 파일을 불러옵니다.

7. 언어:
   - 처음 실행하면 기기의 표시 언어에 맞는 언어를 자동으로 선택하며, 지원하지 않는 언어는 영어로 표시합니다.
   - "언어" 메뉴에서 영어, 중국어 간체, 일본어, 한국어, 중국어 번체, 프랑스어, 독일어, 스페인어, 러시아어, 포르투갈어(브라질)를 선택할 수 있습니다.

8. 프로그램 정보와 업데이트:
   - "프로그램 정보"에서 프로젝트 페이지와 새 버전 정보를 확인합니다.
   - "업데이트 보기"로 릴리스 페이지를 열거나 "이 버전 건너뛰기"로 해당 버전의 알림을 숨길 수 있습니다.

9. 영역 위치 확인과 강조 표시:
   - 영역 목록의 항목에 마우스를 올리거나 선택하면 해당 영역을 파란색 테두리로 표시합니다.
   - 자동 클릭 중에는 현재 클릭하는 영역을 초록색 테두리로 강조합니다.
   - "클릭 중인 영역 강조"는 기본적으로 켜져 있으며 설정 파일에 저장됩니다. 끄면 초록색 강조 표시가 사라집니다.
   - 이러한 강조 표시는 "영역 표시"를 켰을 때만 보입니다.

10. 단축키 및 마우스 버튼 설정:
   - "설정" → "단축키 설정": 입력란을 클릭한 뒤 원하는 키를 누릅니다. 지원되는 키는 F6–F12, Insert, Home, End, PageUp, PageDown, Pause입니다. Ctrl/Alt/Shift/Win을 함께 누르지 마세요. 클릭 중에는 ESC로 긴급 정지할 수 있습니다.
   - 목록에서 영역을 마우스 오른쪽 버튼으로 클릭하여 마우스 왼쪽 또는 오른쪽 버튼을 선택합니다. 목록에는 각 영역의 버튼이 [L] 또는 [R]로 표시됩니다.

이용해 주셔서 감사합니다!
""" },
                        { "CloseButton", "닫기" },
                        { "ProjectAddress", "프로젝트 페이지" },
                        { "CheckingForUpdates", "업데이트 확인 중…" },
                        { "UpdateAvailable", "업데이트 있음" },
                        { "UpdateAvailableVersion", "새 버전 {0}을(를) 사용할 수 있습니다." },
                        { "NoUpdateAvailable", "사용 가능한 업데이트가 없습니다." },
                        { "UpdateCheckUnavailable", "업데이트를 확인할 수 없습니다." },
                        { "ViewUpdate", "업데이트 보기" },
                        { "SkipThisVersion", "이 버전 건너뛰기" },
                        { "UpdateSkipped", "버전 {0}을(를) 건너뛰었습니다." },
                        { "UpdatePreferencesError", "건너뛰기 설정을 저장할 수 없습니다: {0}" },
                        { "OpenLinkError", "링크를 열 수 없습니다: {0}" },
                        { "HotkeySettings", "단축키 설정" },
                        { "HotkeyLabel", "시작/중지 단축키:" },
                        { "HotkeyHint", "위 입력란을 클릭한 뒤 키를 누르세요. 클릭 중에는 ESC로 긴급 정지할 수 있습니다." },
                        { "HotkeyApply", "적용" },
                        { "HotkeyInvalidKey", "지원되지 않는 키 또는 조합입니다. Ctrl/Alt/Shift/Win 없이 F6–F12, Insert, Home, End, PageUp, PageDown, Pause를 사용하세요." },
                        { "HotkeyUnavailable", "전역 단축키를 사용할 수 없습니다: {0}" },
                        { "LeftMouseButton", "마우스 왼쪽 버튼" },
                        { "RightMouseButton", "마우스 오른쪽 버튼" },
                        { "MouseInputError", "마우스 입력에 실패하여 자동 클릭을 중지했습니다.\n{0}" }
                    }
                },
                {
                    "zh-TW", new Dictionary<string, string>
                    {
                        { "File", "檔案" },
                        { "SaveConfig", "儲存設定" },
                        { "LoadConfig", "載入設定" },
                        { "Language", "語言" },
                        { "Help", "說明" },
                        { "ViewHelp", "查看教學" },
                        { "About", "關於" },
                        { "Add", "新增" },
                        { "Remove", "移除" },
                        { "MoveUp", "上移" },
                        { "MoveDown", "下移" },
                        { "IntervalLabel", "間隔 (ms):" },
                        { "JitterLabel", "抖動 (ms):" },
                        { "JitterExceedsIntervalHint", "抖動大於間隔時，平均等待時間會變長。" },
                        { "SequenceLabel", "點擊順序:" },
                        { "SequenceLoop", "依序循環" },
                        { "SequenceRandom", "隨機循環" },
                        { "SequenceOnce", "依序點擊一次" },
                        { "ShowOverlay", "顯示區域框" },
                        { "HighlightCurrentArea", "點擊時醒目標示區域" },
                        { "HardwareMode", "硬體模擬 (SendInput)" },
                        { "StartClicking", "開始點擊" },
                        { "StopClicking", "停止點擊 (ESC)" },
                        { "WindowTitle", "滑鼠自動點擊器" },
                        { "SaveConfigTitle", "儲存設定檔" },
                        { "JsonFilter", "JSON 檔案|*.json" },
                        { "ConfigSavedSuccess", "設定已成功儲存。" },
                        { "Success", "成功" },
                        { "SaveConfigError", "儲存設定失敗：{0}" },
                        { "Error", "錯誤" },
                        { "LoadConfigTitle", "載入設定檔" },
                        { "ConfigLoadedSuccess", "設定已成功載入。" },
                        { "LoadConfigError", "載入設定失敗：{0}" },
                        { "AboutMessage", "滑鼠自動點擊器 v{0}\n" },
                        { "SetLabelPrompt", "請輸入此區域的標籤：" },
                        { "SetLabelTitle", "設定標籤" },
                        { "AreaLabelDefault", "區域" },
                        { "AddAreaFirst", "請至少新增一個點擊區域。" },
                        { "HelpTutorialTitle", "軟體使用教學" },
                        { "HelpTutorialContent", """
歡迎使用滑鼠自動點擊器！

1. 新增點擊區域：
   - 按下「新增」。
   - 畫面變暗後，按住滑鼠左鍵拖曳，選取矩形區域。
   - 放開滑鼠後，為區域設定標籤。

2. 管理區域：
   - 「移除」會刪除選取的區域。
   - 「上移」與「下移」會調整清單順序，影響依序點擊模式的次序。

3. 設定間隔與抖動：
   - 「間隔 (ms)」是兩次點擊之間的基本等待時間。
   - 「抖動 (ms)」是加到或減去基本間隔的隨機偏移量範圍。
   - 總等待時間為負數時取絕對值，最後保留最短 10ms 等待。
   - 抖動大於間隔會延長平均等待時間。例如間隔 1000、抖動 100，等待範圍為 900～1100ms。

4. 設定點擊順序：
   - 「依序循環」：由上到下依序點擊並重複。
   - 「隨機循環」：每次隨機選擇一個區域。
   - 「依序點擊一次」：每個區域點擊一次後自動停止。

5. 開始與停止：
   - 按下「開始點擊」啟動。
   - 執行時可隨時按停止按鈕或 ESC 鍵停止。
   - SendInput 選項會透過 Windows API 傳送合成輸入。

6. 設定檔：
   - 「檔案」→「儲存設定」可將區域與設定儲存為 JSON 檔案。
   - 「檔案」→「載入設定」可載入先前儲存的檔案。

7. 語言：
   - 首次啟動會自動配合裝置顯示語言；不支援的語言會使用英文。
   - 「語言」選單支援英文、簡體中文、日文、韓文、繁體中文、法文、德文、西班牙文、俄文及巴西葡萄牙文。

8. 關於與更新：
   - 「關於」提供專案網址與新版本資訊。
   - 可按「查看更新」開啟發行頁，或按「略過此版本」隱藏該版本的通知。

9. 區域定位與醒目標示：
   - 將滑鼠移到區域清單項目上或選取項目時，會以藍色外框標出對應區域。
   - 自動點擊時，目前點擊區域會以綠色外框醒目顯示。
   - 「點擊時醒目標示區域」預設開啟，並隨設定檔儲存；關閉後不再顯示綠色標示。
   - 這些標示僅在開啟「顯示區域框」時可見。

10. 熱鍵與滑鼠按鍵設定：
   - 「設定」→「熱鍵設定」：點擊輸入框後按下所需按鍵。支援的按鍵為 F6–F12、Insert、Home、End、PageUp、PageDown 及 Pause；請勿同時按住 Ctrl/Alt/Shift/Win。點擊過程中 ESC 仍可緊急停止。
   - 在清單中的區域上按滑鼠右鍵，可為該區域選擇滑鼠左鍵或右鍵；清單會以 [L] 或 [R] 顯示目前按鍵。

感謝您的使用！
""" },
                        { "CloseButton", "關閉" },
                        { "ProjectAddress", "專案網址" },
                        { "CheckingForUpdates", "正在檢查更新…" },
                        { "UpdateAvailable", "有更新" },
                        { "UpdateAvailableVersion", "發現新版本 {0}" },
                        { "NoUpdateAvailable", "未發現可用更新。" },
                        { "UpdateCheckUnavailable", "目前無法檢查更新。" },
                        { "ViewUpdate", "查看更新" },
                        { "SkipThisVersion", "略過此版本" },
                        { "UpdateSkipped", "已略過版本 {0}" },
                        { "UpdatePreferencesError", "無法儲存略過設定：{0}" },
                        { "OpenLinkError", "無法開啟連結：{0}" },
                        { "HotkeySettings", "熱鍵設定" },
                        { "HotkeyLabel", "啟停熱鍵：" },
                        { "HotkeyHint", "點擊上方輸入框後按下按鍵。點擊過程中 ESC 仍可緊急停止。" },
                        { "HotkeyApply", "套用" },
                        { "HotkeyInvalidKey", "不支援的按鍵或組合鍵。請使用 F6–F12、Insert、Home、End、PageUp、PageDown 或 Pause，且不要同時按 Ctrl/Alt/Shift/Win。" },
                        { "HotkeyUnavailable", "全域熱鍵無法使用：{0}" },
                        { "LeftMouseButton", "滑鼠左鍵" },
                        { "RightMouseButton", "滑鼠右鍵" },
                        { "MouseInputError", "滑鼠輸入失敗，自動點擊已停止。\n{0}" }
                    }
                },
                {
                    "fr-FR", new Dictionary<string, string>
                    {
                        { "File", "Fichier" },
                        { "SaveConfig", "Enregistrer la configuration" },
                        { "LoadConfig", "Charger la configuration" },
                        { "Language", "Langue" },
                        { "Help", "Aide" },
                        { "ViewHelp", "Voir le guide" },
                        { "About", "À propos" },
                        { "Add", "Ajouter" },
                        { "Remove", "Supprimer" },
                        { "MoveUp", "Monter" },
                        { "MoveDown", "Descendre" },
                        { "IntervalLabel", "Intervalle (ms) :" },
                        { "JitterLabel", "Variation (ms) :" },
                        { "JitterExceedsIntervalHint", "Lorsque la variation dépasse l’intervalle, le temps d’attente moyen augmente." },
                        { "SequenceLabel", "Ordre des clics :" },
                        { "SequenceLoop", "Boucle dans l’ordre" },
                        { "SequenceRandom", "Boucle aléatoire" },
                        { "SequenceOnce", "Un passage dans l’ordre" },
                        { "ShowOverlay", "Afficher les zones" },
                        { "HighlightCurrentArea", "Surligner la zone du clic en cours" },
                        { "HardwareMode", "Simulation matérielle (SendInput)" },
                        { "StartClicking", "Démarrer les clics" },
                        { "StopClicking", "Arrêter les clics (ESC)" },
                        { "WindowTitle", "Cliqueur automatique" },
                        { "SaveConfigTitle", "Enregistrer le fichier de configuration" },
                        { "JsonFilter", "Fichiers JSON|*.json" },
                        { "ConfigSavedSuccess", "La configuration a été enregistrée." },
                        { "Success", "Succès" },
                        { "SaveConfigError", "Impossible d’enregistrer la configuration : {0}" },
                        { "Error", "Erreur" },
                        { "LoadConfigTitle", "Charger un fichier de configuration" },
                        { "ConfigLoadedSuccess", "La configuration a été chargée." },
                        { "LoadConfigError", "Impossible de charger la configuration : {0}" },
                        { "AboutMessage", "Cliqueur automatique v{0}\n" },
                        { "SetLabelPrompt", "Saisissez un libellé pour cette zone :" },
                        { "SetLabelTitle", "Nommer la zone" },
                        { "AreaLabelDefault", "Zone" },
                        { "AddAreaFirst", "Ajoutez au moins une zone de clic." },
                        { "HelpTutorialTitle", "Guide d’utilisation" },
                        { "HelpTutorialContent", """
Bienvenue dans le cliqueur automatique !

1. Ajouter une zone de clic :
   - Cliquez sur « Ajouter ».
   - Quand l’écran s’assombrit, maintenez le bouton gauche de la souris et faites glisser le pointeur pour sélectionner un rectangle.
   - Relâchez le bouton, puis donnez un nom à la zone.

2. Gérer les zones :
   - « Supprimer » retire la zone sélectionnée.
   - « Monter » et « Descendre » modifient l’ordre de la liste et des clics séquentiels.

3. Régler l’intervalle et la variation :
   - « Intervalle (ms) » définit l’attente de base entre deux clics.
   - « Variation (ms) » définit l’amplitude de l’écart aléatoire ajouté à l’intervalle ou soustrait de celui-ci.
   - Si la somme est négative, sa valeur absolue est utilisée. L’attente finale est d’au moins 10 ms.
   - Une variation supérieure à l’intervalle augmente l’attente moyenne. Par exemple, un intervalle de 1000 et une variation de 100 donnent une attente de 900 à 1100 ms.

4. Choisir l’ordre des clics :
   - « Boucle dans l’ordre » parcourt la liste de haut en bas, puis recommence.
   - « Boucle aléatoire » choisit une zone au hasard à chaque clic.
   - « Un passage dans l’ordre » clique une fois dans chaque zone, puis s’arrête.

5. Démarrer et arrêter :
   - Cliquez sur « Démarrer les clics ».
   - Pendant l’exécution, le bouton d’arrêt ou la touche ESC permet d’arrêter à tout moment.
   - L’option SendInput envoie des événements synthétiques via l’API Windows.

6. Fichiers de configuration :
   - « Fichier » → « Enregistrer la configuration » sauvegarde les zones et les réglages dans un fichier JSON.
   - « Fichier » → « Charger la configuration » ouvre un fichier enregistré.

7. Langues :
   - Au premier démarrage, l’application choisit la langue d’affichage de l’appareil, ou l’anglais si elle n’est pas prise en charge.
   - Le menu « Langue » propose l’anglais, le chinois simplifié, le japonais, le coréen, le chinois traditionnel, le français, l’allemand, l’espagnol, le russe et le portugais du Brésil.

8. À propos et mises à jour :
   - « À propos » donne accès à la page du projet et aux informations sur les nouvelles versions.
   - « Voir la mise à jour » ouvre la page de publication. « Ignorer cette version » masque les notifications de cette version.

9. Repérer et surligner les zones :
   - Survolez ou sélectionnez un élément de la liste pour repérer sa zone avec un cadre bleu.
   - Pendant les clics automatiques, la zone du clic en cours est entourée d’un cadre vert.
   - « Surligner la zone du clic en cours » est activé par défaut et enregistré dans la configuration. Désactivez cette option pour masquer le surlignage vert.
   - Ces surlignages sont visibles uniquement lorsque « Afficher les zones » est activé.

10. Raccourci clavier et bouton de la souris :
   - « Paramètres » → « Paramètres des raccourcis » : cliquez dans la zone de saisie, puis appuyez sur la touche souhaitée. Les touches prises en charge sont F6–F12, Insert, Home, End, PageUp, PageDown et Pause. Ne maintenez pas Ctrl/Alt/Shift/Win. ESC reste disponible pour arrêter d'urgence pendant les clics.
   - Cliquez avec le bouton droit sur une zone de la liste pour choisir le bouton gauche ou droit de la souris. La liste affiche [L] ou [R] pour chaque zone.

Merci d’utiliser cette application !
""" },
                        { "CloseButton", "Fermer" },
                        { "ProjectAddress", "Page du projet" },
                        { "CheckingForUpdates", "Recherche de mises à jour…" },
                        { "UpdateAvailable", "Mise à jour disponible" },
                        { "UpdateAvailableVersion", "La version {0} est disponible." },
                        { "NoUpdateAvailable", "Aucune mise à jour disponible." },
                        { "UpdateCheckUnavailable", "Impossible de rechercher des mises à jour." },
                        { "ViewUpdate", "Voir la mise à jour" },
                        { "SkipThisVersion", "Ignorer cette version" },
                        { "UpdateSkipped", "La version {0} a été ignorée." },
                        { "UpdatePreferencesError", "Impossible d’enregistrer la version à ignorer : {0}" },
                        { "OpenLinkError", "Impossible d’ouvrir le lien : {0}" },
                        { "HotkeySettings", "Paramètres des raccourcis" },
                        { "HotkeyLabel", "Raccourci démarrer/arrêter :" },
                        { "HotkeyHint", "Cliquez dans la zone ci-dessus, puis appuyez sur une touche. ESC reste disponible pour arrêter d'urgence pendant les clics." },
                        { "HotkeyApply", "Appliquer" },
                        { "HotkeyInvalidKey", "Touche ou combinaison non prise en charge. Utilisez F6–F12, Insert, Home, End, PageUp, PageDown ou Pause sans Ctrl/Alt/Shift/Win." },
                        { "HotkeyUnavailable", "Le raccourci global n'est pas disponible : {0}" },
                        { "LeftMouseButton", "Bouton gauche" },
                        { "RightMouseButton", "Bouton droit" },
                        { "MouseInputError", "Échec de la saisie de la souris. Les clics automatiques ont été arrêtés.\n{0}" }
                    }
                },
                {
                    "de-DE", new Dictionary<string, string>
                    {
                        { "File", "Datei" },
                        { "SaveConfig", "Konfiguration speichern" },
                        { "LoadConfig", "Konfiguration laden" },
                        { "Language", "Sprache" },
                        { "Help", "Hilfe" },
                        { "ViewHelp", "Anleitung anzeigen" },
                        { "About", "Info" },
                        { "Add", "Hinzufügen" },
                        { "Remove", "Entfernen" },
                        { "MoveUp", "Nach oben" },
                        { "MoveDown", "Nach unten" },
                        { "IntervalLabel", "Intervall (ms):" },
                        { "JitterLabel", "Schwankung (ms):" },
                        { "JitterExceedsIntervalHint", "Wenn die Schwankung das Intervall überschreitet, verlängert sich die mittlere Wartezeit." },
                        { "SequenceLabel", "Klickreihenfolge:" },
                        { "SequenceLoop", "Der Reihe nach wiederholen" },
                        { "SequenceRandom", "Zufällig wiederholen" },
                        { "SequenceOnce", "Einmal der Reihe nach" },
                        { "ShowOverlay", "Bereiche anzeigen" },
                        { "HighlightCurrentArea", "Aktuellen Klickbereich hervorheben" },
                        { "HardwareMode", "Hardwaresimulation (SendInput)" },
                        { "StartClicking", "Klicken starten" },
                        { "StopClicking", "Klicken stoppen (ESC)" },
                        { "WindowTitle", "Automatischer Mausklicker" },
                        { "SaveConfigTitle", "Konfigurationsdatei speichern" },
                        { "JsonFilter", "JSON-Dateien|*.json" },
                        { "ConfigSavedSuccess", "Die Konfiguration wurde gespeichert." },
                        { "Success", "Erfolg" },
                        { "SaveConfigError", "Die Konfiguration konnte nicht gespeichert werden: {0}" },
                        { "Error", "Fehler" },
                        { "LoadConfigTitle", "Konfigurationsdatei laden" },
                        { "ConfigLoadedSuccess", "Die Konfiguration wurde geladen." },
                        { "LoadConfigError", "Die Konfiguration konnte nicht geladen werden: {0}" },
                        { "AboutMessage", "Automatischer Mausklicker v{0}\n" },
                        { "SetLabelPrompt", "Geben Sie eine Bezeichnung für diesen Bereich ein:" },
                        { "SetLabelTitle", "Bereich benennen" },
                        { "AreaLabelDefault", "Bereich" },
                        { "AddAreaFirst", "Fügen Sie mindestens einen Klickbereich hinzu." },
                        { "HelpTutorialTitle", "Bedienungsanleitung" },
                        { "HelpTutorialContent", """
Willkommen beim automatischen Mausklicker!

1. Klickbereich hinzufügen:
   - Klicken Sie auf „Hinzufügen“.
   - Sobald der Bildschirm abgedunkelt wird, ziehen Sie mit gedrückter linker Maustaste ein Rechteck auf.
   - Lassen Sie die Maustaste los und geben Sie dem Bereich einen Namen.

2. Bereiche verwalten:
   - „Entfernen“ löscht den ausgewählten Bereich.
   - „Nach oben“ und „Nach unten“ ändern die Listenreihenfolge und damit die Reihenfolge beim sequenziellen Klicken.

3. Intervall und Schwankung einstellen:
   - „Intervall (ms)“ ist die grundlegende Wartezeit zwischen zwei Klicks.
   - „Schwankung (ms)“ bestimmt den zufälligen Wert, der zum Intervall addiert oder davon abgezogen wird.
   - Ist die Summe negativ, wird ihr Betrag verwendet. Die endgültige Wartezeit beträgt mindestens 10 ms.
   - Übersteigt die Schwankung das Intervall, steigt die mittlere Wartezeit. Bei Intervall 1000 und Schwankung 100 liegt die Wartezeit beispielsweise zwischen 900 und 1100 ms.

4. Klickreihenfolge wählen:
   - „Der Reihe nach wiederholen“ klickt die Liste von oben nach unten durch und beginnt erneut.
   - „Zufällig wiederholen“ wählt bei jedem Klick einen zufälligen Bereich.
   - „Einmal der Reihe nach“ klickt jeden Bereich einmal an und stoppt.

5. Starten und stoppen:
   - Klicken Sie auf „Klicken starten“.
   - Mit der Stopptaste oder ESC können Sie den Vorgang jederzeit beenden.
   - Die SendInput-Option sendet synthetische Eingaben über die Windows-API.

6. Konfigurationsdateien:
   - „Datei“ → „Konfiguration speichern“ speichert Bereiche und Einstellungen als JSON-Datei.
   - „Datei“ → „Konfiguration laden“ öffnet eine zuvor gespeicherte Datei.

7. Sprache:
   - Beim ersten Start wird die Anzeigesprache des Geräts gewählt. Nicht unterstützte Sprachen werden durch Englisch ersetzt.
   - Im Menü „Sprache“ stehen Englisch, vereinfachtes Chinesisch, Japanisch, Koreanisch, traditionelles Chinesisch, Französisch, Deutsch, Spanisch, Russisch und brasilianisches Portugiesisch zur Verfügung.

8. Info und Updates:
   - Unter „Info“ finden Sie die Projektseite und Informationen zu neuen Versionen.
   - „Update ansehen“ öffnet die Veröffentlichungsseite. Mit „Diese Version überspringen“ blenden Sie die Benachrichtigung für diese Version aus.

9. Bereiche finden und hervorheben:
   - Bewegen Sie den Mauszeiger über einen Listeneintrag oder wählen Sie ihn aus, um den zugehörigen Bereich mit einem blauen Rahmen zu markieren.
   - Während des automatischen Klickens wird der aktuelle Klickbereich mit einem grünen Rahmen hervorgehoben.
   - „Aktuellen Klickbereich hervorheben“ ist standardmäßig aktiviert und wird in der Konfiguration gespeichert. Deaktivieren Sie die Option, um die grüne Hervorhebung auszuschalten.
   - Die Hervorhebungen sind nur sichtbar, wenn „Bereiche anzeigen“ aktiviert ist.

10. Tastenkürzel und Maustaste:
   - „Einstellungen“ → „Tastenkürzel“: Klicken Sie in das Eingabefeld und drücken Sie die gewünschte Taste. Unterstützt werden F6–F12, Insert, Home, End, PageUp, PageDown und Pause. Halten Sie Ctrl/Alt/Shift/Win nicht gedrückt. ESC bleibt während des Klickens der Notstopp.
   - Klicken Sie mit der rechten Maustaste auf einen Bereich in der Liste, um die linke oder rechte Maustaste zu wählen. Die Liste zeigt [L] oder [R] für jeden Bereich.

Vielen Dank für die Nutzung!
""" },
                        { "CloseButton", "Schließen" },
                        { "ProjectAddress", "Projektseite" },
                        { "CheckingForUpdates", "Updates werden gesucht…" },
                        { "UpdateAvailable", "Update verfügbar" },
                        { "UpdateAvailableVersion", "Version {0} ist verfügbar." },
                        { "NoUpdateAvailable", "Keine Updates verfügbar." },
                        { "UpdateCheckUnavailable", "Die Updatesuche ist derzeit nicht möglich." },
                        { "ViewUpdate", "Update ansehen" },
                        { "SkipThisVersion", "Diese Version überspringen" },
                        { "UpdateSkipped", "Version {0} wurde übersprungen." },
                        { "UpdatePreferencesError", "Die Einstellung zum Überspringen konnte nicht gespeichert werden: {0}" },
                        { "OpenLinkError", "Der Link konnte nicht geöffnet werden: {0}" },
                        { "HotkeySettings", "Tastenkürzel" },
                        { "HotkeyLabel", "Start/Stopp-Taste:" },
                        { "HotkeyHint", "Klicken Sie oben in das Feld und drücken Sie eine Taste. ESC bleibt während des Klickens als Notstopp verfügbar." },
                        { "HotkeyApply", "Übernehmen" },
                        { "HotkeyInvalidKey", "Nicht unterstützte Taste oder Tastenkombination. Verwenden Sie F6–F12, Insert, Home, End, PageUp, PageDown oder Pause ohne Ctrl/Alt/Shift/Win." },
                        { "HotkeyUnavailable", "Der globale Hotkey ist nicht verfügbar: {0}" },
                        { "LeftMouseButton", "Linke Maustaste" },
                        { "RightMouseButton", "Rechte Maustaste" },
                        { "MouseInputError", "Die Mauseingabe ist fehlgeschlagen. Das automatische Klicken wurde gestoppt.\n{0}" }
                    }
                },
                {
                    "es-ES", new Dictionary<string, string>
                    {
                        { "File", "Archivo" },
                        { "SaveConfig", "Guardar configuración" },
                        { "LoadConfig", "Cargar configuración" },
                        { "Language", "Idioma" },
                        { "Help", "Ayuda" },
                        { "ViewHelp", "Ver guía" },
                        { "About", "Acerca de" },
                        { "Add", "Añadir" },
                        { "Remove", "Eliminar" },
                        { "MoveUp", "Subir" },
                        { "MoveDown", "Bajar" },
                        { "IntervalLabel", "Intervalo (ms):" },
                        { "JitterLabel", "Variación (ms):" },
                        { "JitterExceedsIntervalHint", "Si la variación supera el intervalo, aumenta el tiempo medio de espera." },
                        { "SequenceLabel", "Orden de clics:" },
                        { "SequenceLoop", "Repetir en orden" },
                        { "SequenceRandom", "Repetir al azar" },
                        { "SequenceOnce", "Una vez en orden" },
                        { "ShowOverlay", "Mostrar áreas" },
                        { "HighlightCurrentArea", "Resaltar el área del clic actual" },
                        { "HardwareMode", "Simulación de hardware (SendInput)" },
                        { "StartClicking", "Iniciar clics" },
                        { "StopClicking", "Detener clics (ESC)" },
                        { "WindowTitle", "Clic automático" },
                        { "SaveConfigTitle", "Guardar archivo de configuración" },
                        { "JsonFilter", "Archivos JSON|*.json" },
                        { "ConfigSavedSuccess", "La configuración se ha guardado." },
                        { "Success", "Correcto" },
                        { "SaveConfigError", "No se pudo guardar la configuración: {0}" },
                        { "Error", "Error" },
                        { "LoadConfigTitle", "Cargar archivo de configuración" },
                        { "ConfigLoadedSuccess", "La configuración se ha cargado." },
                        { "LoadConfigError", "No se pudo cargar la configuración: {0}" },
                        { "AboutMessage", "Clic automático v{0}\n" },
                        { "SetLabelPrompt", "Escriba una etiqueta para esta área:" },
                        { "SetLabelTitle", "Nombre del área" },
                        { "AreaLabelDefault", "Área" },
                        { "AddAreaFirst", "Añada al menos un área de clic." },
                        { "HelpTutorialTitle", "Guía de uso" },
                        { "HelpTutorialContent", """
¡Bienvenido a Clic automático!

1. Añadir un área de clic:
   - Pulse «Añadir».
   - Cuando la pantalla se oscurezca, mantenga pulsado el botón izquierdo del ratón y arrastre para seleccionar un rectángulo.
   - Suelte el botón y asigne un nombre al área.

2. Gestionar las áreas:
   - «Eliminar» borra el área seleccionada.
   - «Subir» y «Bajar» cambian el orden de la lista y de los clics secuenciales.

3. Ajustar el intervalo y la variación:
   - «Intervalo (ms)» es la espera básica entre dos clics.
   - «Variación (ms)» indica el margen del valor aleatorio que se suma o se resta al intervalo.
   - Si la suma es negativa, se usa su valor absoluto. La espera final es de al menos 10 ms.
   - Una variación superior al intervalo aumenta la espera media. Por ejemplo, un intervalo de 1000 y una variación de 100 producen esperas de 900 a 1100 ms.

4. Elegir el orden:
   - «Repetir en orden» recorre la lista de arriba abajo y vuelve a empezar.
   - «Repetir al azar» elige un área aleatoria en cada clic.
   - «Una vez en orden» hace un clic en cada área y se detiene.

5. Iniciar y detener:
   - Pulse «Iniciar clics».
   - Puede detener la ejecución en cualquier momento con el botón de parada o la tecla ESC.
   - La opción SendInput envía eventos sintéticos mediante la API de Windows.

6. Archivos de configuración:
   - «Archivo» → «Guardar configuración» guarda las áreas y los ajustes en un archivo JSON.
   - «Archivo» → «Cargar configuración» abre un archivo guardado.

7. Idiomas:
   - En el primer inicio se elige el idioma de visualización del dispositivo; si no está disponible, se utiliza el inglés.
   - El menú «Idioma» ofrece inglés, chino simplificado, japonés, coreano, chino tradicional, francés, alemán, español, ruso y portugués de Brasil.

8. Acerca de y actualizaciones:
   - «Acerca de» muestra la página del proyecto e información sobre nuevas versiones.
   - «Ver actualización» abre la página de la versión. «Omitir esta versión» oculta sus notificaciones.

9. Localizar y resaltar áreas:
   - Pase el puntero por un elemento de la lista o selecciónelo para localizar su área con un borde azul.
   - Durante los clics automáticos, el área del clic actual se resalta con un borde verde.
   - «Resaltar el área del clic actual» está activado de forma predeterminada y se guarda con la configuración. Desactívelo para quitar el resaltado verde.
   - Estos resaltados solo se ven cuando «Mostrar áreas» está activado.

10. Tecla de atajo y botón del ratón:
   - «Ajustes» → «Ajustes de teclas»: haz clic en el cuadro de entrada y pulsa la tecla deseada. Las teclas compatibles son F6–F12, Insert, Home, End, PageUp, PageDown y Pause. No mantengas pulsado Ctrl/Alt/Shift/Win. ESC sigue disponible como parada de emergencia durante los clics.
   - Haz clic con el botón derecho en un área de la lista para elegir el botón izquierdo o derecho del ratón. La lista muestra [L] o [R] para cada área.

¡Gracias por utilizar la aplicación!
""" },
                        { "CloseButton", "Cerrar" },
                        { "ProjectAddress", "Página del proyecto" },
                        { "CheckingForUpdates", "Buscando actualizaciones…" },
                        { "UpdateAvailable", "Actualización disponible" },
                        { "UpdateAvailableVersion", "Está disponible la versión {0}." },
                        { "NoUpdateAvailable", "No hay actualizaciones disponibles." },
                        { "UpdateCheckUnavailable", "No se pueden buscar actualizaciones en este momento." },
                        { "ViewUpdate", "Ver actualización" },
                        { "SkipThisVersion", "Omitir esta versión" },
                        { "UpdateSkipped", "Se ha omitido la versión {0}." },
                        { "UpdatePreferencesError", "No se pudo guardar la versión que se desea omitir: {0}" },
                        { "OpenLinkError", "No se pudo abrir el enlace: {0}" },
                        { "HotkeySettings", "Ajustes de teclas" },
                        { "HotkeyLabel", "Tecla de inicio/parada:" },
                        { "HotkeyHint", "Haz clic en el cuadro superior y pulsa una tecla. ESC sigue disponible como parada de emergencia durante los clics." },
                        { "HotkeyApply", "Aplicar" },
                        { "HotkeyInvalidKey", "Tecla o combinación no compatible. Usa F6–F12, Insert, Home, End, PageUp, PageDown o Pause sin Ctrl/Alt/Shift/Win." },
                        { "HotkeyUnavailable", "El atajo global no está disponible: {0}" },
                        { "LeftMouseButton", "Botón izquierdo" },
                        { "RightMouseButton", "Botón derecho" },
                        { "MouseInputError", "La entrada del ratón ha fallado. Los clics automáticos se han detenido.\n{0}" }
                    }
                },
                {
                    "ru-RU", new Dictionary<string, string>
                    {
                        { "File", "Файл" },
                        { "SaveConfig", "Сохранить настройки" },
                        { "LoadConfig", "Загрузить настройки" },
                        { "Language", "Язык" },
                        { "Help", "Справка" },
                        { "ViewHelp", "Руководство" },
                        { "About", "О программе" },
                        { "Add", "Добавить" },
                        { "Remove", "Удалить" },
                        { "MoveUp", "Вверх" },
                        { "MoveDown", "Вниз" },
                        { "IntervalLabel", "Интервал (мс):" },
                        { "JitterLabel", "Разброс (мс):" },
                        { "JitterExceedsIntervalHint", "Если разброс превышает интервал, среднее время ожидания увеличивается." },
                        { "SequenceLabel", "Порядок нажатий:" },
                        { "SequenceLoop", "Повторять по порядку" },
                        { "SequenceRandom", "Повторять случайно" },
                        { "SequenceOnce", "Один раз по порядку" },
                        { "ShowOverlay", "Показать области" },
                        { "HighlightCurrentArea", "Выделять текущую область нажатия" },
                        { "HardwareMode", "Эмуляция оборудования (SendInput)" },
                        { "StartClicking", "Начать нажатия" },
                        { "StopClicking", "Остановить нажатия (ESC)" },
                        { "WindowTitle", "Автокликер мыши" },
                        { "SaveConfigTitle", "Сохранение файла настроек" },
                        { "JsonFilter", "Файлы JSON|*.json" },
                        { "ConfigSavedSuccess", "Настройки сохранены." },
                        { "Success", "Готово" },
                        { "SaveConfigError", "Не удалось сохранить настройки: {0}" },
                        { "Error", "Ошибка" },
                        { "LoadConfigTitle", "Загрузка файла настроек" },
                        { "ConfigLoadedSuccess", "Настройки загружены." },
                        { "LoadConfigError", "Не удалось загрузить настройки: {0}" },
                        { "AboutMessage", "Автокликер мыши v{0}\n" },
                        { "SetLabelPrompt", "Введите название области:" },
                        { "SetLabelTitle", "Название области" },
                        { "AreaLabelDefault", "Область" },
                        { "AddAreaFirst", "Добавьте хотя бы одну область для нажатий." },
                        { "HelpTutorialTitle", "Руководство пользователя" },
                        { "HelpTutorialContent", """
Добро пожаловать в автокликер мыши!

1. Добавление области:
   - Нажмите «Добавить».
   - Когда экран затемнится, удерживайте левую кнопку мыши и перетащите указатель, чтобы выделить прямоугольник.
   - Отпустите кнопку и задайте название области.

2. Управление областями:
   - «Удалить» удаляет выбранную область.
   - «Вверх» и «Вниз» меняют порядок областей в списке и последовательность нажатий.

3. Интервал и разброс:
   - «Интервал (мс)» — базовое время ожидания между нажатиями.
   - «Разброс (мс)» — диапазон случайного отклонения, которое прибавляется к интервалу или вычитается из него.
   - Если сумма отрицательна, используется её абсолютное значение. Итоговое ожидание составляет не менее 10 мс.
   - Если разброс больше интервала, среднее ожидание увеличивается. Например, интервал 1000 и разброс 100 дают ожидание от 900 до 1100 мс.

4. Порядок нажатий:
   - «Повторять по порядку» проходит список сверху вниз и начинает заново.
   - «Повторять случайно» каждый раз выбирает случайную область.
   - «Один раз по порядку» нажимает в каждой области один раз и останавливается.

5. Запуск и остановка:
   - Нажмите «Начать нажатия».
   - Остановить выполнение можно в любой момент кнопкой остановки или клавишей ESC.
   - Режим SendInput отправляет синтетические события через API Windows.

6. Файлы настроек:
   - «Файл» → «Сохранить настройки» сохраняет области и параметры в файл JSON.
   - «Файл» → «Загрузить настройки» открывает сохранённый файл.

7. Язык:
   - При первом запуске выбирается язык интерфейса устройства. Если он не поддерживается, используется английский.
   - В меню «Язык» доступны английский, упрощённый китайский, японский, корейский, традиционный китайский, французский, немецкий, испанский, русский и бразильский португальский.

8. О программе и обновления:
   - Раздел «О программе» содержит ссылку на проект и информацию о новых версиях.
   - «Посмотреть обновление» открывает страницу выпуска. «Пропустить эту версию» скрывает уведомления об этой версии.

9. Поиск и выделение областей:
   - Наведите указатель на элемент списка или выберите его, чтобы обозначить соответствующую область синей рамкой.
   - Во время автоматических нажатий текущая область выделяется зелёной рамкой.
   - Параметр «Выделять текущую область нажатия» включён по умолчанию и сохраняется в файле настроек. Отключите его, чтобы убрать зелёное выделение.
   - Выделение видно только при включённом параметре «Показать области».

10. Горячая клавиша и кнопка мыши:
   - «Настройки» → «Настройки горячих клавиш»: щёлкните поле ввода и нажмите нужную клавишу. Поддерживаются клавиши F6–F12, Insert, Home, End, PageUp, PageDown и Pause. Не удерживайте Ctrl/Alt/Shift/Win. Во время нажатий ESC остаётся аварийной остановкой.
   - Щёлкните правой кнопкой мыши по области в списке, чтобы выбрать левую или правую кнопку мыши. В списке отображается [L] или [R] для каждой области.

Спасибо за использование программы!
""" },
                        { "CloseButton", "Закрыть" },
                        { "ProjectAddress", "Страница проекта" },
                        { "CheckingForUpdates", "Проверка обновлений…" },
                        { "UpdateAvailable", "Доступно обновление" },
                        { "UpdateAvailableVersion", "Доступна версия {0}." },
                        { "NoUpdateAvailable", "Доступных обновлений нет." },
                        { "UpdateCheckUnavailable", "Сейчас не удалось проверить обновления." },
                        { "ViewUpdate", "Посмотреть обновление" },
                        { "SkipThisVersion", "Пропустить эту версию" },
                        { "UpdateSkipped", "Версия {0} пропущена." },
                        { "UpdatePreferencesError", "Не удалось сохранить настройку пропуска версии: {0}" },
                        { "OpenLinkError", "Не удалось открыть ссылку: {0}" },
                        { "HotkeySettings", "Настройки горячих клавиш" },
                        { "HotkeyLabel", "Клавиша запуска/остановки:" },
                        { "HotkeyHint", "Щёлкните поле выше и нажмите клавишу. Во время нажатий ESC остаётся аварийной остановкой." },
                        { "HotkeyApply", "Применить" },
                        { "HotkeyInvalidKey", "Неподдерживаемая клавиша или сочетание. Используйте F6–F12, Insert, Home, End, PageUp, PageDown или Pause без Ctrl/Alt/Shift/Win." },
                        { "HotkeyUnavailable", "Глобальная горячая клавиша недоступна: {0}" },
                        { "LeftMouseButton", "Левая кнопка мыши" },
                        { "RightMouseButton", "Правая кнопка мыши" },
                        { "MouseInputError", "Ошибка ввода мыши. Автоматические нажатия остановлены.\n{0}" }
                    }
                },
                {
                    "pt-BR", new Dictionary<string, string>
                    {
                        { "File", "Arquivo" },
                        { "SaveConfig", "Salvar configuração" },
                        { "LoadConfig", "Carregar configuração" },
                        { "Language", "Idioma" },
                        { "Help", "Ajuda" },
                        { "ViewHelp", "Ver guia" },
                        { "About", "Sobre" },
                        { "Add", "Adicionar" },
                        { "Remove", "Remover" },
                        { "MoveUp", "Mover para cima" },
                        { "MoveDown", "Mover para baixo" },
                        { "IntervalLabel", "Intervalo (ms):" },
                        { "JitterLabel", "Variação (ms):" },
                        { "JitterExceedsIntervalHint", "Quando a variação supera o intervalo, o tempo médio de espera aumenta." },
                        { "SequenceLabel", "Ordem dos cliques:" },
                        { "SequenceLoop", "Repetir em ordem" },
                        { "SequenceRandom", "Repetir aleatoriamente" },
                        { "SequenceOnce", "Uma vez em ordem" },
                        { "ShowOverlay", "Mostrar áreas" },
                        { "HighlightCurrentArea", "Destacar a área do clique atual" },
                        { "HardwareMode", "Simulação de hardware (SendInput)" },
                        { "StartClicking", "Iniciar cliques" },
                        { "StopClicking", "Parar cliques (ESC)" },
                        { "WindowTitle", "Clicador automático" },
                        { "SaveConfigTitle", "Salvar arquivo de configuração" },
                        { "JsonFilter", "Arquivos JSON|*.json" },
                        { "ConfigSavedSuccess", "A configuração foi salva." },
                        { "Success", "Concluído" },
                        { "SaveConfigError", "Não foi possível salvar a configuração: {0}" },
                        { "Error", "Erro" },
                        { "LoadConfigTitle", "Carregar arquivo de configuração" },
                        { "ConfigLoadedSuccess", "A configuração foi carregada." },
                        { "LoadConfigError", "Não foi possível carregar a configuração: {0}" },
                        { "AboutMessage", "Clicador automático v{0}\n" },
                        { "SetLabelPrompt", "Digite um nome para esta área:" },
                        { "SetLabelTitle", "Nome da área" },
                        { "AreaLabelDefault", "Área" },
                        { "AddAreaFirst", "Adicione pelo menos uma área de clique." },
                        { "HelpTutorialTitle", "Guia de uso" },
                        { "HelpTutorialContent", """
Bem-vindo ao Clicador automático!

1. Adicionar uma área de clique:
   - Clique em “Adicionar”.
   - Quando a tela escurecer, mantenha o botão esquerdo do mouse pressionado e arraste para selecionar um retângulo.
   - Solte o botão e dê um nome à área.

2. Gerenciar as áreas:
   - “Remover” exclui a área selecionada.
   - “Mover para cima” e “Mover para baixo” alteram a ordem da lista e dos cliques sequenciais.

3. Definir o intervalo e a variação:
   - “Intervalo (ms)” é a espera básica entre dois cliques.
   - “Variação (ms)” define a faixa do valor aleatório somado ao intervalo ou subtraído dele.
   - Se a soma for negativa, seu valor absoluto será usado. A espera final será de pelo menos 10 ms.
   - Uma variação maior que o intervalo aumenta a espera média. Por exemplo, intervalo 1000 e variação 100 produzem esperas de 900 a 1100 ms.

4. Escolher a ordem:
   - “Repetir em ordem” percorre a lista de cima para baixo e recomeça.
   - “Repetir aleatoriamente” escolhe uma área aleatória a cada clique.
   - “Uma vez em ordem” clica uma vez em cada área e para.

5. Iniciar e parar:
   - Clique em “Iniciar cliques”.
   - Use o botão de parada ou a tecla ESC para interromper a execução a qualquer momento.
   - A opção SendInput envia eventos sintéticos pela API do Windows.

6. Arquivos de configuração:
   - “Arquivo” → “Salvar configuração” salva as áreas e os ajustes em um arquivo JSON.
   - “Arquivo” → “Carregar configuração” abre um arquivo salvo.

7. Idiomas:
   - Na primeira execução, o aplicativo escolhe o idioma de exibição do dispositivo. Se não houver suporte, usa o inglês.
   - O menu “Idioma” oferece inglês, chinês simplificado, japonês, coreano, chinês tradicional, francês, alemão, espanhol, russo e português do Brasil.

8. Sobre e atualizações:
   - “Sobre” mostra a página do projeto e informações sobre novas versões.
   - “Ver atualização” abre a página da versão. “Ignorar esta versão” oculta as notificações dessa versão.

9. Localizar e destacar áreas:
   - Passe o ponteiro sobre um item da lista ou selecione-o para localizar sua área com uma borda azul.
   - Durante os cliques automáticos, a área do clique atual é destacada com uma borda verde.
   - “Destacar a área do clique atual” fica ativado por padrão e é salvo com a configuração. Desative a opção para remover o destaque verde.
   - Esses destaques só aparecem quando “Mostrar áreas” está ativado.

10. Atalho e botão do mouse:
   - “Configurações” → “Configurações de atalhos”: clique na caixa de entrada e pressione a tecla desejada. As teclas compatíveis são F6–F12, Insert, Home, End, PageUp, PageDown e Pause. Não mantenha Ctrl/Alt/Shift/Win pressionado. ESC continua disponível como parada de emergência durante os cliques.
   - Clique com o botão direito em uma área da lista para escolher o botão esquerdo ou direito do mouse. A lista mostra [L] ou [R] para cada área.

Obrigado por usar o aplicativo!
""" },
                        { "CloseButton", "Fechar" },
                        { "ProjectAddress", "Página do projeto" },
                        { "CheckingForUpdates", "Verificando atualizações…" },
                        { "UpdateAvailable", "Atualização disponível" },
                        { "UpdateAvailableVersion", "A versão {0} está disponível." },
                        { "NoUpdateAvailable", "Nenhuma atualização disponível." },
                        { "UpdateCheckUnavailable", "Não foi possível verificar atualizações agora." },
                        { "ViewUpdate", "Ver atualização" },
                        { "SkipThisVersion", "Ignorar esta versão" },
                        { "UpdateSkipped", "A versão {0} foi ignorada." },
                        { "UpdatePreferencesError", "Não foi possível salvar a preferência de ignorar a versão: {0}" },
                        { "OpenLinkError", "Não foi possível abrir o link: {0}" },
                        { "HotkeySettings", "Configurações de atalhos" },
                        { "HotkeyLabel", "Atalho iniciar/parar:" },
                        { "HotkeyHint", "Clique na caixa acima e pressione uma tecla. ESC continua disponível como parada de emergência durante os cliques." },
                        { "HotkeyApply", "Aplicar" },
                        { "HotkeyInvalidKey", "Tecla ou combinação não suportada. Use F6–F12, Insert, Home, End, PageUp, PageDown ou Pause sem Ctrl/Alt/Shift/Win." },
                        { "HotkeyUnavailable", "O atalho global não está disponível: {0}" },
                        { "LeftMouseButton", "Botão esquerdo do mouse" },
                        { "RightMouseButton", "Botão direito do mouse" },
                        { "MouseInputError", "Falha na entrada do mouse. Os cliques automáticos foram interrompidos.\n{0}" }
                    }
                }
            };

        static Localization()
        {
            Translations["en-US"]["ConditionEdit"] = "Edit condition";
            Translations["en-US"]["ConditionEnabled"] = "Enable image condition";
            Translations["en-US"]["ConditionRegion"] = "Select detection region";
            Translations["en-US"]["ConditionCapture"] = "Capture template";
            Translations["en-US"]["ConditionImport"] = "Import image";
            Translations["en-US"]["ConditionThreshold"] = "Match threshold";
            Translations["en-US"]["ConditionPoll"] = "Detection interval (ms)";
            Translations["en-US"]["ConditionOnce"] = "Once per appearance";
            Translations["en-US"]["ConditionWhile"] = "Repeat while visible";
            Translations["en-US"]["ConditionMode"] = "Trigger mode";
            Translations["en-US"]["ConditionTimeout"] = "Timeout (s; 0 = unlimited)";
            Translations["en-US"]["ConditionHint"] = "Visible desktop only; do not cover the target. Scores are not probabilities. Automatic sizing searches a bounded set of proportional sizes; disable it for original-size matching. Timeout stops the task.";
            Translations["en-US"]["ConditionTest"] = "Test recognition";
            Translations["en-US"]["ConditionSave"] = "Save";
            Translations["en-US"]["ConditionCancel"] = "Cancel";
            Translations["en-US"]["ConditionInvalid"] = "Invalid image or condition.";
            Translations["en-US"]["ConditionMatched"] = "Matched";
            Translations["en-US"]["ConditionWaiting"] = "Waiting for image";
            Translations["en-US"]["ConditionStopped"] = "Stopping…";
            Translations["zh-CN"]["ConditionEdit"] = "编辑条件";
            Translations["zh-CN"]["ConditionEnabled"] = "启用画面条件";
            Translations["zh-CN"]["ConditionRegion"] = "框选检测区域";
            Translations["zh-CN"]["ConditionCapture"] = "截取模板";
            Translations["zh-CN"]["ConditionImport"] = "导入图片";
            Translations["zh-CN"]["ConditionThreshold"] = "匹配阈值";
            Translations["zh-CN"]["ConditionPoll"] = "检测间隔（毫秒）";
            Translations["zh-CN"]["ConditionOnce"] = "每次出现点击一次";
            Translations["zh-CN"]["ConditionWhile"] = "存在期间持续点击";
            Translations["zh-CN"]["ConditionMode"] = "触发方式";
            Translations["zh-CN"]["ConditionTimeout"] = "超时（秒；0 为无限）";
            Translations["zh-CN"]["ConditionHint"] = "仅识别可见桌面，请勿遮挡目标。匹配分数不是概率。自动适应在有限范围内尝试等比例尺寸；原尺寸模式可关闭该选项。超时会停止任务。";
            Translations["zh-CN"]["ConditionTest"] = "测试识别";
            Translations["zh-CN"]["ConditionSave"] = "保存";
            Translations["zh-CN"]["ConditionCancel"] = "取消";
            Translations["zh-CN"]["ConditionInvalid"] = "图片或条件无效。";
            Translations["zh-CN"]["ConditionMatched"] = "已匹配";
            Translations["zh-CN"]["ConditionWaiting"] = "等待画面";
            Translations["zh-CN"]["ConditionStopped"] = "正在停止…";
            Translations["zh-TW"]["ConditionEdit"] = "編輯條件";
            Translations["zh-TW"]["ConditionEnabled"] = "啟用畫面條件";
            Translations["zh-TW"]["ConditionRegion"] = "框選偵測區域";
            Translations["zh-TW"]["ConditionCapture"] = "擷取範本";
            Translations["zh-TW"]["ConditionImport"] = "匯入圖片";
            Translations["zh-TW"]["ConditionThreshold"] = "比對閾值";
            Translations["zh-TW"]["ConditionPoll"] = "偵測間隔（毫秒）";
            Translations["zh-TW"]["ConditionOnce"] = "每次出現點擊一次";
            Translations["zh-TW"]["ConditionWhile"] = "存在期間持續點擊";
            Translations["zh-TW"]["ConditionMode"] = "觸發方式";
            Translations["zh-TW"]["ConditionTimeout"] = "逾時（秒；0 為無限）";
            Translations["zh-TW"]["ConditionHint"] = "僅辨識可見桌面，請勿遮擋目標。分數不是機率。自動適應在有限範圍內嘗試等比例尺寸；可關閉以使用原尺寸。逾時會停止工作。";
            Translations["zh-TW"]["ConditionTest"] = "測試辨識";
            Translations["zh-TW"]["ConditionSave"] = "儲存";
            Translations["zh-TW"]["ConditionCancel"] = "取消";
            Translations["zh-TW"]["ConditionInvalid"] = "圖片或條件無效。";
            Translations["zh-TW"]["ConditionMatched"] = "已比對";
            Translations["zh-TW"]["ConditionWaiting"] = "等待畫面";
            Translations["zh-TW"]["ConditionStopped"] = "正在停止…";
            Translations["ja-JP"]["ConditionEdit"] = "条件を編集";
            Translations["ja-JP"]["ConditionEnabled"] = "画像条件を有効化";
            Translations["ja-JP"]["ConditionRegion"] = "検出範囲を選択";
            Translations["ja-JP"]["ConditionCapture"] = "テンプレートを撮影";
            Translations["ja-JP"]["ConditionImport"] = "画像を読み込み";
            Translations["ja-JP"]["ConditionThreshold"] = "一致しきい値";
            Translations["ja-JP"]["ConditionPoll"] = "検出間隔（ミリ秒）";
            Translations["ja-JP"]["ConditionOnce"] = "出現ごとに一度";
            Translations["ja-JP"]["ConditionWhile"] = "表示中に繰り返す";
            Translations["ja-JP"]["ConditionMode"] = "動作モード";
            Translations["ja-JP"]["ConditionTimeout"] = "タイムアウト（秒、0は無制限）";
            Translations["ja-JP"]["ConditionHint"] = "可視デスクトップのみを認識します。対象を隠さないでください。スコアは確率ではありません。自動対応は有限範囲の等比サイズを検索します。無効にすると元サイズ照合です。タイムアウトで停止します。";
            Translations["ja-JP"]["ConditionTest"] = "認識をテスト";
            Translations["ja-JP"]["ConditionSave"] = "保存";
            Translations["ja-JP"]["ConditionCancel"] = "キャンセル";
            Translations["ja-JP"]["ConditionInvalid"] = "画像または条件が無効です。";
            Translations["ja-JP"]["ConditionMatched"] = "一致";
            Translations["ja-JP"]["ConditionWaiting"] = "画像を待機";
            Translations["ja-JP"]["ConditionStopped"] = "停止中…";
            Translations["ko-KR"]["ConditionEdit"] = "조건 편집";
            Translations["ko-KR"]["ConditionEnabled"] = "이미지 조건 사용";
            Translations["ko-KR"]["ConditionRegion"] = "감지 영역 선택";
            Translations["ko-KR"]["ConditionCapture"] = "템플릿 캡처";
            Translations["ko-KR"]["ConditionImport"] = "이미지 가져오기";
            Translations["ko-KR"]["ConditionThreshold"] = "일치 임계값";
            Translations["ko-KR"]["ConditionPoll"] = "감지 간격 (ms)";
            Translations["ko-KR"]["ConditionOnce"] = "등장할 때 한 번";
            Translations["ko-KR"]["ConditionWhile"] = "표시 중 반복";
            Translations["ko-KR"]["ConditionMode"] = "실행 방식";
            Translations["ko-KR"]["ConditionTimeout"] = "시간 제한 (초, 0은 무제한)";
            Translations["ko-KR"]["ConditionHint"] = "보이는 화면만 인식하므로 대상을 가리지 마세요. 점수는 확률이 아닙니다. 자동 조정은 제한된 비례 크기를 검색하며 끄면 원본 크기로 비교합니다. 시간 초과 시 작업이 중지됩니다.";
            Translations["ko-KR"]["ConditionTest"] = "인식 테스트";
            Translations["ko-KR"]["ConditionSave"] = "저장";
            Translations["ko-KR"]["ConditionCancel"] = "취소";
            Translations["ko-KR"]["ConditionInvalid"] = "잘못된 이미지 또는 조건입니다.";
            Translations["ko-KR"]["ConditionMatched"] = "일치";
            Translations["ko-KR"]["ConditionWaiting"] = "이미지 대기";
            Translations["ko-KR"]["ConditionStopped"] = "중지 중…";
            Translations["fr-FR"]["ConditionEdit"] = "Modifier la condition";
            Translations["fr-FR"]["ConditionEnabled"] = "Activer la condition visuelle";
            Translations["fr-FR"]["ConditionRegion"] = "Choisir la zone de détection";
            Translations["fr-FR"]["ConditionCapture"] = "Capturer le modèle";
            Translations["fr-FR"]["ConditionImport"] = "Importer une image";
            Translations["fr-FR"]["ConditionThreshold"] = "Seuil de correspondance";
            Translations["fr-FR"]["ConditionPoll"] = "Intervalle de détection (ms)";
            Translations["fr-FR"]["ConditionOnce"] = "Une fois par apparition";
            Translations["fr-FR"]["ConditionWhile"] = "Répéter tant que visible";
            Translations["fr-FR"]["ConditionMode"] = "Déclenchement";
            Translations["fr-FR"]["ConditionTimeout"] = "Délai (s ; 0 = illimité)";
            Translations["fr-FR"]["ConditionHint"] = "Bureau visible uniquement : ne masquez pas la cible. Le score n’est pas une probabilité. L’adaptation cherche des tailles proportionnelles dans une plage limitée ; désactivez-la pour la taille d’origine. Le délai dépassé arrête la tâche.";
            Translations["fr-FR"]["ConditionTest"] = "Tester la détection";
            Translations["fr-FR"]["ConditionSave"] = "Enregistrer";
            Translations["fr-FR"]["ConditionCancel"] = "Annuler";
            Translations["fr-FR"]["ConditionInvalid"] = "Image ou condition invalide.";
            Translations["fr-FR"]["ConditionMatched"] = "Correspondance";
            Translations["fr-FR"]["ConditionWaiting"] = "En attente de l’image";
            Translations["fr-FR"]["ConditionStopped"] = "Arrêt en cours…";
            Translations["de-DE"]["ConditionEdit"] = "Bedingung bearbeiten";
            Translations["de-DE"]["ConditionEnabled"] = "Bildbedingung aktivieren";
            Translations["de-DE"]["ConditionRegion"] = "Suchbereich wählen";
            Translations["de-DE"]["ConditionCapture"] = "Vorlage aufnehmen";
            Translations["de-DE"]["ConditionImport"] = "Bild importieren";
            Translations["de-DE"]["ConditionThreshold"] = "Übereinstimmungsschwelle";
            Translations["de-DE"]["ConditionPoll"] = "Prüfintervall (ms)";
            Translations["de-DE"]["ConditionOnce"] = "Einmal pro Erscheinen";
            Translations["de-DE"]["ConditionWhile"] = "Solange sichtbar wiederholen";
            Translations["de-DE"]["ConditionMode"] = "Auslösung";
            Translations["de-DE"]["ConditionTimeout"] = "Zeitlimit (s; 0 = unbegrenzt)";
            Translations["de-DE"]["ConditionHint"] = "Nur sichtbarer Desktop; Ziel nicht verdecken. Werte sind keine Wahrscheinlichkeiten. Automatik prüft proportionale Größen in begrenztem Bereich; abschalten für Originalgröße. Zeitüberschreitung stoppt die Aufgabe.";
            Translations["de-DE"]["ConditionTest"] = "Erkennung testen";
            Translations["de-DE"]["ConditionSave"] = "Speichern";
            Translations["de-DE"]["ConditionCancel"] = "Abbrechen";
            Translations["de-DE"]["ConditionInvalid"] = "Ungültiges Bild oder Bedingung.";
            Translations["de-DE"]["ConditionMatched"] = "Gefunden";
            Translations["de-DE"]["ConditionWaiting"] = "Warte auf Bild";
            Translations["de-DE"]["ConditionStopped"] = "Wird angehalten…";
            Translations["es-ES"]["ConditionEdit"] = "Editar condición";
            Translations["es-ES"]["ConditionEnabled"] = "Activar condición visual";
            Translations["es-ES"]["ConditionRegion"] = "Seleccionar zona de detección";
            Translations["es-ES"]["ConditionCapture"] = "Capturar plantilla";
            Translations["es-ES"]["ConditionImport"] = "Importar imagen";
            Translations["es-ES"]["ConditionThreshold"] = "Umbral de coincidencia";
            Translations["es-ES"]["ConditionPoll"] = "Intervalo de detección (ms)";
            Translations["es-ES"]["ConditionOnce"] = "Una vez por aparición";
            Translations["es-ES"]["ConditionWhile"] = "Repetir mientras sea visible";
            Translations["es-ES"]["ConditionMode"] = "Activación";
            Translations["es-ES"]["ConditionTimeout"] = "Tiempo límite (s; 0 = ilimitado)";
            Translations["es-ES"]["ConditionHint"] = "Solo escritorio visible; no tape el objetivo. La puntuación no es una probabilidad. La adaptación busca tamaños proporcionales en un rango limitado; desactívela para tamaño original. El tiempo agotado detiene la tarea.";
            Translations["es-ES"]["ConditionTest"] = "Probar detección";
            Translations["es-ES"]["ConditionSave"] = "Guardar";
            Translations["es-ES"]["ConditionCancel"] = "Cancelar";
            Translations["es-ES"]["ConditionInvalid"] = "Imagen o condición no válida.";
            Translations["es-ES"]["ConditionMatched"] = "Coincidencia";
            Translations["es-ES"]["ConditionWaiting"] = "Esperando imagen";
            Translations["es-ES"]["ConditionStopped"] = "Deteniendo…";
            Translations["ru-RU"]["ConditionEdit"] = "Изменить условие";
            Translations["ru-RU"]["ConditionEnabled"] = "Включить условие по изображению";
            Translations["ru-RU"]["ConditionRegion"] = "Выбрать область поиска";
            Translations["ru-RU"]["ConditionCapture"] = "Снять шаблон";
            Translations["ru-RU"]["ConditionImport"] = "Загрузить изображение";
            Translations["ru-RU"]["ConditionThreshold"] = "Порог совпадения";
            Translations["ru-RU"]["ConditionPoll"] = "Интервал проверки (мс)";
            Translations["ru-RU"]["ConditionOnce"] = "Один раз при появлении";
            Translations["ru-RU"]["ConditionWhile"] = "Повторять пока видно";
            Translations["ru-RU"]["ConditionMode"] = "Режим срабатывания";
            Translations["ru-RU"]["ConditionTimeout"] = "Ожидание (с; 0 = без ограничения)";
            Translations["ru-RU"]["ConditionHint"] = "Только видимый рабочий стол; не закрывайте цель. Оценка не является вероятностью. Автоподбор проверяет пропорциональные размеры в ограниченном диапазоне; отключите его для исходного размера. Тайм-аут останавливает задачу.";
            Translations["ru-RU"]["ConditionTest"] = "Проверить распознавание";
            Translations["ru-RU"]["ConditionSave"] = "Сохранить";
            Translations["ru-RU"]["ConditionCancel"] = "Отмена";
            Translations["ru-RU"]["ConditionInvalid"] = "Недопустимое изображение или условие.";
            Translations["ru-RU"]["ConditionMatched"] = "Совпадение";
            Translations["ru-RU"]["ConditionWaiting"] = "Ожидание изображения";
            Translations["ru-RU"]["ConditionStopped"] = "Остановка…";
            Translations["pt-BR"]["ConditionEdit"] = "Editar condição";
            Translations["pt-BR"]["ConditionEnabled"] = "Ativar condição visual";
            Translations["pt-BR"]["ConditionRegion"] = "Selecionar área de detecção";
            Translations["pt-BR"]["ConditionCapture"] = "Capturar modelo";
            Translations["pt-BR"]["ConditionImport"] = "Importar imagem";
            Translations["pt-BR"]["ConditionThreshold"] = "Limiar de correspondência";
            Translations["pt-BR"]["ConditionPoll"] = "Intervalo de detecção (ms)";
            Translations["pt-BR"]["ConditionOnce"] = "Uma vez por aparição";
            Translations["pt-BR"]["ConditionWhile"] = "Repetir enquanto visível";
            Translations["pt-BR"]["ConditionMode"] = "Acionamento";
            Translations["pt-BR"]["ConditionTimeout"] = "Tempo limite (s; 0 = ilimitado)";
            Translations["pt-BR"]["ConditionHint"] = "Só a área de trabalho visível; não cubra o alvo. A pontuação não é probabilidade. A adaptação testa tamanhos proporcionais em faixa limitada; desative para tamanho original. O tempo esgotado encerra a tarefa.";
            Translations["pt-BR"]["ConditionTest"] = "Testar reconhecimento";
            Translations["pt-BR"]["ConditionSave"] = "Salvar";
            Translations["pt-BR"]["ConditionCancel"] = "Cancelar";
            Translations["pt-BR"]["ConditionInvalid"] = "Imagem ou condição inválida.";
            Translations["pt-BR"]["ConditionMatched"] = "Correspondência";
            Translations["pt-BR"]["ConditionWaiting"] = "Aguardando imagem";
            Translations["pt-BR"]["ConditionStopped"] = "Parando…";
            Translations["en-US"]["ConditionErrorDetectionArea"] = "Invalid detection region (maximum 32 megapixels).";
            Translations["en-US"]["ConditionErrorSettings"] = "Invalid image condition settings.";
            Translations["en-US"]["ConditionErrorDecode"] = "The template PNG could not be decoded.";
            Translations["en-US"]["ConditionErrorEncoding"] = "Invalid template image encoding.";
            Translations["en-US"]["ConditionErrorPng"] = "The template must be a PNG image.";
            Translations["en-US"]["ConditionErrorDimensions"] = "Invalid template dimensions: each side must be at least 2 pixels, with at most 32M pixels in total.";
            Translations["en-US"]["ConditionErrorCaptureArea"] = "Invalid screen capture region.";
            Translations["en-US"]["ConditionErrorOutsideDisplays"] = "The detection region extends beyond the available displays.";
            Translations["en-US"]["ConditionErrorLowDetail"] = "The template has too little visual detail. Select a distinctive icon or text.";
            Translations["en-US"]["ConditionErrorTemplateFit"] = "The original-size template exceeds the detection image. Enable automatic sizing, crop the template, or enlarge detection.";
            Translations["en-US"]["ConditionErrorScore"] = "Image matching returned invalid scores.";
            Translations["en-US"]["ConditionErrorTemplateEmpty"] = "Capture or import a template image first.";
            Translations["en-US"]["ConditionErrorTemplateSize"] = "Original templates are limited to 32 MiB and 32M pixels.";
            Translations["en-US"]["ConditionErrorTimeout"] = "Timed out waiting for the image condition.";
            Translations["en-US"]["ConditionErrorRecognition"] = "Image recognition failed.";
            Translations["zh-CN"]["ConditionErrorDetectionArea"] = "检测区域无效，最多支持 3200 万像素。";
            Translations["zh-CN"]["ConditionErrorSettings"] = "画面条件设置无效。";
            Translations["zh-CN"]["ConditionErrorDecode"] = "无法解码模板 PNG 图片。";
            Translations["zh-CN"]["ConditionErrorEncoding"] = "模板图片编码无效。";
            Translations["zh-CN"]["ConditionErrorPng"] = "模板必须为 PNG 图片。";
            Translations["zh-CN"]["ConditionErrorDimensions"] = "模板尺寸无效：每边至少 2 像素，总计最多 32M 像素。";
            Translations["zh-CN"]["ConditionErrorCaptureArea"] = "截图区域无效。";
            Translations["zh-CN"]["ConditionErrorOutsideDisplays"] = "检测区域超出可用显示器范围。";
            Translations["zh-CN"]["ConditionErrorLowDetail"] = "模板画面特征不足，请选择有明显图案或文字的区域。";
            Translations["zh-CN"]["ConditionErrorTemplateFit"] = "原尺寸模式下模板大于检测画面。请开启自动适应目标大小、裁剪模板或扩大检测区域。";
            Translations["zh-CN"]["ConditionErrorScore"] = "图像匹配返回了无效分数。";
            Translations["zh-CN"]["ConditionErrorTemplateEmpty"] = "请先截取或导入模板图片。";
            Translations["zh-CN"]["ConditionErrorTemplateSize"] = "原始模板上限为 32 MiB、32M 像素。";
            Translations["zh-CN"]["ConditionErrorTimeout"] = "等待画面条件超时。";
            Translations["zh-CN"]["ConditionErrorRecognition"] = "画面识别失败。";
            Translations["zh-TW"]["ConditionErrorDetectionArea"] = "偵測區域無效，最多支援 3200 萬像素。";
            Translations["zh-TW"]["ConditionErrorSettings"] = "畫面條件設定無效。";
            Translations["zh-TW"]["ConditionErrorDecode"] = "無法解碼範本 PNG 圖片。";
            Translations["zh-TW"]["ConditionErrorEncoding"] = "範本圖片編碼無效。";
            Translations["zh-TW"]["ConditionErrorPng"] = "範本必須為 PNG 圖片。";
            Translations["zh-TW"]["ConditionErrorDimensions"] = "範本尺寸無效：每邊至少 2 像素，總計最多 32M 像素。";
            Translations["zh-TW"]["ConditionErrorCaptureArea"] = "擷取區域無效。";
            Translations["zh-TW"]["ConditionErrorOutsideDisplays"] = "偵測區域超出可用顯示器範圍。";
            Translations["zh-TW"]["ConditionErrorLowDetail"] = "範本畫面特徵不足，請選擇有明顯圖案或文字的區域。";
            Translations["zh-TW"]["ConditionErrorTemplateFit"] = "原尺寸模式下範本大於偵測畫面。請啟用自動適應目標大小、裁切範本或擴大偵測區域。";
            Translations["zh-TW"]["ConditionErrorScore"] = "圖像比對傳回無效分數。";
            Translations["zh-TW"]["ConditionErrorTemplateEmpty"] = "請先擷取或匯入範本圖片。";
            Translations["zh-TW"]["ConditionErrorTemplateSize"] = "原始範本上限為 32 MiB、32M 像素。";
            Translations["zh-TW"]["ConditionErrorTimeout"] = "等待畫面條件逾時。";
            Translations["zh-TW"]["ConditionErrorRecognition"] = "畫面辨識失敗。";
            Translations["ja-JP"]["ConditionErrorDetectionArea"] = "検出範囲が無効です。最大3200万画素です。";
            Translations["ja-JP"]["ConditionErrorSettings"] = "画像条件の設定が無効です。";
            Translations["ja-JP"]["ConditionErrorDecode"] = "テンプレートのPNG画像をデコードできません。";
            Translations["ja-JP"]["ConditionErrorEncoding"] = "テンプレート画像のエンコードが無効です。";
            Translations["ja-JP"]["ConditionErrorPng"] = "テンプレートはPNG画像にしてください。";
            Translations["ja-JP"]["ConditionErrorDimensions"] = "無効な画像サイズです。各辺2画素以上、合計32M画素以下にしてください。";
            Translations["ja-JP"]["ConditionErrorCaptureArea"] = "画面キャプチャ範囲が無効です。";
            Translations["ja-JP"]["ConditionErrorOutsideDisplays"] = "検出範囲がディスプレイの範囲外です。";
            Translations["ja-JP"]["ConditionErrorLowDetail"] = "画像の特徴が不足しています。はっきりした図柄や文字を選択してください。";
            Translations["ja-JP"]["ConditionErrorTemplateFit"] = "元サイズの画像が検出画像より大きいため、自動対応を有効にするか、切り抜くか、検出領域を広げてください。";
            Translations["ja-JP"]["ConditionErrorScore"] = "画像照合で無効なスコアが返されました。";
            Translations["ja-JP"]["ConditionErrorTemplateEmpty"] = "先にテンプレート画像を撮影または読み込んでください。";
            Translations["ja-JP"]["ConditionErrorTemplateSize"] = "元画像の上限は32 MiB、32M画素です。";
            Translations["ja-JP"]["ConditionErrorTimeout"] = "画像条件の待機がタイムアウトしました。";
            Translations["ja-JP"]["ConditionErrorRecognition"] = "画像認識に失敗しました。";
            Translations["ko-KR"]["ConditionErrorDetectionArea"] = "감지 영역이 잘못되었습니다. 최대 3200만 픽셀입니다.";
            Translations["ko-KR"]["ConditionErrorSettings"] = "이미지 조건 설정이 잘못되었습니다.";
            Translations["ko-KR"]["ConditionErrorDecode"] = "템플릿 PNG 이미지를 디코딩할 수 없습니다.";
            Translations["ko-KR"]["ConditionErrorEncoding"] = "템플릿 이미지 인코딩이 잘못되었습니다.";
            Translations["ko-KR"]["ConditionErrorPng"] = "템플릿은 PNG 이미지여야 합니다.";
            Translations["ko-KR"]["ConditionErrorDimensions"] = "잘못된 템플릿 크기입니다. 각 변은 2픽셀 이상, 전체는 32M픽셀 이하여야 합니다.";
            Translations["ko-KR"]["ConditionErrorCaptureArea"] = "캡처 영역이 잘못되었습니다.";
            Translations["ko-KR"]["ConditionErrorOutsideDisplays"] = "감지 영역이 사용 가능한 화면을 벗어났습니다.";
            Translations["ko-KR"]["ConditionErrorLowDetail"] = "템플릿 특징이 부족합니다. 뚜렷한 그림이나 글자를 선택하세요.";
            Translations["ko-KR"]["ConditionErrorTemplateFit"] = "원본 크기 템플릿이 감지 이미지보다 큽니다. 자동 조정을 켜거나 자르거나 감지 영역을 넓히세요.";
            Translations["ko-KR"]["ConditionErrorScore"] = "이미지 일치 점수가 잘못되었습니다.";
            Translations["ko-KR"]["ConditionErrorTemplateEmpty"] = "먼저 템플릿 이미지를 캡처하거나 가져오세요.";
            Translations["ko-KR"]["ConditionErrorTemplateSize"] = "원본 템플릿 한도는 32 MiB, 32M픽셀입니다.";
            Translations["ko-KR"]["ConditionErrorTimeout"] = "이미지 조건 대기 시간이 초과되었습니다.";
            Translations["ko-KR"]["ConditionErrorRecognition"] = "이미지 인식에 실패했습니다.";
            Translations["fr-FR"]["ConditionErrorDetectionArea"] = "Zone de détection invalide (32 mégapixels maximum).";
            Translations["fr-FR"]["ConditionErrorSettings"] = "Paramètres de condition visuelle invalides.";
            Translations["fr-FR"]["ConditionErrorDecode"] = "Impossible de décoder le modèle PNG.";
            Translations["fr-FR"]["ConditionErrorEncoding"] = "Encodage du modèle invalide.";
            Translations["fr-FR"]["ConditionErrorPng"] = "Le modèle doit être une image PNG.";
            Translations["fr-FR"]["ConditionErrorDimensions"] = "Dimensions invalides : au moins 2 pixels par côté et au plus 32M pixels au total.";
            Translations["fr-FR"]["ConditionErrorCaptureArea"] = "Zone de capture invalide.";
            Translations["fr-FR"]["ConditionErrorOutsideDisplays"] = "La zone de détection dépasse les écrans disponibles.";
            Translations["fr-FR"]["ConditionErrorLowDetail"] = "Le modèle manque de détails. Sélectionnez une icône ou un texte distinctif.";
            Translations["fr-FR"]["ConditionErrorTemplateFit"] = "Le modèle à sa taille d’origine dépasse la zone. Activez l’adaptation, recadrez ou agrandissez la zone.";
            Translations["fr-FR"]["ConditionErrorScore"] = "La détection a renvoyé des scores invalides.";
            Translations["fr-FR"]["ConditionErrorTemplateEmpty"] = "Capturez ou importez d’abord une image modèle.";
            Translations["fr-FR"]["ConditionErrorTemplateSize"] = "Les modèles originaux sont limités à 32 MiB et 32M pixels.";
            Translations["fr-FR"]["ConditionErrorTimeout"] = "Le délai d’attente de la condition visuelle est écoulé.";
            Translations["fr-FR"]["ConditionErrorRecognition"] = "La reconnaissance visuelle a échoué.";
            Translations["de-DE"]["ConditionErrorDetectionArea"] = "Ungültiger Suchbereich (maximal 32 Megapixel).";
            Translations["de-DE"]["ConditionErrorSettings"] = "Ungültige Einstellungen der Bildbedingung.";
            Translations["de-DE"]["ConditionErrorDecode"] = "Die PNG-Vorlage konnte nicht dekodiert werden.";
            Translations["de-DE"]["ConditionErrorEncoding"] = "Ungültige Bildkodierung der Vorlage.";
            Translations["de-DE"]["ConditionErrorPng"] = "Die Vorlage muss ein PNG-Bild sein.";
            Translations["de-DE"]["ConditionErrorDimensions"] = "Ungültige Vorlagengröße: mindestens 2 Pixel je Seite und insgesamt höchstens 32M Pixel.";
            Translations["de-DE"]["ConditionErrorCaptureArea"] = "Ungültiger Aufnahmebereich.";
            Translations["de-DE"]["ConditionErrorOutsideDisplays"] = "Der Suchbereich liegt außerhalb der verfügbaren Bildschirme.";
            Translations["de-DE"]["ConditionErrorLowDetail"] = "Die Vorlage enthält zu wenig Details. Wählen Sie ein markantes Symbol oder Text.";
            Translations["de-DE"]["ConditionErrorTemplateFit"] = "Die Originalvorlage übersteigt den Suchbereich. Größenanpassung aktivieren, Vorlage zuschneiden oder Bereich vergrößern.";
            Translations["de-DE"]["ConditionErrorScore"] = "Die Bilderkennung lieferte ungültige Werte.";
            Translations["de-DE"]["ConditionErrorTemplateEmpty"] = "Bitte zuerst eine Vorlage aufnehmen oder importieren.";
            Translations["de-DE"]["ConditionErrorTemplateSize"] = "Originalvorlagen sind auf 32 MiB und 32M Pixel begrenzt.";
            Translations["de-DE"]["ConditionErrorTimeout"] = "Zeitlimit beim Warten auf die Bildbedingung erreicht.";
            Translations["de-DE"]["ConditionErrorRecognition"] = "Die Bilderkennung ist fehlgeschlagen.";
            Translations["es-ES"]["ConditionErrorDetectionArea"] = "Zona de detección no válida (máximo 32 megapíxeles).";
            Translations["es-ES"]["ConditionErrorSettings"] = "Configuración de condición visual no válida.";
            Translations["es-ES"]["ConditionErrorDecode"] = "No se pudo decodificar la plantilla PNG.";
            Translations["es-ES"]["ConditionErrorEncoding"] = "Codificación de plantilla no válida.";
            Translations["es-ES"]["ConditionErrorPng"] = "La plantilla debe ser una imagen PNG.";
            Translations["es-ES"]["ConditionErrorDimensions"] = "Dimensiones no válidas: cada lado debe tener al menos 2 píxeles y el total no debe superar 32M píxeles.";
            Translations["es-ES"]["ConditionErrorCaptureArea"] = "Zona de captura no válida.";
            Translations["es-ES"]["ConditionErrorOutsideDisplays"] = "La zona de detección supera las pantallas disponibles.";
            Translations["es-ES"]["ConditionErrorLowDetail"] = "La plantilla tiene pocos detalles. Seleccione un icono o texto distintivo.";
            Translations["es-ES"]["ConditionErrorTemplateFit"] = "La plantilla original supera el área. Active adaptación, recorte o amplíe el área.";
            Translations["es-ES"]["ConditionErrorScore"] = "La detección devolvió puntuaciones no válidas.";
            Translations["es-ES"]["ConditionErrorTemplateEmpty"] = "Primero capture o importe una imagen de plantilla.";
            Translations["es-ES"]["ConditionErrorTemplateSize"] = "Las plantillas originales tienen un límite de 32 MiB y 32M píxeles.";
            Translations["es-ES"]["ConditionErrorTimeout"] = "Se agotó el tiempo de espera de la condición visual.";
            Translations["es-ES"]["ConditionErrorRecognition"] = "Falló el reconocimiento visual.";
            Translations["ru-RU"]["ConditionErrorDetectionArea"] = "Недопустимая область поиска (не более 32 мегапикселей).";
            Translations["ru-RU"]["ConditionErrorSettings"] = "Недопустимые настройки условия изображения.";
            Translations["ru-RU"]["ConditionErrorDecode"] = "Не удалось декодировать шаблон PNG.";
            Translations["ru-RU"]["ConditionErrorEncoding"] = "Недопустимая кодировка шаблона.";
            Translations["ru-RU"]["ConditionErrorPng"] = "Шаблон должен быть изображением PNG.";
            Translations["ru-RU"]["ConditionErrorDimensions"] = "Недопустимые размеры: каждая сторона не менее 2 пикселей, всего не более 32M пикселей.";
            Translations["ru-RU"]["ConditionErrorCaptureArea"] = "Недопустимая область снимка.";
            Translations["ru-RU"]["ConditionErrorOutsideDisplays"] = "Область поиска выходит за пределы доступных экранов.";
            Translations["ru-RU"]["ConditionErrorLowDetail"] = "В шаблоне слишком мало деталей. Выберите характерный значок или текст.";
            Translations["ru-RU"]["ConditionErrorTemplateFit"] = "Исходный шаблон больше области. Включите подбор размера, обрежьте шаблон или увеличьте область.";
            Translations["ru-RU"]["ConditionErrorScore"] = "Сопоставление вернуло недопустимые оценки.";
            Translations["ru-RU"]["ConditionErrorTemplateEmpty"] = "Сначала снимите или загрузите изображение шаблона.";
            Translations["ru-RU"]["ConditionErrorTemplateSize"] = "Исходный шаблон ограничен 32 MiB и 32M пикселей.";
            Translations["ru-RU"]["ConditionErrorTimeout"] = "Время ожидания условия изображения истекло.";
            Translations["ru-RU"]["ConditionErrorRecognition"] = "Не удалось распознать изображение.";
            Translations["pt-BR"]["ConditionErrorDetectionArea"] = "Área de detecção inválida (máximo de 32 megapixels).";
            Translations["pt-BR"]["ConditionErrorSettings"] = "Configurações de condição visual inválidas.";
            Translations["pt-BR"]["ConditionErrorDecode"] = "Não foi possível decodificar o modelo PNG.";
            Translations["pt-BR"]["ConditionErrorEncoding"] = "Codificação do modelo inválida.";
            Translations["pt-BR"]["ConditionErrorPng"] = "O modelo deve ser uma imagem PNG.";
            Translations["pt-BR"]["ConditionErrorDimensions"] = "Dimensões inválidas: pelo menos 2 pixels por lado e no máximo 32M pixels no total.";
            Translations["pt-BR"]["ConditionErrorCaptureArea"] = "Área de captura inválida.";
            Translations["pt-BR"]["ConditionErrorOutsideDisplays"] = "A área de detecção ultrapassa as telas disponíveis.";
            Translations["pt-BR"]["ConditionErrorLowDetail"] = "O modelo tem poucos detalhes. Selecione um ícone ou texto distinto.";
            Translations["pt-BR"]["ConditionErrorTemplateFit"] = "O modelo original supera a área. Ative adaptação, recorte ou amplie a área.";
            Translations["pt-BR"]["ConditionErrorScore"] = "A detecção retornou pontuações inválidas.";
            Translations["pt-BR"]["ConditionErrorTemplateEmpty"] = "Primeiro capture ou importe uma imagem de modelo.";
            Translations["pt-BR"]["ConditionErrorTemplateSize"] = "Modelos originais têm limite de 32 MiB e 32M pixels.";
            Translations["pt-BR"]["ConditionErrorTimeout"] = "O tempo de espera da condição visual acabou.";
            Translations["pt-BR"]["ConditionErrorRecognition"] = "O reconhecimento visual falhou.";
            Translations["zh-CN"]["ShowDetectionOverlay"] = "显示检测区域";
            Translations["zh-CN"]["DetectionOverlayLabel"] = "检测：{0}";
            Translations["zh-TW"]["ShowDetectionOverlay"] = "顯示偵測區域";
            Translations["zh-TW"]["DetectionOverlayLabel"] = "偵測：{0}";
            Translations["en-US"]["ShowDetectionOverlay"] = "Show detection areas";
            Translations["en-US"]["DetectionOverlayLabel"] = "Detection: {0}";
            Translations["ja-JP"]["ShowDetectionOverlay"] = "検出領域を表示";
            Translations["ja-JP"]["DetectionOverlayLabel"] = "検出：{0}";
            Translations["ko-KR"]["ShowDetectionOverlay"] = "감지 영역 표시";
            Translations["ko-KR"]["DetectionOverlayLabel"] = "감지: {0}";
            Translations["fr-FR"]["ShowDetectionOverlay"] = "Afficher les zones de détection";
            Translations["fr-FR"]["DetectionOverlayLabel"] = "Détection : {0}";
            Translations["de-DE"]["ShowDetectionOverlay"] = "Suchbereiche anzeigen";
            Translations["de-DE"]["DetectionOverlayLabel"] = "Erkennung: {0}";
            Translations["es-ES"]["ShowDetectionOverlay"] = "Mostrar zonas de detección";
            Translations["es-ES"]["DetectionOverlayLabel"] = "Detección: {0}";
            Translations["ru-RU"]["ShowDetectionOverlay"] = "Показывать области поиска";
            Translations["ru-RU"]["DetectionOverlayLabel"] = "Поиск: {0}";
            Translations["pt-BR"]["ShowDetectionOverlay"] = "Mostrar áreas de detecção";
            Translations["pt-BR"]["DetectionOverlayLabel"] = "Detecção: {0}";
            Translations["zh-CN"]["ConditionTemplateDimensions"] = "模板：{0}×{1}";
            Translations["zh-CN"]["ConditionDetectionDimensions"] = "检测区域：{0}×{1}，位置 ({2}, {3})";
            Translations["zh-CN"]["ConditionTemplateFit"] = "原尺寸模式下，模板 {0}×{1} 超过检测区域 {2}×{3}。可开启自动适应目标大小、裁剪模板或扩大检测区域。";
            Translations["zh-CN"]["ConditionSetupRegionFirst"] = "请先框选检测区域，再截图或导入模板；模板截图位置可与检测区域不同。";
            Translations["zh-CN"]["ConditionSetupTemplate"] = "请截图或导入模板。";
            Translations["zh-CN"]["ConditionResizeDetection"] = "重新框选检测区域";
            Translations["zh-TW"]["ConditionTemplateDimensions"] = "範本：{0}×{1}";
            Translations["zh-TW"]["ConditionDetectionDimensions"] = "偵測區域：{0}×{1}，位置 ({2}, {3})";
            Translations["zh-TW"]["ConditionTemplateFit"] = "原尺寸模式下，範本 {0}×{1} 超過偵測區域 {2}×{3}。可啟用自動適應目標大小、裁切範本或擴大偵測區域。";
            Translations["zh-TW"]["ConditionSetupRegionFirst"] = "請先選取偵測區域，再擷取或匯入範本；擷取位置可以不同。";
            Translations["zh-TW"]["ConditionSetupTemplate"] = "請擷取或匯入範本。";
            Translations["zh-TW"]["ConditionResizeDetection"] = "重新選取偵測區域";
            Translations["en-US"]["ConditionTemplateDimensions"] = "Template: {0}×{1}";
            Translations["en-US"]["ConditionDetectionDimensions"] = "Detection area: {0}×{1}, position ({2}, {3})";
            Translations["en-US"]["ConditionTemplateFit"] = "In original-size mode, template {0}×{1} exceeds detection area {2}×{3}. Enable automatic target sizing, crop the template, or enlarge the detection area.";
            Translations["en-US"]["ConditionSetupRegionFirst"] = "Select a detection area before capturing or importing a template; the capture location may be different.";
            Translations["en-US"]["ConditionSetupTemplate"] = "Capture or import a template.";
            Translations["en-US"]["ConditionResizeDetection"] = "Reselect detection area";
            Translations["ja-JP"]["ConditionTemplateDimensions"] = "テンプレート：{0}×{1}";
            Translations["ja-JP"]["ConditionDetectionDimensions"] = "検出範囲：{0}×{1}、位置 ({2}, {3})";
            Translations["ja-JP"]["ConditionTemplateFit"] = "元サイズ照合では画像 {0}×{1} が検出領域 {2}×{3} を超えます。サイズ自動対応を有効にするか、切り抜くか、領域を広げてください。";
            Translations["ja-JP"]["ConditionSetupRegionFirst"] = "先に検出範囲を選んでから撮影または読込してください。撮影場所は別でも構いません。";
            Translations["ja-JP"]["ConditionSetupTemplate"] = "テンプレートを撮影または読み込んでください。";
            Translations["ja-JP"]["ConditionResizeDetection"] = "検出範囲を選び直す";
            Translations["ko-KR"]["ConditionTemplateDimensions"] = "템플릿: {0}×{1}";
            Translations["ko-KR"]["ConditionDetectionDimensions"] = "감지 영역: {0}×{1}, 위치 ({2}, {3})";
            Translations["ko-KR"]["ConditionTemplateFit"] = "원본 크기 모드에서 템플릿 {0}×{1}이 감지 영역 {2}×{3}보다 큽니다. 크기 자동 조정을 켜거나 템플릿을 자르거나 감지 영역을 넓히세요.";
            Translations["ko-KR"]["ConditionSetupRegionFirst"] = "먼저 감지 영역을 선택한 후 캡처하거나 가져오세요. 캡처 위치는 달라도 됩니다.";
            Translations["ko-KR"]["ConditionSetupTemplate"] = "템플릿을 캡처하거나 가져오세요.";
            Translations["ko-KR"]["ConditionResizeDetection"] = "감지 영역 다시 선택";
            Translations["fr-FR"]["ConditionTemplateDimensions"] = "Modèle : {0}×{1}";
            Translations["fr-FR"]["ConditionDetectionDimensions"] = "Zone de détection : {0}×{1}, position ({2}, {3})";
            Translations["fr-FR"]["ConditionTemplateFit"] = "À la taille d’origine, le modèle {0}×{1} dépasse la zone {2}×{3}. Activez l’adaptation, recadrez le modèle ou agrandissez la zone.";
            Translations["fr-FR"]["ConditionSetupRegionFirst"] = "Choisissez d’abord une zone de détection avant la capture ou l’importation ; le lieu de capture peut être différent.";
            Translations["fr-FR"]["ConditionSetupTemplate"] = "Capturez ou importez un modèle.";
            Translations["fr-FR"]["ConditionResizeDetection"] = "Resélectionner la zone";
            Translations["de-DE"]["ConditionTemplateDimensions"] = "Vorlage: {0}×{1}";
            Translations["de-DE"]["ConditionDetectionDimensions"] = "Erkennungsbereich: {0}×{1}, Position ({2}, {3})";
            Translations["de-DE"]["ConditionTemplateFit"] = "Bei Originalgröße übersteigt Vorlage {0}×{1} den Bereich {2}×{3}. Aktivieren Sie die Größenanpassung, schneiden Sie die Vorlage zu oder vergrößern Sie den Bereich.";
            Translations["de-DE"]["ConditionSetupRegionFirst"] = "Wählen Sie vor Aufnahme oder Import einen Erkennungsbereich. Der Aufnahmeort darf davon abweichen.";
            Translations["de-DE"]["ConditionSetupTemplate"] = "Erfassen oder importieren Sie eine Vorlage.";
            Translations["de-DE"]["ConditionResizeDetection"] = "Erkennungsbereich neu wählen";
            Translations["es-ES"]["ConditionTemplateDimensions"] = "Plantilla: {0}×{1}";
            Translations["es-ES"]["ConditionDetectionDimensions"] = "Área de detección: {0}×{1}, posición ({2}, {3})";
            Translations["es-ES"]["ConditionTemplateFit"] = "En tamaño original, la plantilla {0}×{1} supera el área {2}×{3}. Active la adaptación, recorte la plantilla o amplíe el área.";
            Translations["es-ES"]["ConditionSetupRegionFirst"] = "Seleccione primero el área de detección para capturar o importar; el lugar de captura puede ser distinto.";
            Translations["es-ES"]["ConditionSetupTemplate"] = "Capture o importe una plantilla.";
            Translations["es-ES"]["ConditionResizeDetection"] = "Volver a seleccionar el área";
            Translations["ru-RU"]["ConditionTemplateDimensions"] = "Шаблон: {0}×{1}";
            Translations["ru-RU"]["ConditionDetectionDimensions"] = "Область поиска: {0}×{1}, позиция ({2}, {3})";
            Translations["ru-RU"]["ConditionTemplateFit"] = "В исходном размере шаблон {0}×{1} больше области {2}×{3}. Включите подбор размера, обрежьте шаблон или увеличьте область.";
            Translations["ru-RU"]["ConditionSetupRegionFirst"] = "Перед снимком или импортом выберите область поиска. Место снимка может отличаться.";
            Translations["ru-RU"]["ConditionSetupTemplate"] = "Снимите или импортируйте шаблон.";
            Translations["ru-RU"]["ConditionResizeDetection"] = "Выбрать область заново";
            Translations["pt-BR"]["ConditionTemplateDimensions"] = "Modelo: {0}×{1}";
            Translations["pt-BR"]["ConditionDetectionDimensions"] = "Área de detecção: {0}×{1}, posição ({2}, {3})";
            Translations["pt-BR"]["ConditionTemplateFit"] = "No tamanho original, o modelo {0}×{1} supera a área {2}×{3}. Ative a adaptação, recorte o modelo ou amplie a área.";
            Translations["pt-BR"]["ConditionSetupRegionFirst"] = "Selecione primeiro a área de detecção para capturar ou importar; o local da captura pode ser diferente.";
            Translations["pt-BR"]["ConditionSetupTemplate"] = "Capture ou importe um modelo.";
            Translations["pt-BR"]["ConditionResizeDetection"] = "Selecionar a área novamente";
            Translations["en-US"]["TemplateCaptureInstructions"] = "Drag inside the frame to move it anywhere. Drag an edge or corner to resize. Enter: capture • Esc: cancel Move with the ↔ handle or arrow keys; Shift moves faster.";
            Translations["en-US"]["TemplateCaptureSize"] = "Template: {0} × {1} px   Maximum: {2} × {3} px";
            Translations["en-US"]["TemplateCaptureInvalidArea"] = "Keep the entire frame on visible screens, away from gaps between monitors. Reduce the frame if needed.";
            Translations["en-US"]["TemplateCropTitle"] = "Crop template";
            Translations["en-US"]["TemplateCropHint"] = "Drag inside the image to select a crop. Drag the selection to move it or its corner handles to resize. The crop keeps original pixels without scaling.";
            Translations["en-US"]["TemplateCropConfirm"] = "Use crop";
            Translations["en-US"]["TemplateCropDimensions"] = "Crop {0} × {1} px · Maximum {2} × {3} px";
            Translations["en-US"]["ConditionImportTooLarge"] = "Imported images are limited to 32 MiB and about 32 million pixels. Reduce the source image first.";
            Translations["zh-CN"]["TemplateCaptureInstructions"] = "拖动框内可移到任意位置，拖动边或角可调整大小。Enter 确认截图 · Esc 取消 使用 ↔ 手柄或方向键移动，Shift 加速。";
            Translations["zh-CN"]["TemplateCaptureSize"] = "模板：{0} × {1} 像素　最大：{2} × {3} 像素";
            Translations["zh-CN"]["TemplateCaptureInvalidArea"] = "请将整个截图框放在可见屏幕内，避开显示器之间的空隙；必要时缩小截图框。";
            Translations["zh-CN"]["TemplateCropTitle"] = "裁剪模板";
            Translations["zh-CN"]["TemplateCropHint"] = "在图片内拖动框选，拖动选框可移动，拖动四角可调整大小。裁剪保留原始像素，不会缩放。";
            Translations["zh-CN"]["TemplateCropConfirm"] = "确认裁剪";
            Translations["zh-CN"]["TemplateCropDimensions"] = "当前 {0} × {1} 像素 · 最大 {2} × {3} 像素";
            Translations["zh-CN"]["ConditionImportTooLarge"] = "导入原图最多 32 MiB、约 3200 万像素。请先缩小原图。";
            Translations["zh-TW"]["TemplateCaptureInstructions"] = "拖曳框內可移到任意位置，拖曳邊或角可調整大小。Enter 確認擷取 · Esc 取消 使用 ↔ 把手或方向鍵移動，Shift 加速。";
            Translations["zh-TW"]["TemplateCaptureSize"] = "範本：{0} × {1} 像素　最大：{2} × {3} 像素";
            Translations["zh-TW"]["TemplateCaptureInvalidArea"] = "請將整個擷取框放在可見螢幕內，避開顯示器之間的空隙；必要時縮小擷取框。";
            Translations["zh-TW"]["TemplateCropTitle"] = "裁切範本";
            Translations["zh-TW"]["TemplateCropHint"] = "在圖片內拖曳框選，拖曳選框可移動，拖曳四角可調整大小。裁切保留原始像素，不會縮放。";
            Translations["zh-TW"]["TemplateCropConfirm"] = "確認裁切";
            Translations["zh-TW"]["TemplateCropDimensions"] = "目前 {0} × {1} 像素 · 最大 {2} × {3} 像素";
            Translations["zh-TW"]["ConditionImportTooLarge"] = "匯入原圖最多 32 MiB、約 3200 萬像素。請先縮小原圖。";
            Translations["ja-JP"]["TemplateCaptureInstructions"] = "枠内をドラッグして移動し、辺や角でサイズを変更します。Enter：撮影・Esc：キャンセル ↔ ハンドルか矢印キーで移動。Shiftで加速。";
            Translations["ja-JP"]["TemplateCaptureSize"] = "テンプレート：{0} × {1} px　最大：{2} × {3} px";
            Translations["ja-JP"]["TemplateCaptureInvalidArea"] = "枠全体を画面内に収め、モニター間の隙間を避けてください。必要に応じて枠を小さくしてください。";
            Translations["ja-JP"]["TemplateCropTitle"] = "テンプレートを切り抜く";
            Translations["ja-JP"]["TemplateCropHint"] = "画像内をドラッグして範囲を選択します。枠の内側で移動、四隅でサイズ変更ができます。元の画素を保ち、拡大縮小しません。";
            Translations["ja-JP"]["TemplateCropConfirm"] = "切り抜きを確定";
            Translations["ja-JP"]["TemplateCropDimensions"] = "選択 {0} × {1} px · 最大 {2} × {3} px";
            Translations["ja-JP"]["ConditionImportTooLarge"] = "読込画像は32 MiB、約3200万画素までです。元画像を小さくしてください。";
            Translations["ko-KR"]["TemplateCaptureInstructions"] = "프레임 안을 끌어 이동하고 가장자리나 모서리를 끌어 크기를 조절하세요. Enter: 캡처 · Esc: 취소 ↔ 손잡이나 방향키로 이동합니다. Shift로 빠르게 이동합니다.";
            Translations["ko-KR"]["TemplateCaptureSize"] = "템플릿: {0} × {1} 픽셀　최대: {2} × {3} 픽셀";
            Translations["ko-KR"]["TemplateCaptureInvalidArea"] = "프레임 전체를 화면 안에 놓고 모니터 사이 빈 공간을 피하세요. 필요하면 프레임을 줄이세요.";
            Translations["ko-KR"]["TemplateCropTitle"] = "템플릿 자르기";
            Translations["ko-KR"]["TemplateCropHint"] = "이미지 안에서 드래그하여 선택하세요. 선택 영역을 끌어 이동하고 모서리를 끌어 크기를 조절하세요. 원본 픽셀을 유지하며 확대하거나 축소하지 않습니다.";
            Translations["ko-KR"]["TemplateCropConfirm"] = "자르기 확인";
            Translations["ko-KR"]["TemplateCropDimensions"] = "선택 {0} × {1} px · 최대 {2} × {3} px";
            Translations["ko-KR"]["ConditionImportTooLarge"] = "가져올 원본은 32 MiB, 약 3200만 픽셀까지입니다. 먼저 원본 크기를 줄이세요.";
            Translations["fr-FR"]["TemplateCaptureInstructions"] = "Déplacez le cadre par son intérieur, redimensionnez par un bord ou un coin. Entrée : capturer · Échap : annuler Déplacez avec la poignée ↔ ou les flèches ; Maj accélère.";
            Translations["fr-FR"]["TemplateCaptureSize"] = "Modèle : {0} × {1} px　Maximum : {2} × {3} px";
            Translations["fr-FR"]["TemplateCaptureInvalidArea"] = "Placez le cadre entièrement sur les écrans, sans espace vide entre moniteurs. Réduisez-le si nécessaire.";
            Translations["fr-FR"]["TemplateCropTitle"] = "Recadrer le modèle";
            Translations["fr-FR"]["TemplateCropHint"] = "Tracez une sélection dans l’image. Déplacez-la en la faisant glisser ou redimensionnez-la avec ses coins. Les pixels d’origine sont conservés, sans mise à l’échelle.";
            Translations["fr-FR"]["TemplateCropConfirm"] = "Valider le recadrage";
            Translations["fr-FR"]["TemplateCropDimensions"] = "Sélection {0} × {1} px · Maximum {2} × {3} px";
            Translations["fr-FR"]["ConditionImportTooLarge"] = "L’image importée est limitée à 32 MiB et environ 32 millions de pixels. Réduisez d’abord l’original.";
            Translations["de-DE"]["TemplateCaptureInstructions"] = "Im Rahmen ziehen zum Verschieben; Kanten oder Ecken ziehen zum Skalieren. Eingabe: aufnehmen · Esc: abbrechen Mit ↔-Griff oder Pfeiltasten verschieben; Umschalt beschleunigt.";
            Translations["de-DE"]["TemplateCaptureSize"] = "Vorlage: {0} × {1} px　Maximum: {2} × {3} px";
            Translations["de-DE"]["TemplateCaptureInvalidArea"] = "Der gesamte Rahmen muss auf sichtbaren Bildschirmen liegen, ohne Lücken zwischen Monitoren. Bei Bedarf verkleinern.";
            Translations["de-DE"]["TemplateCropTitle"] = "Vorlage zuschneiden";
            Translations["de-DE"]["TemplateCropHint"] = "Ziehen Sie eine Auswahl im Bild. Ziehen Sie innen zum Verschieben oder an den Ecken zur Größenänderung. Originalpixel bleiben ohne Skalierung erhalten.";
            Translations["de-DE"]["TemplateCropConfirm"] = "Zuschnitt übernehmen";
            Translations["de-DE"]["TemplateCropDimensions"] = "Auswahl {0} × {1} px · Maximal {2} × {3} px";
            Translations["de-DE"]["ConditionImportTooLarge"] = "Importierte Bilder sind auf 32 MiB und etwa 32 Millionen Pixel begrenzt. Verkleinern Sie zuerst das Original.";
            Translations["es-ES"]["TemplateCaptureInstructions"] = "Arrastre dentro para mover el marco; use los bordes o esquinas para ajustar el tamaño. Intro: capturar · Esc: cancelar Mueva con el asa ↔ o las flechas; Mayús acelera.";
            Translations["es-ES"]["TemplateCaptureSize"] = "Plantilla: {0} × {1} px　Máximo: {2} × {3} px";
            Translations["es-ES"]["TemplateCaptureInvalidArea"] = "Coloque todo el marco en las pantallas, evitando huecos entre monitores. Reduzca el marco si es necesario.";
            Translations["es-ES"]["TemplateCropTitle"] = "Recortar plantilla";
            Translations["es-ES"]["TemplateCropHint"] = "Arrastre en la imagen para seleccionar. Arrastre la selección para moverla o sus esquinas para cambiar el tamaño. Se conservan los píxeles originales, sin escalar.";
            Translations["es-ES"]["TemplateCropConfirm"] = "Confirmar recorte";
            Translations["es-ES"]["TemplateCropDimensions"] = "Selección {0} × {1} px · Máximo {2} × {3} px";
            Translations["es-ES"]["ConditionImportTooLarge"] = "La imagen importada admite hasta 32 MiB y unos 32 millones de píxeles. Reduzca primero el original.";
            Translations["ru-RU"]["TemplateCaptureInstructions"] = "Перетащите внутри рамки для перемещения, край или угол — для размера. Enter: снимок · Esc: отмена Перемещайте ручкой ↔ или стрелками; Shift ускоряет.";
            Translations["ru-RU"]["TemplateCaptureSize"] = "Шаблон: {0} × {1} пикс.　Максимум: {2} × {3} пикс.";
            Translations["ru-RU"]["TemplateCaptureInvalidArea"] = "Разместите всю рамку на видимых экранах без промежутков между мониторами. При необходимости уменьшите рамку.";
            Translations["ru-RU"]["TemplateCropTitle"] = "Обрезать шаблон";
            Translations["ru-RU"]["TemplateCropHint"] = "Выделите область на изображении. Перетаскивайте её для перемещения или тяните за углы для изменения размера. Исходные пиксели сохраняются без масштабирования.";
            Translations["ru-RU"]["TemplateCropConfirm"] = "Применить обрезку";
            Translations["ru-RU"]["TemplateCropDimensions"] = "Выделено {0} × {1} пкс · Максимум {2} × {3} пкс";
            Translations["ru-RU"]["ConditionImportTooLarge"] = "Исходное изображение ограничено 32 MiB и примерно 32 миллионами пикселей. Сначала уменьшите оригинал.";
            Translations["pt-BR"]["TemplateCaptureInstructions"] = "Arraste por dentro para mover; arraste bordas ou cantos para redimensionar. Enter: capturar · Esc: cancelar Mova com a alça ↔ ou as setas; Shift acelera.";
            Translations["pt-BR"]["TemplateCaptureSize"] = "Modelo: {0} × {1} px　Máximo: {2} × {3} px";
            Translations["pt-BR"]["TemplateCaptureInvalidArea"] = "Mantenha todo o quadro nas telas, sem espaços entre monitores. Reduza o quadro se necessário.";
            Translations["pt-BR"]["TemplateCropTitle"] = "Recortar modelo";
            Translations["pt-BR"]["TemplateCropHint"] = "Arraste na imagem para selecionar. Arraste a seleção para movê-la ou os cantos para ajustar o tamanho. Os pixels originais são preservados, sem redimensionamento.";
            Translations["pt-BR"]["TemplateCropConfirm"] = "Confirmar recorte";
            Translations["pt-BR"]["TemplateCropDimensions"] = "Seleção {0} × {1} px · Máximo {2} × {3} px";
            Translations["pt-BR"]["ConditionImportTooLarge"] = "A imagem importada tem limite de 32 MiB e cerca de 32 milhões de pixels. Reduza primeiro a imagem original.";
            Translations["en-US"]["TemplateCropKeyboardHint"] = "For tiny selections, press Tab to focus the image and move with arrow keys (1 pixel, or 10 with Shift).";
            Translations["zh-CN"]["TemplateCropKeyboardHint"] = "选框过小时，可按 Tab 聚焦图片，再用方向键每次移动 1 像素；按住 Shift 每次移动 10 像素。";
            Translations["zh-TW"]["TemplateCropKeyboardHint"] = "選框太小時，可按 Tab 聚焦圖片，再用方向鍵每次移動 1 像素；按住 Shift 每次移動 10 像素。";
            Translations["ja-JP"]["TemplateCropKeyboardHint"] = "小さい選択範囲は Tab で画像に移動し、矢印キーで1画素ずつ、Shift 併用で10画素ずつ動かせます。";
            Translations["ko-KR"]["TemplateCropKeyboardHint"] = "작은 선택 영역은 Tab으로 이미지에 초점을 맞춘 후 방향키로 1픽셀씩 이동하세요. Shift를 누르면 10픽셀씩 이동합니다.";
            Translations["fr-FR"]["TemplateCropKeyboardHint"] = "Pour une petite sélection, utilisez Tab pour cibler l’image, puis les flèches pour déplacer de 1 pixel (10 avec Maj).";
            Translations["de-DE"]["TemplateCropKeyboardHint"] = "Bei kleiner Auswahl mit Tab das Bild fokussieren und mit Pfeiltasten um 1 Pixel verschieben (mit Umschalt um 10).";
            Translations["es-ES"]["TemplateCropKeyboardHint"] = "Para selecciones pequeñas, enfoque la imagen con Tab y use las flechas para mover 1 píxel (10 con Mayús).";
            Translations["ru-RU"]["TemplateCropKeyboardHint"] = "Для малого выделения нажмите Tab, чтобы перейти к изображению, и двигайте стрелками на 1 пиксель (с Shift — на 10).";
            Translations["pt-BR"]["TemplateCropKeyboardHint"] = "Para seleções pequenas, use Tab para focar a imagem e as setas para mover 1 pixel (10 com Shift).";
            Translations["zh-CN"]["ConditionUseMultiScale"] = "自动适应目标大小";
            Translations["zh-CN"]["ConditionCropTemplate"] = "裁剪模板（可选）";
            Translations["zh-CN"]["ConditionScaleHint"] = "保留完整模板，等比例匹配不同大小的目标。缩放搜索较慢，请先测试识别。";
            Translations["zh-CN"]["ConditionMatchScale"] = "匹配比例：{0:P0}，大小：{1}×{2}";
            Translations["zh-TW"]["ConditionUseMultiScale"] = "自動適應目標大小";
            Translations["zh-TW"]["ConditionCropTemplate"] = "裁切範本（選用）";
            Translations["zh-TW"]["ConditionScaleHint"] = "保留完整範本，等比例比對不同大小的目標。縮放搜尋較慢，請先測試辨識。";
            Translations["zh-TW"]["ConditionMatchScale"] = "比對比例：{0:P0}，大小：{1}×{2}";
            Translations["en-US"]["ConditionUseMultiScale"] = "Automatically adapt to target size";
            Translations["en-US"]["ConditionCropTemplate"] = "Crop template (optional)";
            Translations["en-US"]["ConditionScaleHint"] = "Keep the full template and match targets at proportional sizes. Scale searches are slower; test recognition first.";
            Translations["en-US"]["ConditionMatchScale"] = "Matched scale: {0:P0}, size: {1}×{2}";
            Translations["ja-JP"]["ConditionUseMultiScale"] = "対象サイズに自動対応";
            Translations["ja-JP"]["ConditionCropTemplate"] = "テンプレートを切り抜く（任意）";
            Translations["ja-JP"]["ConditionScaleHint"] = "画像全体を保持し、異なる大きさの対象を等比で照合します。倍率検索は時間がかかるため、先に認識テストをしてください。";
            Translations["ja-JP"]["ConditionMatchScale"] = "一致倍率：{0:P0}、サイズ：{1}×{2}";
            Translations["ko-KR"]["ConditionUseMultiScale"] = "대상 크기에 자동으로 맞추기";
            Translations["ko-KR"]["ConditionCropTemplate"] = "템플릿 자르기(선택)";
            Translations["ko-KR"]["ConditionScaleHint"] = "전체 템플릿을 유지하고 비례 크기로 대상을 비교합니다. 배율 검색은 느리므로 먼저 인식 테스트를 하세요.";
            Translations["ko-KR"]["ConditionMatchScale"] = "일치 배율: {0:P0}, 크기: {1}×{2}";
            Translations["fr-FR"]["ConditionUseMultiScale"] = "Adapter automatiquement la taille cible";
            Translations["fr-FR"]["ConditionCropTemplate"] = "Recadrer le modèle (facultatif)";
            Translations["fr-FR"]["ConditionScaleHint"] = "Gardez le modèle entier et cherchez des tailles proportionnelles. Cette recherche est plus lente : testez d’abord la reconnaissance.";
            Translations["fr-FR"]["ConditionMatchScale"] = "Échelle : {0:P0}, taille : {1}×{2}";
            Translations["de-DE"]["ConditionUseMultiScale"] = "Automatisch an Zielgröße anpassen";
            Translations["de-DE"]["ConditionCropTemplate"] = "Vorlage zuschneiden (optional)";
            Translations["de-DE"]["ConditionScaleHint"] = "Vorlage vollständig behalten und proportionale Zielgrößen suchen. Die Skalensuche ist langsamer; zuerst die Erkennung testen.";
            Translations["de-DE"]["ConditionMatchScale"] = "Treffermaßstab: {0:P0}, Größe: {1}×{2}";
            Translations["es-ES"]["ConditionUseMultiScale"] = "Adaptar automáticamente al tamaño del objetivo";
            Translations["es-ES"]["ConditionCropTemplate"] = "Recortar plantilla (opcional)";
            Translations["es-ES"]["ConditionScaleHint"] = "Conserve la plantilla completa y busque tamaños proporcionales. La búsqueda es más lenta; pruebe primero el reconocimiento.";
            Translations["es-ES"]["ConditionMatchScale"] = "Escala: {0:P0}, tamaño: {1}×{2}";
            Translations["ru-RU"]["ConditionUseMultiScale"] = "Автоматически подбирать размер цели";
            Translations["ru-RU"]["ConditionCropTemplate"] = "Обрезать шаблон (необязательно)";
            Translations["ru-RU"]["ConditionScaleHint"] = "Сохраняет весь шаблон и ищет пропорциональные размеры цели. Поиск масштаба медленнее — сначала проверьте распознавание.";
            Translations["ru-RU"]["ConditionMatchScale"] = "Масштаб: {0:P0}, размер: {1}×{2}";
            Translations["pt-BR"]["ConditionUseMultiScale"] = "Adaptar automaticamente ao tamanho do alvo";
            Translations["pt-BR"]["ConditionCropTemplate"] = "Recortar modelo (opcional)";
            Translations["pt-BR"]["ConditionScaleHint"] = "Mantenha o modelo inteiro e busque tamanhos proporcionais. A busca é mais lenta; teste primeiro o reconhecimento.";
            Translations["pt-BR"]["ConditionMatchScale"] = "Escala: {0:P0}, tamanho: {1}×{2}";
            Translations["en-US"]["Settings"] = "Settings";
            Translations["en-US"]["MainAreasTitle"] = "1 · Click areas";
            Translations["en-US"]["MainTimingTitle"] = "2 · Click behavior";
            Translations["en-US"]["MainDisplayTitle"] = "Screen guides";
            Translations["en-US"]["MainAreasHint"] = "Add an area first; image conditions are optional.";
            Translations["en-US"]["MainEmptyAreas"] = "Click Add to select your first click area";
            Translations["en-US"]["MainRunHint"] = "{0} to start or stop · Esc to stop immediately";
            Translations["zh-CN"]["Settings"] = "设置";
            Translations["zh-CN"]["MainAreasTitle"] = "1 · 点击区域";
            Translations["zh-CN"]["MainTimingTitle"] = "2 · 点击方式";
            Translations["zh-CN"]["MainDisplayTitle"] = "屏幕辅助";
            Translations["zh-CN"]["MainAreasHint"] = "先添加区域；画面条件可按需设置。";
            Translations["zh-CN"]["MainEmptyAreas"] = "点击“添加”框选第一个点击区域";
            Translations["zh-CN"]["MainRunHint"] = "使用 {0} 开始或停止 · Esc 紧急停止";
            Translations["zh-TW"]["Settings"] = "設定";
            Translations["zh-TW"]["MainAreasTitle"] = "1 · 點擊區域";
            Translations["zh-TW"]["MainTimingTitle"] = "2 · 點擊方式";
            Translations["zh-TW"]["MainDisplayTitle"] = "螢幕輔助";
            Translations["zh-TW"]["MainAreasHint"] = "先新增區域；影像條件可依需要設定。";
            Translations["zh-TW"]["MainEmptyAreas"] = "按「新增」框選第一個點擊區域";
            Translations["zh-TW"]["MainRunHint"] = "使用 {0} 開始或停止 · Esc 緊急停止";
            Translations["ja-JP"]["Settings"] = "設定";
            Translations["ja-JP"]["MainAreasTitle"] = "1 · クリック領域";
            Translations["ja-JP"]["MainTimingTitle"] = "2 · クリック方法";
            Translations["ja-JP"]["MainDisplayTitle"] = "画面ガイド";
            Translations["ja-JP"]["MainAreasHint"] = "まず領域を追加。画像条件は必要に応じて設定できます。";
            Translations["ja-JP"]["MainEmptyAreas"] = "「追加」で最初のクリック領域を選択";
            Translations["ja-JP"]["MainRunHint"] = "{0} で開始・停止 · Esc で緊急停止";
            Translations["ko-KR"]["Settings"] = "설정";
            Translations["ko-KR"]["MainAreasTitle"] = "1 · 클릭 영역";
            Translations["ko-KR"]["MainTimingTitle"] = "2 · 클릭 방식";
            Translations["ko-KR"]["MainDisplayTitle"] = "화면 안내";
            Translations["ko-KR"]["MainAreasHint"] = "먼저 영역을 추가하세요. 이미지 조건은 선택 사항입니다.";
            Translations["ko-KR"]["MainEmptyAreas"] = "추가를 눌러 첫 클릭 영역을 선택하세요";
            Translations["ko-KR"]["MainRunHint"] = "{0}: 시작 또는 중지 · Esc: 긴급 중지";
            Translations["fr-FR"]["Settings"] = "Paramètres";
            Translations["fr-FR"]["MainAreasTitle"] = "1 · Zones de clic";
            Translations["fr-FR"]["MainTimingTitle"] = "2 · Comportement";
            Translations["fr-FR"]["MainDisplayTitle"] = "Repères à l’écran";
            Translations["fr-FR"]["MainAreasHint"] = "Ajoutez une zone ; les conditions visuelles sont facultatives.";
            Translations["fr-FR"]["MainEmptyAreas"] = "Cliquez sur Ajouter pour sélectionner votre première zone";
            Translations["fr-FR"]["MainRunHint"] = "{0} pour démarrer ou arrêter · Esc pour arrêter immédiatement";
            Translations["de-DE"]["Settings"] = "Einstellungen";
            Translations["de-DE"]["MainAreasTitle"] = "1 · Klickbereiche";
            Translations["de-DE"]["MainTimingTitle"] = "2 · Klickverhalten";
            Translations["de-DE"]["MainDisplayTitle"] = "Bildschirmhilfen";
            Translations["de-DE"]["MainAreasHint"] = "Zuerst einen Bereich hinzufügen; Bildbedingungen sind optional.";
            Translations["de-DE"]["MainEmptyAreas"] = "Mit Hinzufügen den ersten Klickbereich auswählen";
            Translations["de-DE"]["MainRunHint"] = "{0} zum Starten oder Stoppen · Esc zum sofortigen Stoppen";
            Translations["es-ES"]["Settings"] = "Ajustes";
            Translations["es-ES"]["MainAreasTitle"] = "1 · Zonas de clic";
            Translations["es-ES"]["MainTimingTitle"] = "2 · Comportamiento";
            Translations["es-ES"]["MainDisplayTitle"] = "Guías en pantalla";
            Translations["es-ES"]["MainAreasHint"] = "Añada una zona; las condiciones visuales son opcionales.";
            Translations["es-ES"]["MainEmptyAreas"] = "Pulse Añadir para seleccionar su primera zona";
            Translations["es-ES"]["MainRunHint"] = "{0} para iniciar o detener · Esc para detener de inmediato";
            Translations["ru-RU"]["Settings"] = "Настройки";
            Translations["ru-RU"]["MainAreasTitle"] = "1 · Области кликов";
            Translations["ru-RU"]["MainTimingTitle"] = "2 · Параметры кликов";
            Translations["ru-RU"]["MainDisplayTitle"] = "Экранные подсказки";
            Translations["ru-RU"]["MainAreasHint"] = "Сначала добавьте область; условия изображения необязательны.";
            Translations["ru-RU"]["MainEmptyAreas"] = "Нажмите «Добавить», чтобы выбрать первую область";
            Translations["ru-RU"]["MainRunHint"] = "{0} — запуск или остановка · Esc — немедленная остановка";
            Translations["pt-BR"]["Settings"] = "Configurações";
            Translations["pt-BR"]["MainAreasTitle"] = "1 · Áreas de clique";
            Translations["pt-BR"]["MainTimingTitle"] = "2 · Comportamento";
            Translations["pt-BR"]["MainDisplayTitle"] = "Guias na tela";
            Translations["pt-BR"]["MainAreasHint"] = "Adicione uma área; as condições visuais são opcionais.";
            Translations["pt-BR"]["MainEmptyAreas"] = "Clique em Adicionar para selecionar a primeira área";
            Translations["pt-BR"]["MainRunHint"] = "{0} para iniciar ou parar · Esc para parar imediatamente";
            foreach (var language in Translations.Values)
            {
                language["HelpTutorialContent"] += "\n\n" + language["ConditionEdit"] + ":\n" +
                    language["ConditionRegion"] + " → " + language["ConditionCapture"] + " / " + language["ConditionImport"] +
                    " → " + language["ConditionTest"] + " → " + language["ConditionSave"] + "\n" + language["ConditionHint"];
            }
        }

        public static string Get(string key)
        {
            if (Translations.TryGetValue(Language, out var language) && language.TryGetValue(key, out var translated))
                return translated;
            return Translations["en-US"].TryGetValue(key, out var fallback) ? fallback : key;
        }
    }
}
