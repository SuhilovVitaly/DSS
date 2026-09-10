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

    /// <summary>Headless female body-in-spacesuit variants; one is picked deterministically per personKey.</summary>
    private static readonly ImmutableArray<string> FemaleBodyImages = ImmutableArray.Create(
        "Images/Persons/Body/W/SPC-JSYQRS/SPC-JSYQRS-0.png",
        "Images/Persons/Body/W/SPC-JSYQRS/SPC-JSYQRS-1.png",
        "Images/Persons/Body/W/SPC-JSYQRS/SPC-JSYQRS-2.png",
        "Images/Persons/Body/W/SPC-JSYQRS/SPC-JSYQRS-3.png");

    /// <summary>Headless male body-in-spacesuit variants; one is picked deterministically per personKey.</summary>
    private static readonly ImmutableArray<string> MaleBodyImages = ImmutableArray.Create(
        "Images/Persons/Body/M/SPC-UYSLLR/SPC-UYSLLR-0.png",
        "Images/Persons/Body/M/SPC-UYSLLR/SPC-UYSLLR-1.png",
        "Images/Persons/Body/M/SPC-UYSLLR/SPC-UYSLLR-2.png",
        "Images/Persons/Body/M/SPC-UYSLLR/SPC-UYSLLR-3.png");

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
    /// Builds the composed 250×250 portrait, or null if <paramref name="portraitImagePath"/>
    /// cannot be loaded (missing file or a decode failure) — callers must be able to handle a
    /// null result (e.g. by not drawing a portrait, or showing a placeholder). Body-variant
    /// selection uses <paramref name="personKey"/> (a stable id such as crewId/objectId) purely
    /// as a client-side, non-persisted render choice — deterministic within the running process,
    /// not via the Engine's seeded RNG streams.
    /// </summary>
    public static SKBitmap? Compose(string portraitImagePath, PersonSex sex, string personKey)
    {
        using var portrait = LoadImage(portraitImagePath);
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
                string bodyPath = bodyPool[System.Math.Abs(personKey.GetHashCode()) % bodyPool.Length];
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
}
