using Content.Goobstation.Shared.IAmAtomic;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using System.Numerics;
using Robust.Client.GameObjects;

namespace Content.Goobstation.Client.IAmAtomic;

/// <summary>
/// Рисуется под всеми объектами — расширяющееся фиолетовое пятно на полу.
/// </summary>
public sealed class IAmAtomicFloorOverlay : Overlay
{
    private readonly IEntityManager _entManager;
    private readonly IGameTiming _timing;
    private readonly TransformSystem _transform;
    private readonly Texture _glowTex;
    private readonly Texture _ringTex;
    private ShaderInstance _shader;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    public IAmAtomicFloorOverlay(IEntityManager entManager, IGameTiming timing, IResourceCache resCache, IPrototypeManager proto)
    {
        _entManager = entManager;
        _timing = timing;
        _transform = _entManager.System<TransformSystem>();
        _glowTex = resCache.GetResource<TextureResource>("/Textures/Objects/Goobstation/IAmAtomic/atomic_fx.rsi/glow.png").Texture;
        _ringTex = resCache.GetResource<TextureResource>("/Textures/Objects/Goobstation/IAmAtomic/atomic_fx.rsi/ring.png").Texture;
        _shader = proto.Index<ShaderPrototype>("unshaded").Instance();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;
        handle.UseShader(_shader);

        var t = (float)_timing.RealTime.TotalSeconds;
        var query = _entManager.EntityQueryEnumerator<IAmAtomicComponent, TransformComponent>();

        while (query.MoveNext(out _, out var comp, out var xform))
        {
            if (!comp.IsCasting) continue;
            if (xform.MapID != args.MapId) continue;

            var pos = _transform.GetWorldPosition(xform);
            var p = comp.CastProgress;
            var maxR = comp.VisualRadius;
            var isGlowing = comp.IsGlowing;

            DrawFloor(handle, pos, t, p, maxR, isGlowing);
        }

        handle.SetTransform(Matrix3x2.Identity);
        handle.UseShader(null);
    }

    private void DrawFloor(DrawingHandleWorld h, Vector2 pos, float t, float p, float maxR, bool isGlowing)
    {
        var pulse = 0.5f + 0.5f * MathF.Sin(t * 2f);
        var curR = maxR * Math.Min(p * 1.1f, 1f);

        // === ОСНОВНОЕ ПЯТНО НА ПОЛУ — нарастает с прогрессом ===
        // Тёмная подложка — почти непрозрачная в центре
        DrawTex(h, _glowTex, pos, (float)curR * 2f,
            new Color(0.15f, 0f, 0.3f, p * 0.55f));

        // Средний слой
        DrawTex(h, _glowTex, pos, (float)curR * 1.3f,
            new Color(0.25f, 0f, 0.5f, p * 0.45f));

        // Яркий центр
        DrawTex(h, _glowTex, pos, (float)curR * 0.6f,
            new Color(0.4f, 0f, 0.7f, p * 0.4f));

        // === КОЛЬЦА НА ПОЛУ ===
        // Граница зоны
        DrawRing(h, pos, (float)curR, 0f,
            new Color(0.7f, 0f, 1f, (0.3f + p * 0.4f) * (0.6f + 0.4f * MathF.Sin(t * 3f))));

        // Внешнее свечение границы
        DrawTex(h, _glowTex, pos, ((float)curR + 1.5f) * 2f,
            new Color(0.3f, 0f, 0.6f, p * 0.15f));

        // === СПИРАЛЬ — кольца расходятся наружу ===
        var waveCount = isGlowing ? 6 : 4;
        var waveSpeed = isGlowing ? 0.8f : 0.5f;
        for (var w = 0; w < waveCount; w++)
        {
            var phase = ((t * waveSpeed + w * (1f / waveCount)) % 1f);
            var wR = (float)curR * phase;
            var wA = (1f - phase) * p * (isGlowing ? 0.45f : 0.3f);
            if (wA > 0.02f && wR > 0.5f)
                DrawRing(h, pos, wR, 0f, new Color(0.6f, 0f, 1f, wA));
        }

        // === В РЕЖИМЕ СИЯНИЯ — пол ярче и пульсирует ===
        if (isGlowing)
        {
            DrawTex(h, _glowTex, pos, maxR * 2.2f,
                new Color(0.3f, 0f, 0.7f, 0.2f + pulse * 0.1f));

            // Яркое пятно в центре
            DrawTex(h, _glowTex, pos, 4f,
                new Color(0.6f, 0f, 1f, 0.35f + pulse * 0.2f));
        }
    }

    private void DrawTex(DrawingHandleWorld h, Texture tex, Vector2 center, float diameter, Color color)
    {
        if (color.A < 0.01f || diameter <= 0f) return;
        var half = diameter / 2f;
        h.SetTransform(Matrix3x2.Identity);
        h.DrawTextureRect(tex, new Box2(center.X - half, center.Y - half, center.X + half, center.Y + half), color);
    }

    private void DrawRing(DrawingHandleWorld h, Vector2 center, float radius, float rot, Color color)
    {
        if (color.A < 0.01f || radius <= 0f) return;
        var half = radius;
        h.SetTransform(Matrix3x2.CreateRotation(rot, center));
        h.DrawTextureRect(_ringTex, new Box2(center.X - half, center.Y - half, center.X + half, center.Y + half), color);
        h.SetTransform(Matrix3x2.Identity);
    }
}