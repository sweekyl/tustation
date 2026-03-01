using Content.Goobstation.Shared.IAmAtomic;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using System.Numerics;

namespace Content.Goobstation.Client.IAmAtomic;

public sealed class IAmAtomicOverlay : Overlay
{
    private readonly IEntityManager _entManager;
    private readonly IGameTiming _timing;
    private readonly TransformSystem _transform;
    private readonly IPlayerManager _playerManager;

    private readonly Texture _ringTex;
    private readonly Texture _arcTex;
    private readonly Texture _spikeTex;
    private readonly Texture _glowTex;
    private readonly Texture _starTex;

    private ShaderInstance _shader;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public IAmAtomicOverlay(IEntityManager entManager, IGameTiming timing, IResourceCache resCache, IPrototypeManager proto, IPlayerManager playerManager)
    {
        _entManager = entManager;
        _timing = timing;
        _transform = _entManager.System<TransformSystem>();
        _playerManager = playerManager;

        _ringTex  = resCache.GetResource<TextureResource>("/Textures/Objects/Goobstation/IAmAtomic/atomic_fx.rsi/ring.png").Texture;
        _arcTex   = resCache.GetResource<TextureResource>("/Textures/Objects/Goobstation/IAmAtomic/atomic_fx.rsi/arc.png").Texture;
        _spikeTex = resCache.GetResource<TextureResource>("/Textures/Objects/Goobstation/IAmAtomic/atomic_fx.rsi/spike.png").Texture;
        _glowTex  = resCache.GetResource<TextureResource>("/Textures/Objects/Goobstation/IAmAtomic/atomic_fx.rsi/glow.png").Texture;
        _starTex  = resCache.GetResource<TextureResource>("/Textures/Objects/Goobstation/IAmAtomic/atomic_fx.rsi/star.png").Texture;

        _shader = proto.Index<ShaderPrototype>("unshaded").Instance();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;
        handle.UseShader(_shader);

        var t = (float)_timing.RealTime.TotalSeconds;
        var query = _entManager.EntityQueryEnumerator<IAmAtomicComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            if (!comp.IsCasting) continue;
            if (xform.MapID != args.MapId) continue;

            var pos = _transform.GetWorldPosition(xform);
            var p = comp.CastProgress;
            var maxR = comp.VisualRadius;
            var isGlowing = comp.IsGlowing;
            var curR = (float)(maxR * Math.Min(p * 1.3f, 1f));

            // === ЛЕВИТАЦИЯ — только визуальная через SetTransform ===
            // Считаем визуальное смещение персонажа
            var bobSpeed = isGlowing ? 3.5f : 2f;
            var bobHeight = 0.08f + p * 0.18f;
            var bob = MathF.Sin(t * bobSpeed) * bobHeight + 0.1f + p * 0.12f;
            var sway = MathF.Sin(t * (isGlowing ? 2.5f : 1.5f) + 1f) * (isGlowing ? 0.06f : 0.03f);
            if (isGlowing)
            {
                sway += MathF.Sin(t * 18f) * 0.015f * Math.Max(p - 0.4f, 0f);
                bob  += MathF.Sin(t * 7f) * 0.02f;
            }
            var visualOffset = new Vector2(sway, bob);

            // Эффекты всегда от реальной позиции
            DrawPlayerWithOffset(handle, uid, visualOffset, t, p, isGlowing);

            if (isGlowing)
                DrawGlowPhase(handle, pos, t, p, maxR, curR);
            else
                DrawNormalPhase(handle, pos, t, p, maxR, curR);
        }

