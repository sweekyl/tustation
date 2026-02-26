using Content.Goobstation.Common.CCVar;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using System.Numerics;

namespace Content.Goobstation.Client.Hardcode;

public sealed class HardcodeZoomSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;

    private bool _enabled;
    private float _zoomLevel;

    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(GoobCVars.HardcodeZoomEnabled,
            v => { _enabled = v; ApplyZoom(); }, true);

        _cfg.OnValueChanged(GoobCVars.HardcodeZoomLevel,
            v => { _zoomLevel = v; ApplyZoom(); }, true);
    }

    private void ApplyZoom()
    {
        var eye = _eyeManager.CurrentEye;
        if (eye == null)
            return;

        eye.Zoom = _enabled
            ? new Vector2(_zoomLevel, _zoomLevel)
            : new Vector2(1f, 1f);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_enabled)
            return;

        var eye = _eyeManager.CurrentEye;
        if (eye == null)
            return;

        var target = new Vector2(_zoomLevel, _zoomLevel);
        if (eye.Zoom != target)
            eye.Zoom = target;
    }
}
