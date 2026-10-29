using System.Collections.Generic;
using System.Drawing;
using System.Text.Json;
using Xunit;

namespace MouseClicker.Tests
{
    public class ConfigDataTests
    {
        [Fact]
        public void DetectionOverlayDefaultsOffForOldConfigurationsAndRoundTripsIndependently()
        {
            var old = JsonSerializer.Deserialize<ConfigData>("{\"ShowOverlay\":true}")!;
            Assert.False(old.ShowDetectionOverlay);
            var restored = JsonSerializer.Deserialize<ConfigData>(JsonSerializer.Serialize(
                new ConfigData { ShowOverlay = false, ShowDetectionOverlay = true }))!;
            Assert.False(restored.ShowOverlay);
            Assert.True(restored.ShowDetectionOverlay);
        }

        // ──────────────────────────────────────
        // 1. 新建 ConfigData 的默认值
        // ──────────────────────────────────────
        [Fact]
        public void NewConfigData_HasDefaultValues()
        {
            var config = new ConfigData();

            // 集合默认为非 null 的空列表
            Assert.NotNull(config.ClickAreas);
            Assert.Empty(config.ClickAreas);

            // 数字 / 枚举字段默认为零
            Assert.Equal(0m, config.Interval);
            Assert.Equal(0m, config.Jitter);
            Assert.Equal(0, config.SequenceIndex);

            // 现有开关默认关闭，当前区域高亮默认开启。
            Assert.False(config.ShowOverlay);
            Assert.False(config.HardwareMode);
            Assert.True(config.HighlightCurrentArea);
        }

        // ──────────────────────────────────────
        // 2. 序列化 – 验证 JSON 结构
        // ──────────────────────────────────────
        [Fact]
        public void Serialize_WithData_ProducesCorrectJsonStructure()
        {
            var config = new ConfigData
            {
                ClickAreas = new List<ClickArea>
                {
                    new ClickArea
                    {
                        Area = new Rectangle(10, 20, 100, 50),
                        Label = "Button1"
                    },
                    new ClickArea
                    {
                        Area = new Rectangle(200, 300, 80, 40),
                        Label = "Button2"
                    }
                },
                Interval = 500m,
                Jitter = 50m,
                SequenceIndex = 1,
                ShowOverlay = true,
                HardwareMode = false
            };

            string json = JsonSerializer.Serialize(config);

            // 每个属性都应出现在输出中
            Assert.Contains("\"ClickAreas\"", json);
            Assert.Contains("\"Interval\"", json);
            Assert.Contains("\"Jitter\"", json);
            Assert.Contains("\"SequenceIndex\"", json);
            Assert.Contains("\"ShowOverlay\"", json);
            Assert.Contains("\"HardwareMode\"", json);

            // 值已正确编码
            Assert.Contains("\"Label\":\"Button1\"", json);
            Assert.Contains("\"Label\":\"Button2\"", json);
            Assert.Contains("\"Interval\":500", json);
            Assert.Contains("\"Jitter\":50", json);
            Assert.Contains("\"SequenceIndex\":1", json);
            Assert.Contains("\"ShowOverlay\":true", json);
            Assert.Contains("\"HardwareMode\":false", json);

            // Rectangle 字段存在（PascalCase，默认策略）
            Assert.Contains("\"X\":10", json);
            Assert.Contains("\"Y\":20", json);
            Assert.Contains("\"Width\":100", json);
            Assert.Contains("\"Height\":50", json);
        }

