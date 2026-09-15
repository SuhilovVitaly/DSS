using DeepSpaceSaga.Client.Portraits;
using DeepSpaceSaga.Client.UI.Controls;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.TempCharacterImage;

/// <summary>Frontal portrait workshop with independently selectable compatible parts.</summary>
public sealed class TempCharacterImageScreen : IScreen
{
    private const float Width = 1120, Height = 748;
    private const int PortraitsPerPage = 3;
    private static readonly SKRect Preview = new(280, 108, 792, 620);
    private string _assetRoot, _presetPath;
    private readonly string _initialRoot;
    private readonly string? _customPresetPath;
    private readonly List<(SKRect Rect, Action Action)> _buttons = [];
    private readonly List<(string Label, SKBitmap Image)> _constructionPreviews = [];
    private PortraitAssetRepository? _assets;
    private PortraitRenderer? _renderer;
    private CharacterAppearance? _appearance;
    private SKImage? _portrait;
    private SKBitmap? _layer, _reference;
    private string[] _references = [];
    private int _referenceIndex;
    private int _portraitPage;
    private int _seed = 1;
    private string? _selectedLayer;
    private bool _guides;
    private bool _hasRendered;
    private float _scale = 1, _left, _top, _mouseX = -1, _mouseY = -1;
    private string _status = "Стрелки меняют деталь. Нажмите на портрет, чтобы получить новое сочетание.";
    internal CharacterAppearance? Appearance => _appearance;
    internal SKRect PortraitRect => Preview;
    internal string? SelectedLayer => _selectedLayer;
    private bool IsCompleteFacePack => _assets?.Style.Layers.Any(l => l.Category == "Oval") == true;
    private bool IsWholeHeadPack => _assets?.Style.Layers.Any(l => l.Category == "Neck") == true;
    private bool IsUnifiedPortraitPack => _assets?.Style.Layers.Any(l => l.Category == "Portrait") == true;
    private long CombinationCount => _assets is null ? 0 : _assets.Style.Layers
        .Where(l => l.IntroducedInVersion <= _assets.Style.LibraryVersion)
        .Aggregate(1L, (total, layer) => total * _assets.Parts.Count(p => p.Category == layer.Category && p.IntroducedInVersion <= _assets.Style.LibraryVersion));

    public TempCharacterImageScreen(string? assetRoot = null, string? presetPath = null)
    {
        _assetRoot = assetRoot ?? Path.Combine(AppContext.BaseDirectory, "Images", "Persons", "W4");
        _assetRoot = Path.GetFullPath(Path.TrimEndingDirectorySeparator(_assetRoot));
        _initialRoot = _assetRoot;
        _customPresetPath = presetPath;
        _presetPath = ResolvePresetPath();
    }

