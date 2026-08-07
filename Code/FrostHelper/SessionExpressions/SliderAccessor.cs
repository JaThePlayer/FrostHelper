using FrostHelper.Helpers;
using System.Reflection.Emit;
using OpCodes = System.Reflection.Emit.OpCodes;

namespace FrostHelper.SessionExpressions;

internal sealed class SliderAccessorCondition(string name) : ConditionHelper.Condition {
    private static readonly MethodInfo MethodSessionGetSlider =
        typeof(Session).GetMethod(nameof(Session.GetSlider), BindingFlags.Instance | BindingFlags.Public)!;

    private WeakReference<Session.Slider>? _slider;
    private WeakReference<Session>? _lastSession;

    public override object Get(Session session, object? userdata) {
        if ((_lastSession?.TryGetTarget(out var last) ?? false) && last != session) {
            _slider = null;
            _lastSession = null;
        }

        _lastSession ??= new WeakReference<Session>(session);

        if (_slider?.TryGetTarget(out var slider) is not true) {
            slider = session.GetSliderObject(name);
            _slider = new(slider);
        }

        return slider.Value;
    }

    internal override void Emit(ConditionCompilationCtx ctx, Type targetType) {
        ctx.EmitLoadSession();
        ctx.Il.Emit(OpCodes.Ldstr, name);
        ctx.Il.Emit(OpCodes.Callvirt, MethodSessionGetSlider);
        ctx.EmitConvertTo(typeof(float), targetType);
    }

    internal override bool UsesCurrentConditionLocalInEmit => false;

    protected internal override Type ReturnType => typeof(float);
}

internal sealed class IndirectSliderAccessor(ConditionHelper.Condition nameCond) : ConditionHelper.Condition {
    private readonly ConditionHelper.Condition _nameCondition = nameCond;

    private static readonly FieldInfo FieldNameCondition =
        typeof(IndirectSliderAccessor).GetField(nameof(_nameCondition),
            BindingFlags.Instance | BindingFlags.NonPublic)!;

    private static readonly MethodInfo MethodSessionGetSlider =
        typeof(Session).GetMethod(nameof(Session.GetSlider), BindingFlags.Instance | BindingFlags.Public)!;

    public override object Get(Session session, object? userdata) {
        var name = _nameCondition.GetString(session, userdata);

        return session.GetSlider(name);
    }

    internal override void Emit(ConditionCompilationCtx ctx, Type targetType) {
        LocalBuilder? temp = null;
        ctx.EmitLoadSession();

        ctx.EmitSwapOutCurrentCondition(ref temp, _nameCondition, FieldNameCondition);
        _nameCondition.Emit(ctx, typeof(string));
        ctx.EmitRevertCurrentCondition(temp);

        ctx.Il.Emit(OpCodes.Callvirt, MethodSessionGetSlider);
        ctx.EmitConvertTo(typeof(float), targetType);
    }

    internal override bool UsesCurrentConditionLocalInEmit => _nameCondition.UsesCurrentConditionLocalInEmit;

    protected internal override Type ReturnType => typeof(float);

    protected override IEnumerable<object> GetArgsForDebugPrint() => [_nameCondition];
}
