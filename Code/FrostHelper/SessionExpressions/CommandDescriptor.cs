using FrostHelper.API;
using System.Collections.Concurrent;

namespace FrostHelper.SessionExpressions;

internal sealed class TypeDescriptor {
    private static readonly ConcurrentDictionary<Type, TypeDescriptor> Descriptors = new() {
        [typeof(int)] = new TypeDescriptor(typeof(int), "int"),
        [typeof(float)] = new TypeDescriptor(typeof(float), "float"),
        [typeof(string)] = new TypeDescriptor(typeof(string), "string"),
        [typeof(bool)] = new TypeDescriptor(typeof(bool), "bool"),
        [typeof(object)] = new TypeDescriptor(typeof(object), "any"),
    };
    
    public Type CSharpType { get; }
    
    public string CanonName { get; init; }

    public TypeDescriptor(Type type) : this(type, type.Name) {
    }
    
    public TypeDescriptor(Type type, string name) {
        CSharpType = type;
        CanonName = name;
    }

    public static TypeDescriptor Any { get; } = For(typeof(object));
    
    public static TypeDescriptor For(Type type) {
        return Descriptors.GetOrAdd(type, static type => new TypeDescriptor(type));
    }
}

internal sealed class ArgumentDescriptor {
    public required string Name { get; init; }
 
    public required TypeDescriptor Type { get; init; }
}

internal sealed class CommandDescriptor {
    public required string Name { get; init; }
    
    public IReadOnlyList<ApiRenderPart> Description { get; init; }
    
    public string? DeclaringMod { get; init; }
    
    public TypeDescriptor? DeclaringType { get; init; }
    
    public TypeDescriptor ReturnType { get; init; }

    public IReadOnlyList<ArgumentDescriptor> Arguments { get; init; } = [];
}
