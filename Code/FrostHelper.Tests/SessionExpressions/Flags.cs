using Celeste;
using FrostHelper.Components;
using Monocle;
using static FrostHelper.Helpers.ConditionHelper;

namespace FrostHelper.Tests.SessionExpressions;

[Collection("FrostHelper")]
public class Flags {
    [Fact]
    public void SingleFlag() {
        var flagExpr = TestUtils.CreateExpr<FlagAccessor>("flagA");
        Assert.Equal("flagA", flagExpr.Flag);
        Assert.False(flagExpr.Inverted);
        Assert.Equal(typeof(int), flagExpr.ReturnType);
        Assert.True(flagExpr.OnlyChecksFlags());

        var inverted = Assert.IsType<FlagAccessor>(flagExpr.CreateInverted());
        Assert.Equal("flagA", inverted.Flag);
        Assert.True(inverted.Inverted);
        Assert.Equal(typeof(int), inverted.ReturnType);
        Assert.True(inverted.OnlyChecksFlags());

        var session = new Session();
        Assert.False(flagExpr.Check(session));
        Assert.True(inverted.Check(session));
        
        session.SetFlag("flagA");
        Assert.True(flagExpr.Check(session));
        Assert.False(inverted.Check(session));

        // Manual edits to this hashset should be detected as well.
        session.Flags.Remove("flagA");
        Assert.False(flagExpr.Check(session));
        Assert.True(inverted.Check(session));
        session.Flags.Add("flagA");
        Assert.True(flagExpr.Check(session));
        Assert.False(inverted.Check(session));
    }
    
    [Fact]
    public void FlagAnd() {
        var e = TestUtils.CreateExpr<OperatorAnd>("a && b");
        
        var session = new Session();
        Assert.False(e.Check(session));
        Assert.True(e.OnlyChecksFlags());

        foreach (var (a, b) in TestUtils.BoolPermutations) {
            session.SetFlag("a", a);
            session.SetFlag("b", b);
            if (a && b)
                Assert.True(e.Check(session));
            else
                Assert.False(e.Check(session));
        }
    }
    
    [Fact]
    public void FlagOr() {
        var e = TestUtils.CreateExpr<OperatorOr>("a || b");
        
        var session = new Session();
        Assert.False(e.Check(session));
        Assert.True(e.OnlyChecksFlags());

        foreach (var (a, b) in TestUtils.BoolPermutations) {
            session.SetFlag("a", a);
            session.SetFlag("b", b);
            if (a || b)
                Assert.True(e.Check(session));
            else
                Assert.False(e.Check(session));
        }
    }

    [Fact]
    public void Listener() {
        var level = TestUtils.CreateLevel();

        int amt = 0;
        ExpressionListener<bool> listener = new(TestUtils.CreateExpr("a"), (entity, old, newVal) => {
            amt++;
            switch (amt) {
                case 1:
                    Assert.False(old.HasValue);
                    Assert.False(newVal);
                    break;
                case 2:
                    Assert.True(old.HasValue);
                    Assert.False(old.Value);
                    Assert.True(newVal);
                    break;
                case 3:
                    Assert.True(old.HasValue);
                    Assert.True(old.Value);
                    Assert.False(newVal);
                    break;
            }
        }, activateOnStart: true);

        level.Add([ listener ]);
        
        level.Entities.UpdateLists();
        
        level.Update();
        Assert.Equal(1, amt);
        
        level.Session.SetFlag("a");
        level.Update();
        Assert.Equal(2, amt);
        
        level.Session.SetFlag("a");
        level.Update();
        Assert.Equal(2, amt);
        
        level.Session.SetFlag("a", false);
        level.Update();
        Assert.Equal(3, amt);
        
        // Currently, multiple flag updates in one frame lead to no detection
        // Should this be the case? This behavior will change if FlagListener is used directly...
        level.Session.SetFlag("a");
        level.Session.SetFlag("a", false);
        level.Update();
        Assert.Equal(3, amt);
    }
}