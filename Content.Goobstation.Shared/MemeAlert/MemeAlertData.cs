namespace Content.Shared.MemeAlerts;

[DataDefinition]
public sealed partial class MemeAlertData
{
    /// <summary>Текст объявления.</summary>
    [DataField("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>Заголовок/отправитель объявления (вместо "Центральное командование").</summary>
    [DataField("sender")]
    public string Sender { get; set; } = "Центральное командование";

    /// <summary>Цвет текста в чате (hex).</summary>
    [DataField("color")]
    public string Color { get; set; } = "#ffffff";

    /// <summary>Звук при появлении оповещения.</summary>
    [DataField("announce_sound")]
    public string? AnnounceSound { get; set; }

    /// <summary>Звук который начнётся сразу после оповещения.</summary>
    [DataField("during_sound")]
    public string? DuringSound { get; set; }
}
