using System.Diagnostics.CodeAnalysis;

namespace FrostHelper.Helpers;

internal readonly record struct CsvArrayWithTricks(EquatableArray<int> Array) : IDetailedParsable<CsvArrayWithTricks> {
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out CsvArrayWithTricks result,
        [NotNullWhen(false)] out string? errorMessage) {
        try {
            result = new CsvArrayWithTricks(Calc.ReadCSVIntWithTricks(s.ToString()));
            errorMessage = null;
            return true;
        } catch (Exception ex) {
            result = default;
            errorMessage = ex.Message;
            return false;
        }
    }
}