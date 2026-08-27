using FrostHelper.Helpers;
using FrostHelper.SessionExpressions;
using System.Diagnostics;

namespace FrostHelper.API;

// [ModExportName("FrostHelper")] - defined in API.cs
public static partial class API {
    /// <summary>
    /// Creates a Session Expression Inspector object, which can be used by other apis to inspect contents of a session expression,
    /// to be used by REPLs and such.
    /// </summary>
    /// <param name="expressionContext">The Session Expression Context to use, leave null to use default context.</param>
    /// <returns>An Inspector object, which can be passed to other APIs.</returns>
    public static object CreateSessionExpressionInspector(object? expressionContext) {
        return new InspectorSession(AssertContext(expressionContext ?? ExpressionContext.Default));
    }

    /// <summary>
    /// Gets the text currently stored in the inspector.
    /// Empty string at first.
    /// </summary>
    public static string GetInspectorText(object inspector) {
        InspectorSession inspectorSession = AssertIs<InspectorSession>(inspector);

        return inspectorSession.CurrentExpression;
    }
    
    /// <summary>
    /// Sets the text currently stored in the inspector.
    /// After doing this, you can use other APIs to retrieve information about the current session expression.
    /// </summary>
    public static void SetInspectorText(object inspector, string newText) {
        InspectorSession inspectorSession = AssertIs<InspectorSession>(inspector);

        inspectorSession.CurrentExpression = newText;
    }
    
    /// <summary>
    /// Gets the session expression currently stored in the inspector.
    /// Can be null if errors occured during creation.
    /// </summary>
    public static object? GetInspectorSessionExpression(object inspector) {
        InspectorSession inspectorSession = AssertIs<InspectorSession>(inspector);

        return inspectorSession.Condition;
    }

    /// <summary>
    /// Returns a list of text parts that need to be displayed to render the current session expression with syntax highlighting.
    /// Might be empty if the expression failed to parse.
    ///
    /// Color IDs represent the kind of text to render, their actual color is dependent on the user.
    /// </summary>
    public static
        IReadOnlyList<(string Contents, string ColorId, IReadOnlyList<(string Contents, string ColorId)>? Tooltip)>
        GetInspectorRenderParts(object inspector) {
        InspectorSession inspectorSession = AssertIs<InspectorSession>(inspector);

        return inspectorSession.GetRenderParts();
    }

    /// <summary>
    /// Gets the errors the inspector encountered while parsing the expression.
    /// Empty if no errors were found.
    /// </summary>
    public static IReadOnlyList<string> GetInspectorErrors(object inspector) {
        InspectorSession inspectorSession = AssertIs<InspectorSession>(inspector);
        
        return inspectorSession.Errors;
    }
}

internal sealed class InspectorSession {
    private readonly IExpressionContext _ctx;

    public string CurrentExpression {
        get;
        set {
            if (field == value)
                return;
            
            field = value;

            _errorsMutable.Clear();
            _renderParts = null;
            
            if (ExpressionToken.Tokenize(value.AsSpan(), _notificationLogger, out _tokens) 
                is not ExpressionToken.TokenizerState.End) {
                //NotificationHelper.Notify($"Failed to tokenize Session Expression:\n{str}");
                
                return;
            }

            using (var sink = API.RegisterNotificationSink((_, text) => {
                       _errorsMutable.Add(text);
                       return false;
            })) {
                if (!ConditionHelper.TryCreate(value, _ctx, out var condition)) {
                    Condition = null;
                    return;
                }
                
                Condition = condition;
            }
        }
    } = "";

    public IReadOnlyList<string> Errors => _errorsMutable;
    
    private readonly List<string> _errorsMutable = [];

    private readonly NotificationLogger _notificationLogger;

    private List<ExpressionToken> _tokens = [];

    public ConditionHelper.Condition? Condition { get; private set; }

    private IReadOnlyList<(string Contents, string ColorId, IReadOnlyList<(string Contents, string ColorId)>? Tooltip)>?
        _renderParts;

    public IReadOnlyList<(string Contents, string ColorId, IReadOnlyList<(string Contents, string ColorId)>? Tooltip)>
        GetRenderParts() {
        if (_renderParts is not null)
            return _renderParts;
        
        List<(string Contents, string ColorId, IReadOnlyList<(string Contents, string ColorId)>? Tooltip)> ret = [];

        foreach (var tok in _tokens) {
            var parts = CreateRenderParts(tok, false);
            
            ret.AddRange(parts.Select(p => (p.Contents, p.ColorId, 
                p.Tooltip?.Select(tp => (tp.Contents, tp.ColorId)).ToList() as IReadOnlyList<(string Contents, string ColorId)>)));
        }

        return _renderParts = ret;
    }

