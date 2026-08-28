using FrostHelper.SessionExpressions;

namespace FrostHelper.API;

public record struct RenderPart(string Contents, string ColorId, IReadOnlyList<RenderPart>? Tooltip) {
    public static RenderPart Default(string contents) => new RenderPart(contents, "default", null);
    
    public static RenderPart Trivia(string contents) => new RenderPart(contents, "whitespace", null);
    
    public static RenderPart Operator(string type) => new RenderPart(type, "operator", null);
    
    public static RenderPart Literal(string value) => new RenderPart(value, "literal", null);
    
    public static RenderPart StringContent(string value) => new RenderPart(value, "string", null);
    
    public static RenderPart Flag(string flagName, IReadOnlyList<RenderPart>? tooltip = null)
        => new RenderPart(flagName, "flag", tooltip);
    
    public static RenderPart Counter(string counterName, IReadOnlyList<RenderPart>? tooltip = null)
        => new RenderPart(counterName, "counter", tooltip);
    
    public static RenderPart Slider(string sliderName, IReadOnlyList<RenderPart>? tooltip = null)
        => new RenderPart(sliderName, "slider", tooltip);
    
    public static RenderPart Field(string flagName, IReadOnlyList<RenderPart>? tooltip = null)
        => new RenderPart(flagName, "field", tooltip);
    public static RenderPart Command(string flagName, IReadOnlyList<RenderPart>? tooltip = null)
        => new RenderPart(flagName, "command", tooltip);

    public static RenderPart Type(TypeDescriptor type) => new RenderPart(type.CanonName, "type", null);

    public ApiRenderPart ToApi() {
        return (Contents, ColorId, Tooltip?.Select(tp => (tp.Contents, tp.ColorId)).ToList());
    }
    
    public (string Contents, string ColorId) ToApiNoTooltip() {
        return (Contents, ColorId);
    }
}
