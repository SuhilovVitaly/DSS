using System.Collections.Immutable;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Portraits;

/// <summary>Biological sex used to pick the docking-confirmation background/body/portrait set.</summary>
public enum PersonSex
{
    Female,
    Male,
}

/// <summary>
/// Composes a 250×250 docking-confirmation portrait by layering a background, a headless
/// body-in-spacesuit sprite, and a small head/collar portrait on a shared 1024×1536 canvas,
/// then cropping the neck-seam region where the three line up. Reusable by any screen that
/// needs this composite image (first consumer: the docking confirmation dialog, added in a
/// separate batch — this class has no dialog/window logic of its own).
///
/// Scale/position/crop-rect geometry per sex below was measured and visually verified against
/// the current asset set (station-flight-docking-control-cente.png / SPC-JSYQRS-* for Female,
/// background-command-deck.png / SPC-UYSLLR-* for Male); do not recompute it without a reason —
/// if a real render looks off, first suspect a SkiaSharp draw-call mistake (e.g. destRect
/// axis/origin handling) rather than the numbers themselves.
/// </summary>
public static class PortraitComposer
{
    private const int CanvasWidth = 1024;
    private const int CanvasHeight = 1536;
    private const int CropSize = 250;

    private const string FemaleBackgroundImage = "Images/Persons/Background/station-flight-docking-control-cente.png";
    private const string MaleBackgroundImage = "Images/Persons/Background/background-command-deck.png";

    /// <summary>
    /// Headless female body-in-spacesuit variants; see <see cref="SelectBodyImage"/>. The
    /// only current Female-sex consumer is the dock operator (a station employee), so this
    /// pool is the station's corporate suit rather than the generic SPC-JSYQRS set — every
    /// employee of the docked station wears it.
    /// </summary>
    private static readonly ImmutableArray<string> FemaleBodyImages = ImmutableArray.Create(
        "Images/Persons/Body/W/Corporations/SPC-XXXXXX-2.png");

    /// <summary>Headless male body-in-spacesuit variants; see <see cref="SelectBodyImage"/>.</summary>
    private static readonly ImmutableArray<string> MaleBodyImages = ImmutableArray.Create(
        "Images/Persons/Body/M/SPC-UYSLLR/SPC-UYSLLR-0.png",
        "Images/Persons/Body/M/SPC-UYSLLR/SPC-UYSLLR-1.png",
        "Images/Persons/Body/M/SPC-UYSLLR/SPC-UYSLLR-2.png",
        "Images/Persons/Body/M/SPC-UYSLLR/SPC-UYSLLR-3.png");

    /// <summary>
    /// Fixed body-variant pick: the "-2" spacesuit image (index 2) when the pool has that many
    /// variants, otherwise the pool's last (currently: Female's single-suit pool). Replaces
    /// the earlier personKey-hash pick — the docking screen currently has no product need to
    /// vary body art per crew member.
    /// </summary>
    private const int DefaultBodyVariantIndex = 2;

    private static string SelectBodyImage(ImmutableArray<string> bodyPool) =>
        bodyPool[System.Math.Min(DefaultBodyVariantIndex, bodyPool.Length - 1)];

    private const float FemalePortraitScale = 0.867f;
    private const float FemalePortraitDestX = 430f;
    private const float FemalePortraitDestY = 262f;
    private static readonly SKRectI FemaleCropRect = SKRectI.Create(397, 243, CropSize, CropSize);

    private const float MalePortraitScale = 0.842f;
    private const float MalePortraitDestX = 422f;
    private const float MalePortraitDestY = 284f;
    private static readonly SKRectI MaleCropRect = SKRectI.Create(389, 252, CropSize, CropSize);

    private static readonly SKRect DestRect = new(0, 0, CropSize, CropSize);

    /// <summary>
    /// Docking point: the pixel on the 1024×1536 body-canvas surface (same coordinate space
    /// as the body sprite, which is drawn 1:1 into that canvas — see <see cref="Compose"/>)
    /// where the head portrait's top-left corner should land. Currently shared by both sexes
    /// pending separate calibration; used only by <see cref="ComposeBodyAndHeadPortrait"/>,
    /// not yet by the production <see cref="Compose"/> crop pipeline.
    /// </summary>
    private const float DockingPointX = 395f;
    private const float DockingPointY = 290f;

