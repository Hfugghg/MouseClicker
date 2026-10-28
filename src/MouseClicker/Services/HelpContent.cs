namespace MouseClicker;

internal enum HelpTopic
{
    QuickStart, AreasAndButtons, TimingAndOrder, ImageConditionSetup,
    ImageConditionParameters, ImageConditionTroubleshooting, HotkeysAndConfiguration
}

internal sealed record HelpSection(string Heading, string Body);
internal sealed record HelpPage(HelpTopic Topic, string Title, string Summary, IReadOnlyList<HelpSection> Sections);
internal sealed record HelpCatalog(string Title, string NavigationTitle, string SearchPlaceholder,
    string SearchNoResults, string CloseText, string ConditionGuideText, IReadOnlyList<HelpPage> Pages);

internal static partial class HelpContent
{
    private static readonly IReadOnlyDictionary<string, HelpCatalog> Catalogs = CreateCatalogs();

    internal static HelpCatalog GetCatalog(string language) =>
        Catalogs.TryGetValue(language, out var catalog) ? catalog : Catalogs["en-US"];

    private static HelpPage Page(HelpTopic topic, string title, string summary, params HelpSection[] sections) =>
        new(topic, title, summary, Array.AsReadOnly(sections));

    private static Dictionary<string, HelpCatalog> CreateCatalogs()
    {
        var catalogs = new Dictionary<string, HelpCatalog>();
        catalogs["zh-CN"] = new("使用指南", "按功能查看", "搜索功能、模板、阈值…", "没有找到相关教程，请换个关键词或清空搜索。", "关闭", "识图条件点击指南", Array.AsReadOnly(new[]
        {
            Page(HelpTopic.QuickStart, "快速上手", "把教程放在程序旁边，边看边操作，不必关闭教程。",
                new("先完成一次普通点击", "1. 点击主窗口的“添加”，拖出实际要点击的矩形区域并命名。\n2. 设置点击间隔（毫秒），选择顺序循环、随机或点击一次。\n3. 点击“开始点击”。用“停止点击”或 ESC 随时停止。\n点击位置会在所选矩形内随机选取。"),
                new("想等画面出现后再点击？", "在已有点击区域上右键选择“编辑条件”，也可双击该项。左侧“识图：从零配置”会逐步带你设置检测区域和模板。识图设置窗口也有直达指南的入口。")),
            Page(HelpTopic.AreasAndButtons, "点击区域与鼠标按键", "点击区域决定点哪里；它不必与识图检测区重叠。",
                new("添加与整理", "点击“添加”，在可见桌面上按住左键拖出矩形，再命名。可以使用多个显示器上的区域。选中列表项后可移除、上移或下移；再次点击选中的项目可取消选择。区域内的实际点击位置随机产生。"),
                new("左右键与区域框", "右键菜单可为每项选择左键 [L] 或右键 [R]。开启“显示区域框”后，蓝色表示悬停或选中的区域，绿色表示当前点击区域。包含识图条件的任务运行时会隐藏区域框，避免边框和文字影响识别。SendInput 也是合成输入，不是真实硬件点击。")),
            Page(HelpTopic.TimingAndOrder, "时间间隔与执行顺序", "先了解点击节奏，再安排多个任务。",
                new("间隔和抖动", "点击间隔使用毫秒：1000 毫秒等于 1 秒。抖动会在基础间隔上随机加减。总和为负时取绝对值，最短等待为 10 毫秒；抖动比间隔大时，平均等待可能变长。识图检测间隔是另一项设置，不等于点击间隔。"),
                new("三个序列模式", "顺序循环：按列表执行，最后一项后回到第一项。\n随机：先随机选定一项，再执行该项。\n点击一次：按顺序执行每项一次后停止。\n遇到条件点击时，会等待当前项满足条件才点击并进入下一项；随机模式也会等待选中的项，不会因为没找到图片就重新抽选或跳过。")),
            Page(HelpTopic.ImageConditionSetup, "识图：从零配置", "例子：左上角出现“完成”图标后，点击右下角的“下一步”。教程可以保持打开。",
                new("先分清三个对象", "点击区域 = 最后点哪里，例如右下角“下一步”。\n检测区域 = 去哪里寻找画面，例如左上角的状态栏。\n模板图片 = 要找什么，例如一小块“完成”图标。\n三个对象分别设置。点击区域与检测区域可以不重叠，也可以在不同显示器上；模板必须能放进检测区域。"),
                new("按这八步操作", "1. 添加点击区域：框住“下一步”，命名为容易记住的名称。\n2. 在列表中右键选择“编辑条件”，或双击该项，然后勾选“启用画面条件”。\n3. 点“框选检测区域”，框住预计出现“完成”图标的位置。范围尽量小，但要容纳整个模板。\n4. 点“截取模板”，只框出有特征的图标或文字；也可导入 PNG、JPG 或 BMP 图片。目标暂未出现时，可先导入已有图片。\n5. 保持默认阈值 0.90、检测间隔 200 毫秒，先不要急着改参数。\n6. 点“测试识别”。此操作不点击鼠标。看匹配分数，以及命中时预览中的绿色框是否框住正确目标。\n7. 确认后点“保存”，回主窗口设置点击间隔和序列，再点“开始点击”。\n8. 等画面出现后观察是否点击正确位置；需要结束时按 ESC 或点“停止点击”。"),
                new("截图和运行时的窗口摆放", "选区、截模板和测试识别时，教程会临时隐藏，结束后自动恢复，无须关闭。正常运行时，请把主窗口和教程放在检测区外。只能识别可见桌面，不能穿过其他窗口或读取最小化窗口。")),
            Page(HelpTopic.ImageConditionParameters, "识图：参数与触发规则", "先测试识别，再按实际画面调整；分数不是正确率。",
                new("阈值与检测间隔", "匹配阈值默认 0.90。分数达到阈值才算匹配，连续两次检测匹配才允许点击。不要把 0.90 理解为 90% 正确率。误识别时可提高阈值或换更有特征的模板。\n检测间隔默认 200 毫秒，决定多久检查一次画面；主窗口的点击间隔决定两次点击之间的等待。检测越频繁，计算开销越大；短于检测间隔的画面变化可能漏检。"),
                new("一次出现与持续点击", "“每次出现点击一次”：画面一直存在时只点一次；连续两次未匹配后重新允许触发。启动时画面已经存在，也可触发第一次点击。\n“存在期间重复点击”：画面仍匹配时可以重复执行，但仍受列表顺序、全局点击间隔和抖动控制；并非识别到的每一帧都会点击。"),
                new("等待超时", "默认最多等待当前条件 60 秒，设为 0 表示一直等待。超时会停止整个任务并提示项目名称，不会跳过该项继续点。前一次点击后的强制间隔不算入条件等待超时。停止再开始会重置识图触发状态。")),
            Page(HelpTopic.ImageConditionTroubleshooting, "识图：常见问题排查", "先用“测试识别”定位问题，再开始真正的点击。",
                new("提示没模板、尺寸错误或特征不足", "未选模板：先截取或导入图片；框选检测区不会自动生成模板。\n模板过大：图片不得超过 8 MB，最多约 400 万像素，而且宽高必须不超过检测区。截取更小的、有特征的图标或文字。\n纯色或大片空白：缺少可区分的细节，换一块包含轮廓、图案或文字的模板。"),
                new("画面存在却找不到／找错了", "确认目标真的在检测区中，且没有被主窗口、教程或其他窗口遮住；最小化目标窗口无法识别。目标缩放、DPI、主题或外观改变后，重新截取同尺寸模板。\n误识别时提高阈值、缩小检测区，或用更独特的模板。优先选择稳定的图标，避免动态数字、动画和鼠标悬停才变化的部分。"),
                new("只点一次、一直等待或太慢", "只点一次通常是默认的“每次出现点击一次”规则；想持续点击请选择对应模式。\n列表可能还在等待前一项条件；随机模式也会等待已选中的任务。查看主窗口当前项和匹配状态。\n超时会停止整个任务，可排除识别问题后再调整超时。检测慢时缩小检测范围，避免把整个桌面作为检测区。")),
            Page(HelpTopic.HotkeysAndConfiguration, "快捷键与保存配置", "保留设置，也保留随时停止的方式。",
                new("开始和停止", "默认 F6 切换开始／停止，ESC 在运行中紧急停止。可在“文件 → 热键设置”更改启停键，支持 F6–F12、Insert、Home、End、PageUp、PageDown、Pause，不要组合 Ctrl、Alt、Shift 或 Win。打开输入或选择对话框时不会用启停键意外开始任务。"),
                new("保存与加载", "先停止任务，再通过“文件”保存或加载 JSON 配置。配置保存点击区域、按键、时间、序列及识图条件；模板图片内嵌在 JSON 中，移动配置时不用另带图片。旧配置没有识图条件时仍按普通点击处理。启停热键单独保存在当前 Windows 用户设置中，不包含在 JSON 配置中。"))
        }));
        catalogs["en-US"] = new("User guide", "Browse by feature", "Search features, templates, thresholds…", "No matching topic. Try another word or clear the search.", "Close", "Image condition guide", Array.AsReadOnly(new[]
        {
            Page(HelpTopic.QuickStart, "Quick start", "Keep this guide beside the program and follow along without closing it.",
                new("Your first ordinary click", "1. Choose Add, drag a rectangle over the place to click, and name it.\n2. Set the interval in milliseconds and choose a sequence.\n3. Choose Start Clicking. Stop with the button or ESC.\nEach click uses a random position inside its rectangle."),
                new("Wait for a picture before clicking", "Right-click an existing area and choose Edit condition, or double-click it. Image: setup walks through detection and template selection. The condition editor also has a direct guide button.")),
            Page(HelpTopic.AreasAndButtons, "Click areas and mouse buttons", "A click area is the destination, independent of the detection area.",
                new("Add and organize", "Add opens a rectangular desktop selection. Name each area. Multiple displays are supported. Select an entry to remove it or move it up or down; select it again to clear selection. The actual click position is random within the rectangle."),
                new("Buttons and outlines", "Use the context menu to choose left [L] or right [R] per area. With outlines enabled, blue identifies the hovered or selected area and green the current click area. Outlines are hidden during sessions containing image conditions to avoid recognition interference. SendInput still produces synthetic input, not physical hardware input.")),
            Page(HelpTopic.TimingAndOrder, "Timing and execution order", "Choose the click rhythm and how the list advances.",
                new("Interval and jitter", "Intervals are milliseconds: 1000 ms is one second. Jitter randomly adds or subtracts from the base interval. A negative total becomes its absolute value, with a 10 ms minimum. Jitter larger than the interval can increase the average wait. Detection polling is a separate setting."),
                new("Three sequences", "Loop: execute entries in order and return to the first.\nRandom: select one entry randomly, then execute it.\nOnce: execute every entry once in order, then stop.\nA conditional entry waits until its condition allows a click before advancing. Random mode waits for its selected entry too; a missing picture does not cause a redraw or skip.")),
            Page(HelpTopic.ImageConditionSetup, "Image: setup from scratch", "Example: a Done icon at the upper left triggers a Next button at the lower right. Keep the guide open beside the app.",
                new("Three independent choices", "Click area = where to click, such as Next.\nDetection area = where to look, such as the status panel.\nTemplate = what to look for, such as a distinctive Done icon.\nThe click and detection areas need not overlap and can be on different displays. The template must fit inside the detection area."),
                new("Eight steps", "1. Add the click rectangle around Next and give it a name.\n2. Right-click the entry and choose Edit condition, or double-click it; enable the image condition.\n3. Select the detection area around the expected Done icon. Keep it small but large enough for the template.\n4. Capture a distinctive icon or text as the template, or import PNG, JPG or BMP. An existing image can be imported before the target appears.\n5. Start with threshold 0.90 and polling 200 ms.\n6. Choose Test recognition. This sends no mouse clicks. Check the score and whether the green preview box marks the correct target when matched.\n7. Save, return to the main window, choose click timing and sequence, and start.\n8. Check the click destination when the picture appears. Stop with ESC or Stop Clicking."),
                new("Keep the desktop visible", "The guide temporarily hides during selection, template capture and recognition testing, then returns automatically. During a running session, move both the app and guide outside the detection area. Only the visible desktop is recognized; covered or minimized windows cannot be read.")),
            Page(HelpTopic.ImageConditionParameters, "Image: parameters and triggers", "Test first, then tune to your screen. A score is not a probability of correctness.",
                new("Threshold and polling", "The default threshold is 0.90. Two consecutive matching checks are needed before clicking. A score of 0.90 does not mean 90% accuracy. Raise the threshold or choose a more distinctive template for false matches.\nPolling defaults to 200 ms and controls how often the screen is checked. The main click interval controls time between clicks. Faster polling costs more CPU; appearances shorter than a polling interval can be missed."),
                new("Once or repeat", "Once per appearance clicks once while the picture remains present. Two consecutive misses rearm it. A picture already present at startup can trigger the first click.\nRepeat while visible permits more clicks while matched, subject to the list sequence, global interval and jitter. It does not click on every detected frame."),
                new("Timeout", "The default condition wait is 60 seconds. Zero means unlimited. A timeout stops the entire session and names the entry; it never skips ahead. Mandatory cooldown after the previous click is excluded. Stopping and restarting resets the appearance state.")),
            Page(HelpTopic.ImageConditionTroubleshooting, "Image: troubleshooting", "Use recognition testing to diagnose before enabling real clicks.",
                new("Missing or unsuitable template", "Selecting a detection area does not create a template: capture or import one separately. The template must fit in the detection rectangle, be at most 8 MB and about four million pixels, and contain distinctive details. Avoid plain colour or large blank regions; use an icon or text."),
                new("Target not found or a wrong match", "Confirm the target is inside the detection area and uncovered by the app, guide or other windows. Restore minimized windows. Recapture after DPI, zoom, theme or appearance changes; matching uses the original template size. Raise the threshold or use a smaller detection area and a more distinctive, stable template for false matches. Avoid changing counters, animation and hover effects."),
                new("One click, endless waiting or slowness", "One click while the image stays visible is expected in Once per appearance mode. Choose Repeat for repeated clicks. The list may still be waiting on an earlier entry; Random also waits for the chosen entry. Check the current item and score. A timeout stops the session. Reduce detection size to improve speed rather than scanning the whole desktop.")),
            Page(HelpTopic.HotkeysAndConfiguration, "Hotkeys and configurations", "Save your setup and keep a stop action available.",
                new("Start and stop", "F6 toggles start/stop by default; ESC stops an active session. File → Hotkey settings supports F6–F12, Insert, Home, End, PageUp, PageDown and Pause without Ctrl, Alt, Shift or Win. Input and selection dialogs guard against accidental starts."),
                new("Save and load", "Stop before saving or loading JSON through File. It stores areas, mouse buttons, timing, sequence and image conditions. Templates are embedded, so no separate image files are required. Older configurations without conditions remain ordinary clicks. The global hotkey is saved separately in the Windows user's preferences, not in JSON."))
        }));
        AddAsianLanguages(catalogs);
        AddOtherLanguages(catalogs);
        return catalogs;
    }

