using System.Text.Json;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using SkiaSharp;
using static DeepSpaceSaga.Client.UI.Screens.TacticalBattle.BattleDesign;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle.View;

public sealed partial class Ep0019ViewTests
{
    private static string Output(string name)
    {
        string dir = Path.Combine(Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT")!, "ep0019", name);
        Directory.CreateDirectory(dir); return dir;
    }
    private static void Save(string name, SKBitmap bitmap, BattleLayout layout)
    {
        string dir = Output(name);
        using var image = SKImage.FromBitmap(bitmap); using var png = image.Encode(SKEncodedImageFormat.Png, 100); File.WriteAllBytes(Path.Combine(dir, "render.png"), png.ToArray());
        var v = layout.Camera;
        File.WriteAllText(Path.Combine(dir, "layout.json"), JsonSerializer.Serialize(new { Camera = new { scale = v.PixelsPerUnit, ox = v.PixelWidth / 2.0 - v.CenterX * v.PixelsPerUnit, oy = v.PixelHeight / 2.0 - v.CenterY * v.PixelsPerUnit }, Rects = layout.Rects.ToDictionary(p => p.Key, p => new { p.Value.Left, p.Value.Top, p.Value.Right, p.Value.Bottom }), layout.Texts }, new JsonSerializerOptions { WriteIndented = true }));
    }
    [Theory, MemberData(nameof(Scenes)), Trait("Category", "Ep0019View")]
    public async Task Scene_layout_matches_design_spec(string name)
    {

        await using var s = await Ep0019Scenes.Create(name); using var locale = Localization.UseLocaleForTests("Russian"); using var bitmap = s.Render(); var f = s.Screen.CaptureFrame(1920, 1080)!; var l = s.Screen.Layout;
        float db = name switch { "3-salvo-flight" => 262, "4-salvo-results" => 246, "5-outcome" => 238, _ => 250 };
        Rect(new(1040, 6, 1274, db + 120), l.Rects["phase"]); Rect(new(1040, db + 28, 1274, db + 120), l.Rects["action"]);
        int i = 0;
        foreach (var u in f.Battle.Units.Where(u => u.Kind == TacticalUnitKind.Drone && u.Side == f.Battle.PlayerSide))
        { Rect(new(6, 6 + 87 * i, 196, 88 + 87 * i), l.Rects["pilot:" + u.Id]); i++; }
        bool hover = name is not ("1-move-no-hover" or "5-outcome"); Assert.Equal(hover, l.Rects.ContainsKey("hover"));
        if (hover) Rect(new(1040, db + 126, 1274, db + 230), l.Rects["hover"]);
        if (f.Movement is { } movement)
            foreach (var row in movement.Orders)
            { var p = f.View.Project(row.Path[^1]); float r = row.Unit.Id == movement.Selected ? 5 : 4; Rect(new(p.X - r, p.Y - r, p.X + r, p.Y + r), l.Rects["endpoint:" + row.Unit.Id]); }
        var points = f.Battle.Units.Where(u => name.StartsWith("1-", StringComparison.Ordinal) || u.Kind == TacticalUnitKind.Drone).Select(u => u.Pose).Concat(f.Battle.Paths.SelectMany(p => p.Points)).ToList();
        if (f.Movement is { } m) points.AddRange(m.Orders.Select(o => o.Path[^1]));
        double margin = name.StartsWith("1-", StringComparison.Ordinal) ? 50 : 70;
        double expectedScale = Math.Min((832 - 2 * margin) / Math.Max(1, points.Max(p => p.X) - points.Min(p => p.X)), (708 - 2 * margin) / Math.Max(1, points.Max(p => p.Y) - points.Min(p => p.Y)));
        Assert.InRange(Math.Abs(f.View.PixelsPerUnit - expectedScale), 0, .00001);
        var center = f.View.Project((points.Min(p => p.X) + points.Max(p => p.X)) / 2, (points.Min(p => p.Y) + points.Max(p => p.Y)) / 2);
        Assert.InRange(center.X, 617.99, 618.01); Assert.InRange(center.Y, 359.99, 360.01);
        if (name.StartsWith("2-", StringComparison.Ordinal))
        {
            Rect(new(1048, 242, 1154, 264), l.Rects["button:hold"]);
            Rect(new(1160, 242, 1266, 264), l.Rects["button:evade"]);
            var unit = f.Battle.Units.Single(u => u.Id == "Марта"); var position = f.View.Project(unit.Pose);
            using var text = BattleTypography.TextPaint(12.5f, false);
            float width = Math.Max(32, text.MeasureText("Марта"));
            // The selected unit is placed first; its first left candidate is clear in the supplied seed1 geometry.
            Rect(SKRect.Create(position.X - 11 - width, position.Y - 11.75f, width, 23.5f), l.Rects["label:Марта"]);
        }
        Save(name, bitmap, l);
        static void Rect(SKRect expected, SKRect actual)
        { Assert.InRange(Math.Abs(expected.Left - actual.Left), 0, .01); Assert.InRange(Math.Abs(expected.Top - actual.Top), 0, .01); Assert.InRange(Math.Abs(expected.Right - actual.Right), 0, .01); Assert.InRange(Math.Abs(expected.Bottom - actual.Bottom), 0, .01); }
    }
    private static string[] ExpectedTexts(string name) => name switch
    {
        "1-move" or "1-move-no-hover" => ["ХОД 1  •", "ДВИЖЕНИЕ", "Начальный бой · свои 4 · враг 6 · потери 0:0", "Вера · манёвр", "ПЛАН ДВИЖЕНИЯ", "Скорость: 77,3", "50,0", "77,3", "Поворот: +5,5°", "−38,4°", "+38,4°", "4 предложения ИИ", "Движение не выполнено", "Выполнить движение [Enter]", "Движение: скорость 77,4 · поворот −24,3°", "Движение: скорость 81,4 · поворот +4,3°", "Движение: скорость 81,6 · поворот −6,8°", "ИИ"],
        "2-fire" or "2-fire-hover-own" => ["ХОД 3  •", "СТРЕЛЬБА", "Марта → Таг-4", "ПЛАН ВЫСТРЕЛА", "56%", "шанс попадания", "Дистанция 112,2", "Угол 16,1°", "Меч I · урон 50–75", "Таг-4: броня 20/20, затем корпус 40/40", "Ловушка [D]", "Не стрелять", "Уклониться [E]", "2 стреляют · 2 без цели", "План готов к отправке", "Выполнить залп [Enter]", "нет целей", "Таг-4 56%", "Таг-5 54%", "Стрельба: целей 1 · урон 50–75 · иниц. 59"],
        "3-salvo-flight" => ["ХОД 3  •", "ЗАЛП", "Марта → Таг-4", "ВЫСТРЕЛ В ПОЛЁТЕ", "Очерёдность огня", "Снаряды в полёте", "Выстрел 1 из 2", "Пропустить полёт [Enter]", "огонь", "ждёт", "без цели"],
        "4-salvo-results" => ["ХОД 3  •", "РЕЗУЛЬТАТЫ", "Марта → Таг-4", "РЕЗУЛЬТАТ ВЫСТРЕЛА", "ПРОМАХ", "Нанесённый урон: 0", "Повреждения Марты", "Броня: 80 → 80", "100%", "Корпус: 55/55 · без урона", "В строю", "Залп завершён", "2 выстрела · 0 попаданий", "ловушка цели", "56% → 34%", "↺ Повтор", "К движению [Enter]", "не стреляла", "не стрелял", "промах"],
        _ => ["ХОД 16  •", "ИТОГ", "Начальный бой · свои 4 · враг 1 · потери 0:6", "Начальный бой", "ИТОГ БОЯ", "ПОБЕДА", "у противника не осталось дронов", "Ходов", "16", "Выстрелов", "23", "Попаданий", "13", "Потери", "свои 0 · враг 6", "Бой окончен", "Esc — вернуться на карту мира", "Новый бой", "Выйти [Esc]", "Итог: нанесено 193 · получено 0", "Итог: нанесено 14 · получено −74", "Итог: нанесено 193 · получено 0", "Итог: нанесено 93 · получено 0"]
    };
    [Theory, MemberData(nameof(Scenes)), Trait("Category", "Ep0019View")]
    public async Task Scene_texts_match_design_spec(string name)
    {
        await using var s = await Ep0019Scenes.Create(name); using var locale = Localization.UseLocaleForTests("Russian"); using var bitmap = s.Render();
        foreach (string text in ExpectedTexts(name)) Assert.Contains(s.Screen.Layout.Texts, t => t.Text == text);
        if (name is not ("1-move-no-hover" or "5-outcome"))
            foreach (string text in name == "2-fire-hover-own" ? new[] { "ПОД КУРСОРОМ · ПИЛОТ", "Олег", "Корпус 55/55", "Броня 80/80", "Копьё I · 40–60 · точн. 50 · иниц. 50" } : new[] { "ПОД КУРСОРОМ · ПРОТИВНИК", "Таг-6", "Корпус 40/40", "Броня 20/20", "Обрез I · 40–50 · дальн. 110 · иниц. 41" }) Assert.Contains(s.Screen.Layout.Texts, t => t.Text == text);
        if (name == "3-salvo-flight")
            foreach (string row in new[] { "Марта → Таг-4", "Вера — нет цели", "Таг-2 — нет цели", "Юн — нет цели", "Олег → Таг-5", "Таг-1 — нет цели", "Таг-5 — нет цели", "Таг-3 — нет цели", "Таг-4 — нет цели", "Таг-6 — нет цели" }) Assert.Contains(s.Screen.Layout.Texts, t => t.Text == row);
        Assert.DoesNotContain(s.Screen.Layout.Texts, t => t.Text.Contains("TacticalBattle.", StringComparison.Ordinal));
    }
    [Theory, MemberData(nameof(Scenes)), Trait("Category", "Ep0019View")]
    public async Task Scene_probes_pass(string name)
    {
        await using var s = await Ep0019Scenes.Create(name); using var locale = Localization.UseLocaleForTests("Russian"); using var b = s.Render(); var f = s.Screen.CaptureFrame(1920, 1080)!; var l = s.Screen.Layout;
        var probes = new List<object>(); var failures = new List<string>();
        void Probe(string key, SKRect r, double limit, params SKColor[] colors)
        { double fraction = RasterProbe.Fraction(b, r, colors); probes.Add(new { key, area = new { r.Left, r.Top, r.Right, r.Bottom }, fraction, limit, passed = fraction >= limit }); if (fraction < limit) failures.Add(key + "=" + fraction.ToString("F3")); }
        void BarProbe(string key, SKRect area, SKRect card, PilotStat stat, float centerY)
        {
            double x0 = card.Left + 60, x1 = card.Right - 41;
            double end = x0 + (x1 - x0) * Math.Clamp(stat.Fill, 0, 1);
            int matches = 0, count = 0;
            for (int y = (int)Math.Ceiling(area.Top * 1.5); y < area.Bottom * 1.5; y++)
                for (int x = (int)Math.Ceiling(area.Left * 1.5); x < area.Right * 1.5; x++)
                {
                    double px = (x + .5) / 1.5, py = (y + .5) / 1.5;
                    double dx = px - Math.Clamp(px, x0, end), dy = py - centerY;
                    var expected = stat.Fill > 0 && dx * dx + dy * dy <= 2.25 ? stat.Tone : Track;
                    var actual = b.GetPixel(x, y);
                    if (Math.Abs(actual.Red - expected.Red) <= 40 && Math.Abs(actual.Green - expected.Green) <= 40
                        && Math.Abs(actual.Blue - expected.Blue) <= 40) matches++;
                    count++;
                }
            double fraction = matches / (double)count;
            probes.Add(new { key, area = new { area.Left, area.Top, area.Right, area.Bottom }, stat.Fill, fraction, limit = .8 });
            if (fraction < .8) failures.Add(key + "=" + fraction.ToString("F3"));
        }
        Probe("panel", new(1046, 62, 1270, 66), .9, Panel); Probe("tag", new(1049.5f, 94, 1053, 108), .9, YellowDark);
        Probe("map", new(700, 690, 760, 700), .95, MapBg, Grid);
        var action = l.Rects["action"]; Probe("primary", new(name is "4-salvo-results" or "5-outcome" ? 1150 : 1078, action.Top + 58.5f, 1236, action.Top + 61), .9, Yellow);
        var cards = PilotCardModel.Project(f);
        foreach (var card in cards)
        {
            var r = l.Rects["pilot:" + card.UnitId]; BarProbe("armor:" + card.UnitId, new(r.Left + 61, r.Top + 40.5f, r.Left + 70, r.Top + 41.5f), r, card.Stats[0], r.Top + 41);
            BarProbe("mind:" + card.UnitId, new(r.Left + 61, r.Top + 62.5f, r.Left + 70, r.Top + 63.5f), r, card.Stats[2], r.Top + 63);
            if (card.Selected) Probe("selected:" + card.UnitId, new(r.Left + 20, r.Top - .4f, r.Right - 20, r.Top + .4f), .6, Yellow);
        }
        foreach (var u in f.Battle.Units.Where(u => u.Kind == TacticalUnitKind.Drone && u.State != TacticalUnitState.Destroyed))
        { var p = f.View.Project(u.Pose); Probe("marker:" + u.Id, SKRect.Create(p.X - 1.5f, p.Y - 1.5f, 3, 3), .5, u.Side == f.Battle.PlayerSide ? Cyan : Orange); }
        if (f.HoverUnitId is { } id)
        {
            var u = f.Battle.Units.Single(u => u.Id == id); var p = f.View.Project(u.Pose); var color = u.Side == f.Battle.PlayerSide ? Cyan : Orange;
            foreach (int x in new[] { -1, 1 }) foreach (int y in new[] { -1, 1 })
            {
                // Three physical pixels along each straight stroke end, as specified in §12.4.
                void End(string key, float xx, float yy, bool horizontal)
                {
                    int hits = 0; int px = (int)Math.Floor(xx * 1.5), py = (int)Math.Floor(yy * 1.5);
                    for (int d = 0; d < 3; d++) { var value = b.GetPixel(px + (horizontal ? d * x : 0), py + (horizontal ? 0 : d * y)); if (Math.Abs(value.Red - color.Red) <= 40 && Math.Abs(value.Green - color.Green) <= 40 && Math.Abs(value.Blue - color.Blue) <= 40) hits++; }
                    probes.Add(new { key, fraction = hits / 3.0, limit = .5 }); if (hits < 2) failures.Add(key + "=" + hits + "/3");
                }
                End($"hover-h:{x}:{y}", p.X + 9 * x, p.Y + 14 * y, true);
                End($"hover-v:{x}:{y}", p.X + 14 * x, p.Y + 9 * y, false);
            }
        }
        if (name.StartsWith("1-", StringComparison.Ordinal))
        { Probe("slider", new(1056, 139, 1060, 141), .9, Cyan); var row = f.Movement!.SelectedOrder!; var p = f.View.Project(row.Path[row.Path.Length / 2]); Probe("path", SKRect.Create(p.X - 1, p.Y - 1, 2, 2), .6, Cyan); }
        if (name.StartsWith("2-", StringComparison.Ordinal))
        {
            var row = f.FirePlan!.Selected!; var a = f.View.Project(row.Drone.Pose); var z = f.View.Project(f.Battle.Units.Single(u => u.Id == row.TargetId).Pose);
            Probe("arrow", SKRect.Create((a.X + z.X) / 2 - 1, (a.Y + z.Y) / 2 - 1, 2, 2), .6, Yellow);
            double angle = row.Drone.Pose.Course * Math.PI / 180, radius = row.Drone.Build!.Range * f.View.PixelsPerUnit * .6;
            Probe("sector", SKRect.Create(a.X + (float)(radius * Math.Cos(angle)) - 1.5f, a.Y + (float)(radius * Math.Sin(angle)) - 1.5f, 3, 3), .5, SKColor.Parse("#174A47"));
        }
        if (name == "3-salvo-flight") { var r = l.Rects["projectile"]; Probe("shell", SKRect.Create(r.MidX - 1, r.MidY - 1, 2, 2), .75, Shell); }
        if (name == "4-salvo-results")
        {
            foreach (var damage in f.Salvo!.Damage.Where(d => d.Armor > 0 && d.Unit.Side == f.Battle.PlayerSide))
            {
                var r = l.Rects["callout:" + damage.Unit.Id];
                Probe("damage-callout:" + damage.Unit.Id, SKRect.Create(r.Left + 3, r.Top + 3, 4, 2), .9, YellowDark);
            }
            if (f.Salvo.Damage.All(d => d.Armor == 0 || d.Unit.Side != f.Battle.PlayerSide))
                Assert.DoesNotContain(l.Rects.Keys, key => key.StartsWith("callout:", StringComparison.Ordinal));
        }
        if (name == "5-outcome") Probe("victory", new(1052, 128, 1060, 140), .15, Yellow);
        Save(name, b, l); File.WriteAllText(Path.Combine(Output(name), "probes.json"), JsonSerializer.Serialize(probes, new JsonSerializerOptions { WriteIndented = true }));
        Assert.True(failures.Count == 0, name + ": " + string.Join(", ", failures));
    }
}
