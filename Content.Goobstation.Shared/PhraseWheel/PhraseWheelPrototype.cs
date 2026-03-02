using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Goobstation.Shared.PhraseWheel;

/// <summary>
/// Одна фраза в колесе. Добавляй сколько угодно в YAML.
/// </summary>
[Prototype("phraseWheelEntry")]
public sealed partial class PhraseWheelEntryPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>Текст фразы — говорится в чат</summary>
    [DataField(required: true)]
    public string Text { get; private set; } = string.Empty;

    /// <summary>Тип сообщения: Speak, Whisper, Emote</summary>
    [DataField]
    public PhraseWheelChatType ChatType { get; private set; } = PhraseWheelChatType.Speak;

    /// <summary>Звук который играет при произношении фразы. Можно не указывать.</summary>
    [DataField]
    public SoundSpecifier? Sound { get; private set; }

    /// <summary>Иконка в колесе</summary>
    [DataField]
    public SpriteSpecifier Icon { get; private set; } =
        new SpriteSpecifier.Texture(new("/Textures/Interface/emotes.svg.192dpi.png"));

    /// <summary>Цвет кнопки в меню (hex)</summary>
    [DataField]
    public Color Color { get; private set; } = Color.MediumPurple;

    /// <summary>Подпись под иконкой</summary>
    [DataField]
    public string Label { get; private set; } = string.Empty;
}

public enum PhraseWheelChatType : byte
{
    Speak,
    Whisper,
    Emote,
    Shout,
}
