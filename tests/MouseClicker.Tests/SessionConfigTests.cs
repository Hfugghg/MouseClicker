using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Windows.Forms;
using Xunit;

namespace MouseClicker.Tests;

public class SessionConfigTests
{
    private static T Field<T>(Form1 form, string name) =>
        (T)typeof(Form1).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(form)!;

    [Fact]
    public void CloseAndReopenRestoresEditedConfigurationIncludingImageCondition()
    {
        OnSta(() =>
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
            try
            {
                var store = new SessionConfigStore(path);
                using var image = new Bitmap(4, 4);
                using var stream = new MemoryStream();
                image.Save(stream, ImageFormat.Png);
                var condition = new ClickCondition
                {
                    DetectionArea = new Rectangle(10, 20, 100, 80),
                    TemplateImageBase64 = Convert.ToBase64String(stream.ToArray()),
                    Threshold = .82, PollIntervalMs = 350, TimeoutMs = 9000,
                    TriggerMode = ConditionTriggerMode.WhileVisible, UseMultiScale = true
                };
                using (var form = new Form1(null, null, sessionConfigStore: store))
                {
                    Field<List<ClickArea>>(form, "_clickAreas").Add(new ClickArea
                    { Area = new Rectangle(1, 2, 30, 40), Label = "saved", Condition = condition });
                    Field<NumericUpDown>(form, "intervalNumericUpDown").Value = 777;
                    Field<CheckBox>(form, "showOverlayCheckBox").Checked = false;
                    typeof(Form1).GetMethod("OnFormClosing", BindingFlags.Instance | BindingFlags.NonPublic)!
                        .Invoke(form, [new FormClosingEventArgs(CloseReason.UserClosing, false)]);
                }
                using var restored = new Form1(null, null, sessionConfigStore: store);
                var area = Assert.Single(Field<List<ClickArea>>(restored, "_clickAreas"));
                Assert.Equal("saved", area.Label);
                Assert.Equal(777, Field<NumericUpDown>(restored, "intervalNumericUpDown").Value);
                Assert.Equal(System.Text.Json.JsonSerializer.Serialize(condition),
                    System.Text.Json.JsonSerializer.Serialize(area.Condition));
                Assert.False(Field<bool>(restored, "_isClicking"));
                Assert.False(Field<CheckBox>(restored, "showOverlayCheckBox").Checked);
            }
            finally
            {
                File.Delete(path);
                foreach (string backup in Directory.GetFiles(Path.GetDirectoryName(path)!, Path.GetFileName(path) + ".*.bak"))
                    File.Delete(backup);
            }
        });
    }

    [Fact]
    public void RestoreAppliesEveryPersistedFieldAndMultipleAreas()
    {
        OnSta(() =>
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
            try
            {
                var store = new SessionConfigStore(path);
                store.Save(new ConfigData
                {
                    ClickAreas =
                    [
                        new ClickArea { Area = new Rectangle(1, 2, 30, 40), Label = "right", RightButton = true },
                        new ClickArea { Area = new Rectangle(5, 6, 70, 80), Label = "left" }
                    ],
                    Interval = 1234,
                    Jitter = 321,
                    SequenceIndex = 2,
                    ShowOverlay = false,
                    ShowDetectionOverlay = true,
                    HighlightCurrentArea = false,
                    HardwareMode = true
                });
                using var form = new Form1(null, null, sessionConfigStore: store);

                var areas = Field<List<ClickArea>>(form, "_clickAreas");
                Assert.Equal(2, areas.Count);
                Assert.Equal("right", areas[0].Label);
                Assert.True(areas[0].RightButton);
                Assert.Equal("left", areas[1].Label);
                Assert.False(areas[1].RightButton);
                Assert.Equal(1234, Field<NumericUpDown>(form, "intervalNumericUpDown").Value);
                Assert.Equal(321, Field<NumericUpDown>(form, "jitterNumericUpDown").Value);
                Assert.Equal(2, Field<ComboBox>(form, "sequenceComboBox").SelectedIndex);
                Assert.False(Field<CheckBox>(form, "showOverlayCheckBox").Checked);
                Assert.True(Field<CheckBox>(form, "showDetectionOverlayCheckBox").Checked);
                Assert.False(Field<CheckBox>(form, "highlightCurrentAreaCheckBox").Checked);
                Assert.True(Field<CheckBox>(form, "hardwareModeCheckBox").Checked);
            }
            finally { File.Delete(path); }
        });
    }

    [Theory]
    [InlineData("{broken")]
    [InlineData("{\"ClickAreas\":[],\"Interval\":-1}")]
    public void InvalidSessionDoesNotPreventStartup(string json)
    {
        OnSta(() =>
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
            try
            {
                File.WriteAllText(path, json);
                using var form = new Form1(null, null, sessionConfigStore: new SessionConfigStore(path));
                string backup = Assert.Single(Directory.GetFiles(Path.GetDirectoryName(path)!, Path.GetFileName(path) + ".*.bak"));
                Assert.Equal(json, File.ReadAllText(backup));
                Assert.Empty(Field<List<ClickArea>>(form, "_clickAreas"));
                Assert.False(Field<bool>(form, "_isClicking"));
                Assert.NotNull(Field<Exception?>(form, "_sessionRestoreError"));
                Assert.False(Field<bool>(form, "_sessionSaveBlocked"));
            }
            finally
            {
                File.Delete(path);
                foreach (string backup in Directory.GetFiles(Path.GetDirectoryName(path)!, Path.GetFileName(path) + ".*.bak"))
                    File.Delete(backup);
            }
        });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RestoredAreasAreVisibleOnFirstShowWithoutAddingAnotherArea(bool hasAreas)
    {
        OnSta(() =>
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
            try
            {
                var store = new SessionConfigStore(path);
                store.Save(new ConfigData
                {
                    Interval = 1000, Jitter = 100,
                    ClickAreas = hasAreas ? [new ClickArea { Label = "restored", Area = new Rectangle(10, 20, 30, 40) }] : []
                });
                using var form = new Form1(null, null, sessionConfigStore: store) { ShowInTaskbar = false, Opacity = 0 };
                typeof(Form1).GetField("_updateCheckStarted", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(form, true);
                typeof(Form1).GetField("_hotkeyFailureShown", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(form, true);
                form.Show();
                Application.DoEvents();
                var list = Field<ListBox>(form, "areasListBox");
                Assert.True(list.Visible);
                Assert.True(list.IsHandleCreated);
                if (hasAreas)
                {
                    Assert.Equal("restored", Assert.IsType<ClickArea>(Assert.Single(list.Items.Cast<object>())).Label);
                    Assert.True(list.ClientRectangle.IntersectsWith(list.GetItemRectangle(0)));
                }
                else Assert.Empty(list.Items.Cast<object>());
                Assert.Equal(!hasAreas, Field<Label>(form, "emptyAreasLabel").Visible);
                Assert.False(Field<bool>(form, "_isClicking"));
            }
            finally { File.Delete(path); }
        });
    }

    [Fact]
    public void FailedSavePreservesExistingConfigurationAndCleansTemporaryFile()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        try
        {
            File.WriteAllText(path, "original configuration");
            var store = new SessionConfigStore(path);
            using (var locked = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var error = Record.Exception(() => store.Save(new ConfigData()));
                Assert.True(error is IOException or UnauthorizedAccessException,
                    $"Expected a file-access failure, got {error?.GetType().Name ?? "no exception"}.");
            }
            Assert.Equal("original configuration", File.ReadAllText(path));
            Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(path)!, Path.GetFileName(path) + ".*.tmp"));
        }
        finally { File.Delete(path); }
    }

    private static void OnSta(Action action)
    {
        Exception? error = null;
        var thread = new Thread(() => { try { action(); } catch (Exception ex) { error = ex; } });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(15)));
        if (error != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
    }
}