    /// <summary>
    /// Final-portrait crop, applied to the body+head canvas produced by
    /// <see cref="ComposeBodyAndHeadPortrait"/> — same 1024×1536 coordinate space as
    /// <see cref="DockingPointX"/>/<see cref="DockingPointY"/>. Verified visually against the
    /// "-2" body variant only; not yet re-checked against the other three variants.
    /// </summary>
    private const int PortraitCropSize = 300;
    private static readonly SKRectI PortraitCropRect = SKRectI.Create(375, 305, PortraitCropSize, PortraitCropSize);
    private static readonly SKRect PortraitCropDestRect = new(0, 0, PortraitCropSize, PortraitCropSize);

    /// <summary>
    /// Builds the composed 250×250 portrait, or null if <paramref name="portraitImagePath"/>
    /// cannot be loaded (missing file or a decode failure) — callers must be able to handle a
    /// null result (e.g. by not drawing a portrait, or showing a placeholder). Body-variant
    /// selection uses <paramref name="personKey"/> (a stable id such as crewId/objectId) purely
    /// as a client-side, non-persisted render choice — deterministic within the running process,
    /// not via the Engine's seeded RNG streams.
    /// </summary>
    public static SKBitmap? Compose(string portraitImagePath, PersonSex sex, string personKey)
    {
        using var portrait = LoadPortraitImage(portraitImagePath);
        if (portrait is null)
            return null;

        string backgroundPath = sex == PersonSex.Female ? FemaleBackgroundImage : MaleBackgroundImage;
        var bodyPool = sex == PersonSex.Female ? FemaleBodyImages : MaleBodyImages;
        float scale = sex == PersonSex.Female ? FemalePortraitScale : MalePortraitScale;
        float destX = sex == PersonSex.Female ? FemalePortraitDestX : MalePortraitDestX;
        float destY = sex == PersonSex.Female ? FemalePortraitDestY : MalePortraitDestY;
        SKRectI cropRect = sex == PersonSex.Female ? FemaleCropRect : MaleCropRect;

        using var fullCanvasBitmap = new SKBitmap(CanvasWidth, CanvasHeight);
        using (var canvas = new SKCanvas(fullCanvasBitmap))
        {
            canvas.Clear(SKColors.Transparent);

            using var background = LoadImage(backgroundPath);
            if (background is not null)
                canvas.DrawBitmap(background, new SKRect(0, 0, CanvasWidth, CanvasHeight));

            if (!bodyPool.IsEmpty)
            {
                string bodyPath = SelectBodyImage(bodyPool);
                using var body = LoadImage(bodyPath);
                if (body is not null)
                    canvas.DrawBitmap(body, new SKRect(0, 0, CanvasWidth, CanvasHeight));

                DrawPortrait(canvas, portrait, destX, destY, scale);
            }
            else
            {
                // Defensive fallback (not expected with the current asset set): no body
                // sprite for this sex, so there is no neck seam to align to. Center the
                // portrait horizontally at the same scale, in the upper third of the canvas,
                // rather than throwing or leaving a blank result.
                float fallbackDestX = CanvasWidth / 2f - portrait.Width * scale / 2f;
                float fallbackDestY = CanvasHeight / 6f;
                DrawPortrait(canvas, portrait, fallbackDestX, fallbackDestY, scale);
            }
        }

        var result = new SKBitmap(CropSize, CropSize);
        using (var resultCanvas = new SKCanvas(result))
        {
            resultCanvas.Clear(SKColors.Transparent);
            resultCanvas.DrawBitmap(fullCanvasBitmap, cropRect, DestRect);
        }

        return result;
    }

