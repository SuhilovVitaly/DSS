using SkiaSharp;

namespace DeepSpaceSaga.Client.UI;

internal static class SkiaGpuOptions
{
    internal static GRContextOptions Create() => new()
    {
        // Map paths move and change every frame. Retaining their raster masks
        // fills the GPU cache with one-use textures and stalls when it churns.
        // Keep Skia's other caches (images, glyphs and programs) enabled.
        AllowPathMaskCaching = false
    };
}
