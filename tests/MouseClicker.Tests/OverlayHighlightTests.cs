using System.Drawing;
using Xunit;

namespace MouseClicker.Tests
{
    public class OverlayHighlightTests
    {
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
