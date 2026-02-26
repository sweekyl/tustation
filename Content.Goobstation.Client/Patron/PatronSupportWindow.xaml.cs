using Content.Client.UserInterface.Controls;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;

namespace Content.Goobstation.Client.Patron;

public sealed partial class PatronSupportWindow : FancyWindow
{
    public Button PatreonButton { get; private set; } = default!;

    public PatronSupportWindow()
    {
        RobustXamlLoader.Load(this);
        PatreonButton = FindControl<Button>("PatreonButton");
    }
}
