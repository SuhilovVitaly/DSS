using System.Security.Cryptography;
using DeepSpaceSaga.Client.Portraits;
using DeepSpaceSaga.Client.UI.Controls;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.TempCharacterImage;

/// <summary>Temporary female portrait workshop. All coordinates share the same scaled input/render space.</summary>
public sealed class TempCharacterImageScreen : IScreen
{
    private const float Width = 1120, Height = 748;
    private static readonly SKRect Preview = new(264, 108, 776, 620);
    private static readonly SKRect SeedBox = new(804, 118, 994, 150);
    private static readonly Dictionary<string, string> CategoryNames = new(StringComparer.Ordinal)
    {
        ["HairBack"] = "Волосы сзади", ["Face"] = "Форма лица", ["Clothes"] = "Одежда",
        ["Eyes"] = "Глаза", ["Eyebrows"] = "Брови", ["Nose"] = "Нос", ["Mouth"] = "Губы",
        ["HairFront"] = "Чёлка", ["Accessory"] = "Аксессуар"
    };
    private readonly string _assetRoot;
    private readonly string _presetPath;
    private readonly TextInputBox _seed = new(11);
    private readonly List<(SKRect Rect, Action Action)> _buttons = [];
    private readonly HashSet<string> _locks = new(StringComparer.Ordinal);
    private readonly List<CharacterAppearance> _undo = [], _redo = [];
    private PortraitAssetRepository? _assets;
    private PortraitGenerator? _generator;
    private PortraitRenderer? _renderer;
    private CharacterAppearance? _appearance;
    private SKImage? _portrait;
    private SKBitmap? _reference;
    private string[] _references = [];
    private int _referenceIndex;
    private float _scale = 1, _left, _top, _mouseX = -1, _mouseY = -1;
    private bool _seedFocused, _compare;
    private string _status = "Нажмите на портрет, чтобы создать нового персонажа";
    internal CharacterAppearance? Appearance => _appearance;
    internal SKRect PortraitRect => Preview;

    public TempCharacterImageScreen(string? assetRoot = null, string? presetPath = null)
    {
        _assetRoot = assetRoot ?? Path.Combine(AppContext.BaseDirectory, "Images", "PortraitGenerator");
        _presetPath = presetPath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DeepSpaceSaga", "PortraitPresets", "temp-character.json");
    }

    public void OnActivated()
    {
        try
        {
            _assets ??= new PortraitAssetRepository(_assetRoot);
            _generator ??= new PortraitGenerator(_assets);
            _renderer = new PortraitRenderer(_assets);
            SetAppearance(_appearance ?? _generator.Generate(RandomNumberGenerator.GetInt32(int.MaxValue)), false);
            var referenceDirectory = Path.Combine(AppContext.BaseDirectory, "Images", "Persons", "W");
            _references = Directory.Exists(referenceDirectory) ? Directory.GetFiles(referenceDirectory, "*.png").Order(StringComparer.Ordinal).ToArray() : [];
            LoadReference();
        }
        catch (Exception ex) when (ex is IOException or ArgumentException or System.Text.Json.JsonException)
        {
            _status = "Не удалось загрузить библиотеку портретов. " + ex.Message;
            InterfaceLog.Write(_status);
        }
    }

    public void OnDeactivated()
    {
        _portrait = null; _renderer?.Dispose(); _renderer = null;
        _reference?.Dispose(); _reference = null;
    }

    private void SetAppearance(CharacterAppearance appearance, bool remember = true)
    {
        if (_assets is null || _renderer is null) return;
        _assets.ValidateAppearance(appearance);
        var portrait = _renderer.Render(appearance);
        if (remember && _appearance is not null)
        {
            _undo.Add(_appearance); if (_undo.Count > 30) _undo.RemoveAt(0); _redo.Clear();
        }
        _appearance = appearance; _portrait = portrait;
        _seed.Clear(); foreach (char c in appearance.Seed.ToString(System.Globalization.CultureInfo.InvariantCulture)) _seed.TryAppendChar(c);
    }

