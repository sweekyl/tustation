using Content.Goobstation.Shared.PhraseWheel;
using Content.Server.Chat.Systems;
using Content.Shared.Chat;
using Robust.Server.Audio;
using Robust.Server.Console;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Console;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Server.PhraseWheel;

public sealed class PhraseWheelSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IServerConsoleHost _console = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<PlayPhraseWheelMessage>(OnPlayPhrase);
        _console.RegisterCommand("phrasewheel", PhraseWheelCommand);
    }

    private void OnPlayPhrase(PlayPhraseWheelMessage msg, EntitySessionEventArgs args)
    {
        var player = args.SenderSession.AttachedEntity;
        if (player == null) return;

        // Проверяем что у игрока есть доступ
        if (!HasComp<PhraseWheelComponent>(player.Value)) return;

        if (!_proto.TryIndex<PhraseWheelEntryPrototype>(msg.PhraseId, out var phrase)) return;

        // Говорим фразу в чат
        var chatType = phrase.ChatType switch
        {
            PhraseWheelChatType.Whisper => InGameICChatType.Whisper,
            PhraseWheelChatType.Emote   => InGameICChatType.Emote,
            PhraseWheelChatType.Shout   => InGameICChatType.Speak,
            _                           => InGameICChatType.Speak,
        };

        _chat.TrySendInGameICMessage(player.Value, phrase.Text, chatType, false);

        // Играем звук если есть
        if (phrase.Sound != null)
        {
            try
            {
                _audio.PlayPvs(phrase.Sound, player.Value,
                    AudioParams.Default.WithVolume(6f).WithMaxDistance(15f));
            }
            catch { }
        }
    }

    /// <summary>
    /// phrasewheel [ник] — выдаёт или забирает доступ к колесу фраз
    /// </summary>
    private void PhraseWheelCommand(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1)
        {
            shell.WriteError("Использование: phrasewheel <ник>");
            return;
        }

        var name = args[0];
        ICommonSession? targetSession = null;

        foreach (var session in _playerManager.Sessions)
        {
            if (session.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                targetSession = session;
                break;
            }
        }

        if (targetSession?.AttachedEntity == null)
        {
            shell.WriteError($"Игрок '{name}' не найден или не в игре.");
            return;
        }

        var uid = targetSession.AttachedEntity.Value;

        if (HasComp<PhraseWheelComponent>(uid))
        {
            RemComp<PhraseWheelComponent>(uid);
            shell.WriteLine($"Доступ к колесу фраз ЗАБРАН у {name}.");
        }
        else
        {
            EnsureComp<PhraseWheelComponent>(uid);
            shell.WriteLine($"Доступ к колесу фраз ВЫДАН игроку {name}.");
        }
    }
}
