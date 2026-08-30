using FrostHelper.SessionExpressions;

namespace FrostHelper.API;

public record struct RenderPart(string Contents, string ColorId, IReadOnlyList<RenderPart>? Tooltip) {
    public const string DefaultColorId = "default",
                        WhitespaceColorId = "whitespace",
                        OperatorColorId = "operator",
                        LiteralColorId = "literal",
                        StringContentColorId = "string",
                        FlagColorId = "flag",
                        CounterColorId = "counter",
                        SliderColorId = "slider",
                        FieldColorId = "field",
                        CommandColorId = "command",
                        TypeColorId = "type";
    
    public static RenderPart Default(string contents) => new RenderPart(contents, DefaultColorId, null);
    
    public static RenderPart Trivia(string contents) => new RenderPart(contents, WhitespaceColorId, null);
    
    public static RenderPart Operator(string type) => new RenderPart(type, OperatorColorId, null);
    
    public static RenderPart Literal(string value) => new RenderPart(value, LiteralColorId, null);
    
    public static RenderPart StringContent(string value) => new RenderPart(value, StringContentColorId, null);
    
    public static RenderPart Flag(string flagName, IReadOnlyList<RenderPart>? tooltip = null)
        => new RenderPart(flagName, FlagColorId, tooltip);
    
    public static RenderPart Counter(string counterName, IReadOnlyList<RenderPart>? tooltip = null)
        => new RenderPart(counterName, CounterColorId, tooltip);
    
    public static RenderPart Slider(string sliderName, IReadOnlyList<RenderPart>? tooltip = null)
        => new RenderPart(sliderName, SliderColorId, tooltip);
    
    public static RenderPart Field(string flagName, IReadOnlyList<RenderPart>? tooltip = null)
        => new RenderPart(flagName, FieldColorId, tooltip);
    public static RenderPart Command(string flagName, IReadOnlyList<RenderPart>? tooltip = null)
        => new RenderPart(flagName, CommandColorId, tooltip);

    public static RenderPart Type(TypeDescriptor type) => new RenderPart(type.CanonName, TypeColorId, null);

    public ApiRenderPart ToApi() {
        return (Contents, ColorId, Tooltip?.Select(tp => (tp.Contents, tp.ColorId)).ToList());
    }
    
    public (string Contents, string ColorId) ToApiNoTooltip() {
        return (Contents, ColorId);
    }
}
