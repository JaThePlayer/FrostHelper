using FrostHelper.ModIntegration;
using System.Diagnostics.CodeAnalysis;

namespace FrostHelper.Helpers;

internal readonly record struct Recovery(int DashRecovery, int StaminaRecovery, int JumpRecovery) : ISpanParsable<Recovery> {
    internal struct SavedPlayerData(Player player) {
        public int PrevDashes = player.Dashes;
        public float PrevStamina = player.Stamina;
        public int PrevJumps = ExtVariantsAPI.LoadIfNeeded() ? ExtVariantsAPI.GetJumpCount?.Invoke() ?? 1 : 1;
    }

    /// <summary>
    /// Sentinel value which makes you not recover anything.
    /// </summary>
    internal const int RecoveryIsIgnored = 10001;
    
    /// <summary>
    /// Sentinel value which refills your dashes/stamina instead of adding/removing from it.
    /// </summary>
    internal const int RecoveryIsARefill = 10000;

    public static Recovery DefaultRefill => new(RecoveryIsARefill, RecoveryIsARefill, RecoveryIsIgnored);

    public bool ShowPostcardIfNeeded(string entityName) {
        if (JumpRecovery != RecoveryIsIgnored && (!ExtVariantsAPI.LoadIfNeeded() || ExtVariantsAPI.GetCurrentVariantValue is null)) {
            PostcardHelper.Start($"{entityName} with custom Jump Recovery is used, but Extended Variants is not loaded! Report this to the mapmaker.");
            return true;
        }

        return false;
    }
    
    public SavedPlayerData SavePlayerData(Player player) => new(player);

    public bool CanUse(SavedPlayerData data) {
        if (DashRecovery < 0 && data.PrevDashes < -DashRecovery)
            return false;
        if (StaminaRecovery < 0 && data.PrevStamina < -StaminaRecovery)
            return false;
        if (JumpRecovery < 0 && data.PrevJumps < -JumpRecovery)
            return false;

        return true;
    }

    public void Recover(Player player, SavedPlayerData data) {
        RecoverDashes(player, data.PrevDashes, true);
        RecoverStamina(player, data.PrevStamina, true);
        RecoverJumps(player, data.PrevJumps);
    }
    
    public void RecoverDashes(Player player, int prevDashes, bool refilledPreviously) {
        switch (DashRecovery) {
            case RecoveryIsARefill:
                if (!refilledPreviously)
                    player.RefillDash();
                break;
            case RecoveryIsIgnored:
                player.Dashes = prevDashes;
                break;
            case > RecoveryIsIgnored:
                NotificationHelper.Notify($"Dash Recovery value of {DashRecovery} is invalid and reserved for future use.\nPlease use a different value!");
                break;
            case < 0:
                player.Dashes = prevDashes + DashRecovery;
                break;
            default:
                player.Dashes = DashRecovery;
                break;
        }
    }

    public void RecoverStamina(Player player, float prevStamina, bool refilledPreviously) {
        switch (StaminaRecovery) {
            case RecoveryIsARefill:
                if (!refilledPreviously)
                    player.RefillStamina();
                break;
            case RecoveryIsIgnored:
                player.Stamina = prevStamina;
                break;
            case > RecoveryIsIgnored:
                NotificationHelper.Notify($"StaminaRecovery value of {StaminaRecovery} is invalid and reserved for future use.\nPlease use a different value!");
                break;
            case < 0:
                player.Stamina = prevStamina + StaminaRecovery;
                break;
            default:
                player.Stamina = StaminaRecovery;
                break;
        }
    }

    public void RecoverJumps(Player player, int prevJumps) {
        switch (JumpRecovery) {
            case RecoveryIsARefill:
                ExtVariantsAPI.SetJumpCount!(int.Max(ExtVariantsAPI.GetVariantInt(ExtVariantsAPI.Variant.JumpCount, 1) - 1, prevJumps));
                break;
            case RecoveryIsIgnored:
                break;
            case > RecoveryIsIgnored:
                NotificationHelper.Notify($"Jump Recovery value of {JumpRecovery} is invalid and reserved for future use.\nPlease use a different value!");
                break;
            case < 0:
                ExtVariantsAPI.SetJumpCount!(prevJumps + JumpRecovery);
                break;
            default:
                ExtVariantsAPI.SetJumpCount!(JumpRecovery);
                break;
        }
    }

    public static Recovery Parse(string s, IFormatProvider? provider) => Parse(s.AsSpan(), provider);

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out Recovery result)
        => TryParse(s.AsSpan(), provider, out result);

    public static Recovery Parse(ReadOnlySpan<char> s, IFormatProvider? provider) {
        if (!TryParse(s, provider, out var result)) {
            NotificationHelper.Notify($"Failed to parse {s} as a Recovery.");
        }

        return result;
    }

    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Recovery result) {
        var parser = new SpanParser(s);
        result = default;
        if (!parser.ReadUntil<int>(';').TryUnpack(out var dash))
            return false;
        if (!parser.ReadUntil<int>(';').TryUnpack(out var stamina))
            return false;
        if (!parser.Read<int>().TryUnpack(out var jumps))
            return false;
        if (!parser.IsEmpty)
            return false;
        
        result = new Recovery(dash, stamina, jumps);
        return true;
    }
}