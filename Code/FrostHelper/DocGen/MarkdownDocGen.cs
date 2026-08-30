using FrostHelper.API;
using FrostHelper.SessionExpressions;
using System.Globalization;
using System.Text;

namespace FrostHelper.DocGen;

internal static class MarkdownDocGen {
    public static string CreateMarkdownDocumentation()
    {
        StringBuilder builder = new StringBuilder();
        
        builder.AppendLine(SessionState);
        builder.AppendLine();
        
        builder.AppendLine(TypesHeader);
        builder.AppendLine();
        
        foreach (var type in TypeDescriptor.AllKnownDescriptors.OrderBy(x => x.CanonName)) {
            builder.AppendLine(CultureInfo.InvariantCulture, $"## `{type.CanonName}`");
            builder.AppendLine(FormatRenderParts(type.Description));

            var firstField = true;
            foreach (var ((_, name), accessorCommand) in FieldAccessCommands.Accessors.Where(x => x.Key.Item1 == type.CSharpType)) {
                if (firstField) {
                    firstField = false;
                    builder.AppendLine("\nFields:");
                }
                
                builder.AppendLine(CultureInfo.InvariantCulture, $"- {FormatCommand(accessorCommand.Descriptor, ".")}");
            }
            
            var firstFunction = true;
            foreach (var ((_, name), function) in InstanceFunctionCommands.Functions.Where(x => x.Key.Item1 == type.CSharpType)) {
                if (firstFunction) {
                    firstFunction = false;
                    builder.AppendLine("\nFunctions:");
                }
                
                builder.AppendLine(CultureInfo.InvariantCulture, $"- {FormatFunction(function.Descriptor, ".")}");
            }

            builder.AppendLine();
        }
        builder.AppendLine();
        
        builder.AppendLine(CommandsHeader);
        builder.AppendLine();
        foreach (var (_, command) in SimpleCommands.Registry.OrderBy(x => x.Key)) {
            builder.AppendLine(CultureInfo.InvariantCulture, $"- {FormatCommand(command.Descriptor, "$")}");
        }
        builder.AppendLine();
        
        builder.AppendLine(FunctionsHeader);
        builder.AppendLine();
        foreach (var (_, command) in FunctionCommands.Registry.OrderBy(x => x.Key)) {
            builder.AppendLine(CultureInfo.InvariantCulture, $"- {FormatFunction(command.Descriptor, "$")}");
        }
        builder.AppendLine();
        
        builder.AppendLine(InputsHeader);

        return builder.ToString();
    }
    
    private static string FormatCommand(CommandDescriptor function, string namePrefix) {
        return
            $"`{namePrefix}{function.Name}` -> {FormatType(function.ReturnType)} - {FormatRenderParts(function.Description)}";
    }

    private static string FormatFunction(CommandDescriptor function, string namePrefix) {
        return
            $"`{namePrefix}{function.Name}({string.Join(", ", function.Arguments.Select(x => $"{x.Type.CanonName} {x.Name}"))})` -> {FormatType(function.ReturnType)} - {FormatRenderParts(function.Description)}";
    }
    
    private static string FormatType(TypeDescriptor descriptor) {
        return $"`{descriptor.CanonName}`";
    }

    private static string FormatType(Type type) {
        var descriptor = TypeDescriptor.For(type);

        return FormatType(descriptor);
    }

    private static string FormatRenderParts(IReadOnlyList<RenderPart>? parts) {
        if (parts is null)
            return "";
        
        StringBuilder builder = new StringBuilder();
        foreach (var part in parts) {
            builder.Append(FormatRenderPart(part));
        }
        
        return builder.ToString();
    }
    
    private static string FormatRenderPart(RenderPart part) {
        return part.ColorId switch {
            RenderPart.LiteralColorId or
            RenderPart.TypeColorId => $"`{part.Contents}`",
            _ => part.Contents
        };
    }
    
    private const string SessionState = """
    # Reading Session State
    - `flagName` - Providing a flag name on its own checks if that flag is set, returning `1` if it is, `0` otherwise.
    - `!flagName` - Inverts the logic for checking flags, returning `0` if the flag is set, `1` otherwise.
    - `#counterName` - Reads the value of a Session Counter (integer) instead of a flag.
    - `@sliderName` - Reads a Session Slider (floating-point number).                                 
    """;
    
    private const string TypesHeader = """
    # Types
    Session Expressions can operate on many types of values:
    """;
    
    private const string CommandsHeader = """
    # Commands
    All commands start with `$`, and allow you to get access to various values, not just those coming from the session. Mods can add their own commands.
    """;
    
    private const string FunctionsHeader = """
    # Functions
    Functions can be called like `$func(arg1, arg2, ...)`:
    """;

    private const string InputsHeader = """
    # Input Commands
    
    ## Buttons
    To read vanilla buttons, you may use any of these Commands:
    - `$input.esc`, `$input.pause`
    - `$input.menuLeft`, `$input.menuRight`, `$input.menuUp`, `$input.menuDown`
    - `$input.menuConfirm`, `$input.menucancel`, `$input.menujournal`
    - `$input.quickrestart`
    - `$input.jump`
    - `$input.dash`
    - `$input.grab`
    - `$input.talk`
    - `$input.crouchDash` - Demodash
    
    Modded buttons can be read via `$input.mod.modName.buttonName`, for example `$input.mod.MaxHelpingHand.ShowHints`.
    
    By default, these commands return 1 if the button is held, 0 if it isn't. If you wish to check for something else, you can add a suffix to the command:
    - `.check` - default behavior.
    - `.pressed` - Whether the button just got pressed.
    - `.released` - Whether the button is not held.
    
    For example, `$input.grab.pressed` checks if the player has just pressed the Grab button.
    
    ## Directional Inputs
    To read directional inputs, you can use these commands:
    - `$input.aim` - `Vector2` - used for dashing.
    - `$input.feather` - `Vector2` - used for flying in feathers.
    - `$input.mountainaim` - `Vector2` - used in the overworld.
    
    These values are Vector2's containing floats in the range [-1, 1]. When playing on a controller, these might be non-integer values, but on keyboard, they will always be -1, 0 or 1. Make sure to keep this in mind, and check the *sign* of the values (using `< 0` or `> 0`) in most cases.
    
    For example `$input.aim.y < 0` checks if the player is aiming downwards.
    """;
}