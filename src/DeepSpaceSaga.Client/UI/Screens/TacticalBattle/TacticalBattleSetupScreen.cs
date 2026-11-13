using System.Globalization;
using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion.TacticalBattle;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

public sealed class TacticalBattleSetupScreen : TacticalBattleModalScreen
{
    private sealed record Choice(string Id, string Label, int Count = 0);
    private sealed record DroneRow(TacticalDroneSetupOption Drone, DropDown<Choice> Gun, DropDown<Choice> Plate, DropDown<Choice> Shield);
    private readonly TextInputBox _seed = new(20, c => c is >= '0' and <= '9' or '-' or '−');
    private readonly ShortcutButton _random = new(L("RandomSeed"), tooltip: L("RandomSeedHelp"));
    private readonly ShortcutButton _start = new(L("Start"), "Enter");
    private readonly ShortcutButton _back = new(L("ToMap"), "Esc");
    private readonly List<DroneRow> _rows = [];
    private readonly List<DropDown<Choice>> _fields = [];
    private TacticalBattleOptions? _options;
    private TacticalScenarioOption? _scenario;
    private DropDown<Choice>? _scenarios, _ownTactic, _enemyTactic;
    private TacticalBattleSetupLayout? _layout;
    private int _width, _height, _focus = -1;
    private float _scroll;
    internal TacticalBattleSetupLayout? CurrentLayout => _layout;
    internal string SeedText => _seed.Text;
    private int SeedFocus => _fields.Count;
    private int RandomFocus => SeedFocus + 1;
    private int StartFocus => SeedFocus + 2;
    private int BackFocus => SeedFocus + 3;

