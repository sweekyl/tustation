using System.Net.Http;
using System.Text;
using System.Text.Json;
using Content.Goobstation.Common.CCVar;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;

namespace Content.Goobstation.Server.DiscordLogger;

public sealed class DiscordPlayerLogger : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IServerNetManager _net = default!;

    private static readonly HttpClient Http = new();
    private string _webhookUrl = string.Empty;

    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(GoobCVars.DiscordPlayerWebhook,
            url => _webhookUrl = url, true);

        _net.Connected += OnConnected;
        _net.Disconnect += OnDisconnected;
    }

    private void OnConnected(object? sender, NetChannelArgs e)
    {
        SendWebhook($"✅ **{e.Channel.UserName}** подключился к серверу");
    }

    private void OnDisconnected(object? sender, NetDisconnectedArgs e)
    {
        SendWebhook($"❌ **{e.Channel.UserName}** отключился от сервера");
    }

    private void SendWebhook(string message)
    {
        if (string.IsNullOrEmpty(_webhookUrl))
            return;

        var payload = JsonSerializer.Serialize(new { content = message });
        var content = new StringContent(payload, Encoding.UTF8, "application/json");
        Http.PostAsync(_webhookUrl, content);
    }
}
