using System.Collections.Generic;

namespace MouseClicker
{
    // 用于存储和序列化应用程序的配置。
    public class ConfigData
    {
        // 定义的所有点击区域的列表。
        public List<ClickArea> ClickAreas { get; set; } = new List<ClickArea>();

        // 点击之间的时间间隔（毫秒）。
        public decimal Interval { get; set; }

        // 时间间隔的随机抖动范围（毫秒）。
        public decimal Jitter { get; set; }

        // 点击序列模式的索引。
        public int SequenceIndex { get; set; }

        // 是否显示点击区域的覆盖层。
        public bool ShowOverlay { get; set; }

        // 是否启用硬件模拟模式。
        public bool HardwareMode { get; set; }
    }
}
