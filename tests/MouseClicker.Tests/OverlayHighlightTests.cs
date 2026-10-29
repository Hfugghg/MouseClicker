using System.Drawing;
using Xunit;

namespace MouseClicker.Tests
{
    public class OverlayHighlightTests
    {
        [Fact]
        public void MainDetectionSwitchWorksWithClickOverlayOffAndHidesDuringSelection()
        {
            Exception? error = null;
            var thread = new Thread(() =>
            {
                try
                {
                    using var main = new Form1 { Opacity = 0, ShowInTaskbar = false };
                    const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                    object Field(string name) => typeof(Form1).GetField(name, flags)!.GetValue(main)!;
                    var clicks = (System.Windows.Forms.CheckBox)Field("showOverlayCheckBox");
                    var detection = (System.Windows.Forms.CheckBox)Field("showDetectionOverlayCheckBox");
                    var highlight = (System.Windows.Forms.CheckBox)Field("highlightCurrentAreaCheckBox");
                    var overlay = (OverlayForm)Field("_overlayForm");
                    Assert.False(detection.Checked);
                    clicks.Checked = false;
                    detection.Checked = true;
                    Assert.True(overlay.Visible);
                    Assert.False(overlay.ShowClickAreas);
                    Assert.True(overlay.ShowDetectionAreas);
                    Assert.False(highlight.Enabled);
                    typeof(Form1).GetField("_isSelectingArea", flags)!.SetValue(main, true);
                    typeof(Form1).GetMethod("UpdateOverlayState", flags)!.Invoke(main, null);
                    Assert.False(overlay.Visible);
                    typeof(Form1).GetField("_isSelectingArea", flags)!.SetValue(main, false);
                    clicks.Checked = true;
                    detection.Checked = false;
                    Assert.True(overlay.ShowClickAreas);
                    Assert.False(overlay.ShowDetectionAreas);
                }
                catch (Exception ex) { error = ex; }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            Assert.True(thread.Join(TimeSpan.FromSeconds(10)));
            if (error != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
        }

        [Fact]
        public void DetectionAreasUseIndependentCoordinatesAndExcludeOrdinaryClicks()
        {
            var ordinary = DuplicateArea();
            var conditional = DuplicateArea();
            conditional.Condition = new ClickCondition { DetectionArea = new Rectangle(-1500, -250, 90, 60) };
            Assert.Same(conditional, Assert.Single(OverlayForm.GetDetectionAreas([ordinary, conditional])));
            Assert.Equal(new Rectangle(100, 50, 90, 60), OverlayForm.ToLocalArea(conditional.Condition.DetectionArea, new Point(-1600, -300)));
            Assert.DoesNotContain(OverlayForm.DetectionColor, new[] { Color.Red, OverlayForm.PreviewColor, OverlayForm.ActiveColor });
        }

        [Fact]
        public void DetectionOnlyRenderingDoesNotDrawClickBorders()
        {
            Exception? error = null;
            var thread = new Thread(() =>
            {
                try
                {
                    var origin = System.Windows.Forms.SystemInformation.VirtualScreen.Location;
                    using var overlay = new RenderedOverlay { ShowClickAreas = false, ShowDetectionAreas = true };
                    overlay.AreasToDraw = [new ClickArea { Label = "region", Area = new Rectangle(origin.X + 10, origin.Y + 10, 60, 60),
                        Condition = new ClickCondition { DetectionArea = new Rectangle(origin.X + 120, origin.Y + 80, 100, 70) } }];
                    using var bitmap = new Bitmap(260, 200);
                    using var graphics = Graphics.FromImage(bitmap);
                    graphics.Clear(Color.Magenta);
                    overlay.Render(graphics);
                    Assert.Equal(Color.Magenta.ToArgb(), bitmap.GetPixel(10, 30).ToArgb());
                    Assert.Contains(Enumerable.Range(120, 100), x => bitmap.GetPixel(x, 80).ToArgb() == OverlayForm.DetectionColor.ToArgb());
                    overlay.ShowDetectionAreas = false;
                    overlay.ShowClickAreas = true;
                    graphics.Clear(Color.Magenta);
                    overlay.Render(graphics);
                    Assert.Equal(Color.Red.ToArgb(), bitmap.GetPixel(10, 30).ToArgb());
                    Assert.Equal(Color.Magenta.ToArgb(), bitmap.GetPixel(130, 80).ToArgb());
                }
                catch (Exception ex) { error = ex; }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            Assert.True(thread.Join(TimeSpan.FromSeconds(10)));
            if (error != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
        }

        private sealed class RenderedOverlay : OverlayForm
        {
            internal void Render(Graphics graphics) => OnPaint(new System.Windows.Forms.PaintEventArgs(graphics, new Rectangle(0, 0, 260, 200)));
        }

        [Fact]
        public void CaptureExclusionTracksHandleRecreationAndDisposal()
        {
            Exception? error = null;
            var thread = new Thread(() =>
            {
                try
                {
                    using var overlay = new RecreatedOverlay();
                    bool allowed = overlay.CanShowDuringCondition();
                    Assert.True(overlay.IsHandleCreated);
                    Assert.Equal(overlay.IsExcludedFromCapture, allowed);
                    if (allowed)
                    {
                        Assert.True(GetWindowDisplayAffinity(overlay.Handle, out uint affinity));
                        Assert.Equal(0x11u, affinity);
                    }
                    overlay.Recreate();
                    Assert.Equal(overlay.IsExcludedFromCapture, overlay.CanShowDuringCondition());
                    if (overlay.IsExcludedFromCapture)
                    {
                        Assert.True(GetWindowDisplayAffinity(overlay.Handle, out uint affinity));
                        Assert.Equal(0x11u, affinity);
                    }
                    overlay.Dispose();
                    Assert.False(overlay.IsExcludedFromCapture);
                    Assert.False(overlay.CanShowDuringCondition());
                }
                catch (Exception ex) { error = ex; }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            Assert.True(thread.Join(TimeSpan.FromSeconds(10)));
            if (error != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
        }

        private sealed class RecreatedOverlay : OverlayForm
        {
            internal void Recreate() => RecreateHandle();
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool GetWindowDisplayAffinity(IntPtr window, out uint affinity);

        private static ClickArea DuplicateArea() => new() { Label = "Same name", Area = new Rectangle(10, 20, 100, 80) };

        [Fact]
        public void Preview_UsesObjectIdentityDespiteDuplicateNamesAndBounds()
        {
            var chosen = DuplicateArea();
            var duplicate = DuplicateArea();
            Assert.Equal(Color.DodgerBlue, OverlayForm.GetBorderColor(chosen, chosen, null));
            Assert.Equal(Color.Red, OverlayForm.GetBorderColor(duplicate, chosen, null));
        }

        [Fact]
        public void ActiveColor_WinsWhenTheSameObjectIsAlsoPreviewed()
        {
            var area = DuplicateArea();
            Assert.Equal(Color.LimeGreen, OverlayForm.GetBorderColor(area, area, area));
            Assert.Equal(Color.Red, OverlayForm.GetBorderColor(DuplicateArea(), area, area));
        }

        [Fact]
        public void OverlappingAreas_AreDrawnNormalThenPreviewThenActiveRegardlessOfListOrder()
        {
            var active = DuplicateArea();
            var preview = DuplicateArea();
            var ordinary = DuplicateArea();
            Assert.Collection(OverlayForm.GetDrawOrder(new[] { active, preview, ordinary }, preview, active),
                area => Assert.Same(ordinary, area),
                area => Assert.Same(preview, area),
                area => Assert.Same(active, area));
        }

        [Fact]
        public void RemovedHighlightReferences_DoNotIntroduceExtraAreasOrHighlightEqualReplacements()
        {
            var replacement = DuplicateArea();
            var removed = DuplicateArea();
            Assert.Same(replacement, Assert.Single(OverlayForm.GetDrawOrder(new[] { replacement }, removed, removed)));
            Assert.Equal(Color.Red, OverlayForm.GetBorderColor(replacement, removed, removed));
        }

        [Fact]
        public void UnhighlightedAreas_KeepTheirOriginalRelativeDrawingOrder()
        {
            var first = DuplicateArea();
            var second = DuplicateArea();
            Assert.Collection(OverlayForm.GetDrawOrder(new[] { first, second }, null, null),
                area => Assert.Same(first, area), area => Assert.Same(second, area));
        }
    }
}