    private static partial void AddOtherLanguages(Dictionary<string, HelpCatalog> catalogs);

    private static void AddAsianLanguages(Dictionary<string, HelpCatalog> catalogs)
    {
        catalogs["zh-TW"] = new("使用指南", "按功能瀏覽", "搜尋功能、範本、門檻…", "找不到相關教學，請換關鍵字或清空搜尋。", "關閉", "影像條件點擊指南", Array.AsReadOnly(new[]
        {
            Page(HelpTopic.QuickStart,"快速上手","將教學放在程式旁，邊看邊操作，不必關閉。",new("第一次點擊","1. 按新增並框選要點擊的位置，再命名。\n2. 設定毫秒間隔及順序。\n3. 開始點擊；用停止或 ESC 結束。點擊位置會在矩形內隨機選取。"),new("等待影像再點擊","在區域上按右鍵選編輯條件，或連按兩下。左側影像設定教學和條件視窗中的指南可帶您完成設定。")),
            Page(HelpTopic.AreasAndButtons,"點擊區域與滑鼠按鍵","點擊位置與偵測位置可以分開。",new("管理區域","新增後拖出矩形並命名，可使用多個螢幕。選取後可移除、上移或下移，再次點選可取消。實際位置在矩形內隨機產生。"),new("按鍵與邊框","右鍵選單可設定左鍵 [L] 或右鍵 [R]。顯示區域框時，藍色標示懸停或選取項目，綠色標示目前點擊區域。含影像條件的工作會隱藏邊框以免干擾。SendInput 仍是合成輸入。")),
            Page(HelpTopic.TimingAndOrder,"時間與執行順序","設定點擊節奏和項目順序。",new("間隔與抖動","1000 毫秒為 1 秒。抖動隨機加減基本間隔，負總和取絕對值，最短等待 10 毫秒。抖動大於間隔可能使平均等待變長。偵測間隔是獨立設定。"),new("三種模式","循環依序執行後回到開頭；隨機先選一項再等待它；一次依序執行後停止。條件不符不會略過項目，隨機也不會重新抽選。")),
            Page(HelpTopic.ImageConditionSetup,"影像：從零設定","例如左上角出現完成圖示後，點擊右下角下一步。教學可保持開啟。",new("三個不同對象","點擊區域是最後點哪裡；偵測區域是去哪裡找；範本圖片是要找什麼。兩區可以不重疊，也可位於不同螢幕，範本必須放得進偵測區。"),new("八個步驟","1. 新增下一步按鈕的點擊區域並命名。\n2. 右鍵編輯條件或連按兩下，啟用影像條件。\n3. 框選預期圖示出現的偵測區。\n4. 擷取有特徵的小圖示或文字，或匯入 PNG、JPG、BMP；目標未出現也可先匯入圖片。\n5. 先用預設 0.90 門檻及 200 毫秒。\n6. 測試辨識不會點擊，請看分數與命中時的綠框。\n7. 儲存並回主視窗設定間隔、順序後開始。\n8. 觀察點擊位置，用 ESC 或停止結束。"),new("視窗擺放","選區、擷取和測試時教學會暫時隱藏，結束後自動恢復。正常執行時將程式和教學移出偵測區；只讀取可見桌面，無法辨識遮住或最小化的視窗。")),
            Page(HelpTopic.ImageConditionParameters,"影像：參數與觸發","先測試，再依實際畫面調整。",new("門檻與偵測間隔","預設 0.90 是比對分數，不是 90% 正確率。連續兩次符合才允許點擊。誤認時提高門檻或換更獨特範本。偵測預設 200 毫秒，與主視窗的點擊間隔不同；越頻繁越耗 CPU，過短的畫面可能漏掉。"),new("觸發和逾時","每次出現一次：圖像持續存在只點一次，連續兩次不符合才重新允許；開始時已存在也可首次觸發。存在時重複仍受順序、全域間隔及抖動限制。預設等候 60 秒，0 為無限；逾時停止整個工作，不計前一點擊的強制冷卻。重新開始會重設觸發狀態。")),
            Page(HelpTopic.ImageConditionTroubleshooting,"影像：常見問題","先用測試辨識排查，不發送點擊。",new("範本或辨識失敗","先擷取或匯入範本，框偵測區不會建立範本。上限 8 MB、約 400 萬像素且必須小於偵測區。避免純色和空白。檢查遮擋或最小化，縮放、DPI、主題改變後重新擷取同尺寸範本。"),new("誤認、等待或慢","提高門檻、縮小偵測區並用穩定且獨特的圖案。一直存在只點一次是預設模式，想連點請選存在時重複。可能仍在等待前一項或隨機選中的項目；逾時會停止工作。縮小範圍能減少計算。")),
            Page(HelpTopic.HotkeysAndConfiguration,"快捷鍵與設定檔","保留設定，也能隨時停止。",new("啟停按鍵","預設 F6 切換啟停，ESC 緊急停止。檔案中的熱鍵設定支援 F6–F12、Insert、Home、End、PageUp、PageDown、Pause，不搭配 Ctrl、Alt、Shift、Win。"),new("儲存與載入","先停止後再儲存或載入 JSON，內含區域、按鍵、時間、順序、條件與範本，移動時不必另帶圖片。舊設定仍正常點擊。啟停熱鍵另存在 Windows 使用者偏好，不在 JSON 中。"))
        }));
        catalogs["ja-JP"] = new("使い方ガイド", "機能から探す", "機能・テンプレート・しきい値を検索…", "該当する項目がありません。検索語を変えるか消してください。", "閉じる", "画像条件クリックのガイド", Array.AsReadOnly(new[]
        {
            Page(HelpTopic.QuickStart,"はじめに","ガイドをアプリの横に置き、開いたまま操作できます。",new("最初のクリック","1. 追加でクリック先の矩形を選び、名前を付けます。\n2. ミリ秒の間隔と順序を設定します。\n3. 開始し、停止ボタンまたは ESC で終了します。矩形内の位置はランダムです。"),new("画像を待つには","登録済み領域を右クリックして条件を編集するか、ダブルクリックします。画像設定の項目で手順を確認できます。条件画面にもガイドへの入口があります。")),
            Page(HelpTopic.AreasAndButtons,"領域とマウスボタン","クリック先と画像を探す場所は別々に設定できます。",new("領域の管理","追加で矩形をドラッグし、名前を付けます。複数画面に対応します。選択して削除・上へ・下へ移動でき、再クリックで選択を解除します。クリック位置は領域内でランダムです。"),new("ボタンと枠","右クリックメニューで左 [L]・右 [R] を選択します。枠表示では青が選択やホバー、緑が現在のクリック領域です。画像条件を含む実行中は認識への干渉を避けるため枠を隠します。SendInput も合成入力です。")),
            Page(HelpTopic.TimingAndOrder,"間隔と実行順序","クリックの速さと順序を設定します。",new("間隔と揺らぎ","1000 ミリ秒は 1 秒です。揺らぎを基本間隔にランダムに加減し、負の合計は絶対値にします。最短は 10 ミリ秒です。揺らぎが大きいと平均待機が長くなる場合があります。画像の確認間隔とは別です。"),new("順序の動作","ループは順番に繰り返し、ランダムは一つ選んでから実行、一回は順番に実行して停止します。条件が不成立ならその項目を待ちます。ランダムでも再抽選やスキップはしません。")),
            Page(HelpTopic.ImageConditionSetup,"画像：設定手順","例：左上の完了アイコンを見つけて、右下の次へをクリック。ガイドは開いたまま使えます。",new("三つの設定","クリック領域は押す場所、検出領域は探す範囲、テンプレートは探す画像です。二つの領域は重ならなくても別画面でも構いません。テンプレートは検出領域内に収まる必要があります。"),new("八つの手順","1. 次へボタンのクリック領域を追加して命名。\n2. 右クリックの条件編集またはダブルクリックで画像条件を有効に。\n3. アイコンが出る検出範囲を選択。\n4. 特徴のある小さいアイコンや文字を撮影、または PNG・JPG・BMP を読込。まだ表示されていない画像も読込可能。\n5. まず既定の 0.90 と 200 ミリ秒を使用。\n6. 認識テストはクリックしません。スコアと一致時の緑枠を確認。\n7. 保存して主画面で間隔と順序を設定し開始。\n8. クリック先を確認し ESC または停止で終了。"),new("画面を隠さない","領域選択・撮影・認識テスト中はガイドが一時的に消え、終了後に戻ります。通常実行時はアプリとガイドを検出範囲外へ移動してください。見えているデスクトップだけを読み、隠れた画面や最小化した画面は認識できません。")),
            Page(HelpTopic.ImageConditionParameters,"画像：パラメーターと発火","先にテストし、画面に合わせて調整します。",new("しきい値と確認間隔","既定の 0.90 はスコアであり、正解率90%ではありません。連続2回一致してからクリック可能です。誤認時はしきい値を上げるか特徴のある画像に変更します。確認間隔は200ミリ秒で、クリック間隔とは別です。頻度を上げるとCPU負荷が増え、短い表示は見逃す場合があります。"),new("一回・繰り返し・待機","出現ごとに一回は表示中一度だけクリックし、連続2回不一致で再発火可能になります。開始時に表示済みでも最初は発火します。表示中の繰り返しも順序と全体間隔・揺らぎに従います。待機は既定60秒、0は無制限。時間切れは全体停止です。前のクリックのクールダウンは待機に含まず、再開始で状態をリセットします。")),
            Page(HelpTopic.ImageConditionTroubleshooting,"画像：問題を解決する","クリックしない認識テストから確認してください。",new("画像なし・大きすぎる・一致しない","検出領域を選んでもテンプレートは作られません。別途撮影か読込が必要です。最大8MB・約400万画素で検出範囲より小さくしてください。単色や空白を避けます。遮蔽と最小化を確認し、倍率・DPI・テーマ変更後は同じサイズで撮影し直します。"),new("誤認・一回だけ・遅い","しきい値を上げ、検出範囲を絞り、安定した特徴のある画像を使います。一回だけは既定の発火方式です。繰り返す場合は表示中の繰り返しを選びます。前の項目やランダムで選んだ項目を待っている場合もあります。時間切れは全体停止。範囲を狭めると負荷を減らせます。")),
            Page(HelpTopic.HotkeysAndConfiguration,"ショートカットと設定保存","停止方法を確保して設定を保存します。",new("開始と停止","既定F6で開始・停止、ESCで緊急停止します。ファイルのキー設定ではF6–F12、Insert、Home、End、PageUp、PageDown、Pauseを使用でき、Ctrl・Alt・Shift・Winは組み合わせません。"),new("保存と読込","停止してからJSONを保存・読込します。領域・ボタン・時間・順序・条件と画像を内蔵するため別画像は不要です。旧設定は通常クリックとして使えます。開始キーはWindowsユーザー設定に別保存され、JSONには含まれません。"))
        }));
        catalogs["ko-KR"] = new("사용 가이드", "기능별 보기", "기능, 템플릿, 임계값 검색…", "관련 항목이 없습니다. 검색어를 바꾸거나 지워 주세요.", "닫기", "이미지 조건 클릭 가이드", Array.AsReadOnly(new[]
        {
            Page(HelpTopic.QuickStart,"빠른 시작","가이드를 프로그램 옆에 두고 닫지 않은 채 따라 하세요.",new("첫 번째 클릭","1. 추가를 눌러 클릭할 사각형을 지정하고 이름을 정합니다.\n2. 밀리초 간격과 순서를 설정합니다.\n3. 시작한 뒤 중지 버튼이나 ESC로 끝냅니다. 실제 위치는 영역 안에서 무작위입니다."),new("이미지를 기다려 클릭하기","등록된 영역을 오른쪽 클릭하여 조건을 편집하거나 두 번 클릭합니다. 이미지 설정 항목에 자세한 절차가 있으며 조건 창에도 가이드 버튼이 있습니다.")),
            Page(HelpTopic.AreasAndButtons,"클릭 영역과 마우스 버튼","클릭 위치와 이미지를 찾는 위치는 별개입니다.",new("영역 관리","추가 후 사각형을 드래그하고 이름을 지정합니다. 여러 모니터를 사용할 수 있습니다. 선택한 항목을 제거하거나 위아래로 이동하고 다시 클릭하면 선택이 해제됩니다. 클릭 위치는 영역 안에서 무작위로 정합니다."),new("버튼과 테두리","오른쪽 메뉴에서 왼쪽 [L] 또는 오른쪽 [R] 버튼을 고릅니다. 테두리 표시 시 파란색은 선택 또는 가리킨 영역, 초록색은 현재 클릭 영역입니다. 이미지 조건 실행 중에는 인식 방해를 막기 위해 테두리를 숨깁니다. SendInput도 합성 입력입니다.")),
            Page(HelpTopic.TimingAndOrder,"간격과 실행 순서","클릭 속도와 목록 순서를 정합니다.",new("간격과 변동","1000밀리초는 1초입니다. 기본 간격에 변동을 무작위로 더하거나 빼며 음수 합계는 절댓값으로 바꿉니다. 최소 대기는 10밀리초입니다. 변동이 간격보다 크면 평균 대기가 길어질 수 있습니다. 감지 간격과 클릭 간격은 다릅니다."),new("세 가지 순서","반복은 목록을 순서대로 반복하고, 무작위는 한 항목을 먼저 고른 뒤 실행하며, 한 번은 순서대로 한 번씩 실행하고 멈춥니다. 조건이 맞지 않으면 해당 항목을 기다립니다. 무작위도 다시 뽑거나 건너뛰지 않습니다.")),
            Page(HelpTopic.ImageConditionSetup,"이미지: 처음부터 설정","예: 왼쪽 위 완료 아이콘이 나타나면 오른쪽 아래 다음을 클릭합니다. 가이드는 열어 둘 수 있습니다.",new("세 가지 독립 설정","클릭 영역은 누를 위치, 감지 영역은 찾을 범위, 템플릿은 찾을 그림입니다. 두 영역은 겹칠 필요가 없으며 서로 다른 모니터여도 됩니다. 템플릿 크기는 감지 영역 안에 들어가야 합니다."),new("여덟 단계","1. 다음 버튼의 클릭 영역을 추가하고 이름을 정합니다.\n2. 오른쪽 메뉴의 조건 편집 또는 두 번 클릭으로 이미지 조건을 켭니다.\n3. 아이콘이 나타날 감지 영역을 지정합니다.\n4. 특징이 있는 작은 아이콘이나 글자를 캡처하거나 PNG, JPG, BMP를 가져옵니다. 아직 보이지 않는 그림도 미리 가져올 수 있습니다.\n5. 기본값 0.90과 200밀리초로 시작합니다.\n6. 인식 테스트는 클릭하지 않습니다. 점수와 일치 시 초록 테두리가 올바른지 봅니다.\n7. 저장하고 주 창에서 클릭 간격과 순서를 정해 시작합니다.\n8. 클릭 위치를 확인하고 ESC 또는 중지로 끝냅니다."),new("창 배치","영역 선택, 캡처, 인식 테스트 때 가이드가 잠시 숨겨졌다가 자동으로 돌아옵니다. 실제 실행 중에는 프로그램과 가이드를 감지 영역 밖에 두세요. 보이는 바탕 화면만 읽으며 가려지거나 최소화된 창은 인식할 수 없습니다.")),
            Page(HelpTopic.ImageConditionParameters,"이미지: 매개변수와 트리거","먼저 테스트하고 실제 화면에 맞춰 조절하세요.",new("임계값과 감지 간격","기본 0.90은 점수이며 정확도90%가 아닙니다. 연속 두 번 일치해야 클릭합니다. 잘못 인식하면 값을 높이거나 독특한 템플릿을 사용하세요. 기본 감지는 200밀리초이며 클릭 간격과 별개입니다. 자주 확인하면 CPU 부담이 늘고 매우 짧은 화면은 놓칠 수 있습니다."),new("한 번, 반복, 시간 초과","나타날 때 한 번은 계속 보이는 동안 한 번만 클릭하고 연속 두 번 불일치하면 다시 허용합니다. 시작할 때 이미 보여도 첫 클릭은 가능합니다. 보이는 동안 반복도 목록 순서와 전역 간격, 변동을 따릅니다. 기본 대기는60초, 0은 무제한입니다. 초과 시 전체 작업이 멈춥니다. 이전 클릭의 필수 대기시간은 제외하며 다시 시작하면 상태가 초기화됩니다.")),
            Page(HelpTopic.ImageConditionTroubleshooting,"이미지: 문제 해결","실제 클릭 전에 인식 테스트로 확인하세요.",new("템플릿 없음 또는 인식 실패","감지 영역을 정해도 템플릿은 생기지 않습니다. 별도로 캡처하거나 가져오세요. 최대8MB, 약400만 픽셀이며 감지 영역보다 작아야 합니다. 단색이나 빈 공간은 피하세요. 가림과 최소화를 확인하고 배율, DPI, 테마 변경 후 같은 크기로 다시 캡처하세요."),new("오인식, 한 번만 클릭, 느림","임계값을 높이거나 감지 범위를 좁히고 안정적인 특징의 그림을 쓰세요. 한 번만 클릭하는 것은 기본 모드의 정상 동작입니다. 연속 클릭은 보이는 동안 반복을 선택하세요. 앞 항목이나 무작위로 선택된 항목을 기다릴 수도 있습니다. 시간 초과는 전체를 중지합니다. 범위를 줄이면 처리가 빨라집니다.")),
            Page(HelpTopic.HotkeysAndConfiguration,"단축키와 설정 저장","설정을 보관하고 언제든 멈출 수 있게 하세요.",new("시작과 중지","기본 F6로 시작과 중지를 전환하고 ESC로 긴급 중지합니다. 파일의 단축키 설정에서 F6–F12, Insert, Home, End, PageUp, PageDown, Pause를 쓸 수 있으며 Ctrl, Alt, Shift, Win과 조합하지 않습니다."),new("저장과 불러오기","중지한 뒤 JSON을 저장하거나 불러오세요. 영역, 버튼, 시간, 순서, 조건과 템플릿이 내장되어 별도 그림 파일이 필요 없습니다. 이전 설정은 일반 클릭으로 유지됩니다. 시작 키는 Windows 사용자 설정에 따로 보관되며 JSON에 포함되지 않습니다."))
        }));
    }
}
