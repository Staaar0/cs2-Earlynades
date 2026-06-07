using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using Microsoft.Extensions.Logging;

namespace EarlyNades;

public class EarlyNades : BasePlugin
{
    public override string ModuleName => "EarlyNades";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "✪ Stαr";
    public override string ModuleDescription =>
        "Shortens grenade deploy time so they can be thrown sooner.";

    private float _deploy = 0.8f;

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
            ApplyToAllExisting();
            info.ReplyToCommand($"[EarlyNades] Deploy duration set to {_deploy.ToString("0.###", CultureInfo.InvariantCulture)}s");
        }
        else
        {
            info.ReplyToCommand("[EarlyNades] Usage: css_earlynades_delay <seconds 0.0-2.0>");
        }
    }
}