    private void Randomize(string group = "All")
    {
        if (_appearance is null || _generator is null || _assets is null) return;
        for (int attempt = 0; attempt < 128; attempt++)
        {
            var next = _generator.Generate(RandomNumberGenerator.GetInt32(int.MaxValue));
            var parts = _appearance.Parts.ToBuilder(); var colors = _appearance.Colors.ToBuilder();
            foreach (var layer in _assets.Style.Layers)
            {
                bool selected = group == "All" || (group == "Hair" && layer.Category.StartsWith("Hair", StringComparison.Ordinal)) ||
                    (group == "Face" && layer.Category is "Face" or "Eyes" or "Eyebrows" or "Nose" or "Mouth") ||
                    (group == "Accessory" && layer.Category == "Accessory");
                if (!selected || _locks.Contains(layer.Category)) continue;
                parts.Remove(layer.Category);
                if (next.Parts.TryGetValue(layer.Category, out var id)) parts[layer.Category] = id;
            }
            if (group is "All" or "Colors")
                foreach (var entry in next.Colors) if (!_locks.Contains(entry.Key + "Color")) colors[entry.Key] = entry.Value;
            next = next with { Parts = parts.ToImmutable(), Colors = colors.ToImmutable() };
            if (!_assets.IsCompatible(next)) continue;
            SetAppearance(next); _status = "Новый персонаж · внешность можно сохранить в JSON"; return;
        }
        _status = "Снимите блокировку: выбранные детали ограничивают комбинации.";
    }

    private void Cycle(string category)
    {
        if (_assets is null || _appearance is null) return;
        var ids = _assets.Parts.Where(p => p.Category == category).Select(p => p.Id).Order(StringComparer.Ordinal).ToList();
        if (!_assets.Style.Layers.Single(l => l.Category == category).Required) ids.Insert(0, "");
        int current = ids.IndexOf(_appearance.Parts.GetValueOrDefault(category, ""));
        for (int offset = 1; offset <= ids.Count; offset++)
        {
            var id = ids[(current + offset) % ids.Count];
            var parts = id.Length == 0 ? _appearance.Parts.Remove(category) : _appearance.Parts.SetItem(category, id);
            var next = _appearance with { Parts = parts, LibraryVersion = _assets.Style.LibraryVersion };
            if (_assets.IsCompatible(next)) { SetAppearance(next); return; }
        }
    }

    private void ApplySeed()
    {
        if (_generator is null) return;
        if (int.TryParse(_seed.Text, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int seed))
        {
            SetAppearance(_generator.Generate(seed)); _status = "Восстановлена исходная внешность по seed (блокировки не применяются).";
        }
        else _status = "Seed должен быть целым числом от −2147483648 до 2147483647.";
    }

