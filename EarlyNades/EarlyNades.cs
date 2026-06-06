using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using Microsoft.Extensions.Logging;

namespace EarlyNades;

// Shortens the grenade pull-out (deploy) time by writing m_flDeployDuration
// directly into each grenade's weapon-data in the server's memory at runtime.
// This is authoritative on the server regardless of whether a workshop vdata
// addon is mounted, and uses no signatures.
public class EarlyNades : BasePlugin
{
    public override string ModuleName => "Early Nades";
    public override string ModuleVersion => "3.0.0";
    public override string ModuleAuthor => "EarlyNades";
    public override string ModuleDescription =>
        "Shortens grenade deploy time so they can be thrown sooner.";

    // Desired deploy duration in seconds. Default game value is 1.0.
    private float _deploy = 0.8f;

    // Remember which grenade types we've already logged, to keep the console clean.
    private readonly HashSet<string> _logged = new();

    private static readonly HashSet<string> GrenadeNames = new()
    {
        "weapon_hegrenade",
        "weapon_flashbang",
        "weapon_smokegrenade",
        "weapon_molotov",
        "weapon_incgrenade",
        "weapon_decoy",
        "weapon_tagrenade",
    };

    public override void Load(bool hotReload)
    {
        RegisterListener<Listeners.OnEntitySpawned>(OnEntitySpawned);

        // Only sweep already-existing grenades on a HOT RELOAD, when the entity
        // system is already up. On a cold server start the entity system isn't
        // initialized yet (calling entity lookups there throws), and
        // OnEntitySpawned will catch grenades as they get created during play.
        if (hotReload)
            ApplyToAllExisting();

        Logger.LogInformation("=== Early Nades v3 LOADED. deploy={Deploy}s ===", _deploy);
    }

    private void OnEntitySpawned(CEntityInstance entity)
    {
        try
        {
            if (entity is null || !entity.IsValid)
                return;

            string name = entity.DesignerName;
            if (!GrenadeNames.Contains(name))
                return;

            ApplyToWeapon(entity, name);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Early Nades: failed to apply deploy time on spawn");
        }
    }

    private void ApplyToWeapon(CEntityInstance entity, string name)
    {
        var weapon = entity.As<CCSWeaponBase>();
        var vdata = weapon?.VData;
        if (vdata is null)
            return;

        // VData is shared per weapon type, so this effectively sets it for all
        // grenades of this kind. Writing every spawn is cheap and idempotent.
        // DeployDuration is the generated property for the schema member
        // "m_flDeployDuration" on CBasePlayerWeaponVData.
        float old = vdata.DeployDuration;

        if (_logged.Add(name))
            Logger.LogInformation(
                "EarlyNades: {Weapon} deploy {Old:0.###}s -> {New:0.###}s", name, old, _deploy);

        vdata.DeployDuration = _deploy;
    }

    private void ApplyToAllExisting()
    {
        foreach (var name in GrenadeNames)
        {
            try
            {
                // The enumeration itself can throw if the entity system isn't
                // ready, so keep it inside the try.
                foreach (var ent in Utilities.FindAllEntitiesByDesignerName<CCSWeaponBase>(name))
                    ApplyToWeapon(ent, name);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Early Nades: existing-entity sweep failed for {Name}", name);
            }
        }
    }

    [ConsoleCommand("css_earlynades_delay",
        "Grenade deploy duration in seconds (lower = throw sooner)")]
    public void OnDelayCommand(CCSPlayerController? player, CommandInfo info)
    {
        if (info.ArgCount < 2)
        {
            info.ReplyToCommand($"[EarlyNades] Current deploy duration: {_deploy.ToString("0.###", CultureInfo.InvariantCulture)}s");
            return;
        }

        if (float.TryParse(info.GetArg(1), NumberStyles.Float, CultureInfo.InvariantCulture, out float v)
            && v >= 0.0f && v <= 2.0f)
        {
            _deploy = v;
            _logged.Clear();
            ApplyToAllExisting(); // re-apply to grenades currently in the world
            info.ReplyToCommand($"[EarlyNades] Deploy duration set to {_deploy.ToString("0.###", CultureInfo.InvariantCulture)}s");
        }
        else
        {
            info.ReplyToCommand("[EarlyNades] Usage: css_earlynades_delay <seconds 0.0-2.0>");
        }
    }
}