    /// <summary>
    /// Docking-confirmation portrait, background-less pipeline: draws ONLY the head portrait
    /// at its natural size (top-left corner placed exactly on the docking point), THEN the
    /// headless body-in-spacesuit sprite (no scene-background layer, always the
    /// <see cref="DefaultBodyVariantIndex"/> variant) on top of it — the suit's collar/neck
    /// ring overlaps and covers the base of the head, instead of the head covering the suit.
    /// Crops the result to <see cref="PortraitCropRect"/> (300×300) — the region around the
    /// docking point that frames head+shoulders — and returns that crop. Null on a missing
    /// portrait/body file. Not wired into the production <see cref="Compose"/>
    /// (background+crop) pipeline yet.
    /// </summary>
    public static SKBitmap? ComposeBodyAndHeadPortrait(string portraitImagePath, PersonSex sex, string personKey)
    {
        using var portrait = LoadPortraitImage(portraitImagePath);
        if (portrait is null)
            return null;

        var bodyPool = sex == PersonSex.Female ? FemaleBodyImages : MaleBodyImages;
        if (bodyPool.IsEmpty)
            return null;

        string bodyPath = SelectBodyImage(bodyPool);
        using var body = LoadImage(bodyPath);
        if (body is null)
            return null;

        using var fullCanvasBitmap = new SKBitmap(CanvasWidth, CanvasHeight);
        using (var canvas = new SKCanvas(fullCanvasBitmap))
        {
            canvas.Clear(SKColors.Transparent);

            var destRect = new SKRect(
                DockingPointX, DockingPointY,
                DockingPointX + portrait.Width, DockingPointY + portrait.Height);
            canvas.DrawBitmap(portrait, destRect);

            canvas.DrawBitmap(body, new SKRect(0, 0, CanvasWidth, CanvasHeight));
        }

        var result = new SKBitmap(PortraitCropSize, PortraitCropSize);
        using (var resultCanvas = new SKCanvas(result))
        {
            resultCanvas.Clear(SKColors.Transparent);
            resultCanvas.DrawBitmap(fullCanvasBitmap, PortraitCropRect, PortraitCropDestRect);
        }

        return result;
    }

    private static void DrawPortrait(SKCanvas canvas, SKBitmap portrait, float destX, float destY, float scale)
    {
        var destRect = new SKRect(destX, destY, destX + portrait.Width * scale, destY + portrait.Height * scale);
        canvas.DrawBitmap(portrait, destRect);
    }

    private static SKBitmap? LoadImage(string path)
    {
        try { return File.Exists(path) ? SKBitmap.Decode(path) : null; }
        catch { return null; }
    }

    /// <summary>
    /// Below this much "green excess" (<c>Green − max(Red, Blue)</c>) a pixel is left
    /// completely untouched — protects skin/hair/eye tones, which never have Green
    /// dominating Red and Blue by more than a small margin, from any keying at all.
    /// </summary>
    private const int GreenScreenKeyMargin = 15;

    /// <summary>
    /// Excess-green range (beyond <see cref="GreenScreenKeyMargin"/>) over which a pixel
    /// fades from fully opaque to fully transparent, instead of a hard in/out cutoff. A solid
    /// 00FF00 background (excess 255) always ends up fully transparent either way; this range
    /// is what keys out the anti-aliased green/hair-color blend pixels at strand tips — a hard
    /// exact-color match misses those because they're a blend, not pure green.
    /// </summary>
    private const int GreenScreenKeySpread = 70;

    /// <summary>
    /// Loads a head-portrait image and chroma-keys out its 00FF00-ish green-screen
    /// background (see <see cref="GreenScreenKeyMargin"/>/<see cref="GreenScreenKeySpread"/>)
    /// — some exports ship with a solid green background instead of real alpha. Also
    /// despills partially-keyed edge pixels (hair-strand tips etc.) so they don't keep a
    /// green tint. Unlike <see cref="LoadImage"/> (used for backgrounds/bodies, which are
    /// never green-screened), the caller owns the returned bitmap regardless of whether
    /// keying changed anything.
    /// </summary>
    private static SKBitmap? LoadPortraitImage(string path)
    {
        using var raw = LoadImage(path);
        return raw is null ? null : ChromaKeyGreenScreen(raw);
    }

    private static SKBitmap ChromaKeyGreenScreen(SKBitmap source)
    {
        var pixels = source.Pixels;
        for (int i = 0; i < pixels.Length; i++)
        {
            var p = pixels[i];
            int maxRedBlue = System.Math.Max(p.Red, p.Blue);
            int excessGreen = p.Green - maxRedBlue;
            if (excessGreen <= GreenScreenKeyMargin)
                continue;

            float keyStrength = System.Math.Min(1f, (excessGreen - GreenScreenKeyMargin) / (float)GreenScreenKeySpread);
            byte newAlpha = (byte)(p.Alpha * (1f - keyStrength));
            // Despill: clamp the green channel to max(Red, Blue) unconditionally — NOT scaled
            // by keyStrength. A partially-opaque edge pixel (majority hair color, minor green
            // contamination) still has G only a little above max(R,B); a keyStrength-scaled
            // despill leaves that excess mostly intact and the pixel reads as a muddy olive
            // tint along the whole strand, not just at the fully-transparent tip.
            byte newGreen = (byte)maxRedBlue;
            pixels[i] = new SKColor(p.Red, newGreen, p.Blue, newAlpha);
        }

        var result = new SKBitmap(source.Info);
        result.Pixels = pixels;
        return result;
    }
}
