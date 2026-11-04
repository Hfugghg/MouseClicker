using System.Globalization;

namespace MouseClicker.Tests;

/// <summary>在独立进程中验证设备语言的首次初始化，避免影响 WPF 的程序集资源缓存。</summary>
internal static class LocalizationInitializationProbe
{
    private static int Main(string[] args)
    {
        if (args.Length != 2 || args[0] != "--localization-probe") return 2;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(args[1]);
            // 此前没有访问过生产类型，读取字段将实际执行其静态初始化器。
            Console.Write(Localization.Language);
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
    }
}