        // ──────────────────────────────────────
        // 3. 反序列化 – 验证所有属性
        // ──────────────────────────────────────
        [Fact]
        public void Deserialize_ValidJson_RestoresAllProperties()
        {
            string json = @"
{
    ""ClickAreas"": [
        {
            ""Area"": { ""X"": 10, ""Y"": 20, ""Width"": 100, ""Height"": 50 },
            ""Label"": ""Button1""
        },
        {
            ""Area"": { ""X"": 200, ""Y"": 300, ""Width"": 80, ""Height"": 40 },
            ""Label"": ""Button2""
        }
    ],
    ""Interval"": 1000,
    ""Jitter"": 200,
    ""SequenceIndex"": 2,
    ""ShowOverlay"": true,
    ""HardwareMode"": true
}";

            var config = JsonSerializer.Deserialize<ConfigData>(json);

            Assert.NotNull(config);

            // 点击区域
            Assert.Equal(2, config.ClickAreas.Count);

            Assert.Equal(new Rectangle(10, 20, 100, 50), config.ClickAreas[0].Area);
            Assert.Equal("Button1", config.ClickAreas[0].Label);

            Assert.Equal(new Rectangle(200, 300, 80, 40), config.ClickAreas[1].Area);
            Assert.Equal("Button2", config.ClickAreas[1].Label);

            // 标量设置
            Assert.Equal(1000m, config.Interval);
            Assert.Equal(200m, config.Jitter);
            Assert.Equal(2, config.SequenceIndex);
            Assert.True(config.ShowOverlay);
            Assert.True(config.HardwareMode);
        }

        // ──────────────────────────────────────
        // 4. 往返：序列化 → 反序列化
        // ──────────────────────────────────────
        [Fact]
        public void RoundTrip_SerializeThenDeserialize_ProducesEquivalentObject()
        {
            var original = new ConfigData
            {
                ClickAreas = new List<ClickArea>
                {
                    new ClickArea
                    {
                        Area = new Rectangle(5, 10, 200, 100),
                        Label = "AreaA"
                    },
                    new ClickArea
                    {
                        Area = new Rectangle(500, 600, 150, 75),
                        Label = "AreaB"
                    }
                },
                Interval = 2500m,
                Jitter = 500m,
                SequenceIndex = 0,
                ShowOverlay = false,
                HardwareMode = true
            };

            string json = JsonSerializer.Serialize(original);
            var roundTripped = JsonSerializer.Deserialize<ConfigData>(json);

            Assert.NotNull(roundTripped);

            // 顶层标量
            Assert.Equal(original.Interval, roundTripped.Interval);
            Assert.Equal(original.Jitter, roundTripped.Jitter);
            Assert.Equal(original.SequenceIndex, roundTripped.SequenceIndex);
            Assert.Equal(original.ShowOverlay, roundTripped.ShowOverlay);
            Assert.Equal(original.HardwareMode, roundTripped.HardwareMode);

            // 点击区域
            Assert.Equal(original.ClickAreas.Count, roundTripped.ClickAreas.Count);

            for (int i = 0; i < original.ClickAreas.Count; i++)
            {
                Assert.Equal(original.ClickAreas[i].Label, roundTripped.ClickAreas[i].Label);
                Assert.Equal(original.ClickAreas[i].Area, roundTripped.ClickAreas[i].Area);
            }
        }

        // ──────────────────────────────────────
        // 5. HardwareMode 为 true
        // ──────────────────────────────────────
        [Fact]
        public void Deserialize_HardwareModeTrue_ReturnsTrue()
        {
            string json = @"{ ""HardwareMode"": true }";

            var config = JsonSerializer.Deserialize<ConfigData>(json);

            Assert.NotNull(config);
            Assert.True(config.HardwareMode);
        }

        // ──────────────────────────────────────
        // 6. 缺少 HardwareMode（向后兼容）
        // ──────────────────────────────────────
        [Fact]
        public void Deserialize_MissingHardwareMode_DefaultsToFalse()
        {
            string json = @"{ ""Interval"": 100, ""ShowOverlay"": true }";

            var config = JsonSerializer.Deserialize<ConfigData>(json);

            Assert.NotNull(config);
            Assert.False(config.HardwareMode);
        }

        // ──────────────────────────────────────
        // 7. 空 JSON 对象 → 所有字段使用默认值
        // ──────────────────────────────────────
        [Fact]
        public void Deserialize_EmptyJsonObject_ReturnsDefaultValues()
        {
            string json = @"{}";

            var config = JsonSerializer.Deserialize<ConfigData>(json);

            Assert.NotNull(config);
            Assert.NotNull(config.ClickAreas);
            Assert.Empty(config.ClickAreas);
            Assert.Equal(0m, config.Interval);
            Assert.Equal(0m, config.Jitter);
            Assert.Equal(0, config.SequenceIndex);
            Assert.False(config.ShowOverlay);
            Assert.False(config.HardwareMode);
        }

