using System.Linq;
using Content.Goobstation.Common.CCVar;
using Content.Server.Chat.Managers;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Goobstation.Server.Verify;

public sealed class VerifySystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IServerNetManager _net = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    // Ники игроков онлайн которые можно верифицировать
    // username → UserId
    public static readonly Dictionary<string, NetUserId> OnlinePlayers = new(StringComparer.OrdinalIgnoreCase);

    private readonly List<(NetUserId UserId, string Message, TimeSpan SendAt)> _pendingMessages = new();

    private string _discordLink = string.Empty;

    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(GoobCVars.VerifyDiscordLink,
            link => _discordLink = link, true);

        _net.Connected += OnConnected;
        _net.Disconnect += OnDisconnected;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.RealTime;
        var toSend = _pendingMessages.Where(x => now >= x.SendAt).ToList();

        foreach (var item in toSend)
        {
            _pendingMessages.Remove(item);

            if (!_playerManager.TryGetSessionById(item.UserId, out var session))
                continue;

            _chatManager.DispatchServerMessage(session, item.Message);
        }
    }

    private void OnConnected(object? sender, NetChannelArgs e)
    {
        var userId = e.Channel.UserId;
        var username = e.Channel.UserName;

        OnlinePlayers[username] = userId;

        var msg =
            $"Добро пожаловать на сервер!\n" +
            $"Для получения всех игровых ролей верифицируйся в Discord:\n" +
            $"Напиши боту: !takeroles {username}\n" +
            $"Наш Discord: {_discordLink}";

        _pendingMessages.Add((userId, msg, _timing.RealTime + TimeSpan.FromSeconds(5)));
    }

    private void OnDisconnected(object? sender, NetDisconnectedArgs e)
    {
        OnlinePlayers.Remove(e.Channel.UserName);
    }
}
