using FrostHelper.Helpers;
using System.Diagnostics.CodeAnalysis;
using static FrostHelper.Helpers.ConditionHelper;

namespace FrostHelper.SessionExpressions;

internal static class InputCommands {
    public static bool TryParseInput(string inputString, [NotNullWhen(true)] out Condition? condition) {
        string inputName;
        string action;
        var nextDotIdx = inputString.LastIndexOf('.');
        if (nextDotIdx == -1) {
            inputName = inputString;
            action = "";
        } else {
            inputName = inputString[..nextDotIdx];
            action = inputString[(nextDotIdx + 1)..];
        }

        VirtualInput? input = inputName.ToLowerInvariant() switch {
            "esc" => Input.ESC,
            "pause" => Input.Pause,
            "menuleft" => Input.MenuLeft,
            "menuright" => Input.MenuRight,
            "menuup" => Input.MenuUp,
            "menudown" => Input.MenuDown,
            "menuconfirm" => Input.MenuConfirm,
            "menucancel" => Input.MenuCancel,
            "menujournal" => Input.MenuJournal,
            "quickrestart" => Input.QuickRestart,
            "aim" => Input.Aim,
            "feather" => Input.Feather,
            "mountainaim" => Input.MountainAim,
            /*
            public static VirtualIntegerAxis MoveY;
            public static VirtualIntegerAxis GliderMoveY;
             */
            "jump" => Input.Jump,
            "dash" => Input.Dash,
            "grab" => Input.Grab,
            "talk" => Input.Talk,
            "crouchdash" => Input.CrouchDash,
            _ => null,
        };

        if (input is null && inputName.StartsWith("mod.", StringComparison.OrdinalIgnoreCase)) {
            EverestModule? FindMod(ReadOnlySpan<char> modNameSpan) {
                var modName = modNameSpan.ToString();
                return Everest.Modules.FirstOrDefault(m =>
                    m.Metadata.Name.Equals(modName, StringComparison.OrdinalIgnoreCase));
            }

            // formatted like `$input.mod.MaxHelpingHand.ShowHints`
            if (!inputName.AsSpan()["mod.".Length..].ParsePair('.', out var modNameSpan, out var settingName)) {
                if (action is not "") {
                    // Didn't find another dot, but the part after 'mod.' might be a valid mod name.
                    // That means no action was provided explicitly, so
                    settingName = action;
                    inputName = $"{inputName}.{action}"; // for logging purposes
                    action = "";
                } else {
                    NotificationHelper.Notify(
                        $"Tried to access mod input, but no input name is provided. '{inputName}'");
                    condition = null;
                    return false;
                }
            }

            var module = FindMod(modNameSpan);
            if (module is null) {
                NotificationHelper.Notify(
                    $"Tried to get mod input '{inputName}', but mod '{modNameSpan}' is not loaded.");
                condition = null;
                return false;
            }

            if (module?.SettingsType is null) {
                NotificationHelper.Notify(
                    $"Tried to get input '{inputName}', but mod '{modNameSpan}' does not have settings.");
                condition = null;
                return false;
            }

            PropertyInfo? matchingInput;
            try {
                var props = module.SettingsType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                var settingNameStr = settingName.ToString();
                matchingInput = props.First(p =>
                    p.Name.Equals(settingNameStr, StringComparison.OrdinalIgnoreCase)
                    && p.PropertyType.IsAssignableTo(typeof(ButtonBinding))
                    && p.GetMethod is { });
            } catch (Exception ex) {
                matchingInput = null;
            }

            var val = matchingInput?.GetGetMethod()?.Invoke(module._Settings, null) as ButtonBinding;
            if (val?.Button != null) {
                input = val.Button;
            } else {
                NotificationHelper.Notify(
                    $"Tried to get mod input {inputName},\nbut public ButtonBinding property not found in '{module.SettingsType}'");
                condition = null;
                return false;
            }
        }

        switch (input) {
            case VirtualButton button: {
                OperatorCheckButton.Modes mode = action.ToLowerInvariant() switch {
                    "check" or "" => OperatorCheckButton.Modes.Check,
                    "repeating" => OperatorCheckButton.Modes.Repeating,
                    "pressed" => OperatorCheckButton.Modes.Pressed,
                    "released" => OperatorCheckButton.Modes.Released,
                    _ => OperatorCheckButton.Modes.Unknown,
                };

                if (mode == OperatorCheckButton.Modes.Unknown) {
                    NotificationHelper.Notify($"Unrecognized button action: {action}");
                    condition = null;
                    return false;
                }

                condition = new OperatorCheckButton(button, mode);
                return true;
            }
            case VirtualJoystick joystick: {
                OperatorCheckJoystick.Modes mode = action.ToLowerInvariant() switch {
                    "x" => OperatorCheckJoystick.Modes.X,
                    "y" => OperatorCheckJoystick.Modes.Y,
                    _ => OperatorCheckJoystick.Modes.Unknown,
                };

                if (mode == OperatorCheckJoystick.Modes.Unknown) {
                    NotificationHelper.Notify($"Unrecognized joystick action: {action}");
                    condition = null;
                    return false;
                }

                condition = new OperatorCheckJoystick(joystick, mode);
                return true;
            }

            default: {
                if (input is not { }) {
                    NotificationHelper.Notify($"Cannot find input with name '{inputName}'");
                    condition = null;
                    return false;
                }

                NotificationHelper.Notify($"Cannot use Session Expressions with input type '{input.GetType()}'");
                condition = null;
                return false;
            }
        }
    }


    private sealed class OperatorCheckButton(VirtualButton button, OperatorCheckButton.Modes mode) : Condition {
        public override object Get(Session session) {
            return mode switch {
                Modes.Check => button.Check ? 1 : 0,
                Modes.Repeating => button.Repeating ? 1 : 0,
                Modes.Pressed => button.Pressed ? 1 : 0,
                Modes.Released => button.Released ? 1 : 0,
                _ => 0
            };
        }

        public override bool OnlyChecksFlags() => false;
        
        internal enum Modes {
            Check,
            Repeating,
            Pressed,
            Released,
            Unknown = -1,
        }
    }
    
    private sealed class OperatorCheckJoystick(VirtualJoystick joystick, OperatorCheckJoystick.Modes mode) : Condition {
        public override object Get(Session session) {
            return mode switch {
                Modes.X => joystick.Value.X,
                Modes.Y => joystick.Value.Y,
                _ => 0
            };
        }

        public override bool OnlyChecksFlags() => false;
        
        internal enum Modes {
            X,
            Y,
            Unknown = -1,
        }
    }
}