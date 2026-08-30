global using ApiRenderPart = (string Contents, string ColorId, System.Collections.Generic.IReadOnlyList<(string Contents, string ColorId)>? Tooltip);
global using ApiAutoCompletion = (
    System.Collections.Generic.IReadOnlyList<(string Contents, string ColorId)> Name,
    System.Collections.Generic.IReadOnlyList<(string Contents, string ColorId)> Description,
    string Contents
);

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
    public static IReadOnlyList<ApiRenderPart> GetInspectorRenderParts(object inspector) {
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

    /*
    public static IReadOnlyList<ApiAutoCompletion> GetAutoCompletions(object inspector, int cursorIndex) {
        InspectorSession inspectorSession = AssertIs<InspectorSession>(inspector);

        return inspectorSession.GetAutoCompletions(cursorIndex);
    }
    */
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

    private IReadOnlyList<ApiRenderPart>? _renderParts;

    public IReadOnlyList<ApiRenderPart> GetRenderParts() {
        if (_renderParts is not null)
            return _renderParts;
        
        List<ApiRenderPart> ret = [];

        foreach (var tok in _tokens) {
            var parts = CreateRenderParts(tok, false);
            
            ret.AddRange(parts.Select(p => p.ToApi()));
        }

        return _renderParts = ret;
    }

    public InspectorSession(IExpressionContext ctx) {
        _ctx = ctx;
        _notificationLogger = new NotificationLogger(this);
    }

    internal IReadOnlyList<RenderPart> CreateRenderParts(ExpressionToken token, bool insideStringLiteral) {
        var operand = token.Operand!;
        List<RenderPart> renderParts = token.Kind switch {
            ExpressionToken.Kinds.Add => [ RenderPart.Operator("+") ],
            ExpressionToken.Kinds.Sub => [ RenderPart.Operator("-") ],
            ExpressionToken.Kinds.Mul => [ RenderPart.Operator("*") ],
            ExpressionToken.Kinds.Div => [ RenderPart.Operator("/") ],
            ExpressionToken.Kinds.DivFloat => [ RenderPart.Operator("//") ],
            ExpressionToken.Kinds.Modulo => [ RenderPart.Operator("%") ],
            ExpressionToken.Kinds.Flag => [ token.IsUnaryOnStrings 
                ? RenderPart.Flag("f") 
                : RenderPart.Flag(operand.ToString()!, [
                    RenderPart.Default($"Checks the flag '{operand}', returns 1 if its set, 0 otherwise.")
                ])
            ],
            ExpressionToken.Kinds.Counter => [ token.IsUnaryOnStrings 
                ? RenderPart.Counter("#") 
                : RenderPart.Counter($"#{operand}", [
                    RenderPart.Default($"Gets the value of the counter '{operand}'.")
                ])
            ],
            ExpressionToken.Kinds.Slider => [ token.IsUnaryOnStrings
                ? RenderPart.Slider("@")
                : RenderPart.Slider($"@{operand}", [
                    RenderPart.Default($"Gets the value of the slider '{operand}'.")
                ])
            ],
            ExpressionToken.Kinds.Command => HandleCommandRenderParts(token),
            ExpressionToken.Kinds.Invert => [ RenderPart.Operator("!") ],
            ExpressionToken.Kinds.LitString => [ insideStringLiteral ? RenderPart.StringContent($"{operand}") : RenderPart.StringContent($"\"{operand}\"") ],
            ExpressionToken.Kinds.InterpolatedString => HandleInterpolatedStringRenderParts(token),
            ExpressionToken.Kinds.LitInt => [ RenderPart.Literal(((LiteralOperand<int>)operand).SourceText) ],
            ExpressionToken.Kinds.LitFloat => [ RenderPart.Literal(((LiteralOperand<float>)operand).SourceText) ],
            ExpressionToken.Kinds.Eq => [ RenderPart.Operator("==") ],
            ExpressionToken.Kinds.Ne => [ RenderPart.Operator("!=") ],
            ExpressionToken.Kinds.Lt => [ RenderPart.Operator("<") ],
            ExpressionToken.Kinds.Le => [ RenderPart.Operator("<=") ],
            ExpressionToken.Kinds.Gt => [ RenderPart.Operator(">") ],
            ExpressionToken.Kinds.Ge => [ RenderPart.Operator(">=") ],
            ExpressionToken.Kinds.SingleEquals => [ RenderPart.Operator("=") ],
            ExpressionToken.Kinds.And => [ RenderPart.Operator("&&") ],
            ExpressionToken.Kinds.Or => [ RenderPart.Operator("||") ],
            ExpressionToken.Kinds.BitwiseAnd => [ RenderPart.Operator("&") ],
            ExpressionToken.Kinds.BitwiseOr => [ RenderPart.Operator("|") ],
            ExpressionToken.Kinds.Bracket => HandleBracketRenderParts(token),
            ExpressionToken.Kinds.FieldAccess => HandleFieldAccessRenderParts(token),
            ExpressionToken.Kinds.LambdaArrow => [ RenderPart.Operator("=>") ],
            ExpressionToken.Kinds.UnaryMinus => [ RenderPart.Operator("-") ],
            _ => throw new ArgumentOutOfRangeException()
        };

        if (token.Trivia is not null and not "") {
            renderParts.Insert(0, RenderPart.Trivia(token.Trivia));
        }

        return renderParts;
    }

    private List<RenderPart> HandleInterpolatedStringRenderParts(ExpressionToken token) {
        List<InterpolationHole> operand =
            token.Operand as List<InterpolationHole> ?? throw new UnreachableException();

        List<RenderPart> parts = [
            RenderPart.StringContent("\"")
        ];

        foreach (var argumentTokens in operand) {
            if (argumentTokens.IsLiteral) {
                parts.AddRange(argumentTokens.Tokens.SelectMany(t => CreateRenderParts(t, insideStringLiteral: true)));
            } else {
                parts.Add(RenderPart.Command("$("));
                parts.AddRange(argumentTokens.Tokens.SelectMany(t => CreateRenderParts(t, insideStringLiteral: false)));
                parts.Add(RenderPart.Trivia(argumentTokens.PreEndBracketTrivia));
                parts.Add(RenderPart.Command(")"));
            }
        }
        
        parts.Add(RenderPart.StringContent("\""));
        return parts;
    }

    private List<RenderPart> HandleBracketRenderParts(ExpressionToken token) {
        BracketOperand innerTokens = token.Operand as BracketOperand ?? throw new UnreachableException();

        return [
            RenderPart.Operator("("),
            .. innerTokens.Tokens.SelectMany(t => CreateRenderParts(t, insideStringLiteral: false)),
            RenderPart.Trivia(innerTokens.PreEndBracketTrivia),
            RenderPart.Operator(")")
        ];
    }

    private List<RenderPart> HandleFieldAccessRenderParts(ExpressionToken token) {
        FieldAccessTokenOperand operand = token.Operand as FieldAccessTokenOperand ?? throw new UnreachableException();

        List<RenderPart> parts = [];
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
                RenderPart.Operator("."),
                RenderPart.Field(operand.Name, CreateDescriptionTooltip(descriptor))
            ];

        if (operand.Arguments is not null) {
            parts.Add(RenderPart.Operator("("));
            var first = true;
            foreach (var argumentTokens in operand.Arguments) {
                if (!first) {
                    parts.Add(RenderPart.Operator(","));
                }
                first = false;
                parts.AddRange(argumentTokens.Tokens.SelectMany(t => CreateRenderParts(t, insideStringLiteral: false)));
                if (argumentTokens.PreEndBracketTrivia is not "")
                    parts.Add(RenderPart.Trivia(argumentTokens.PreEndBracketTrivia));
            }
            parts.Add(RenderPart.Operator(")"));
        }

        return parts;
    }

    private List<RenderPart>? CreateDescriptionTooltip(CommandDescriptor? descriptor) {
        if (descriptor is null || descriptor.Description is [])
            return null;

        List<RenderPart> parts = [];
        if (descriptor.DeclaringType is { } type) {
            parts.Add(RenderPart.Type(type));
            parts.Add(RenderPart.Operator("."));
        }
        
        parts.Add(RenderPart.Command(descriptor.Name));

        if (descriptor.Arguments is not []) {
            parts.Add(RenderPart.Operator("("));
            bool first = true;
            foreach (var arg in descriptor.Arguments) {
                if (!first) {
                    parts.Add(RenderPart.Operator(", "));
                }
                first = false;
                
                parts.Add(RenderPart.Type(arg.Type));
                parts.Add(RenderPart.Trivia(" "));
                parts.Add(RenderPart.Default(arg.Name));
            }
            parts.Add(RenderPart.Operator(")"));
        }
        
        if (descriptor.ReturnType != TypeDescriptor.Any) {
            parts.Add(RenderPart.Default(" -> "));
            parts.Add(RenderPart.Type(descriptor.ReturnType));
        }
        parts.Add(RenderPart.Trivia("\n"));
        parts.AddRange(descriptor.Description);

        return parts;
    }

    private List<RenderPart> HandleAccessorParts(string fullOperationText, ConditionHelper.Condition condition) {
        List<RenderPart> parts = [];
        switch (condition)
        {
            case KnownFieldAccessor fieldAccessor:
            {
                var postfix = fieldAccessor.SourceText ?? throw new UnreachableException();
            
                parts.AddRange(HandleAccessorParts(fullOperationText[..^postfix.Length], fieldAccessor.Target));
                if (postfix.StartsWith('.')) {
                    parts.Add(RenderPart.Operator("."));
                    postfix = postfix[1..];
                }
                
                parts.Add(RenderPart.Field(postfix, CreateDescriptionTooltip(fieldAccessor.Descriptor)));
                break;
            }
            case InstanceFunctionCommands.IInstanceFunctionCommand functionCommand: {
                var postfix = condition.Descriptor?.Name ?? throw new UnreachableException();
            
                parts.AddRange(HandleAccessorParts(fullOperationText[..^(postfix.Length + 1)], functionCommand.FieldCondition));
                parts.Add(RenderPart.Operator("."));
                parts.Add(RenderPart.Field(postfix, CreateDescriptionTooltip(condition.Descriptor)));
                break;
            }
            default:
            {
                parts.Add(RenderPart.Command($"${fullOperationText}", CreateDescriptionTooltip(condition.Descriptor)));
                break;
            }
        }
        
        return parts;
    }
    
    private List<RenderPart> HandleCommandRenderParts(ExpressionToken token) {
        CommandTokenOperand operand = token.Operand as CommandTokenOperand ?? throw new UnreachableException();

        CommandDescriptor? descriptor = null;

        List<RenderPart> parts = [];

        using (_ = API.RegisterNotificationSink((_, _) => false)) {
            if (AbstractExpression.Parse([token], out AbstractExpression? expression)
                && ConditionHelper.TryCreate(expression, _ctx, out var condition)) {
                parts = HandleAccessorParts(operand.Name, condition);
                descriptor = condition.Descriptor;
            }
        }

        if (parts.Count == 0) {
            parts = [
                RenderPart.Command($"${operand.Name}", CreateDescriptionTooltip(descriptor)),
            ];  
        }

        if (operand.Arguments is not null) {
            parts.Add(RenderPart.Operator("("));
            var first = true;
            foreach (var argumentTokens in operand.Arguments) {
                if (!first) {
                    parts.Add(RenderPart.Operator(","));
                }
                first = false;
                parts.AddRange(argumentTokens.Tokens.SelectMany(t => CreateRenderParts(t, insideStringLiteral: false)));
                if (argumentTokens.PreEndBracketTrivia is not "")
                    parts.Add(RenderPart.Trivia(argumentTokens.PreEndBracketTrivia));
            }
            parts.Add(RenderPart.Operator(")"));
        }

        return parts;
    }

    private ExpressionToken? GetTokenAt(int cursorIndex, out int tokenStart, out int tokenEnd) {
        var i = 0;
        foreach (var token in _tokens) {
            tokenStart = i;
            var tokenParts = CreateRenderParts(token, false);
            tokenEnd = tokenStart + (token.Trivia?.Length ?? 0) + tokenParts.Sum(p => p.Contents.Length);
            if (tokenEnd <= cursorIndex)
                return token;

            i = tokenEnd + 1;
        }

        tokenStart = -1;
        tokenEnd = -1;
        return null;
    }
    
    public IReadOnlyList<ApiAutoCompletion> GetAutoCompletions(int cursorIndex) {
        if (GetTokenAt(cursorIndex, out var tokenStart, out var tokenEnd) is not { } token)
            return [];

        var session = Engine.Scene.MaybeLevel()?.Session;
        IEnumerable<AutoCompletion> nonApi;
        switch (token.Kind) {
            case ExpressionToken.Kinds.Flag: {
                var currentFlag = token.Operand?.ToString() ?? "";
                nonApi = session?.Flags.Where(f => f.StartsWith(currentFlag, StringComparison.OrdinalIgnoreCase))
                    .Select(f => new AutoCompletion([RenderPart.Flag(f)], [], f[currentFlag.Length..])) ?? [];
                break;
            }
            case ExpressionToken.Kinds.Counter:
                nonApi = [];
                break;
            case ExpressionToken.Kinds.Slider:
                nonApi = [];
                break;
            case ExpressionToken.Kinds.Command:
                nonApi = [];
                break;
            //ExpressionToken.Kinds.InterpolatedString => expr,
            case ExpressionToken.Kinds.FieldAccess:
                nonApi = [];
                break;
            default:
                //ExpressionToken.Kinds.LambdaArrow => expr,
                nonApi = [];
                break;
        }

        return nonApi.Select(x => x.ToApi()).ToList();
    }
    
    private sealed class NotificationLogger(InspectorSession session) : IAbstractExpressionErrorLogger {
        public void Error(string message) {
            session._errorsMutable.Add(message);
        }
    }
}

internal record AutoCompletion(
    IReadOnlyList<RenderPart> Name,
    IReadOnlyList<RenderPart> Description,
    string Contents) {

    public ApiAutoCompletion ToApi() => (
        Name.Select(x => x.ToApiNoTooltip()).ToList(),
        Description.Select(x => x.ToApiNoTooltip()).ToList(),
        Contents
    );
}
