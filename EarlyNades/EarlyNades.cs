using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration; // <-- ConsoleCommand attribute lives here
using CounterStrikeSharp.API.Modules.Commands;
using Microsoft.Extensions.Logging;

namespace EarlyNades;

// Lets players throw grenades sooner after pulling one out.
// By default CS2 makes you wait ~1.0s after switching to a nade before it can
// be thrown. This plugin shortens that window (default 0.8s).
public class EarlyNades : BasePlugin
{
    public override string ModuleName => "Early Nades";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "EarlyNades";
    public override string ModuleDescription =>
        "Allows players to throw grenades a bit earlier after switching to them.";

    // CS2 dedicated servers tick at 64 Hz.
    private const float TickInterval = 1.0f / 64.0f;

    // Seconds after pulling out a nade before it can be thrown.
    // Game default is ~1.0s. Change the number here, or live with
    // the "css_earlynades_delay" console command.
    private float _throwDelay = 0.8f;

    private const int MaxSlots = 64;

    // Per-player state, indexed by player slot.
    private readonly uint[] _lastWeaponIndex = new uint[MaxSlots];
    private readonly int[] _pinnedTick = new int[MaxSlots];

    // Every throwable utility in CS2.
    private static readonly HashSet<string> GrenadeNames = new()
    {
        "weapon_hegrenade",
        "weapon_flashbang",
        "weapon_smokegrenade",
        "weapon_molotov",
        "weapon_incgrenade",
        "weapon_decoy",
        "weapon_tagrenade", // snowball / tactical awareness grenade
    };

    public override void Load(bool hotReload)
    {
        for (int i = 0; i < MaxSlots; i++)
            _pinnedTick[i] = -1;

        RegisterListener<Listeners.OnTick>(OnTick);
        Logger.LogInformation("Early Nades loaded. Throw delay = {Delay}s", _throwDelay);
    }

    private void OnTick()
    {
        int delayTicks = (int)Math.Round(_throwDelay / TickInterval);
        int now = Server.TickCount;

        foreach (var player in Utilities.GetPlayers())
        {
            try
            {
                if (player is null || !player.IsValid)
                    continue;

                int slot = player.Slot;
                if (slot < 0 || slot >= MaxSlots)
                    continue;

                var pawn = player.PlayerPawn?.Value;
                if (pawn is null || !pawn.IsValid)
                {
                    Reset(slot);
                    continue;
                }

                var active = pawn.WeaponServices?.ActiveWeapon?.Value;
                if (active is null || !active.IsValid)
                {
                    Reset(slot);
                    continue;
                }

                // Not a grenade -> nothing to do, just remember it.
                if (!GrenadeNames.Contains(active.DesignerName))
                {
                    _lastWeaponIndex[slot] = active.Index;
                    _pinnedTick[slot] = -1;
                    continue;
                }

                // Did the player just switch to this grenade this tick?
                if (active.Index != _lastWeaponIndex[slot])
                {
                    _lastWeaponIndex[slot] = active.Index;

                    int target = now + delayTicks;

                    // Only help when the game would otherwise force a LONGER wait.
                    // (Never make a nade slower than the game already allows.)
                    _pinnedTick[slot] = active.NextPrimaryAttackTick > target ? target : -1;
                }

                // While inside the shortened window, hold the throw-allowed time
                // fixed at our target so the game can't push it back out.
                if (_pinnedTick[slot] != -1)
                {
                    if (now >= _pinnedTick[slot])
                    {
                        // Window passed; hand control back to the game.
                        _pinnedTick[slot] = -1;
                    }
                    else
                    {
                        active.NextPrimaryAttackTick = _pinnedTick[slot];
                        active.NextPrimaryAttackTickRatio = 0.0f;
                        Utilities.SetStateChanged(active, "CBasePlayerWeapon", "m_nNextPrimaryAttackTick");
                    }
                }
            }
            catch (Exception ex)
            {
                // Never let one bad player entity kill the whole tick loop.
                Logger.LogWarning(ex, "Early Nades: tick handler error for a player");
            }
        }
    }

    private void Reset(int slot)
    {
        _lastWeaponIndex[slot] = 0;
        _pinnedTick[slot] = -1;
    }

    // Tune the delay live from the server console (or rcon): css_earlynades_delay 0.8
    [ConsoleCommand("css_earlynades_delay",
        "Seconds before a grenade can be thrown after switching to it")]
    public void OnDelayCommand(CCSPlayerController? player, CommandInfo info)
    {
        if (info.ArgCount < 2)
        {
            info.ReplyToCommand($"[EarlyNades] Current throw delay: {_throwDelay.ToString("0.###", CultureInfo.InvariantCulture)}s");
            return;
        }

        if (float.TryParse(info.GetArg(1), NumberStyles.Float, CultureInfo.InvariantCulture, out float v)
            && v >= 0.0f && v <= 2.0f)
        {
            _throwDelay = v;
            info.ReplyToCommand($"[EarlyNades] Throw delay set to {_throwDelay.ToString("0.###", CultureInfo.InvariantCulture)}s");
        }
        else
        {
            info.ReplyToCommand("[EarlyNades] Usage: css_earlynades_delay <seconds 0.0-2.0>");
        }
    }
}