        // ──────────────────────────────────────
        // 7b. JSON 为 null → 应返回 null
        // ──────────────────────────────────────
        [Fact]
        public void Deserialize_NullJson_ReturnsNull()
        {
            string json = "null";

            var config = JsonSerializer.Deserialize<ConfigData>(json);

            Assert.Null(config);
        }

        // ──────────────────────────────────────
        // 8. ClickArea ToString() 格式
        // ──────────────────────────────────────
        [Theory]
        [InlineData("MyLabel", 5, 10, 100, 50, "[L] 'MyLabel' @ (X:5, Y:10, W:100, H:50)")]
        [InlineData("Btn", 0, 0, 1, 1, "[L] 'Btn' @ (X:0, Y:0, W:1, H:1)")]
        [InlineData("", 0, 0, 0, 0, "[L] '' @ (X:0, Y:0, W:0, H:0)")]
        [InlineData("OK", -5, -10, 30, 20, "[L] 'OK' @ (X:-5, Y:-10, W:30, H:20)")]
        public void ClickArea_ToString_ReturnsCorrectFormat(
            string label, int x, int y, int w, int h, string expected)
        {
            var area = new ClickArea
            {
                Label = label,
                Area = new Rectangle(x, y, w, h)
            };

            string result = area.ToString();

            Assert.Equal(expected, result);
        }

        // ──────────────────────────────────────
        // 边界：ClickArea 默认 ToString
        // ──────────────────────────────────────
        [Fact]
        public void ClickArea_ToString_DefaultValues()
        {
            var area = new ClickArea();

            string result = area.ToString();

            // 默认 Label 为 string.Empty，默认 Area 为 Rectangle.Empty
            Assert.Equal("[L] '' @ (X:0, Y:0, W:0, H:0)", result);
        }

        // ──────────────────────────────────────
        // 边界：JSON 中 ClickAreas 列表为 null
        //   （Form1.cs 在加载时用 ?? 做了保护，
        //    但原始反序列化对象应保留 JSON 中的 null）
        // ──────────────────────────────────────
        [Fact]
        public void Deserialize_ClickAreasNull_ListIsNull()
        {
            string json = @"{ ""ClickAreas"": null }";

            var config = JsonSerializer.Deserialize<ConfigData>(json);

            Assert.NotNull(config);
            Assert.Null(config.ClickAreas);
        }

        // ──────────────────────────────────────
        // 边界：小数 Interval / Jitter
        // ──────────────────────────────────────
        [Theory]
        [InlineData(1.5, 2.5)]
        [InlineData(999.99, 0.01)]
        [InlineData(0.0, 0.0)]
        public void Deserialize_FractionalDecimalValues_RoundTripCorrectly(
            decimal interval, decimal jitter)
        {
            var original = new ConfigData { Interval = interval, Jitter = jitter };

            string json = JsonSerializer.Serialize(original);
            var result = JsonSerializer.Deserialize<ConfigData>(json);

            Assert.NotNull(result);
            Assert.Equal(interval, result.Interval);
            Assert.Equal(jitter, result.Jitter);
        }

        [Fact]
        public void Deserialize_OldConfigurationWithoutHighlightOption_DefaultsToEnabled()
        {
            var config = JsonSerializer.Deserialize<ConfigData>("{\"Interval\":100,\"Jitter\":0,\"ShowOverlay\":true,\"HardwareMode\":false}");
            Assert.NotNull(config);
            Assert.True(config.HighlightCurrentArea);
        }

