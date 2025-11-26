using FrostHelper.Helpers;
using FrostHelper.SessionExpressions;
using FrostHelper.Triggers.Activator;
using System.Diagnostics.CodeAnalysis;

namespace FrostHelper.Triggers.Flags;

[CustomEntity("FrostHelper/SequencerTrigger")]
internal sealed class SequencerTrigger(EntityData data, Vector2 offset) : BaseActivator(data, offset) {
    internal override bool NeedsNodeIndexes => true;

    private static List<SequencerCommand> ParseCommands(string commandsText) {
        var parser = new SpanParser(commandsText);
        
        if (parser.TryParseList<SequencerCommand>(';', out var commands))
            return commands;

        NotificationHelper.Notify($"Failed to parse {commandsText} as a Sequence.");
        return [];
    }
    
    private readonly List<SequencerCommand> _commands = ParseCommands(data.Attr("sequence"));
    private readonly ConditionHelper.Condition _terminateCondition = ConditionHelper.CreateOrDefault(data.Attr("terminationCondition"), "0");
    private readonly bool _loop = data.Bool("loop");
    private readonly bool _once = data.Bool("oneUse"); // not once, as that's used for BaseActivator and would break us.

    public override void OnEnter(Player player) {
        base.OnEnter(player);
        
        if (Collidable)
            Add(new Coroutine(Sequence(player)));
        if (_once)
            Collidable = false;
    }

    private IEnumerator Sequence(Player player) {
        start:
        
        foreach (var cmd in _commands) {
            if (!_terminateCondition.Empty && _terminateCondition.Check(player.level.Session))
                yield break;
            
            switch (cmd.Execute(this, player)) {
                case IEnumerator enumerator:
                    while (enumerator.MoveNext()) {
                        yield return enumerator.Current;
                    }
                    break;
                case {} obj:
                    yield return obj;
                    break;
            }
        }

        if (_loop)
            goto start;
    }
}

internal abstract class SequencerCommand : ISpanParsable<SequencerCommand> {
    public abstract object? Execute(SequencerTrigger trigger, Player player);
    
    public static SequencerCommand Parse(string s, IFormatProvider? provider) {
        return Parse(s.AsSpan(), provider);
    }

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out SequencerCommand result) {
        return TryParse(s.AsSpan(), provider, out result);
    }

    public static SequencerCommand Parse(ReadOnlySpan<char> s, IFormatProvider? provider) {
        if (!TryParse(s, provider, out var command))
            throw new FormatException($"Unable to parse \"{s}\" as a SequencerCommand");
        
        return command;
    }

    private static bool TryParseNameValuePair(ref SpanParser parser, [NotNullWhen(true)] out string? name, 
        [NotNullWhen(true)] out ConditionHelper.Condition? condition) {
        name = null;
        condition = null;
        
        if (!parser.SliceUntil('=').TryUnpack(out var flag))
            return false;
        if (!ConditionHelper.TryCreate(parser.Remaining.ToString(), ExpressionContext.Default, out condition))
            return false;

        name = flag.Remaining.ToString();
        return true;
    }
    
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out SequencerCommand result) {
        //flag:name=value;delay:time;counter:name=expr;slider:name=expr
        var parser = new SpanParser(s);
        result = null;

        if (!parser.SliceUntil(':').TryUnpack(out var typeParser))
            return false;
        switch (typeParser.Remaining) {
            case "delay":
                if (!ConditionHelper.TryCreate(parser.Remaining.ToString(), ExpressionContext.Default, out var delayTime))
                    return false;
                result = new DelayCmd(delayTime);
                return true;
            case "flag":
                if (!TryParseNameValuePair(ref parser, out var flag, out var flagValue))
                    return false;
                result = new SetFlagCmd(flag, flagValue);
                return true;
            case "counter":
                if (!TryParseNameValuePair(ref parser, out var counter, out var counterValue))
                    return false;
                result = new SetCounterCmd(counter, counterValue);
                return true;
            case "slider":
                if (!TryParseNameValuePair(ref parser, out var slider, out var sliderValue))
                    return false;
                result = new SetSliderCmd(slider, sliderValue);
                return true;
            case "activateAt":
                if (!ConditionHelper.TryCreate(parser.Remaining.ToString(), ExpressionContext.Default, out var nodeIdx))
                    return false;
                result = new ActivateAtCmd(nodeIdx);
                return true;
            default:
                return false;
        }
    }
}

internal sealed class SetFlagCmd(string flagName, ConditionHelper.Condition value) : SequencerCommand {
    public override object? Execute(SequencerTrigger trigger, Player player) {
        if (FrostModule.TryGetCurrentLevel() is {} level) {
            level.Session.SetFlag(flagName, value.Check(level.Session));
        }
        
        return null;
    }
}

internal sealed class SetCounterCmd(string counterName, ConditionHelper.Condition value) : SequencerCommand {
    public override object? Execute(SequencerTrigger trigger, Player player) {
        if (FrostModule.TryGetCurrentLevel() is {} level) {
            level.Session.SetCounter(counterName, value.GetInt(level.Session));
        }
        
        return null;
    }
}

internal sealed class SetSliderCmd(string sliderName, ConditionHelper.Condition value) : SequencerCommand {
    public override object? Execute(SequencerTrigger trigger, Player player) {
        if (FrostModule.TryGetCurrentLevel() is {} level) {
            level.Session.SetSlider(sliderName, value.GetFloat(level.Session));
        }
        
        return null;
    }
}

internal sealed class DelayCmd(ConditionHelper.Condition time) : SequencerCommand {
    public override object? Execute(SequencerTrigger trigger, Player player) {
        if (FrostModule.TryGetCurrentLevel() is {} level)
            return time.GetFloat(level.Session);

        return null;
    }
}

internal sealed class ActivateAtCmd(ConditionHelper.Condition nodeIdx) : SequencerCommand {
    public override object? Execute(SequencerTrigger trigger, Player player) {
        if (FrostModule.TryGetCurrentLevel() is {} level)
            trigger.ActivateAtNode(player, nodeIdx.GetInt(level.Session));
        
        return null;
    }
}
