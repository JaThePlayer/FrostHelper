namespace FrostHelper.Helpers;

public interface IStaticFunc<in TIn, out TOut> {
    public static abstract TOut Invoke(TIn arg);
}
