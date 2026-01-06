using FrostHelper.Helpers;

namespace FrostHelper.Tests.AbstractExpressions;

[Collection("FrostHelper")]
public class MiscTests {
    [Fact]
    public void ReservedCharsAreDetected() {
        using (var _ = new NotificationExpecter(1, 
                   n => Assert.Contains("contains reserved characters", n.Message))) {
            // For backwards compat reserved chars still return true, they just produce a message.
            Assert.True(AbstractExpression.TryParseCached("{", out var expr));
        }
    }
}