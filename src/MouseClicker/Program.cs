namespace MouseClicker;

static class Program
{
    /// <summary>
    /// 应用程序的主入口点。
    /// </summary>
    [STAThread]
    static void Main()
    {
        // 若要自定义应用程序配置（例如设置高 DPI 设置或默认字体），
        // 请参阅 https://aka.ms/applicationconfiguration。
        ApplicationConfiguration.Initialize();
        Application.Run(new Form1());
    }    
}