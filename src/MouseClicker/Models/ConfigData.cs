using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MouseClicker
{
    // 用于存储、验证和序列化应用程序的配置。
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

        // 缺少该字段的旧配置默认保留当前点击区域高亮。
        public bool HighlightCurrentArea { get; set; } = true;

        // 是否启用 SendInput 附加标记模式。
        public bool HardwareMode { get; set; }

        /// <summary>
        /// 验证配置是否符合当前 UI 支持的范围。
        /// </summary>
        public void Validate(
            decimal minimumInterval,
            decimal maximumInterval,
            decimal minimumJitter,
            decimal maximumJitter,
            int sequenceCount)
        {
            if (ClickAreas == null)
            {
                throw new InvalidDataException("Configuration must contain a click-area list.");
            }

            if (Interval < minimumInterval || Interval > maximumInterval)
            {
                throw new InvalidDataException("Configuration interval is outside the supported range.");
            }

            if (Jitter < minimumJitter || Jitter > maximumJitter)
            {
                throw new InvalidDataException("Configuration jitter is outside the supported range.");
            }

            if (sequenceCount <= 0 || SequenceIndex < 0 || SequenceIndex >= sequenceCount)
            {
                throw new InvalidDataException("Configuration sequence index is invalid.");
            }

            if (ClickAreas.Any(area =>
                area == null ||
                area.Area.Width <= 0 ||
                area.Area.Height <= 0 ||
                (long)area.Area.X + area.Area.Width > int.MaxValue ||
                (long)area.Area.Y + area.Area.Height > int.MaxValue ||
                area.Label == null))
            {
                throw new InvalidDataException("Configuration contains an invalid click area.");
            }
        }
    }
}