        handle.SetTransform(Matrix3x2.Identity);
        handle.UseShader(null);
    }

    private void DrawPlayerWithOffset(DrawingHandleWorld handle, EntityUid uid, Vector2 offset, float t, float p, bool isGlowing)
    {
        // Рисуем дополнительное свечение на реальной позиции игрока
        // Само тело двигается через SpriteSystem в отдельной системе,
        // но одежда привязана к тому же Transform — поэтому двигаем всё через SetTransform

        if (!_entManager.TryGetComponent<SpriteComponent>(uid, out var sprite))
            return;

        if (!_entManager.TryGetComponent<TransformComponent>(uid, out var xform))
            return;

        var realPos = _transform.GetWorldPosition(xform);
        var visualPos = realPos + offset;

        // Смещаем матрицу трансформации для рисования
        // Это влияет ТОЛЬКО на последующие DrawTextureRect вызовы
        var matrix = Matrix3x2.CreateTranslation(offset);

        // Рисуем свечение вокруг визуальной позиции
        var pg = 0.5f + 0.5f * MathF.Sin(t * 4f);
        handle.SetTransform(Matrix3x2.Identity);
        DrawTexCentered(handle, _glowTex, visualPos, 2.5f, new Color(0.8f, 0.1f, 1f, 0.4f * pg * p));
        DrawTexCentered(handle, _glowTex, visualPos, 1.2f, new Color(1f, 0.5f, 1f, 0.6f * pg * p));
    }

    private void DrawNormalPhase(DrawingHandleWorld h, Vector2 pos, float t, float p, float maxR, float curR)
    {
        var pulse = 0.6f + 0.4f * MathF.Sin(t * 2.5f);

        // Свечение вокруг игрока — только маленькое
        DrawTexCentered(h, _glowTex, pos, 3.0f, new Color(0.7f, 0.1f, 1f, 0.35f * p));
        DrawTexCentered(h, _glowTex, pos, 1.5f, new Color(1f, 0.4f, 1f, 0.5f * p));

        // Граница взрыва — главное кольцо, яркое и чёткое
        DrawRingTex(h, pos, maxR, 0f, new Color(1f, 0.85f, 1f, 0.95f * pulse));
        DrawRingTex(h, pos, maxR + 0.4f, 0f, new Color(0.8f, 0.1f, 1f, 0.6f * pulse));
        DrawRingTex(h, pos, maxR + 0.9f, 0f, new Color(0.5f, 0f, 0.9f, 0.25f * pulse));

        // Граница прогресса — нарастающее кольцо
        if (curR > 1f && curR < maxR - 0.5f)
        {
            var pp = 0.7f + 0.3f * MathF.Sin(t * 6f);
            DrawRingTex(h, pos, curR, 0f, new Color(1f, 0.6f, 1f, 0.85f * pp));
            DrawRingTex(h, pos, curR + 0.3f, 0f, new Color(0.7f, 0f, 1f, 0.4f * pp));
        }

        // Волны — исходят из центра наружу
        for (var w = 0; w < 3 + (int)(p * 3f); w++)
        {
            var phase = ((t * 0.5f + w * 0.45f) % 1f);
            var wR = maxR * phase;
            var wA = (1f - phase) * p * 0.5f;
            if (wA > 0.02f && wR > 0.5f)
                DrawRingTex(h, pos, wR, 0f, new Color(0.85f, 0.2f, 1f, wA));
        }

        // Зубцы по границе
        var spikeCount = 10 + (int)(p * 10f);
        for (var i = 0; i < spikeCount; i++)
        {
            var angle = i / (float)spikeCount * MathF.Tau + t * 0.5f;
            var spikeLen = (0.25f + 0.4f * MathF.Abs(MathF.Sin(t * 2.5f + i * 0.8f))) * p;
            var spikeAlpha = (0.6f + 0.35f * MathF.Sin(t * 3f + i)) * p;
            DrawSpike(h, pos, angle, maxR, spikeLen, new Color(1f, 0.6f, 1f, spikeAlpha));
        }

        // Звёздочки по границе — пульсируют
        var starCount = 6 + (int)(p * 6f);
        for (var i = 0; i < starCount; i++)
        {
            var angle = i / (float)starCount * MathF.Tau + t * 0.3f;
            var starPulse = 0.5f + 0.5f * MathF.Sin(t * 4f + i * 1.3f);
            var starSize = (0.4f + 0.3f * starPulse) * p;
            var starPos = pos + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * maxR;
            DrawTexCentered(h, _starTex, starPos, starSize, new Color(1f, 0.7f, 1f, starPulse * p));
        }

        // Дуги вращающиеся
        var arcAlpha = 0.55f * p;
        DrawArcTex(h, pos, maxR * 0.7f,  t * 1.1f,            new Color(1f, 0.5f, 1f, arcAlpha));
        DrawArcTex(h, pos, maxR * 0.7f,  t * 1.1f + MathF.PI, new Color(1f, 0.5f, 1f, arcAlpha));
        DrawArcTex(h, pos, maxR * 0.45f, -t * 1.7f,           new Color(0.8f, 0.2f, 1f, arcAlpha * 0.8f));
        DrawArcTex(h, pos, maxR * 0.45f, -t * 1.7f + MathF.PI,new Color(0.8f, 0.2f, 1f, arcAlpha * 0.8f));
    }

    private void DrawGlowPhase(DrawingHandleWorld h, Vector2 pos, float t, float p, float maxR, float curR)
    {
        var flash = 0.5f + 0.5f * MathF.Sin(t * 5f);
        var spin = t * 3.5f;

        // Заливка — немного прозрачнее
        DrawTexCentered(h, _glowTex, pos, maxR * 1.5f, new Color(0.4f, 0f, 0.8f, 0.18f + flash * 0.06f));
        DrawTexCentered(h, _glowTex, pos, maxR * 0.9f, new Color(0.6f, 0f, 1f,   0.25f + flash * 0.08f));
        DrawTexCentered(h, _glowTex, pos, maxR * 0.55f, new Color(0.8f, 0.1f, 1f, 0.32f + flash * 0.1f));
        DrawTexCentered(h, _glowTex, pos, maxR * 0.25f, new Color(1f, 0.4f, 1f,  0.45f + flash * 0.1f));

        // Граница взрыва — очень яркая с белым контуром
        DrawRingTex(h, pos, maxR, 0f, new Color(1f, 1f, 1f, 0.7f + flash * 0.3f));
        DrawRingTex(h, pos, maxR, 0f, new Color(0.9f, 0.3f, 1f, 0.8f));
        DrawRingTex(h, pos, maxR + 0.5f, 0f, new Color(0.6f, 0f, 1f, 0.4f));
        DrawRingTex(h, pos, maxR + 1.0f, 0f, new Color(0.3f, 0f, 0.8f, 0.15f));
        // Тёмная тень перед границей
        DrawTexCentered(h, _glowTex, pos, (maxR - 1f) * 2f, new Color(0.05f, 0f, 0.15f, 0.2f));

        // Кольца вокруг игрока
        DrawRingTex(h, pos, 2.8f,  spin * 0.3f,  new Color(1f, 0.6f, 1f, 0.85f + flash * 0.15f));
        DrawRingTex(h, pos, 1.8f, -spin * 0.4f,  new Color(1f, 0.85f, 1f, 0.8f));

        // Зубцы — крупные яркие
        for (var i = 0; i < 20; i++)
        {
            var angle = i / 20f * MathF.Tau + t * 1.8f;
            var spikeLen = 0.4f + 0.6f * MathF.Abs(MathF.Sin(t * 5f + i * 0.5f));
            DrawSpike(h, pos, angle, maxR, spikeLen, new Color(1f, 0.8f, 1f, 0.85f));
        }

        // 6 дуг — чередуем розовые и голубоватые
        for (var a = 0; a < 6; a++)
        {
            var dir = a % 2 == 0 ? 1f : -1.3f;
            var aR = maxR * (0.35f + a * 0.1f);
            var aAlpha = 0.65f + 0.3f * MathF.Sin(t * 2.5f + a);
            var col = a % 3 == 0
                ? new Color(1f, 0.5f, 1f, aAlpha)    // розовый
                : new Color(0.7f, 0.1f, 1f, aAlpha); // фиолетовый
            DrawArcTex(h, pos, aR, spin * dir + a * MathF.PI / 3f, col);
        }

        // Быстрые волны
        for (var w = 0; w < 5; w++)
        {
            var phase = ((t * 1.2f + w * 0.35f) % 1f);
            var wR = maxR * phase;
            var wA = (1f - phase) * 0.6f;
            if (wA > 0.02f && wR > 0.3f)
                DrawRingTex(h, pos, wR, 0f, new Color(0.9f, 0.3f, 1f, wA));
        }

        // Финальная вспышка >85%
        if (p > 0.85f)
        {
            var ft = (p - 0.85f) / 0.15f;
            var bigFlash = MathF.Pow(MathF.Abs(MathF.Sin(t * 12f)), 1.5f) * ft;
            DrawRingTex(h, pos, maxR, 0f, new Color(1f, 1f, 1f, bigFlash * 0.95f));
            DrawTexCentered(h, _glowTex, pos, maxR * 2.2f, new Color(1f, 0.8f, 1f, bigFlash * 0.25f));
        }
    }

    private void DrawTexCentered(DrawingHandleWorld h, Texture tex, Vector2 center, float diameter, Color color)
    {
        if (color.A < 0.01f) return;
        var half = diameter / 2f;
        h.SetTransform(Matrix3x2.Identity);
        h.DrawTextureRect(tex, new Box2(center.X - half, center.Y - half, center.X + half, center.Y + half), color);
    }

    private void DrawRingTex(DrawingHandleWorld h, Vector2 center, float radius, float rotOffset, Color color)
    {
        if (color.A < 0.01f || radius <= 0f) return;
        var half = radius;
        h.SetTransform(Matrix3x2.CreateRotation(rotOffset, center));
        h.DrawTextureRect(_ringTex, new Box2(center.X - half, center.Y - half, center.X + half, center.Y + half), color);
        h.SetTransform(Matrix3x2.Identity);
    }

    private void DrawArcTex(DrawingHandleWorld h, Vector2 center, float radius, float angle, Color color)
    {
        if (color.A < 0.01f || radius <= 0f) return;
        var half = radius;
        h.SetTransform(Matrix3x2.CreateRotation(angle, center));
        h.DrawTextureRect(_arcTex, new Box2(center.X - half, center.Y - half, center.X + half, center.Y + half), color);
        h.SetTransform(Matrix3x2.Identity);
    }

    private void DrawSpike(DrawingHandleWorld h, Vector2 center, float angle, float radius, float length, Color color)
    {
        if (color.A < 0.01f) return;
        var dir = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
        var start = center + dir * radius;
        h.SetTransform(Matrix3x2.CreateRotation(angle - MathF.PI / 2f, start));
        h.DrawTextureRect(_spikeTex,
            new Box2(start.X - 0.15f, start.Y, start.X + 0.15f, start.Y + length),
            color);
        h.SetTransform(Matrix3x2.Identity);
    }
}
