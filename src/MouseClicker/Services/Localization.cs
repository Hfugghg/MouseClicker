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

        public static string Get(string key)
        {
            if (Translations.TryGetValue(Language, out var language) && language.TryGetValue(key, out var translated))
                return translated;
            return Translations["en-US"].TryGetValue(key, out var fallback) ? fallback : key;
        }
    }
}
