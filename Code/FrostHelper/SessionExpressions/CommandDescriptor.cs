using FrostHelper.API;
using System.Collections.Concurrent;

namespace FrostHelper.SessionExpressions;

public sealed class TypeDescriptor {
    private static readonly ConcurrentDictionary<Type, TypeDescriptor> Descriptors = new() {
        [typeof(int)] = new TypeDescriptor(typeof(int), "int"),
        [typeof(float)] = new TypeDescriptor(typeof(float), "float"),
        [typeof(string)] = new TypeDescriptor(typeof(string), "string"),
        [typeof(bool)] = new TypeDescriptor(typeof(bool), "bool"),
        [typeof(object)] = new TypeDescriptor(typeof(object), "any"),
    };
    
    public Type CSharpType { get; }
    
    public string CanonName { get; init; }

    private TypeDescriptor(Type type) : this(type, type.Name) {
    }
    
    private TypeDescriptor(Type type, string name) {
        CSharpType = type;
        CanonName = name;
    }

    public static TypeDescriptor Any { get; } = For(typeof(object));
    
    public static TypeDescriptor For(Type type) {
        return Descriptors.GetOrAdd(type, static type => new TypeDescriptor(type));
    }
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
