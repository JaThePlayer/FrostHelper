using System.Globalization;
using System.Text.Json.Serialization;

namespace FrostHelper.Helpers;

internal sealed record LiteralOperand<T>(T Value, string SourceText);

internal class CommandTokenOperand(string name)
{
    public string Name { get; set; } = name;

    public List<BracketOperand>? Arguments { get; set; }
}

internal class BracketOperand {
    public required string PreEndBracketTrivia { get; init; }
    
    public required List<ExpressionToken> Tokens { get; init; }
}

internal record InterpolationHole(bool IsLiteral) {
    public required string PreEndBracketTrivia { get; init; }
    
    public required List<ExpressionToken> Tokens { get; init; }
}

internal class FieldAccessTokenOperand(string name)
{
    public string Name { get; set; } = name;

    public List<ExpressionToken>? ObjectTokens { get; set; }
    
    /// <summary>
    /// For syntax like field.method(arg1, ...), stores the function arguments.
    /// </summary>
    public List<BracketOperand>? Arguments { get; set; }
}

internal interface IAbstractExpressionErrorLogger {
    void Error(string message);
}

internal class NotificationHelperErrorLogger : IAbstractExpressionErrorLogger {
    public void Error(string message) {
        NotificationHelper.Notify(message);
    }

    public static readonly NotificationHelperErrorLogger Instance = new();
}

internal class ExpressionToken {
    /// <summary>
    /// Whitespace before the token.
    /// </summary>
    public string? Trivia { get; set; }
    