    private void SavePreset()
    {
        if (_appearance is null) return;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_presetPath)!);
            File.WriteAllText(_presetPath, AppearanceSerializer.Serialize(_appearance));
            _status = "Сохранено: LocalAppData / DeepSpaceSaga / PortraitPresets / temp-character.json";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { _status = "Ошибка сохранения: " + ex.Message; }
    }

    private void LoadPreset()
    {
        try { SetAppearance(AppearanceSerializer.Deserialize(File.ReadAllText(_presetPath))); _status = "Внешность восстановлена из JSON."; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException or ArgumentException)
        { _status = "Не удалось загрузить preset: " + ex.Message; }
    }

    private void Undo(bool redo)
    {
        var from = redo ? _redo : _undo; var to = redo ? _undo : _redo;
        if (from.Count == 0 || _appearance is null) return;
        to.Add(_appearance); var next = from[^1]; from.RemoveAt(from.Count - 1); SetAppearance(next, false);
    }

    private void LoadReference()
    {
        _reference?.Dispose(); _reference = null;
        if (_references.Length > 0) _reference = SKBitmap.Decode(_references[_referenceIndex % _references.Length]);
    }

    public void Render(SKCanvas canvas, int width, int height)
    {
        _scale = Math.Min(1, Math.Min(width / Width, height / Height));
        _left = (width - Width * _scale) / 2; _top = (height - Height * _scale) / 2;
        canvas.DrawRect(SKRect.Create(width, height), MenuStyle.DimOverlayFill);
        canvas.Save(); canvas.Translate(_left, _top); canvas.Scale(_scale); _buttons.Clear();
        using var paint = new SKPaint { IsAntialias = true, Color = new SKColor(17, 24, 34) };
        canvas.DrawRoundRect(SKRect.Create(Width, Height), 12, 12, paint);
        Text("КОНСТРУКТОР ПОРТРЕТОВ", 26, 40, 22, new SKColor(229, 236, 242));
        Text($"ЖЕНЩИНЫ  /  HUMAN · ADULT  /  БИБЛИОТЕКА {_assets?.Style.LibraryVersion ?? 2:D2}", 26, 66, 11, new SKColor(129, 159, 177));
        Button(new(1025, 22, 1094, 58), "Закрыть", () => { });
        if (_assets is not null && _appearance is not null)
        {
            Text("ДЕТАЛИ", 26, 103, 12);
            for (int i = 0; i < _assets.Style.Layers.Length; i++)
            {
                var category = _assets.Style.Layers[i].Category; float y = 120 + i * 47;
                string id = _appearance.Parts.GetValueOrDefault(category, "—");
                string variant = id == "—" ? id : id.Split('.')[^1];
                string label = category == "Face" && id != "—" && _assets.Get(id).DisplayName is { } name
                    ? $"{name} ›" : $"{CategoryNames.GetValueOrDefault(category, category)}  {variant} ›";
                Button(new(26, y, 205, y + 36), label, () => Cycle(category));
                Lock(new(211, y, 245, y + 36), category);
            }
            Text("Замок сохраняет деталь при генерации", 26, 562, 10);
            Button(new(26, 580, 130, 616), "‹ Отменить", () => Undo(false));
            Button(new(137, 580, 245, 616), "Повторить ›", () => Undo(true));
            Text("SEED", 804, 106, 11); _seed.Render(canvas, SeedBox);
            Button(new(1001, 118, 1094, 150), "Создать", ApplySeed);
            int row = 0;
            foreach (var palette in _assets.Style.Palettes)
            {
                float y = 185 + row++ * 60; string key = palette.Key;
                Text(key switch { "Skin" => "КОЖА", "Hair" => "ВОЛОСЫ", _ => "РАДУЖКА" }, 804, y, 11);
                Lock(new(1066, y - 14, 1094, y + 12), key + "Color");
                for (int i = 0; i < palette.Value.Length; i++)
                {
                    string color = palette.Value[i]; var rect = new SKRect(804 + i * 35, y + 10, 832 + i * 35, y + 36);
                    paint.Color = SKColor.Parse(color); canvas.DrawRoundRect(rect, 4, 4, paint);
                    if (_appearance.Colors[key] == color)
                    {
                        paint.Color = SKColors.White; paint.Style = SKPaintStyle.Stroke; paint.StrokeWidth = 2;
                        canvas.DrawRoundRect(rect, 4, 4, paint); paint.Style = SKPaintStyle.Fill;
                    }
                    _buttons.Add((rect, () => SetAppearance(_appearance with { Colors = _appearance.Colors.SetItem(key, color) })));
                }
            }
        }
        paint.Color = new SKColor(28, 40, 54); canvas.DrawRoundRect(Preview, 8, 8, paint);
        if (_portrait is not null) canvas.DrawImage(_portrait, Preview);
        if (_compare && _reference is not null)
        {
            canvas.Save(); canvas.ClipRect(new(Preview.MidX, Preview.Top, Preview.Right, Preview.Bottom));
            canvas.DrawBitmap(_reference, Preview); canvas.Restore();
            paint.Color = SKColors.White; canvas.DrawLine(Preview.MidX, Preview.Top, Preview.MidX, Preview.Bottom, paint);
        }
        Text("КЛИК ПО ПОРТРЕТУ — НОВЫЙ ПЕРСОНАЖ", 300, 643, 12, new SKColor(132, 190, 204));
        Text("ЭТАЛОН ИЗ PERSONS / W", 804, 373, 11);
        if (_reference is not null) canvas.DrawBitmap(_reference, new SKRect(822, 385, 1057, 620));
        Button(new(804, 628, 942, 656), "Другой эталон", () => { _referenceIndex++; LoadReference(); });
        Button(new(950, 628, 1094, 656), _compare ? "Скрыть 50 / 50" : "Сравнить 50 / 50", () => _compare = !_compare);
        Button(new(26, 674, 195, 710), "Новый персонаж", () => Randomize());
        Button(new(203, 674, 319, 710), "Лицо", () => Randomize("Face"));
        Button(new(327, 674, 443, 710), "Причёска", () => Randomize("Hair"));
        Button(new(451, 674, 567, 710), "Цвета", () => Randomize("Colors"));
        Button(new(575, 674, 703, 710), "Аксессуары", () => Randomize("Accessory"));
        Button(new(795, 674, 940, 710), "Сохранить JSON", SavePreset);
        Button(new(948, 674, 1094, 710), "Загрузить JSON", LoadPreset);
        canvas.Save(); canvas.ClipRect(new(26, 714, 1094, 744)); Text(_status, 26, 732, 10); canvas.Restore();
        canvas.Restore();

        void Text(string text, float x, float y, float size, SKColor? color = null)
        {
            paint.Color = color ?? new SKColor(168, 187, 200); paint.TextSize = size;
            paint.Typeface = MenuStyle.TypefaceRegular; canvas.DrawText(text, x, y, paint);
        }
        void Button(SKRect rect, string text, Action action)
        {
            MenuStyle.DrawButton(canvas, rect, text, rect.Contains(_mouseX, _mouseY) ? ButtonState.Hovered : ButtonState.Normal);
            _buttons.Add((rect, action));
        }
        void Lock(SKRect rect, string key)
        {
            Button(rect, "", () => ToggleLock(key));
            bool locked = _locks.Contains(key);
            paint.Color = locked ? new SKColor(126, 214, 224) : new SKColor(107, 126, 140);
            paint.Style = SKPaintStyle.Stroke; paint.StrokeWidth = 1.5f;
            canvas.DrawRoundRect(new SKRect(rect.MidX - 5, rect.MidY - 1, rect.MidX + 5, rect.MidY + 7), 1, 1, paint);
            canvas.DrawArc(new SKRect(rect.MidX - 4, rect.MidY - 8, rect.MidX + 4, rect.MidY + 4), locked ? 180 : 205, locked ? 180 : 145, false, paint);
            paint.Style = SKPaintStyle.Fill;
        }
    }

    private void ToggleLock(string key) { if (!_locks.Add(key)) _locks.Remove(key); }
    public ScreenEvent OnMouseDown(float x, float y, MouseButton button)
    {
        if (button != MouseButton.Left) return ScreenEvent.None;
        x = (x - _left) / _scale; y = (y - _top) / _scale;
        if (x < 0 || x > Width || y < 0 || y > Height || new SKRect(1025, 22, 1094, 58).Contains(x, y)) return ScreenEvent.CloseTempCharacterImage;
        _seedFocused = SeedBox.Contains(x, y);
        if (Preview.Contains(x, y)) { Randomize(); return ScreenEvent.None; }
        foreach (var item in _buttons) if (item.Rect.Contains(x, y)) { item.Action(); break; }
        return ScreenEvent.None;
    }
    public bool OnMouseMove(float x, float y)
    {
        _mouseX = (x - _left) / _scale; _mouseY = (y - _top) / _scale;
        return Preview.Contains(_mouseX, _mouseY) || SeedBox.Contains(_mouseX, _mouseY) || _buttons.Any(b => b.Rect.Contains(_mouseX, _mouseY));
    }
    public ScreenEvent OnMouseWheel(float x, float y, float delta) => ScreenEvent.None;
    public ScreenEvent OnKeyDown(Key key)
    {
        if (key == Key.Escape) return ScreenEvent.CloseTempCharacterImage;
        if (_seedFocused) { if (key == Key.Enter) ApplySeed(); else _seed.OnKeyDown(key); }
        else if (key == Key.Space) Randomize();
        return ScreenEvent.None;
    }
    public void OnTextInput(char c) { if (_seedFocused && (char.IsAsciiDigit(c) || c == '-')) _seed.TryAppendChar(c); }
}