    private string ResolvePresetPath()
    {
        string pack = Path.GetFileName(_assetRoot);
        if (_customPresetPath is not null)
            return _assetRoot == _initialRoot ? _customPresetPath : Path.Combine(Path.GetDirectoryName(Path.GetFullPath(_customPresetPath))!,
                Path.GetFileNameWithoutExtension(_customPresetPath) + "." + pack.ToLowerInvariant() + ".json");
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DeepSpaceSaga", "PortraitPresets", Path.GetFileName(Path.TrimEndingDirectorySeparator(_assetRoot)) switch
            { "M4" => "temp-character-m4.json", "W4" => "temp-character-w4.json", "W2" => "temp-character-w2.json", "W1" => "temp-character-w1.json", _ => "temp-character.json" });
    }

    private void SwitchPack(string pack)
    {
        string root = Path.Combine(Path.GetDirectoryName(_assetRoot)!, pack);
        if (root.Equals(_assetRoot, StringComparison.OrdinalIgnoreCase)) return;
        OnDeactivated();
        _assetRoot = root;
        _presetPath = ResolvePresetPath();
        _guides = false;
        OnActivated();
    }

    public void OnActivated()
    {
        OnDeactivated();
        _portraitPage = 0;
        var watch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            // Validate metadata and file paths here. Full PNG decoding belongs to
            // asset validation/tests; the renderer loads only the selected layers.
            _assets = new PortraitAssetRepository(_assetRoot, validateTextures: false);
            _renderer = new PortraitRenderer(_assets);
            _seed = IsCompleteFacePack || IsWholeHeadPack || IsUnifiedPortraitPack ? Random.Shared.Next() : 1;
            _appearance = new PortraitGenerator(_assets).Generate(_seed);
            _portrait = _renderer.Render(_appearance);
            string folder = Path.Combine(_assetRoot, "Reference");
            _references = Directory.Exists(folder) ? Directory.GetFiles(folder, "CHR-*.png").Order(StringComparer.Ordinal).ToArray() : [];
            LoadReference();
            RefreshConstructionPreviews();
            _status = "Нажмите на портрет для нового сочетания. Стрелки меняют персонажа и костюм.";
            InterfaceLog.Write($"Portrait workshop ready in {watch.ElapsedMilliseconds} ms; pack={_assetRoot}");
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or ArgumentException or System.Text.Json.JsonException or UnauthorizedAccessException)
        { _status = "Не удалось загрузить портрет: " + ex.Message; InterfaceLog.Write(_status); }
    }
    public void OnDeactivated()
    {
        _hasRendered = false;
        _assets = null; _appearance = null;
        _portrait = null; _renderer?.Dispose(); _renderer = null;
        _layer?.Dispose(); _layer = null; _selectedLayer = null;
        _reference?.Dispose(); _reference = null;
        foreach (var preview in _constructionPreviews) preview.Image.Dispose();
        _constructionPreviews.Clear();
    }
    private void SelectLayer(string? category)
    {
        _layer?.Dispose(); _layer = null; _selectedLayer = category;
        if (category is not null && _assets is not null && _appearance is not null && _appearance.Parts.TryGetValue(category, out string? id))
            _layer = SKBitmap.Decode(_assets.TexturePath(_assets.Get(id).TextureFor(_appearance.LibraryVersion, _appearance.Parts.GetValueOrDefault("Face"))));
    }
    private void Rebuild()
    {
        if (_assets is null || _renderer is null || _appearance is null) return;
        var generator = new PortraitGenerator(_assets);
        var next = _appearance;
        for (int attempt = 0; attempt < 32; attempt++)
        {
            next = generator.Generate(unchecked(++_seed));
            if (!next.Parts.SequenceEqual(_appearance.Parts)) break;
        }
        _appearance = next;
        _portrait = _renderer.Render(_appearance); SelectLayer(null);
        RefreshConstructionPreviews();
        _status = $"Новый портрет. Доступно сочетаний: {CombinationCount}. Каждую деталь можно изменить стрелками.";
    }
    private void Cycle(string category, int direction)
    {
        if (_assets is null || _renderer is null || _appearance is null) return;
        if (_appearance.LibraryVersion != _assets.Style.LibraryVersion)
        {
            var upgraded = _appearance with { LibraryVersion = _assets.Style.LibraryVersion };
            if (!_assets.IsCompatible(upgraded))
            {
                var generated = new PortraitGenerator(_assets).Generate(unchecked(++_seed));
                upgraded = generated with { Parts = generated.Parts.SetItems(_appearance.Parts) };
            }
            _appearance = upgraded;
        }
        var choices = _assets.Parts.Where(p => p.Category == category && p.IntroducedInVersion <= _appearance.LibraryVersion).OrderBy(p => p.Id, StringComparer.Ordinal).ToArray();
        int index = Array.FindIndex(choices, p => p.Id == _appearance.Parts.GetValueOrDefault(category));
        var selected = choices[(index + direction + choices.Length) % choices.Length];
        _appearance = _appearance with { Parts = _appearance.Parts.SetItem(category, selected.Id) };
        _portrait = _renderer.Render(_appearance); SelectLayer(null);
        RefreshConstructionPreviews();
        _status = selected.DisplayName + ". Остальные детали сохранены.";
    }
    private void LoadReference()
    {
        _reference?.Dispose(); _reference = null;
        if (_references.Length > 0) _reference = SKBitmap.Decode(_references[_referenceIndex % _references.Length]);
    }
    private void RefreshConstructionPreviews()
    {
        if (IsUnifiedPortraitPack && _constructionPreviews.Count > 0) return;
        foreach (var preview in _constructionPreviews) preview.Image.Dispose();
        _constructionPreviews.Clear();
        if (IsUnifiedPortraitPack && _assets is not null)
        {
            foreach (var part in _assets.Parts.Where(p => p.Category == "Portrait").OrderBy(p => p.Id, StringComparer.Ordinal))
                _constructionPreviews.Add((part.DisplayName ?? "Портрет", SKBitmap.Decode(_assets.TexturePath(part.Texture))));
            return;
        }
        if ((!IsCompleteFacePack && !IsWholeHeadPack) || _assets is null || _appearance is null) return;
        foreach (var (category, label) in IsWholeHeadPack
            ? new[] { ("Head", "Голова целиком"), ("Neck", "Общая шея"), ("Clothes", "Костюм"), ("Hair", "Новая причёска") }
            : new[] { ("Oval", "Овал и шея"), ("Face", "Цельное лицо"), ("Hair", "Причёска") })
        {
            var part = _assets.Get(_appearance.Parts[category]);
            _constructionPreviews.Add((label, SKBitmap.Decode(_assets.TexturePath(part.Texture))));
        }
    }
    private void SavePreset()
    {
        if (_appearance is null) return;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_presetPath))!);
            File.WriteAllText(_presetPath, AppearanceSerializer.Serialize(_appearance)); _status = "Описание портрета сохранено.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { _status = "Ошибка сохранения: " + ex.Message; }
    }
    private void LoadPreset()
    {
        if (_assets is null || _renderer is null) return;
        try
        {
            var appearance = AppearanceSerializer.Deserialize(File.ReadAllText(_presetPath));
            _assets.ValidateAppearance(appearance);
            _portrait = _renderer.Render(appearance); _appearance = appearance; SelectLayer(null);
            RefreshConstructionPreviews();
            _status = "Портрет восстановлен.";
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or System.Text.Json.JsonException or ArgumentException)
        { _status = "Не удалось загрузить: " + ex.Message; }
    }

    public void Render(SKCanvas canvas, int width, int height)
    {
        _scale = Math.Min(1, Math.Min(width / Width, height / Height)); _left = (width - Width * _scale) / 2; _top = (height - Height * _scale) / 2;
        canvas.DrawRect(SKRect.Create(width, height), MenuStyle.DimOverlayFill);
        canvas.Save(); canvas.Translate(_left, _top); canvas.Scale(_scale); _buttons.Clear();
        using var paint = new SKPaint { IsAntialias = true, Color = new SKColor(17, 24, 34) };
        canvas.DrawRoundRect(SKRect.Create(Width, Height), 12, 12, paint);
        Text(IsUnifiedPortraitPack ? "ПОРТРЕТ · " + Path.GetFileName(_assetRoot) : IsWholeHeadPack ? "ПОРТРЕТ · W2" : IsCompleteFacePack ? "ПОРТРЕТ · W1" : "ПОРТРЕТ В АНФАС", 26, 40, 22);
        Text($"АНФАС · СОЧЕТАНИЙ: {CombinationCount}", 26, 67, 12);
        if (Path.GetFileName(_assetRoot) is "W4" or "M4")
        {
            Button(new(520, 22, 648, 58), (_assets?.Style.Gender == "female" ? "• " : "") + "Женщины", () => SwitchPack("W4"));
            Button(new(660, 22, 788, 58), (_assets?.Style.Gender == "male" ? "• " : "") + "Мужчины", () => SwitchPack("M4"));
        }
        Button(new(1025, 22, 1094, 58), "Закрыть", () => { });
        Text("ПОСМОТРЕТЬ ДЕТАЛЬ", 26, 112, 12);
        Button(new(26, 130, 250, 170), "Портрет целиком", () => SelectLayer(null));
        if (_assets is not null)
        {
            int row = 0;
            foreach (var entry in IsUnifiedPortraitPack
                ? new[] { ("Portrait", "Персонаж"), ("Clothes", "Костюм") }
                : IsWholeHeadPack
                ? new[] { ("Head", "Голова"), ("Hair", "Причёска"), ("Clothes", "Костюм"), ("Neck", "Общая шея") }
                : IsCompleteFacePack
                ? new[] { ("Oval", "Овал + шея"), ("Face", "Лицо"), ("Clothes", "Костюм"), ("Hair", "Причёска") }
                : new[] { ("Face", "Овал"), ("Eyebrows", "Брови"), ("Eyes", "Глаза"), ("Nose", "Нос"), ("Mouth", "Губы"), ("Chin", "Подбородок"), ("Clothes", "Костюм"), ("Hair", "Причёска") })
            {
                string category = entry.Item1;
                if (_appearance is null || !_appearance.Parts.TryGetValue(category, out string? id)) continue;
                var choices = _assets.Parts.Where(p => p.Category == category && p.IntroducedInVersion <= _appearance.LibraryVersion).OrderBy(p => p.Id, StringComparer.Ordinal).ToArray();
                float y = 190 + row++ * 43;
                int number = Array.FindIndex(choices, p => p.Id == id) + 1;
                Button(new(26, y, choices.Length > 1 ? 178 : 250, y + 36), (_selectedLayer == category ? "• " : "") + entry.Item2 + (choices.Length > 1 ? $" {number}/{choices.Length}" : ""), () => SelectLayer(category));
                if (choices.Length > 1)
                {
                    Button(new(182, y, 214, y + 36), "‹", () => Cycle(category, -1));
                    Button(new(218, y, 250, y + 36), "›", () => Cycle(category, 1));
                }
            }
        }
        Text(IsUnifiedPortraitPack ? "Голова, волосы и шея — вместе." : IsWholeHeadPack ? "Голова меняется целиком." : IsCompleteFacePack ? "Лицо меняется целиком." : "Детали меняются независимо.", 26, 549, 12);
        Text("Общая посадка воротника.", 26, 570, 12);
        if (!IsUnifiedPortraitPack) Button(new(26, 586, 250, 627), _guides ? "Скрыть пропорции" : "Показать пропорции", () => { _guides = !_guides; SelectLayer(null); });
        paint.Color = new SKColor(28, 40, 54); canvas.DrawRoundRect(Preview, 8, 8, paint);
        if (_layer is not null) canvas.DrawBitmap(_layer, Preview);
        else if (_portrait is not null) canvas.DrawImage(_portrait, Preview);
        if (_guides && _selectedLayer is null && _assets is not null)
        {
            foreach (var entry in new[] { ("Crown", "Макушка"), ("Brow", "Брови"), ("EyeLeft", "Глаза"), ("NoseBase", "Основание носа"), ("Mouth", "Рот"), ("Chin", "Подбородок") })
            {
                float anchor = _assets.Style.Anchors[entry.Item1].Y;
                if (_appearance?.LibraryVersion == 4) anchor = (anchor * 1024 - 12) / 672;
                float y = Preview.Top + anchor * Preview.Height;
                paint.Color = new SKColor(117, 219, 224, 165); paint.StrokeWidth = 1;
                canvas.DrawLine(Preview.Left, y, Preview.Right, y, paint); Text(entry.Item2, Preview.Left + 8, y - 4, 10);
            }
        }
        Text(_selectedLayer is null ? "СОБРАННЫЙ ПОРТРЕТ" : "ОТДЕЛЬНЫЙ СЛОЙ", 295, 645, 12);
        if (IsUnifiedPortraitPack && _assets is not null && _appearance is not null)
        {
            Text("ВЫБЕРИТЕ ПЕРСОНАЖА", 826, 112, 12);
            var choices = _assets.Parts.Where(p => p.Category == "Portrait").OrderBy(p => p.Id, StringComparer.Ordinal).ToArray();
            int pageCount = Math.Max(1, (choices.Length + PortraitsPerPage - 1) / PortraitsPerPage);
            _portraitPage = Math.Clamp(_portraitPage, 0, pageCount - 1);
            int start = _portraitPage * PortraitsPerPage;
            for (int i = start; i < Math.Min(start + PortraitsPerPage, _constructionPreviews.Count); i++)
            {
                float y = 132 + (i - start) * 147;
                bool selected = _appearance.Parts["Portrait"] == choices[i].Id;
                paint.Color = selected ? new SKColor(39, 65, 78) : new SKColor(23, 33, 44);
                var rect = new SKRect(814, y, 1094, y + 135);
                canvas.DrawRoundRect(rect, 8, 8, paint);
                canvas.DrawBitmap(_constructionPreviews[i].Image, new SKRect(240, 0, 790, 675), new SKRect(821, y + 4, 927, y + 134));
                string label = _constructionPreviews[i].Label;
                paint.TextSize = 13; paint.Typeface = MenuStyle.TypefaceRegular;
                string shortLabel = label;
                while (shortLabel.Length > 1 && paint.MeasureText(shortLabel + "…") > 146) shortLabel = shortLabel[..^1];
                Text(shortLabel.Length < label.Length ? shortLabel + "…" : label, 940, y + 57, 13);
                Text(selected ? "Выбран" : "Нажмите для выбора", 940, y + 80, 10);
                string id = choices[i].Id;
                _buttons.Add((rect, () =>
                {
                    _appearance = _appearance with { Parts = _appearance.Parts.SetItem("Portrait", id) };
                    _portrait = _renderer!.Render(_appearance); SelectLayer(null);
                    _status = $"Выбран: {label}. Костюм сохранён.";
                }));
            }
            if (pageCount > 1)
            {
                Button(new(814, 580, 860, 618), "‹", () => _portraitPage = (_portraitPage + pageCount - 1) % pageCount);
                Text($"{_portraitPage + 1} / {pageCount}", 926, 605, 13);
                Button(new(1048, 580, 1094, 618), "›", () => _portraitPage = (_portraitPage + 1) % pageCount);
            }
            Text($"Персонажей: {choices.Length}", 826, 645, 13);
        }
        else if (IsCompleteFacePack || IsWholeHeadPack)
        {
            Text(IsWholeHeadPack ? "СОСТАВ ПОРТРЕТА" : "ТРИ ЧАСТИ ПОРТРЕТА", 826, 112, 12);
            for (int i = 0; i < _constructionPreviews.Count; i++)
            {
                float y = 132 + i * (IsWholeHeadPack ? 91 : 123);
                float size = IsWholeHeadPack ? 92 : 124;
                canvas.DrawBitmap(_constructionPreviews[i].Image, new SKRect(826, y, 826 + size, y + size));
                Text(_constructionPreviews[i].Label, IsWholeHeadPack ? 930 : 946, y + 48, 12);
            }
            Text(IsWholeHeadPack ? "Лицо, овал, подбородок и уши" : "Глаза, брови, нос и рот", 826, 538, 13);
            Text(IsWholeHeadPack ? "сохраняются вместе — одной головой." : "меняются вместе — одним лицом.", 826, 560, 13);
            Text(IsWholeHeadPack ? "Одна шея для всех трёх голов." : "Единый оттенок кожи.", 826, 590, 13);
        }
        else
        {
            Text("РЕФЕРЕНС", 826, 112, 12);
            if (_reference is not null) canvas.DrawBitmap(_reference, new SKRect(826, 145, 1090, 409));
            Text("DSS-Images / Persons", 826, 438, 12);
            Button(new(826, 464, 1090, 505), "Другой референс", () => { _referenceIndex++; LoadReference(); });
        }
        Button(new(280, 675, 540, 714), "Случайный портрет", Rebuild);
        Button(new(795, 675, 940, 714), "Сохранить JSON", SavePreset);
        Button(new(948, 675, 1094, 714), "Загрузить JSON", LoadPreset);
        canvas.Save(); canvas.ClipRect(new(26, 716, 1094, 745)); Text(_status, 26, 735, 11); canvas.Restore();
        canvas.Restore();
        _hasRendered = true;
        void Text(string value, float x, float y, float size)
        { paint.Color = new SKColor(205, 219, 229); paint.Typeface = MenuStyle.TypefaceRegular; paint.TextSize = size; canvas.DrawText(value, x, y, paint); }
        void Button(SKRect rect, string text, Action action)
        { MenuStyle.DrawButton(canvas, rect, text, rect.Contains(_mouseX, _mouseY) ? ButtonState.Hovered : ButtonState.Normal); _buttons.Add((rect, action)); }
    }
    public ScreenEvent OnMouseDown(float x, float y, MouseButton button)
    {
        if (button != MouseButton.Left || !_hasRendered) return ScreenEvent.None;
        x = (x - _left) / _scale; y = (y - _top) / _scale;
        // A second click on the underlying game button may arrive after opening.
        // Dismiss only via the explicit close button or Escape.
        if (x < 0 || x > Width || y < 0 || y > Height) return ScreenEvent.None;
        if (new SKRect(1025, 22, 1094, 58).Contains(x, y)) return ScreenEvent.CloseTempCharacterImage;
        if (Preview.Contains(x, y)) { Rebuild(); return ScreenEvent.None; }
        foreach (var item in _buttons) if (item.Rect.Contains(x, y)) { item.Action(); break; }
        return ScreenEvent.None;
    }
    public bool OnMouseMove(float x, float y)
    { _mouseX = (x - _left) / _scale; _mouseY = (y - _top) / _scale; return Preview.Contains(_mouseX, _mouseY) || _buttons.Any(b => b.Rect.Contains(_mouseX, _mouseY)); }
    public ScreenEvent OnMouseWheel(float x, float y, float delta) => ScreenEvent.None;
    public ScreenEvent OnKeyDown(Key key)
    { if (key == Key.Escape) return ScreenEvent.CloseTempCharacterImage; if (key == Key.Space) Rebuild(); return ScreenEvent.None; }
}