    public InspectorSession(IExpressionContext ctx) {
        _ctx = ctx;
        _notificationLogger = new NotificationLogger(this);
    }

    internal IReadOnlyList<ApiRenderPart> CreateRenderParts(ExpressionToken token, bool insideStringLiteral) {
        var operand = token.Operand!;
        List<ApiRenderPart> renderParts = token.Kind switch {
            ExpressionToken.Kinds.Add => [ ApiRenderPart.Operator("+") ],
            ExpressionToken.Kinds.Sub => [ ApiRenderPart.Operator("-") ],
            ExpressionToken.Kinds.Mul => [ ApiRenderPart.Operator("*") ],
            ExpressionToken.Kinds.Div => [ ApiRenderPart.Operator("/") ],
            ExpressionToken.Kinds.DivFloat => [ ApiRenderPart.Operator("//") ],
            ExpressionToken.Kinds.Modulo => [ ApiRenderPart.Operator("%") ],
            ExpressionToken.Kinds.Flag => [ token.IsUnaryOnStrings 
                ? ApiRenderPart.Flag("f") 
                : ApiRenderPart.Flag(operand.ToString()!, [
                    ApiRenderPart.Default($"Checks the flag '{operand}', returns 1 if its set, 0 otherwise.")
                ])
            ],
            ExpressionToken.Kinds.Counter => [ token.IsUnaryOnStrings 
                ? ApiRenderPart.Counter("#") 
                : ApiRenderPart.Counter($"#{operand}", [
                    ApiRenderPart.Default($"Gets the value of the counter '{operand}'.")
                ])
            ],
            ExpressionToken.Kinds.Slider => [ token.IsUnaryOnStrings
                ? ApiRenderPart.Slider("@")
                : ApiRenderPart.Slider($"@{operand}", [
                    ApiRenderPart.Default($"Gets the value of the slider '{operand}'.")
                ])
            ],
            ExpressionToken.Kinds.Command => HandleCommandRenderParts(token),
            ExpressionToken.Kinds.Invert => [ ApiRenderPart.Operator("!") ],
            ExpressionToken.Kinds.LitString => [ insideStringLiteral ? ApiRenderPart.StringContent($"{operand}") : ApiRenderPart.StringContent($"\"{operand}\"") ],
            ExpressionToken.Kinds.InterpolatedString => HandleInterpolatedStringRenderParts(token),
            ExpressionToken.Kinds.LitInt => [ ApiRenderPart.Literal(((LiteralOperand<int>)operand).SourceText) ],
            ExpressionToken.Kinds.LitFloat => [ ApiRenderPart.Literal(((LiteralOperand<float>)operand).SourceText) ],
            ExpressionToken.Kinds.Eq => [ ApiRenderPart.Operator("==") ],
            ExpressionToken.Kinds.Ne => [ ApiRenderPart.Operator("!=") ],
            ExpressionToken.Kinds.Lt => [ ApiRenderPart.Operator("<") ],
            ExpressionToken.Kinds.Le => [ ApiRenderPart.Operator("<=") ],
            ExpressionToken.Kinds.Gt => [ ApiRenderPart.Operator(">") ],
            ExpressionToken.Kinds.Ge => [ ApiRenderPart.Operator(">=") ],
            ExpressionToken.Kinds.SingleEquals => [ ApiRenderPart.Operator("=") ],
            ExpressionToken.Kinds.And => [ ApiRenderPart.Operator("&&") ],
            ExpressionToken.Kinds.Or => [ ApiRenderPart.Operator("||") ],
            ExpressionToken.Kinds.BitwiseAnd => [ ApiRenderPart.Operator("&") ],
            ExpressionToken.Kinds.BitwiseOr => [ ApiRenderPart.Operator("|") ],
            ExpressionToken.Kinds.Bracket => HandleBracketRenderParts(token),
            ExpressionToken.Kinds.FieldAccess => HandleFieldAccessRenderParts(token),
            ExpressionToken.Kinds.LambdaArrow => [ ApiRenderPart.Operator("=>") ],
            ExpressionToken.Kinds.UnaryMinus => [ ApiRenderPart.Operator("-") ],
            _ => throw new ArgumentOutOfRangeException()
        };

        if (token.Trivia is not null and not "") {
            renderParts.Insert(0, ApiRenderPart.Trivia(token.Trivia));
        }

        return renderParts;
    }

