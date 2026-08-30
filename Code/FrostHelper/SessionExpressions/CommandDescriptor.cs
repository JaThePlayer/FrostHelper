using FrostHelper.API;
using System.Collections.Concurrent;

namespace FrostHelper.SessionExpressions;

public sealed class TypeDescriptor {
    static TypeDescriptor() {
        Descriptors = [];
        
        Descriptors[typeof(object)] = Any = new TypeDescriptor(typeof(object), "any") {
            Description = [RenderPart.Default("Represents that any type is allowed.")],
        };

        Descriptors[typeof(bool)] = new TypeDescriptor(typeof(bool), "bool") {
            Description = [
                RenderPart.Default("True/False value, represented as "),
                RenderPart.Literal("1"),
                RenderPart.Default(" or "),
                RenderPart.Literal("0"),
                RenderPart.Default(".")
            ],
        };
        
        Descriptors[typeof(int)] = new TypeDescriptor(typeof(int), "int") {
            Description = [RenderPart.Default("32-bit integer value, can be negative.")],
        };

        Descriptors[typeof(float)] = new TypeDescriptor(typeof(float), "float") {
            Description = [RenderPart.Default("32-bit floating-point value, e.g. `1.312`.")],
        };

        Descriptors[typeof(string)] = new TypeDescriptor(typeof(string), "string") {
            Description = [RenderPart.Default("UTF-16 text, e.g. `\"hello\"`.")],
        };

        Descriptors[typeof(Vector2)] = new TypeDescriptor(typeof(Vector2), "Vector2") {
            Description =
                [RenderPart.Default("A pair of 2 floats, available via `.x` and `.y` fields, e.g. `$vec(3, 4)`.")],
        };

        Descriptors[typeof(Entity)] = new TypeDescriptor(typeof(Entity), "Entity") {
            Description = [RenderPart.Default("A Celeste Entity, e.g. `$player`")],
        };

        Descriptors[typeof(Player)] = new TypeDescriptor(typeof(Player), "Player") {
            Description = [
                RenderPart.Default("A Player object, obtained via `$player`. Also acts as an "),
                RenderPart.Type(For(typeof(Entity))),
                RenderPart.Default("."),
            ],
        };

        Descriptors[typeof(IEnumerable)] = new TypeDescriptor(typeof(IEnumerable), "IEnumerable") {
            Description = [RenderPart.Default("Represents a collection of elements, e.g. `$strawberries`")],
        };

        Descriptors[typeof(Color)] = new TypeDescriptor(typeof(Color), "Color") {
            Description = [
                RenderPart.Default(
                    "Represents an RGBA color. Strings and ints can be implicitly converted to colors if needed.")
            ],
        };

        Descriptors[typeof(EntityID)] = new TypeDescriptor(typeof(EntityID), "EntityID") {
            Description = [RenderPart.Default("An EntityID object, used by the game to uniquely identify an entity.")],
        };

        Descriptors[typeof(LambdaCondition)] = new TypeDescriptor(typeof(LambdaCondition), "lambda") {
            Description = [
                RenderPart.Default("""
                A sub-expression that can be evaluated by a Session Expression function.
                A lambda can accept arguments, their meaning depends on the function they are passed to.
                For example in an expression:
                `$strawberries.sum($s => $s.roomName == "coolRoom")`,
                `$s =>` means that the lambda accepts one argument, named `$s`.
                Everything after the `=>` arrow is the contents of the lambda expression and can use `$s` to access the argument.
                Arguments can be named anything.
                """)
            ],
        };
    }

    private static readonly ConcurrentDictionary<Type, TypeDescriptor> Descriptors;
    
    public Type CSharpType { get; }
    
    public string CanonName { get; init; }
    
    public IReadOnlyList<RenderPart> Description { get; init; }

    private TypeDescriptor(Type type) : this(type, CreateCanonName(type)) {
    }

    private static string CreateCanonName(Type type) {
        var generics = type.GenericTypeArguments;
        if (generics.Length == 0)
            return type.Name;
        
        return $"{type.Name[..^2]}<{string.Join(", ", generics.Select(t => For(t).CanonName))}>";
    }
    
    private TypeDescriptor(Type type, string name) {
        CSharpType = type;
        CanonName = name;
        Description = [];
    }

    public static TypeDescriptor Any { get; }
    
    public static TypeDescriptor For(Type type) {
        return Descriptors.GetOrAdd(type, static type => new TypeDescriptor(type));
    }

    public static IEnumerable<TypeDescriptor> AllKnownDescriptors => Descriptors.Values.Where(x => x.Description is not []);
}

public sealed class ArgumentDescriptor(string name, TypeDescriptor type) {
    public string Name { get; init; } = name;

    public TypeDescriptor Type { get; init; } = type;

    public static ArgumentDescriptor VarargFor(TypeDescriptor descriptor)
        => new ArgumentDescriptor("...", descriptor);
}

public sealed class CommandDescriptor {
    public required string Name { get; init; }
    
    public IReadOnlyList<RenderPart> Description { get; init; }
    
    public string? DeclaringMod { get; init; }
    
    public TypeDescriptor? DeclaringType { get; init; }
    
    public TypeDescriptor ReturnType { get; init; }

    public IReadOnlyList<ArgumentDescriptor> Arguments { get; init; } = [];
}
