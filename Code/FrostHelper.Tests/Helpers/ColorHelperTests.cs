namespace FrostHelper.Tests.Helpers;

[Collection("FrostHelper")]
public class ColorHelperTests {
    [Fact]
    public void HexColors() {
        // Empty string is White
        Assert.Equal(Color.White, ColorHelper.GetColor(""));
        
        // # allowed at start
        Assert.Equal(Color.White, ColorHelper.GetColor("#ffffff"));
        // case insensitive
        Assert.Equal(Color.White, ColorHelper.GetColor("ffFfFF"));
        
        // RRGGBB
        Assert.Equal(Color.Red, ColorHelper.GetColor("ff0000"));
        Assert.Equal(new Color(0, 255, 0), ColorHelper.GetColor("00ff00"));
        Assert.Equal(Color.Blue, ColorHelper.GetColor("0000ff"));
        
        // Premultiplied Alpha
        Assert.Equal(new Color(0x10, 0x20, 0x30, 0x40), ColorHelper.GetColor("10203040"));
        
        // 7-digit colors are treated like 6-digit ones - backwards compat with Spring Collab
        // TODO: why are the color channels so off though? I've not heard any bug reports about it...
        Assert.Equal(new Color(2, 3, 4), ColorHelper.GetColor("1020304"));
    }
    
    [Fact]
    public void XnaColors() {
        // Case-insensitive
        Assert.Equal(Color.White, ColorHelper.GetColor("White"));
        Assert.Equal(Color.White, ColorHelper.GetColor("white"));
        Assert.Equal(Color.White, ColorHelper.GetColor("WHITE"));
    }

    [Fact]
    public void InvalidColorNotifs() {
        using (_ = new NotificationExpecter(1)) {
            Assert.Equal(Color.Transparent, ColorHelper.GetColor("invalid"));
        }
        
        // No notifications for repeat calls - would be too spammy.
        ColorHelper.GetColor("invalid");
    }

    [Fact]
    public void GetColors() {
        Assert.Equal([Color.Red], ColorHelper.GetColors("ff0000"));
        Assert.Equal([Color.Red, Color.Blue], ColorHelper.GetColors(" ff0000,0000ff "));
        
        // Since empty colors are White, trailing commas cause an additional White.
        Assert.Equal([Color.Red, Color.Blue, Color.White], ColorHelper.GetColors(" ff0000,0000ff, "));
        using (_ = new NotificationExpecter(1))
            Assert.Equal([Color.Red, Color.Blue, Color.Transparent], ColorHelper.GetColors(" ff0000,0000ff,bad"));
        
        // Caching
        var a = ColorHelper.GetColors("ff0000,0000ff");
        var b = ColorHelper.GetColors("ff0000,0000ff");
        Assert.Same(a, b);
    }
}