    private List<ApiRenderPart> HandleInterpolatedStringRenderParts(ExpressionToken token) {
        List<InterpolationHole> operand =
            token.Operand as List<InterpolationHole> ?? throw new UnreachableException();

        List<ApiRenderPart> parts = [
            ApiRenderPart.StringContent("\"")
        ];

        foreach (var argumentTokens in operand) {
            if (argumentTokens.IsLiteral) {
                parts.AddRange(argumentTokens.Tokens.SelectMany(t => CreateRenderParts(t, insideStringLiteral: true)));
            } else {
                parts.Add(ApiRenderPart.Command("$("));
                parts.AddRange(argumentTokens.Tokens.SelectMany(t => CreateRenderParts(t, insideStringLiteral: false)));
                parts.Add(ApiRenderPart.Trivia(argumentTokens.PreEndBracketTrivia));
                parts.Add(ApiRenderPart.Command(")"));
            }
        }
        
        parts.Add(ApiRenderPart.StringContent("\""));
        return parts;
    }

    private List<ApiRenderPart> HandleBracketRenderParts(ExpressionToken token) {
        BracketOperand innerTokens = token.Operand as BracketOperand ?? throw new UnreachableException();

        return [
            ApiRenderPart.Operator("("),
            .. innerTokens.Tokens.SelectMany(t => CreateRenderParts(t, insideStringLiteral: false)),
            ApiRenderPart.Trivia(innerTokens.PreEndBracketTrivia),
            ApiRenderPart.Operator(")")
        ];
    }

    private List<ApiRenderPart> HandleFieldAccessRenderParts(ExpressionToken token) {
        FieldAccessTokenOperand operand = token.Operand as FieldAccessTokenOperand ?? throw new UnreachableException();

        List<ApiRenderPart> parts = [];
        CommandDescriptor? descriptor = null;
        using (_ = API.RegisterNotificationSink((_, _) => false)) {
            if (AbstractExpression.Parse([token], out AbstractExpression? expression)
                && ConditionHelper.TryCreate(expression, _ctx, out var condition)) {
                descriptor = condition.Descriptor;
            }
        }
        
        if (parts.Count == 0)
            parts = [
                ..operand.ObjectTokens?.SelectMany(t => CreateRenderParts(t, insideStringLiteral: false)) ?? [],
                ApiRenderPart.Operator("."),
                ApiRenderPart.Field(operand.Name, CreateDescriptionTooltip(descriptor))
            ];

        if (operand.Arguments is not null) {
            parts.Add(ApiRenderPart.Operator("("));
            var first = true;
            foreach (var argumentTokens in operand.Arguments) {
                if (!first) {
                    parts.Add(ApiRenderPart.Operator(","));
                }
                first = false;
                parts.AddRange(argumentTokens.Tokens.SelectMany(t => CreateRenderParts(t, insideStringLiteral: false)));
                if (argumentTokens.PreEndBracketTrivia is not "")
                    parts.Add(ApiRenderPart.Trivia(argumentTokens.PreEndBracketTrivia));
            }
            parts.Add(ApiRenderPart.Operator(")"));
        }

        return parts;
    }

    private List<ApiRenderPart>? CreateDescriptionTooltip(CommandDescriptor? descriptor) {
        if (descriptor is null || descriptor.Description is [])
            return null;

        List<ApiRenderPart> parts = [];
        if (descriptor.DeclaringType is { } type) {
            parts.Add(ApiRenderPart.Type(type));
            parts.Add(ApiRenderPart.Operator("."));
        }
        
        parts.Add(ApiRenderPart.Command(descriptor.Name));

        if (descriptor.Arguments is not []) {
            parts.Add(ApiRenderPart.Operator("("));
            bool first = true;
            foreach (var arg in descriptor.Arguments) {
                if (!first) {
                    parts.Add(ApiRenderPart.Operator(", "));
                }
                first = false;
                
                parts.Add(ApiRenderPart.Type(arg.Type));
                parts.Add(ApiRenderPart.Trivia(" "));
                parts.Add(ApiRenderPart.Default(arg.Name));
            }
            parts.Add(ApiRenderPart.Operator(")"));
        }
        
        if (descriptor.ReturnType != TypeDescriptor.Any) {
            parts.Add(ApiRenderPart.Default(" -> "));
            parts.Add(ApiRenderPart.Type(descriptor.ReturnType));
        }
        parts.Add(ApiRenderPart.Trivia("\n"));
        parts.AddRange(descriptor.Description);

        return parts;
    }