    public Kinds Kind { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Operand { get; set; }

    public ExpressionToken(Kinds k, string? trivia, object? operand = null) {
        Kind = k;
        Operand = operand;
        Trivia = trivia;
    }

    [JsonIgnore]
    public bool IsUnaryOnStrings => Kind switch {
        Kinds.Counter when Operand is "" => true,
        Kinds.Slider when Operand is "" => true,
        Kinds.Flag when Operand is "f" => true,
        // Kinds.Command when Operand is CommandTokenOperand { Arguments: [] } => true,
        _ => false,
    };

    [JsonIgnore]
    public bool ShouldTreatNextSubAsUnaryMinus => Kind is
        Kinds.Add or Kinds.Sub or Kinds.Mul or Kinds.Div or Kinds.DivFloat or Kinds.Modulo
        or Kinds.UnaryMinus;

    public static TokenizerState Tokenize(ReadOnlySpan<char> input, IAbstractExpressionErrorLogger logger, out List<ExpressionToken> tokens) {
        var parser = new SpanParser(input);
        return Tokenize(ref parser, 0, logger, out tokens, out _);
    }

    private static TokenizerState Tokenize(ref SpanParser parser, int bracketDepth, IAbstractExpressionErrorLogger logger, out List<ExpressionToken> tokens, out string? endingTrivia) {
        tokens = [];

        var previousRemaining = parser.Remaining;
        
        while (!parser.IsEmpty) {
            parser.TrimStart(out var leftTrivia);

            if (parser.TryTrimPrefix("==")) {
                tokens.Add(new ExpressionToken(Kinds.Eq, leftTrivia));
                continue;
            }
            if (parser.TryTrimPrefix("=>")) {
                tokens.Add(new ExpressionToken(Kinds.LambdaArrow, leftTrivia));
                continue;
            }
            if (parser.TryTrimPrefix("!=")) {
                tokens.Add(new ExpressionToken(Kinds.Ne, leftTrivia));
                continue;
            }
            if (parser.TryTrimPrefix(">=")) {
                tokens.Add(new ExpressionToken(Kinds.Ge, leftTrivia));
                continue;
            }
            if (parser.TryTrimPrefix(">")) {
                tokens.Add(new ExpressionToken(Kinds.Gt, leftTrivia));
                continue;
            }
            if (parser.TryTrimPrefix("<=")) {
                tokens.Add(new ExpressionToken(Kinds.Le, leftTrivia));
                continue;
            }
            if (parser.TryTrimPrefix("<")) {
                tokens.Add(new ExpressionToken(Kinds.Lt, leftTrivia));
                continue;
            }
            if (parser.TryTrimPrefix("=")) {
                tokens.Add(new ExpressionToken(Kinds.SingleEquals, leftTrivia));
                continue;
            }

            if (parser.TryTrimPrefix("+")) {
                if (tokens.Count == 0 || tokens[^1].ShouldTreatNextSubAsUnaryMinus) {
                } else {
                    tokens.Add(new ExpressionToken(Kinds.Add, leftTrivia));
                }
                continue;
            }
            if (parser.TryTrimPrefix("-")) {
                if (tokens.Count == 0 || tokens[^1].ShouldTreatNextSubAsUnaryMinus) {
                    tokens.Add(new ExpressionToken(Kinds.UnaryMinus, leftTrivia));
                } else {
                    tokens.Add(new ExpressionToken(Kinds.Sub, leftTrivia));
                }
                continue;
            }
            if (parser.TryTrimPrefix("*")) {
                tokens.Add(new ExpressionToken(Kinds.Mul, leftTrivia));
                continue;
            }
            if (parser.TryTrimPrefix("//")) {
                tokens.Add(new ExpressionToken(Kinds.DivFloat, leftTrivia));
                continue;
            }
            if (parser.TryTrimPrefix("/")) {
                tokens.Add(new ExpressionToken(Kinds.Div, leftTrivia));
                continue;
            }
            if (parser.TryTrimPrefix("%")) {
                tokens.Add(new ExpressionToken(Kinds.Modulo, leftTrivia));
                continue;
            }
            if (parser.TryTrimPrefix("&&")) {
                tokens.Add(new ExpressionToken(Kinds.And, leftTrivia));
                continue;
            }
            if (parser.TryTrimPrefix("&")) {
                tokens.Add(new ExpressionToken(Kinds.BitwiseAnd, leftTrivia));
                continue;
            }
            if (parser.TryTrimPrefix("||")) {
                tokens.Add(new ExpressionToken(Kinds.Or, leftTrivia));
                continue;
            }
            if (parser.TryTrimPrefix("|")) {
                tokens.Add(new ExpressionToken(Kinds.BitwiseOr, leftTrivia));
                continue;
            }

            if (parser.TryTrimPrefix("#")) {
                tokens.Add(new ExpressionToken(Kinds.Counter, leftTrivia, ReadWord(ref parser).ToString()));
                continue;
            }
            if (parser.TryTrimPrefix("@")) {
                tokens.Add(new ExpressionToken(Kinds.Slider, leftTrivia, ReadWord(ref parser).ToString()));
                continue;
            }
            if (parser.TryTrimPrefix("!")) {
                tokens.Add(new ExpressionToken(Kinds.Invert, leftTrivia));
                continue;
            }

            if (parser.TryTrimPrefix("$")) {
                var cmdName = ReadWord(ref parser).ToString();
                var operand = new CommandTokenOperand(cmdName);

                if (parser.TryTrimPrefix("(")) {
                    TokenizerState inner;
                    bool closed = false;
                    while ((inner = Tokenize(ref parser, 1, logger, out var innerTokens, out var preEndBracketTrivia)) is TokenizerState.Comma
                           or TokenizerState.EndBracket) {
                        operand.Arguments ??= [];
                        operand.Arguments.Add(new BracketOperand { Tokens = innerTokens, PreEndBracketTrivia = preEndBracketTrivia ?? ""});
                        if (inner is TokenizerState.EndBracket) {
                            closed = true;
                            break;
                        }
                    }

                    if (!closed) {
                        logger.Error($"Unclosed bracket in command argument list for ${cmdName}");
                        endingTrivia = null;
                        return TokenizerState.Error;
                    }
                }
                
                // Try finding field access
                switch (TryCreateFieldAccess(ref parser, tokens, [ new ExpressionToken(Kinds.Command, leftTrivia, operand) ], logger)) {
                    case TokenizerState.Normal:
                        break;
                    case null:
                        tokens.Add(new ExpressionToken(Kinds.Command, leftTrivia, operand));
                        break;
                    default:
                        endingTrivia = null;
                        return TokenizerState.Error;
                }
                continue;
            }

            if (parser.TryTrimPrefix("(")) {
                if (Tokenize(ref parser, 1, logger, out var innerTokens, out var preEndBracketTrivia) is not TokenizerState.EndBracket) {
                    endingTrivia = preEndBracketTrivia;
                    logger.Error("Unclosed bracket.");
                    return TokenizerState.Error;
                }
                
                // Try finding field access
                switch (TryCreateFieldAccess(ref parser, tokens, innerTokens, logger)) {
                    case TokenizerState.Normal:
                        break;
                    case null:
                        tokens.Add(new ExpressionToken(Kinds.Bracket, leftTrivia, new BracketOperand {
                            PreEndBracketTrivia = preEndBracketTrivia ?? "",
                            Tokens = innerTokens,
                        }));
                        break;
                    default:
                        endingTrivia = preEndBracketTrivia;
                        return TokenizerState.Error;
                }
                continue;
            }

            if (parser.TryTrimPrefix(")")) {
                bracketDepth--;
                if (bracketDepth <= 0) {
                    endingTrivia = leftTrivia;
                    return TokenizerState.EndBracket;
                }
                continue;
            }

            if (parser.TryTrimPrefix(",")) {
                endingTrivia = leftTrivia;
                return TokenizerState.Comma;
            }

            if (parser.TryTrimPrefix("\"")) {
                List<InterpolationHole> holes = [];

                while (true) {
                    if (!ReadStrLiteralUntilEndOrHole(ref parser, out var innerWord)) {
                        endingTrivia = null;
                        logger.Error("Unclosed parenthesis for string literal.");
                        return TokenizerState.Error;
                    }

                    if (parser.TryTrimPrefix("\"")) {
                        ExpressionToken? stringToken;

                        if (holes.Count == 0)
                            stringToken = new ExpressionToken(Kinds.LitString, leftTrivia, innerWord.ToString());
                        else {
                            if (!innerWord.IsEmpty)
                                holes.Add(new InterpolationHole(true) {
                                    PreEndBracketTrivia = "",
                                    Tokens = [new ExpressionToken(Kinds.LitString, leftTrivia, innerWord.ToString())],
                                });
                            stringToken = new ExpressionToken(Kinds.InterpolatedString, leftTrivia, holes);
                        }
                        
                        // Try finding field access
                        switch (TryCreateFieldAccess(ref parser, tokens, [ stringToken ], logger)) {
                            case TokenizerState.Normal:
                                break;
                            case null:
                                tokens.Add(stringToken);
                                break;
                            default:
                                endingTrivia = null;
                                return TokenizerState.Error;
                        }

                        break;
                    }

                    if (parser.TryTrimPrefix("$(")) {
                        if (Tokenize(ref parser, 1, logger, out var innerTokens, out var preEndTrivia) is not
                            TokenizerState.EndBracket) {
                            endingTrivia = null;
                            logger.Error("Unclosed string interpolation hole bracket.");
                            return TokenizerState.Error;
                        }

                        if (!innerWord.IsEmpty)
                            holes.Add(new InterpolationHole(true) {
                                PreEndBracketTrivia = "",
                                Tokens = [new ExpressionToken(Kinds.LitString, leftTrivia, innerWord.ToString())],
                            });

                        if (innerTokens.Count > 0) {
                            holes.Add(new InterpolationHole(false) {
                                PreEndBracketTrivia = preEndTrivia ?? "",
                                Tokens = innerTokens,
                            });
                        }
                    } else {
                        endingTrivia = null;
                        logger.Error("'$' inside string literal not followed by '('.");
                        return TokenizerState.Error;
                    }
                }

                continue;
            }

            if (parser.IsEmpty) {
                endingTrivia = leftTrivia;
                return TokenizerState.End;
            }

            var rem = parser.Remaining;

            var word = ReadWord(ref parser);
            if (!word.IsEmpty) {
                if (int.TryParse(word, CultureInfo.InvariantCulture, out var intLit)) {
                    tokens.Add(new ExpressionToken(Kinds.LitInt, leftTrivia, new LiteralOperand<int>(intLit, word.ToString())));
                } else if (float.TryParse(word, CultureInfo.InvariantCulture, out var floatLit)) {
                    tokens.Add(new ExpressionToken(Kinds.LitFloat, leftTrivia, new LiteralOperand<float>(floatLit, word.ToString())));
                } else {
                    tokens.Add(new ExpressionToken(Kinds.Flag, leftTrivia, word.ToString()));
                }
                continue;
            }
            
            if (parser.Remaining == previousRemaining) {
                logger.Error("Tokenizer looped infinitely, this is probably a bug!");
                endingTrivia = leftTrivia;
                return TokenizerState.Error;
            }
            previousRemaining = parser.Remaining;
        }

        endingTrivia = "";
        return TokenizerState.End;

        static ReadOnlySpan<char> ReadWord(ref SpanParser input) {
            var idx = input.Remaining.IndexOfAny("+-*/&|#@$(),\"!=<> ");
            if (idx < 0) {
                return input.ReadStr();
            }

            return input.ReadStr(idx);
        }

        static bool ReadStrLiteralUntilEndOrHole(ref SpanParser input, out ReadOnlySpan<char> word) {
            var idx = input.Remaining.IndexOfAny("\"$");
            if (idx < 0) {
                word = input.ReadStr();
                return false;
            }

            word = input.ReadStr(idx);
            return true;
        }

        static TokenizerState? TryCreateFieldAccess(ref SpanParser parser, List<ExpressionToken> tokens, 
            List<ExpressionToken> innerTokens, IAbstractExpressionErrorLogger logger) {
            string leftTrivia = "";
            
            if (parser.TryTrimPrefix(".")) {
                var cmdName = ReadWord(ref parser).ToString();
                if (cmdName.Length == 0) {
                    logger.Error("Missing field name after '.'");
                    return TokenizerState.Error;
                }

                var operand = new FieldAccessTokenOperand(cmdName) { ObjectTokens = innerTokens };
                if (parser.TryTrimPrefix("(")) {
                    TokenizerState inner;
                    bool closed = false;
                    while ((inner = Tokenize(ref parser, 1, logger, out var innerArgTokens, out var preEndTrivia)) is TokenizerState.Comma
                           or TokenizerState.EndBracket) {
                        operand.Arguments ??= [];
                        operand.Arguments.Add(new BracketOperand {
                            Tokens = innerArgTokens,
                            PreEndBracketTrivia = preEndTrivia ?? ""
                        });
                        if (inner is TokenizerState.EndBracket) {
                            closed = true;
                            break;
                        }
                    }

                    if (!closed) {
                        logger.Error("Unclosed bracket in function argument list.");
                        return TokenizerState.Error;
                    }
                    if (inner is TokenizerState.Error)
                        return TokenizerState.Error;
                    
                    // Try finding chained field access
                    switch (TryCreateFieldAccess(ref parser, tokens, [], logger)) {
                        case TokenizerState.Normal:
                            var chainedToken = tokens[^1];
                            var op = (FieldAccessTokenOperand) chainedToken.Operand!;
                            op.ObjectTokens = [ new ExpressionToken(Kinds.FieldAccess, leftTrivia, operand) ];
                            return TokenizerState.Normal;
                        case null:
                            break;
                        default:
                            return TokenizerState.Error;
                    }
                }
                    
                tokens.Add(new ExpressionToken(Kinds.FieldAccess, leftTrivia, operand));
                return TokenizerState.Normal;
            }

            return null;
        }
    }


    public enum Kinds {
        Add, Sub, Mul, Div, DivFloat, Modulo,
        Flag, Counter, Slider, Command, Invert,
        LitString, InterpolatedString, LitInt, LitFloat,
        Eq, Ne, Lt, Le, Gt, Ge,
        SingleEquals,

        And, Or,
        BitwiseAnd, BitwiseOr,

        Bracket,
        FieldAccess,
        
        LambdaArrow,
        UnaryMinus,
    }

    public enum TokenizerState {
        Normal,
        Comma,
        EndBracket,
        End,
        Error,
    }
}
