using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using Microsoft.Extensions.Logging;

namespace EarlyNades;

public class EarlyNades : BasePlugin
{
    public override string ModuleName => "EarlyNades";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "✪ Stαr";
    public override string ModuleDescription => "allow grenades be thrown sooner after switching to them.";

    private float _delay = 0.8f;

    private static readonly string[] Grenades =
    {
        "weapon_hegrenade", "weapon_flashbang", "weapon_smokegrenade",
        "weapon_molotov", "weapon_incgrenade", "weapon_decoy", "weapon_tagrenade"
    };

    private string ConfigFile => Path.Combine(ModuleDirectory, "EarlyNade.json");

    public override void Load(bool hotReload)
    {
        _delay = LoadDelay();
        RegisterListener<Listeners.OnEntitySpawned>(OnEntitySpawned);

        if (hotReload)
            ApplyAll();

        Logger.LogInformation($"EarlyNades loaded, delay {_delay}s");
    }

    private void OnEntitySpawned(CEntityInstance entity)
    {
        if (entity is null || !entity.IsValid)
            return;

        if (!Grenades.Contains(entity.DesignerName))
            return;

        Apply(entity);
    }

    private void Apply(CEntityInstance entity)
    {
        var vdata = entity.As<CCSWeaponBase>()?.VData;
        if (vdata is not null)
            vdata.DeployDuration = _delay;
    }

    private void ApplyAll()
    {
        foreach (var name in Grenades)
            foreach (var nade in Utilities.FindAllEntitiesByDesignerName<CCSWeaponBase>(name))
                Apply(nade);
    }

    [ConsoleCommand("css_earlynades_delay", "Set how long until a grenade can be thrown (seconds)")]
    [RequiresPermissions("@css/generic")]
    public void OnDelayCommand(CCSPlayerController? player, CommandInfo info)
    {
        if (info.ArgCount < 2)
        {
            info.ReplyToCommand($"[EarlyNades] Delay is {_delay}s");
            return;
        }

        if (!float.TryParse(info.GetArg(1), NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            || value < 0f || value > 2f)
        {
            info.ReplyToCommand("[EarlyNades] Usage: css_earlynades_delay <0.0 - 2.0>");
            return;
        }

        _delay = value;
        ApplyAll();
        SaveDelay(value);
        info.ReplyToCommand($"[EarlyNades] Delay set to {value}s");
    }

    private float LoadDelay()
    {
        try
        {
            if (File.Exists(ConfigFile))
            {
                var cfg = JsonSerializer.Deserialize<Config>(File.ReadAllText(ConfigFile));
                if (cfg is not null)
                    return Math.Clamp(cfg.Delay, 0f, 2f);
            }
        }
        catch
        {
        }

        SaveDelay(_delay);
        return _delay;
    }

    private void SaveDelay(float value)
    {
        try
        {
            var json = JsonSerializer.Serialize(new Config { Delay = value },
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigFile, json);
        }
        catch
        {
        }
    }

    private class Config
    {
        [JsonPropertyName("earlynades_delay")]
        public float Delay { get; set; } = 0.8f;
    }
}