    public TacticalBattleSetupScreen(TacticalBattleFlow flow, float uiScale = 1) : base(flow, true, uiScale)
    {
        SetSeed(BattleFormat.Seed(flow.LastStart?.Seed ?? 1));
        _random.Clicked += () => SetSeed(BattleFormat.Seed(Random.Shared.NextInt64(1, 100000)));
    }
    internal override bool CanStart
    {
        get { EnsureForm(); return base.CanStart && _scenario is not null && TrySeed(out _); }
    }
    private void EnsureForm()
    {
        if (Flow.Options is not { Availability.Available: true } options || options.Scenarios.IsDefaultOrEmpty || ReferenceEquals(_options, options)) return;
        _options = options;
        _scenarios = Dropdown(options.Scenarios.Select(s => new Choice(s.Id, s.Name)));
        Select(_scenarios, Flow.LastStart?.ScenarioId ?? options.Scenarios[0].Id);
        _scenarios.Changed += choice =>
        {
            BuildScenario(choice.Id, restore: false);
            _scroll = 0; SetFocus(0); LayoutControls();
        };
        BuildScenario(_scenarios.SelectedItem.Id, restore: true);
    }
    private void BuildScenario(string id, bool restore)
    {
        var options = _options!;
        _scenario = options.Scenarios.First(s => s.Id == id);
        var previous = restore && Flow.LastStart?.ScenarioId == id ? Flow.LastStart : null;
        _rows.Clear(); _fields.Clear(); _fields.Add(_scenarios!);
        var guns = options.Weapons.Select(w => new Choice(w.Id, WeaponLabel(w))).ToArray();
        var plates = options.Armor.Select(p => new Choice(p.Id, F("KitArmor", p.Name, N(p.Armor), N(p.Mass)))).ToArray();
        foreach (var drone in _scenario.Drones.Where(d => d.Side == _scenario.PlayerSide))
        {
            var gun = Dropdown(guns); var plate = Dropdown(plates);
            var shields = new List<Choice> { new("", L("NoShield")) };
            foreach (var shield in options.Shields)
                for (int count = 1; count <= drone.ShieldSlots; count++)
                    shields.Add(new(shield.Id, F("KitShield", shield.Name, N(count), N((double)shield.Defense * count), N(shield.Mass * count)), count));
            var shieldSelect = Dropdown(shields);
            var saved = previous is not null && !previous.Loadout.IsDefaultOrEmpty ? previous.Loadout.FirstOrDefault(l => l.DroneId == drone.Id) : null;
            Select(gun, saved?.GunId ?? drone.WeaponId);
            Select(plate, saved?.PlateId ?? drone.ArmorId);
            Select(shieldSelect, saved is null ? drone.ShieldId ?? "" : saved.ShieldId ?? "", saved?.ShieldCount ?? drone.ShieldCount);
            _rows.Add(new(drone, gun, plate, shieldSelect));
            _fields.Add(gun); _fields.Add(plate); _fields.Add(shieldSelect);
        }
        var tactics = options.Tactics.Select(t => new Choice(t.Id, t.Name)).ToArray();
        _ownTactic = Dropdown(tactics); _enemyTactic = Dropdown(tactics);
        Select(_ownTactic, previous?.OwnTacticId ?? _scenario.OwnTacticId);
        Select(_enemyTactic, previous?.EnemyTacticId ?? _scenario.EnemyTacticId);
        _fields.Add(_ownTactic); _fields.Add(_enemyTactic);
        RefreshFocus();
    }
    private static DropDown<Choice> Dropdown(IEnumerable<Choice> choices) => new(choices, c => c.Label);
    private static void Select(DropDown<Choice> field, string id, int count = 0)
    {
        for (int i = 0; i < field.Items.Length; i++)
            if (field.Items[i].Id == id && field.Items[i].Count == count) { field.SelectIndex(i); return; }
    }
    private static string WeaponLabel(TacticalWeaponOption w) => F("KitWeapon", w.Name, N(w.Tier), BattleFormat.Degrees(2.0 * w.ArcDegrees),
        N(w.Range), N(w.DamageMin), N(w.DamageMax), N(w.Piercing), BattleFormat.Signed(w.HitBonus), N(w.Mass));
    private static string N(double value) => BattleFormat.Number(value);
    private static string F(string key, params object[] values) => string.Format(CultureInfo.InvariantCulture, L(key), values);
    private static string Skill(TacticalSkillRange value) => value.IsRandom ? N(value.Minimum) + "–" + N(value.Maximum) : N(value.Minimum);
    internal string ScenarioSummary
    {
        get
        {
            if (_scenario is not { } scenario) return "";
            string text = scenario.Description + " · " + F("SetupOpponents", N(scenario.Drones.Count(d => d.Side != scenario.PlayerSide)));
            if (scenario.Goal is { } goal) text += "\n" + F("SetupGoal", BattleFormat.Probability(goal.WinMaxOneLoss), BattleFormat.Probability(goal.WinNoLoss), BattleFormat.Probability(goal.Tolerance));
            return text;
        }
    }
    private void SetSeed(string text) { _seed.Clear(); foreach (char c in text) _seed.TryAppendChar(c); }
    private bool TrySeed(out long seed)
    {
        string text = _seed.Text.Replace('−', '-');
        if (text.Length == 0) { seed = 1; return true; }
        if (!long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out seed)) return false;
        if (seed == 0) seed = 1;
        return true;
    }
    public override StartTacticalBattleRequest BuildStartRequest()
    {
        EnsureForm();
        if (_scenario is null || !TrySeed(out long seed)) throw new InvalidOperationException("Battle setup is not ready.");
        return new(TacticalBattleFlow.NewIdentity("start"), seed, _scenario.Id, _ownTactic!.SelectedItem.Id, _enemyTactic!.SelectedItem.Id,
            [.. _rows.Select(r => new TacticalLoadoutChoice(r.Drone.Id, r.Gun.SelectedItem.Id, r.Plate.SelectedItem.Id,
                r.Shield.SelectedItem.Id.Length == 0 ? null : r.Shield.SelectedItem.Id, r.Shield.SelectedItem.Count))]);
    }
    private void LayoutControls()
    {
        if (_width <= 0 || _height <= 0 || _scenarios is null || _ownTactic is null || _enemyTactic is null) return;
        var layout = new TacticalBattleSetupLayout(_width, _height, UiScale, _rows.Count, _scroll);
        _layout = layout; _scroll = layout.Scroll;
        _scenarios.Layout(layout.Scenario, layout.Viewport);
        for (int row = 0; row < _rows.Count; row++)
        {
            _rows[row].Gun.Layout(layout.RowCell(row, 2), layout.Viewport);
            _rows[row].Plate.Layout(layout.RowCell(row, 3), layout.Viewport);
            _rows[row].Shield.Layout(layout.RowCell(row, 4), layout.Viewport);
        }
        _ownTactic.Layout(layout.OwnTactic, layout.Viewport); _enemyTactic.Layout(layout.EnemyTactic, layout.Viewport);
        _random.Bounds = layout.RandomSeed; _start.Bounds = layout.Start; _back.Bounds = layout.Close;
    }
    public override void Render(SKCanvas canvas, int width, int height)
    {
        EnsureForm();
        if (_scenario is null) { base.Render(canvas, width, height); return; }
        _width = width; _height = height; LayoutControls();
        if (_layout is not { } layout) return;
        canvas.Clear(TabletopStyle.Background);
        canvas.Save(); canvas.Scale(UiScale);
        var title = new SKRect(20, 10, layout.Viewport.Right - 20, 47);
        TabletopStyle.Panel(canvas, title);
        ControlVisuals.Label(canvas, L("SetupTitle"), new(32, 10, title.Right - 12, 47), true, 1);
        TabletopStyle.Field(canvas, layout.Content);
        canvas.Save(); canvas.ClipRect(layout.Content);
        ControlVisuals.Label(canvas, L("Scenario"), layout.ScenarioCaption, true, 1);
        DrawParagraph(canvas, ScenarioSummary, layout.Description);
        DrawParagraph(canvas, F("SetupRules", _scenario.PlayerSide), layout.Rules);
        string[] headings = ["Drone", "FrameSkills", "Weapon", "Armor", "Shield"];
        for (int i = 0; i < headings.Length; i++) ControlVisuals.Label(canvas, L(headings[i]), layout.ColumnTitle(i), true, 1);
        for (int i = 0; i < _rows.Count; i++)
        {
            var row = _rows[i];
            DrawParagraph(canvas, row.Drone.Id, layout.RowCell(i, 0));
            DrawParagraph(canvas, row.Drone.FrameName + "\n" + F("SetupSkills", Skill(row.Drone.Accuracy), Skill(row.Drone.Maneuver)), layout.RowCell(i, 1));
        }
        ControlVisuals.Label(canvas, L("OwnTactic"), TacticalBattleSetupLayout.Above(layout.OwnTactic), true, 1);
        ControlVisuals.Label(canvas, L("EnemyTactic"), TacticalBattleSetupLayout.Above(layout.EnemyTactic), true, 1);
        foreach (var field in _fields) { field.Enabled = !Flow.Pending; field.Draw(canvas); }
        ControlVisuals.Label(canvas, L("Seed"), TacticalBattleSetupLayout.Above(layout.Seed), true, 1);
        _seed.Render(canvas, layout.Seed, focused: _focus == SeedFocus);
        _random.Enabled = !Flow.Pending; _random.Draw(canvas);
        DrawParagraph(canvas, L("KitHelp") + "\n" + L("ChanceFormula"), layout.Help);
        canvas.Restore();
        _start.Primary = true;
        _start.Enabled = CanStart; _back.Enabled = !Flow.Pending;
        _start.Draw(canvas); _back.Draw(canvas);
        string status = BlockedNotice is { Length: > 0 } notice ? notice : Flow.Pending ? L("Waiting") : !TrySeed(out _) ? L("InvalidSeed") : Flow.Error;
        if (status.Length > 0) DrawParagraph(canvas, status, layout.Status);
        foreach (var field in _fields) field.DrawPopup(canvas);
        foreach (var field in _fields) field.DrawTooltip(canvas, layout.Viewport);
        _random.DrawTooltip(canvas, layout.Viewport);
        canvas.Restore();
    }
    private static void DrawParagraph(SKCanvas canvas, string text, SKRect bounds)
    {
        if (bounds.IsEmpty) return;
        using var paint = ControlText.Paint(1);
        var lines = ControlText.Wrap(text, bounds.Width, paint);
        canvas.Save(); canvas.ClipRect(bounds);
        float baseline = bounds.Top - paint.FontMetrics.Ascent;
        foreach (string line in lines) { canvas.DrawText(line, bounds.Left, baseline, paint); baseline += 17; }
        canvas.Restore();
    }
    private SKPoint Logical(float x, float y) => new((float)(x / (double)UiScale), (float)(y / (double)UiScale));
    private DropDown<Choice>? OpenField => _fields.FirstOrDefault(f => f.IsOpen);
    public override ScreenEvent OnMouseDown(float x, float y, MouseButton button)
    {
        EnsureForm();
        if (_scenario is null) return base.OnMouseDown(x, y, button);
        if (Flow.Pending || _layout is not { } layout) return ScreenEvent.None;
        var p = Logical(x, y);
        if (OpenField is { } open) { open.OnMouseDown(p.X, p.Y, button); return ScreenEvent.None; }
        if (button != MouseButton.Left) return ScreenEvent.None;
        if (_back.OnMouseDown(p.X, p.Y, button)) return ScreenEvent.CloseTacticalBattle;
        if (CanStart && _start.OnMouseDown(p.X, p.Y, button)) return ScreenEvent.StartTacticalBattle;
        if (!layout.Content.Contains(p.X, p.Y)) return ScreenEvent.None;
        for (int i = 0; i < _fields.Count; i++)
            if (_fields[i].OnMouseDown(p.X, p.Y, button)) { SetFocus(i); return ScreenEvent.None; }
        if (layout.Seed.Contains(p.X, p.Y)) SetFocus(SeedFocus);
        else if (_random.OnMouseDown(p.X, p.Y, button)) SetFocus(RandomFocus);
        return ScreenEvent.None;
    }
    public override bool OnMouseMove(float x, float y)
    {
        if (_scenario is null) return base.OnMouseMove(x, y);
        if (_layout is not { } layout) return false;
        var p = Logical(x, y); bool inContent = layout.Content.Contains(p.X, p.Y);
        bool hover = _start.OnMouseMove(p.X, p.Y) | _back.OnMouseMove(p.X, p.Y);
        foreach (var field in _fields) hover |= inContent || field.IsOpen ? field.OnMouseMove(p.X, p.Y) : field.OnMouseMove(-1, -1);
        hover |= inContent ? _random.OnMouseMove(p.X, p.Y) : _random.OnMouseMove(-1, -1);
        return hover || inContent && layout.Seed.Contains(p.X, p.Y);
    }
    public override ScreenEvent OnMouseWheel(float x, float y, float delta)
    {
        if (_scenario is null) return base.OnMouseWheel(x, y, delta);
        if (_layout is not { } layout || !float.IsFinite(delta) || Flow.Pending) return ScreenEvent.None;
        var p = Logical(x, y);
        if (OpenField is { } open) open.OnMouseWheel(p.X, p.Y, delta);
        else if (layout.Content.Contains(p.X, p.Y)) { _scroll = (float)PyMath.Clamp(_scroll + delta / 100.0 * 42, 0, layout.MaximumScroll); LayoutControls(); }
        return ScreenEvent.None;
    }
    public override ScreenEvent OnKeyDown(Key key, KeyModifiers modifiers, bool repeat)
    {
        EnsureForm();
        if (_scenario is null) return base.OnKeyDown(key, modifiers, repeat);
        if (BlockWorldKey(key, modifiers, _focus == SeedFocus)) return ScreenEvent.None;
        if (Flow.Pending) return ScreenEvent.None;
        if (key == Key.Tab && !repeat)
        {
            OpenField?.ResetInput();
            int direction = modifiers.HasFlag(KeyModifiers.Shift) ? -1 : 1;
            int count = _fields.Count + 4;
            int next = _focus < 0 ? direction > 0 ? 0 : count - 1 : (_focus + count + direction) % count;
            if (next == StartFocus && !CanStart) next = (next + count + direction) % count;
            SetFocus(next); return ScreenEvent.None;
        }
        if (key == Key.Escape)
        {
            if (OpenField is { } open) { open.ResetInput(); return ScreenEvent.None; }
            return ScreenEvent.CloseTacticalBattle;
        }
        if (_focus >= 0 && _focus < _fields.Count && key != Key.Space && _fields[_focus].OnKeyDown(key)) return ScreenEvent.None;
        if (_focus == SeedFocus && key == Key.Backspace) { _seed.Backspace(); return ScreenEvent.None; }
        if (key is Key.Enter or Key.KeypadEnter && !repeat)
        {
            if (_focus == RandomFocus) { _random.OnKeyDown(key); return ScreenEvent.None; }
            if (_focus == BackFocus) return ScreenEvent.CloseTacticalBattle;
            return ActionEvent;
        }
        return ScreenEvent.None;
    }
    public override void OnTextInput(char c) { if (_focus == SeedFocus && !Flow.Pending) _seed.TryAppendChar(c); }
    private void SetFocus(int index)
    {
        _focus = index; RefreshFocus();
        if (_layout is not { } layout || index < 0 || index > RandomFocus) return;
        SKRect rect = index < _fields.Count ? _fields[index].Bounds : index == SeedFocus ? layout.Seed : layout.RandomSeed;
        if (rect.Top < layout.Content.Top) _scroll -= layout.Content.Top - rect.Top;
        else if (rect.Bottom > layout.Content.Bottom) _scroll += rect.Bottom - layout.Content.Bottom;
        LayoutControls();
    }
    private void RefreshFocus()
    {
        for (int i = 0; i < _fields.Count; i++) _fields[i].HasFocus = i == _focus;
        _random.HasFocus = _focus == RandomFocus; _start.HasFocus = _focus == StartFocus; _back.HasFocus = _focus == BackFocus;
    }
    public override void OnInputReset()
    {
        base.OnInputReset();
        foreach (var field in _fields) field.ResetInput();
        _random.ResetInput(); _start.ResetInput(); _back.ResetInput();
    }
}
