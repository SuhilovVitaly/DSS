using System.Text;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal sealed class TacticalBattleSkinCatalog : IDisposable
{
    private const int MaxResources = 32;
    private const int MaxResourceBytes = 8 * 1024 * 1024;
    private const int MaxPackBytes = 32 * 1024 * 1024;
    private const int MaxImagePixels = 4_194_304;
    private const int MaxPackPixels = 8_388_608;
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    private readonly Dictionary<string, TacticalBattleSkin> _skins = new(StringComparer.Ordinal)
    {
        [TacticalBattleSkin.ClassicId] = TacticalBattleSkin.Classic,
    };
    private readonly List<IDisposable> _ownedResources = [];
    private bool _disposed;

    private TacticalBattleSkinCatalog(string root, Action<string>? warning)
    {
        var candidates = ReadManifests(root, warning);
        foreach (var group in candidates.GroupBy(pack => pack.Skin.Id, StringComparer.Ordinal)
                     .OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            var packs = group.ToArray();
            if (packs.Length > 1)
            {
                foreach (var pack in packs)
                    warning?.Invoke($"Skin '{pack.Directory}': duplicate ID '{group.Key}'.");
                continue;
            }

            var (directory, skin) = packs[0];
            try
            {
                var resources = LoadResources(directory, skin);
                _ownedResources.AddRange(resources.Values);
                _skins.Add(skin.Id, skin.WithResources(resources));
            }
            catch (Exception ex) when (IsPackError(ex))
            {
                warning?.Invoke($"Skin '{directory}': {ex.Message}");
            }
        }

        Options = Array.AsReadOnly(_skins.Values.Select(skin => (skin.Id, skin.DisplayName)).ToArray());
    }

    internal IReadOnlyList<(string Id, string DisplayName)> Options { get; }

    internal static TacticalBattleSkinCatalog Load(string root, Action<string>? warning = null) => new(root, warning);

    internal TacticalBattleSkin GetOrClassic(string? id) =>
        id is not null && TryGet(id, out var skin) ? skin : TacticalBattleSkin.Classic;

    internal bool TryGet(string id, out TacticalBattleSkin skin)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _skins.TryGetValue(id, out skin!);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        foreach (var resource in _ownedResources)
            resource.Dispose();
        _ownedResources.Clear();
    }

    private static List<(string Directory, TacticalBattleSkin Skin)> ReadManifests(string root, Action<string>? warning)
    {
        List<(string, TacticalBattleSkin)> packs = [];
        string[] directories;
        try
        {
            root = Path.GetFullPath(root);
            CheckPathEntry(root, directory: true);
            directories = Directory.GetDirectories(root);
        }
        catch (DirectoryNotFoundException)
        {
            return packs;
        }
        catch (FileNotFoundException)
        {
            return packs;
        }
        catch (Exception ex) when (IsPackError(ex))
        {
            warning?.Invoke($"Skin directory '{root}': {ex.Message}");
            return packs;
        }

        foreach (string directory in directories.Order(StringComparer.Ordinal))
        {
            try
            {
                CheckPathEntry(directory, directory: true);
                byte[] bytes;
                try
                {
                    bytes = ReadFile(Path.Combine(directory, "skin.json"), TacticalBattleSkin.MaxManifestBytes);
                }
                catch (FileNotFoundException)
                {
                    continue;
                }

                int bom = bytes.AsSpan().StartsWith(new byte[] { 0xEF, 0xBB, 0xBF }) ? 3 : 0;
                packs.Add((directory, TacticalBattleSkin.Parse(StrictUtf8.GetString(bytes.AsSpan(bom)))));
            }
            catch (Exception ex) when (IsPackError(ex))
            {
                warning?.Invoke($"Skin '{directory}', skin.json: {ex.Message}");
            }
        }

        return packs;
    }

    private static Dictionary<string, IDisposable> LoadResources(string directory, TacticalBattleSkin skin)
    {
        var resources = new Dictionary<string, IDisposable>(StringComparer.Ordinal);
        int encodedBytes = 0;
        int imagePixels = 0;
        try
        {
            foreach (var reference in skin.ResourceReferences.OrderBy(reference => reference.Path, StringComparer.Ordinal))
            {
                if (resources.ContainsKey(reference.Path))
                    continue;

                try
                {
                    if (resources.Count == MaxResources)
                        throw new FormatException($"at most {MaxResources} distinct resources are allowed");
                    string fullPath = ResolveAsset(directory, reference.Path);
                    var bytes = ReadFile(fullPath, Math.Min(MaxResourceBytes, MaxPackBytes - encodedBytes));
                    encodedBytes += bytes.Length;
                    using var data = SKData.CreateCopy(bytes);
                    IDisposable resource = reference.Property == "image"
                        ? DecodePng(data, ref imagePixels)
                        : DecodeFont(data, bytes);
                    resources.Add(reference.Path, resource);
                }
                catch (Exception ex) when (IsPackError(ex))
                {
                    throw new FormatException($"roles.{reference.Role}.{reference.Property} ({reference.Path}): {ex.Message}", ex);
                }
            }
            return resources;
        }
        catch
        {
            foreach (var resource in resources.Values)
                resource.Dispose();
            throw;
        }
    }

    private static string ResolveAsset(string directory, string relative)
    {
        string[] segments = relative.Split('/');
        string path = directory;
        for (int i = 0; i < segments.Length; i++)
        {
            path = Path.Combine(path, segments[i]);
            CheckPathEntry(path, directory: i < segments.Length - 1);
        }

        return path;
    }

    private static void CheckPathEntry(string path, bool directory)
    {
        var attributes = File.GetAttributes(path);
        if ((attributes & (FileAttributes.ReparsePoint | FileAttributes.Device)) != 0)
            throw new IOException($"reparse points and devices are forbidden: '{path}'");
        if (attributes.HasFlag(FileAttributes.Directory) != directory)
            throw new IOException($"expected a {(directory ? "directory" : "file")}: '{path}'");
    }

    private static byte[] ReadFile(string path, int byteLimit)
    {
        CheckPathEntry(path, directory: false);
        using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        long length = stream.Length;
        if (length <= 0 || length > byteLimit)
            throw new FormatException($"file size outside 1..{byteLimit} bytes");
        var bytes = new byte[(int)length];
        stream.ReadExactly(bytes);
        if (stream.ReadByte() != -1)
            throw new IOException("file changed while reading");
        return bytes;
    }

    private static SKImage DecodePng(SKData data, ref int imagePixels)
    {
        using var codec = SKCodec.Create(data);
        if (codec is null || codec.EncodedFormat != SKEncodedImageFormat.Png)
            throw new FormatException("invalid PNG");
        var info = codec.Info;
        long pixels = (long)info.Width * info.Height;
        if (info.Width is <= 0 or > 4096 || info.Height is <= 0 or > 4096 ||
            pixels > MaxImagePixels || imagePixels + pixels > MaxPackPixels)
            throw new FormatException("PNG exceeds the image or pack pixel limit");

        var output = new SKImageInfo(info.Width, info.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var bitmap = new SKBitmap(output);
        if (codec.GetPixels(output, bitmap.GetPixels()) != SKCodecResult.Success)
            throw new FormatException("incomplete or invalid PNG");
        bitmap.SetImmutable();
        imagePixels += (int)pixels;
        return SKImage.FromBitmap(bitmap) ?? throw new FormatException("unable to prepare PNG");
    }

    private static SKTypeface DecodeFont(SKData data, byte[] bytes)
    {
        var signature = bytes.AsSpan(0, Math.Min(bytes.Length, 4));
        if (!signature.SequenceEqual("OTTO"u8) && !signature.SequenceEqual(new byte[] { 0, 1, 0, 0 }) &&
            !signature.SequenceEqual("true"u8))
            throw new FormatException("invalid TTF or OTF header");
        return SKTypeface.FromData(data) ?? throw new FormatException("invalid TTF or OTF");
    }

    private static bool IsPackError(Exception ex) =>
        ex is IOException or UnauthorizedAccessException or FormatException or ArgumentException;
}