    private List<ApiRenderPart> HandleAccessorParts(string fullOperationText, ConditionHelper.Condition condition) {
        List<ApiRenderPart> parts = [];
        switch (condition)
        {
            case KnownFieldAccessor fieldAccessor:
            {
                var postfix = fieldAccessor.SourceText ?? throw new UnreachableException();
            
                parts.AddRange(HandleAccessorParts(fullOperationText[..^postfix.Length], fieldAccessor.Target));
                if (postfix.StartsWith('.')) {
                    parts.Add(ApiRenderPart.Operator("."));
                    postfix = postfix[1..];
                }
                
                parts.Add(ApiRenderPart.Field(postfix, CreateDescriptionTooltip(fieldAccessor.Descriptor)));
                break;
            }
            case InstanceFunctionCommands.IInstanceFunctionCommand functionCommand: {
                var postfix = condition.Descriptor?.Name ?? throw new UnreachableException();
            
                parts.AddRange(HandleAccessorParts(fullOperationText[..^(postfix.Length + 1)], functionCommand.FieldCondition));
                parts.Add(ApiRenderPart.Operator("."));
                parts.Add(ApiRenderPart.Field(postfix, CreateDescriptionTooltip(condition.Descriptor)));
                break;
            }
            default:
            {
                parts.Add(ApiRenderPart.Command($"${fullOperationText}", CreateDescriptionTooltip(condition.Descriptor)));
                break;
            }
        }
        
        return parts;
    }
    
    private List<ApiRenderPart> HandleCommandRenderParts(ExpressionToken token) {
        CommandTokenOperand operand = token.Operand as CommandTokenOperand ?? throw new UnreachableException();

        CommandDescriptor? descriptor = null;

        List<ApiRenderPart> parts = [];

        using (_ = API.RegisterNotificationSink((_, _) => false)) {
            if (AbstractExpression.Parse([token], out AbstractExpression? expression)
                && ConditionHelper.TryCreate(expression, _ctx, out var condition)) {
                parts = HandleAccessorParts(operand.Name, condition);
                descriptor = condition.Descriptor;
            }
        }

        if (parts.Count == 0) {
            parts = [
                ApiRenderPart.Command($"${operand.Name}", CreateDescriptionTooltip(descriptor)),
            ];  
        }

        if (operand.Arguments is not null) {
            parts.Add(ApiRenderPart.Operator("("));
            var first = true;
            foreach (var argumentTokens in operand.Arguments) {
                if (!first) {
                    parts.Add(ApiRenderPart.Operator(","));
                }
                first = false;
                parts.AddRange(argumentTokens.Tokens.SelectMany(t => CreateRenderParts(t, insideStringLiteral: false)));
                if (argumentTokens.PreEndBracketTrivia is not "")
                    parts.Add(ApiRenderPart.Trivia(argumentTokens.PreEndBracketTrivia));
            }
            parts.Add(ApiRenderPart.Operator(")"));
        }

        return parts;
    }
    
    private sealed class NotificationLogger(InspectorSession session) : IAbstractExpressionErrorLogger {
        public void Error(string message) {
            session._errorsMutable.Add(message);
        }
    }
}

public record struct ApiRenderPart(string Contents, string ColorId, IReadOnlyList<ApiRenderPart>? Tooltip) {
    public static ApiRenderPart Default(string contents) => new ApiRenderPart(contents, "default", null);
    
    public static ApiRenderPart Trivia(string contents) => new ApiRenderPart(contents, "whitespace", null);
    
    public static ApiRenderPart Operator(string type) => new ApiRenderPart(type, "operator", null);
    
    public static ApiRenderPart Literal(string value) => new ApiRenderPart(value, "literal", null);
    
    public static ApiRenderPart StringContent(string value) => new ApiRenderPart(value, "string", null);
    
    public static ApiRenderPart Flag(string flagName, IReadOnlyList<ApiRenderPart>? tooltip = null)
        => new ApiRenderPart(flagName, "flag", tooltip);
    
    public static ApiRenderPart Counter(string counterName, IReadOnlyList<ApiRenderPart>? tooltip = null)
        => new ApiRenderPart(counterName, "counter", tooltip);
    
    public static ApiRenderPart Slider(string sliderName, IReadOnlyList<ApiRenderPart>? tooltip = null)
        => new ApiRenderPart(sliderName, "slider", tooltip);
    
    public static ApiRenderPart Field(string flagName, IReadOnlyList<ApiRenderPart>? tooltip = null)
        => new ApiRenderPart(flagName, "field", tooltip);
    public static ApiRenderPart Command(string flagName, IReadOnlyList<ApiRenderPart>? tooltip = null)
        => new ApiRenderPart(flagName, "command", tooltip);

    public static ApiRenderPart Type(TypeDescriptor type) => new ApiRenderPart(type.CanonName, "type", null);
}
