// A type alias for a part of a fancy-styled description, used by Session Expressions.
// Contents is the text to render, ColorId decides the color/style used for that text.
// Recognized ColorId's are: "default", "whitespace", "operator", "literal", "string", "flag", "counter", "slider", "field", "command", "type";
global using ApiRenderPart = (string Contents, string ColorId, System.Collections.Generic.IReadOnlyList<(string Contents, string ColorId)>? Tooltip);

namespace FrostHelper.API;


