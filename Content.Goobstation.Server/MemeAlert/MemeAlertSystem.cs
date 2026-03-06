using System.Collections.Generic;
using System.IO;
using Content.Server.Administration;
using Content.Server.Chat.Systems;
using Content.Shared.Administration;
using Content.Shared.MemeAlerts;
using Robust.Server.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Content.Server.MemeAlerts;

public sealed class MemeAlertSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem        _chat  = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IResourceManager  _res   = default!;

    private Dictionary<string, MemeAlertData> _alerts = new();

    public override void Initialize()
    {
        base.Initialize();
        LoadAlerts();
    }

    public void LoadAlerts()
    {
        _alerts.Clear();

        var path = new ResPath("/MemeAlerts/meme_alerts.yml");

        if (!_res.TryContentFileRead(path, out var stream))
        {
            Logger.Warning($"[MemeAlerts] Файл не найден: {path}");
            return;
        }

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        using var reader = new StreamReader(stream);
        var root = deserializer.Deserialize<AlertsRoot>(reader);

        if (root?.Alerts == null)
        {
            Logger.Warning("[MemeAlerts] YAML пустой или неверный формат.");
            return;
        }

        foreach (var (key, raw) in root.Alerts)
        {
            _alerts[key] = new MemeAlertData
            {
                Message       = raw.Message       ?? string.Empty,
                Sender        = raw.Sender        ?? "Центральное командование",
                Color         = raw.Color         ?? "#ffffff",
                AnnounceSound = raw.AnnounceSound,
                DuringSound   = raw.DuringSound,
            };
        }

        Logger.Info($"[MemeAlerts] Загружено алертов: {_alerts.Count}");
    }

    public bool TryShowAlert(string alertId)
    {
        if (!_alerts.TryGetValue(alertId, out var data))
            return false;

        _chat.DispatchGlobalAnnouncement(
            data.Message,
            sender: data.Sender,
            colorOverride: Color.TryFromHex(data.Color),
            playSound: false
        );

        if (!string.IsNullOrEmpty(data.AnnounceSound))
            _audio.PlayGlobal(data.AnnounceSound, Filter.Broadcast(), recordReplay: true);

        if (!string.IsNullOrEmpty(data.DuringSound))
            _audio.PlayGlobal(data.DuringSound, Filter.Broadcast(), recordReplay: true);

        return true;
    }

    public IEnumerable<string> GetAlertIds() => _alerts.Keys;

    private sealed class AlertsRoot
    {
        public Dictionary<string, RawAlert>? Alerts { get; set; }
    }

    private sealed class RawAlert
    {
        public string? Message       { get; set; }
        public string? Sender        { get; set; }
        public string? Color         { get; set; }
        [YamlMember(Alias = "announce_sound")]
        public string? AnnounceSound { get; set; }
        [YamlMember(Alias = "during_sound")]
        public string? DuringSound   { get; set; }
    }
}

// ══════════════════════════════════════════════════════════════
//  Консольная команда: admemealert <id>
// ══════════════════════════════════════════════════════════════

[AdminCommand(AdminFlags.Admin)]
public sealed class AdMemeAlertCommand : IConsoleCommand
{
    public string Command     => "admemealert";
    public string Description => "Показать кастомный алерт всем игрокам.";
    public string Help        => "admemealert <id> | admemealert list | admemealert reload";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var system = IoCManager.Resolve<IEntitySystemManager>()
                               .GetEntitySystem<MemeAlertSystem>();

        if (args.Length == 0)
        {
            shell.WriteError("Укажи ID алерта. Список: admemealert list");
            return;
        }

        switch (args[0].ToLower())
        {
            case "list":
                var ids = string.Join(", ", system.GetAlertIds());
                shell.WriteLine($"Доступные алерты: {(string.IsNullOrEmpty(ids) ? "(нет)" : ids)}");
                return;

            case "reload":
                system.LoadAlerts();
                shell.WriteLine("[MemeAlerts] Конфиг перезагружен.");
                return;

            default:
                if (!system.TryShowAlert(args[0]))
                    shell.WriteError($"Алерт '{args[0]}' не найден. Список: admemealert list");
                else
                    shell.WriteLine($"[MemeAlerts] Алерт '{args[0]}' отправлен.");
                return;
        }
    }
}