        [Fact]
        public void HighlightCurrentArea_Disabled_RoundTripsWithoutReenabling()
        {
            var original = new ConfigData { HighlightCurrentArea = false };
            string json = JsonSerializer.Serialize(original);
            var restored = JsonSerializer.Deserialize<ConfigData>(json);
            Assert.NotNull(restored);
            Assert.False(restored.HighlightCurrentArea);
        }
        [Fact]
        public void ClickArea_RightButton_RoundTripsAndOldJsonDefaultsToLeft()
        {
            var source = new ClickArea
            {
                Label = "Right",
                Area = new Rectangle(1, 2, 3, 4),
                RightButton = true
            };
            var restored = JsonSerializer.Deserialize<ClickArea>(JsonSerializer.Serialize(source));
            Assert.NotNull(restored);
            Assert.True(restored.RightButton);

            const string oldJson = "{\"Label\":\"Legacy\",\"Area\":{\"X\":1,\"Y\":2,\"Width\":3,\"Height\":4}}";
            var legacy = JsonSerializer.Deserialize<ClickArea>(oldJson);
            Assert.NotNull(legacy);
            Assert.False(legacy.RightButton);
        }

        // 配置校验独立于 WinForms 界面进行测试。
        [Fact]
        public void Validate_ValidConfiguration_DoesNotThrow()
        {
            var config = new ConfigData
            {
                ClickAreas = new List<ClickArea>
                {
                    new ClickArea { Label = "Button", Area = new Rectangle(-100, 10, 20, 30) }
                },
                Interval = 100,
                Jitter = 10,
                SequenceIndex = 1
            };

            config.Validate(10, 600000, 0, 10000, sequenceCount: 3);
        }

        [Theory]
        [InlineData(9)]
        [InlineData(600001)]
        public void Validate_IntervalOutsideRange_Throws(decimal interval)
        {
            var config = new ConfigData { Interval = interval, Jitter = 0, SequenceIndex = 0 };

            Assert.Throws<System.IO.InvalidDataException>(
                () => config.Validate(10, 600000, 0, 10000, sequenceCount: 3));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(10001)]
        public void Validate_JitterOutsideRange_Throws(decimal jitter)
        {
            var config = new ConfigData { Interval = 100, Jitter = jitter, SequenceIndex = 0 };

            Assert.Throws<System.IO.InvalidDataException>(
                () => config.Validate(10, 600000, 0, 10000, sequenceCount: 3));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(3)]
        public void Validate_InvalidSequenceIndex_Throws(int sequenceIndex)
        {
            var config = new ConfigData { Interval = 100, Jitter = 0, SequenceIndex = sequenceIndex };

            Assert.Throws<System.IO.InvalidDataException>(
                () => config.Validate(10, 600000, 0, 10000, sequenceCount: 3));
        }

        [Fact]
        public void Validate_NullClickAreaList_Throws()
        {
            var config = new ConfigData
            {
                ClickAreas = null!,
                Interval = 100,
                Jitter = 0,
                SequenceIndex = 0
            };

            Assert.Throws<System.IO.InvalidDataException>(
                () => config.Validate(10, 600000, 0, 10000, sequenceCount: 3));
        }

        [Fact]
        public void Validate_InvalidClickArea_Throws()
        {
            var config = new ConfigData
            {
                ClickAreas = new List<ClickArea>
                {
                    new ClickArea { Label = "Invalid", Area = new Rectangle(0, 0, 0, 10) }
                },
                Interval = 100,
                Jitter = 0,
                SequenceIndex = 0
            };

            Assert.Throws<System.IO.InvalidDataException>(
                () => config.Validate(10, 600000, 0, 10000, sequenceCount: 3));
        }

        [Fact]
        public void Validate_ClickAreaCoordinateOverflow_Throws()
        {
            var config = new ConfigData
            {
                ClickAreas = new List<ClickArea>
                {
                    new ClickArea
                    {
                        Label = "Overflow",
                        Area = new Rectangle(int.MaxValue - 5, 0, 10, 10)
                    }
                },
                Interval = 100,
                Jitter = 0,
                SequenceIndex = 0
            };

            Assert.Throws<System.IO.InvalidDataException>(
                () => config.Validate(10, 600000, 0, 10000, sequenceCount: 3));
        }
    }
}
