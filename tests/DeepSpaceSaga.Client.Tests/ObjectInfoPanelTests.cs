using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Client.UI.Screens.Trade;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>
/// Object Info panel (top-right) — mirrors the Commands Panel (top-left) chrome and
/// shows two fixed rows: "Player Ship" and "Selected Object" (Image/Speed/Direction/Name).
/// </summary>
public class ObjectInfoPanelTests
{
    private const int ScreenWidth = 1280;
    private const int ScreenHeight = 720;
    private const string PlayerShipId = "SPC-0001";

    private static (SnapshotBuffer buffer, GameSessionScreen screen) CreateScreen()
    {
        var buffer = new SnapshotBuffer();
        var predictor = new LinearMotionPredictor();
        var screen = new GameSessionScreen(buffer, predictor);
        return (buffer, screen);
    }

    private static void RenderScreen(GameSessionScreen screen, int width = ScreenWidth, int height = ScreenHeight)
    {
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, width, height);
    }

    private static void UpdateBufferWithShip(
        SnapshotBuffer buffer,
        string playerShipId,
        double speedKmS = 0,
        double direction = 0,
        double x = 10000,
        double y = 10000,
        string? displayName = null,
        string? image = null)
    {
        var ship = new ObjectMotionSnapshot(
            playerShipId, x, y, speedKmS, direction, DisplayName: displayName, Image: image);
        buffer.Update(new AuthoritativeSnapshot(
            SnapshotSequence: 1,
            GameTimeMs: 0,
            CurrentSpeed: SimulationSpeed.Speed1,
            Objects: ImmutableArray.Create(ship),
            PlayerShipObjectId: playerShipId));
    }

    // ── BuildLines (pure formatting) ────────────────────────────────

    [Fact]
    public void Lines_are_all_placeholders_when_no_data()
    {
        var lines = ObjectInfoPanel.BuildLines(null);

        Assert.Equal(3, lines.Count);
        Assert.Equal(("Name", "—"), lines[0]);
        Assert.Equal(("Speed", "—"), lines[1]);
        Assert.Equal(("Direction", "—"), lines[2]);
    }

    [Fact]
    public void Speed_is_formatted_with_three_decimal_places()
    {
        var data = new ObjectInfoPanelData("OBJ-1", null, SpeedKmS: 12.3456, Direction: 0, RenderObjectType: null);
        var lines = ObjectInfoPanel.BuildLines(data);
        Assert.Equal("12.346 km/s", Assert.Single(lines, l => l.Label == "Speed").Value);
    }

    [Fact]
    public void Speed_zero_is_formatted_correctly()
    {
        var data = new ObjectInfoPanelData("OBJ-1", null, SpeedKmS: 0, Direction: 0, RenderObjectType: null);
        var lines = ObjectInfoPanel.BuildLines(data);
        Assert.Equal("0 km/s", Assert.Single(lines, l => l.Label == "Speed").Value);
    }

    [Theory]
    [InlineData(0, "0°")]
    [InlineData(90, "90°")]
    [InlineData(359, "359°")]
    public void Direction_is_formatted_as_integer_degrees(double direction, string expected)
    {
        var data = new ObjectInfoPanelData("OBJ-1", null, SpeedKmS: 5, Direction: direction, RenderObjectType: null);
        var lines = ObjectInfoPanel.BuildLines(data);
        Assert.Equal(expected, Assert.Single(lines, l => l.Label == "Direction").Value);
    }

    [Fact]
    public void Name_falls_back_to_ObjectId_when_DisplayName_is_null()
    {
        var data = new ObjectInfoPanelData("OBJ-1", null, SpeedKmS: 5, Direction: 0, RenderObjectType: null);
        var lines = ObjectInfoPanel.BuildLines(data);
        Assert.Equal("OBJ-1", Assert.Single(lines, l => l.Label == "Name").Value);
    }

    [Fact]
    public void Name_uses_DisplayName_when_present()
    {
        var data = new ObjectInfoPanelData("OBJ-1", "Prospector", SpeedKmS: 5, Direction: 0, RenderObjectType: null);
        var lines = ObjectInfoPanel.BuildLines(data);
        Assert.Equal("Prospector", Assert.Single(lines, l => l.Label == "Name").Value);
    }

    private static StationMarketKnowledgeSnapshot Market(string id = "STATION-A", bool stale = false, bool available = true) =>
        new(id, "Mining", available, 731, 53, stale,
            [new("item.water", StationMarketStockState.Shortage), new("item.steel", StationMarketStockState.Normal),
             new("item.ice", StationMarketStockState.Surplus), new("item.iron-ore", StationMarketStockState.Surplus)]);

    private static ObjectInfoPanelData MarketData(StationMarketKnowledgeSnapshot? market) =>
        new("STATION-A", "Station A", 0, 90, SpaceObjectType.Station, MarketKnowledge: market);

    private static AuthoritativeSnapshot MarketSnapshot(ulong sequence = 1,
        ImmutableArray<StationMarketKnowledgeSnapshot> markets = default, string targetType = SpaceObjectType.Station) =>
        new(sequence, 999999, SimulationSpeed.Speed0,
            [new(PlayerShipId, 10000, 10000, 0, 0, ObjectType: SpaceObjectType.PlayerShip, RenderObjectType: SpaceObjectType.PlayerShip),
             new("STATION-A", 10000, 10060, 0, 90, ObjectType: targetType, RenderObjectType: targetType),
             new("STATION-B", 10060, 10000, 0, 0, ObjectType: SpaceObjectType.Station, RenderObjectType: SpaceObjectType.Station)],
            PlayerShipObjectId: PlayerShipId, StationMarketKnowledge: markets,
            DockedStationTrade: new("STATION-A", [new("item.ice", 88114455, 99112233, 77115566)]));

    [Theory]
    [InlineData(false, true, "Available / FRESH")]
    [InlineData(true, true, "Available / STALE")]
    [InlineData(false, false, "Unavailable / FRESH")]
    [InlineData(true, false, "Unavailable / STALE")]
    public void Market_lines_show_role_availability_observed_time_and_freshness(bool stale, bool available, string text)
    {
        var lines = ObjectInfoPanel.BuildLines(MarketData(Market(stale: stale, available: available)));
        Assert.Equal(new[] { "Name", "Speed", "Direction", "Role", "Market", "Observed", "Shortage", "Normal", "Surplus" }, lines.Select(l => l.Label));
        Assert.Equal(("Role", "Mining"), lines[3]);
        Assert.Equal(("Market", text), lines[4]);
        Assert.Equal(("Observed", "T+731 ms"), lines[5]);
    }

    [Fact]
    public void Market_lines_group_sorted_item_ids_by_authoritative_band()
    {
        var market = Market() with { StockBands = [new("z", StationMarketStockState.Normal), new("a", StationMarketStockState.Normal)] };
        var lines = ObjectInfoPanel.BuildLines(MarketData(market));
        Assert.Equal(("Shortage", "—"), lines[6]);
        Assert.Equal(("Normal", "a, z"), lines[7]);
        Assert.Equal(("Surplus", "—"), lines[8]);
        Assert.Equal("—", ObjectInfoPanel.BuildLines(MarketData(market with { StockBands = default }))[7].Value);
    }

    [Fact]
    public void Stale_market_is_explicit_text_not_inferred_client_side()
    {
        var (buffer, screen) = CreateScreen();
        buffer.Update(MarketSnapshot(markets: [Market()]));
        RenderScreen(screen);
        screen.OnMouseDown(640, 420);
        Assert.Equal("Available / FRESH", Assert.Single(ObjectInfoPanel.BuildLines(screen.SelectedOrActiveObjectInfo), l => l.Label == "Market").Value);
        buffer.Update(MarketSnapshot(2, [Market(stale: true)]));
        RenderScreen(screen);
        Assert.Equal("Available / STALE", Assert.Single(ObjectInfoPanel.BuildLines(screen.SelectedOrActiveObjectInfo), l => l.Label == "Market").Value);
        Assert.Equal("T+731 ms", Assert.Single(ObjectInfoPanel.BuildLines(screen.SelectedOrActiveObjectInfo), l => l.Label == "Observed").Value);
    }

    [Fact]
    public void Station_without_observation_has_no_market_lines()
    {
        Assert.Equal(3, ObjectInfoPanel.BuildLines(MarketData(null)).Count);
        var (buffer, screen) = CreateScreen();
        buffer.Update(MarketSnapshot());
        RenderScreen(screen);
        screen.OnMouseDown(640, 420);
        Assert.Null(screen.SelectedOrActiveObjectInfo!.Value.MarketKnowledge);
        Assert.DoesNotContain(ObjectInfoPanel.BuildLines(screen.SelectedOrActiveObjectInfo), l => l.Label == "Market");
    }

    [Theory]
    [InlineData(SpaceObjectType.PlayerShip)]
    [InlineData(SpaceObjectType.Asteroid)]
    [InlineData(SpaceObjectType.NpcShip)]
    [InlineData(SpaceObjectType.UnknownSpaceObject)]
    public void Non_station_never_receives_station_market_knowledge(string type)
    {
        var (buffer, screen) = CreateScreen();
        buffer.Update(MarketSnapshot(markets: [Market(), Market(PlayerShipId)], targetType: type));
        RenderScreen(screen);
        screen.OnMouseDown(640, 420);
        Assert.Null(screen.SelectedOrActiveObjectInfo!.Value.MarketKnowledge);
        Assert.Null(screen.PlayerShipInfo!.Value.MarketKnowledge);
        Assert.Equal(3, ObjectInfoPanel.BuildLines(MarketData(Market()) with { RenderObjectType = type }).Count);
    }

    [Fact]
    public void Hovered_station_knowledge_has_priority_over_selected_station()
    {
        var (buffer, screen) = CreateScreen();
        var a = Market();
        var b = Market("STATION-B", stale: true);
        buffer.Update(MarketSnapshot(markets: [a, b]));
        RenderScreen(screen);
        screen.OnMouseDown(640, 420);
        Assert.Same(a, screen.SelectedOrActiveObjectInfo!.Value.MarketKnowledge);
        screen.OnMouseMove(700, 360);
        RenderScreen(screen);
        Assert.Equal("STATION-A", screen.SelectedObjectId);
        Assert.Equal("STATION-B", screen.ActiveObjectId);
        Assert.Same(b, screen.SelectedOrActiveObjectInfo!.Value.MarketKnowledge);
        screen.OnMouseMove(-1, -1);
        RenderScreen(screen);
        Assert.Same(a, screen.SelectedOrActiveObjectInfo!.Value.MarketKnowledge);
    }

    [Fact]
    public void New_snapshot_replaces_market_knowledge_without_client_cache()
    {
        var (buffer, screen) = CreateScreen();
        buffer.Update(MarketSnapshot(markets: [Market()]));
        RenderScreen(screen);
        screen.OnMouseDown(640, 420);
        buffer.Update(MarketSnapshot(2, [Market() with { StationRole = "Industrial" }]));
        RenderScreen(screen);
        Assert.Equal("Industrial", screen.SelectedOrActiveObjectInfo!.Value.MarketKnowledge!.StationRole);
        buffer.Update(MarketSnapshot(3));
        RenderScreen(screen);
        Assert.Null(screen.SelectedOrActiveObjectInfo!.Value.MarketKnowledge);
        buffer.Update(MarketSnapshot(4, [Market("station-a")]));
        RenderScreen(screen);
        Assert.Null(screen.SelectedOrActiveObjectInfo!.Value.MarketKnowledge);
    }

    [Fact]
    public void Market_lines_never_show_price_quantity_budget_or_quote()
    {
        var (buffer, screen) = CreateScreen();
        buffer.Update(MarketSnapshot(markets: [Market()]));
        RenderScreen(screen);
        screen.OnMouseDown(640, 420);
        var lines = JsonSerializer.Serialize(ObjectInfoPanel.BuildLines(screen.SelectedOrActiveObjectInfo)
            .Select(l => new { l.Label, l.Value }));
        foreach (string forbidden in new[] { "88114455", "99112233", "77115566", "Price", "Quantity", "Budget", "Quote", "Credits" })
            Assert.DoesNotContain(forbidden, lines, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(3, ObjectInfoPanel.BuildLines(MarketData(Market("WRONG-ID"))).Count);
    }

    [Fact]
    public void Cluster_directions_scroll_within_small_viewport_and_reset_on_selection()
    {
        var panel = new ObjectInfoPanel();
        var data = MarketData(Market()) with
        {
            ClusterName = "District 5", ClusterProfile = "market.transit", StraightFlightDays = 12,
            EstimateMotionTimeMs = 123, ClusterDirections = string.Join("; ", Enumerable.Range(1, 60).Select(i => $"Station {i}: visit / return"))
        };
        var lines = panel.BuildRenderLines(data);
        Assert.True(lines.FindIndex(l => l.Label == "Straight flight estimate") < lines.FindIndex(l => l.Label == "Potential cargo"));
        Assert.Contains("Station 60", string.Join(" ", lines.Select(l => l.Value)));
        using var bitmap = new SKBitmap(1280, 480);
        using var canvas = new SKCanvas(bitmap);
        panel.Render(canvas, 1280, 8, null, data, 480);
        var body = panel.RowBodyRects[1];
        Assert.InRange(body.Bottom, 0, 472);
        Assert.True(panel.Scroll(body.MidX, body.MidY, -1));
        Assert.True(panel.ScrollOffset(1) > 0);
        Assert.False(panel.Scroll(0, 0, -1));
        Assert.False(panel.Scroll(body.MidX, body.MidY, float.NaN));
        for (int i = 0; i < 200; i++) panel.Scroll(body.MidX, body.MidY, -1);
        Assert.InRange(panel.ScrollOffset(1), 0, lines.Count * 16 + 12 - body.Height);
        panel.Render(canvas, 1280, 8, null, data with { ObjectId = "STATION-B" }, 480);
        Assert.Equal(0, panel.ScrollOffset(1));
    }

    [Fact]
    public void Market_values_wrap_inside_existing_panel_and_determine_body_height()
    {
        var panel = new ObjectInfoPanel();
        var data = MarketData(Market() with { StockBands = [new(new string('x', 120), StationMarketStockState.Normal)] });
        var wrapped = panel.BuildRenderLines(data);
        Assert.True(wrapped.Count > 9);
        Assert.Equal(new string('x', 120), string.Concat(wrapped.SkipWhile(l => l.Label != "Normal").TakeWhile(l => l.Label != "Surplus").Select(l => l.Value)));
        using var bitmap = new SKBitmap(1280, 800);
        using var canvas = new SKCanvas(bitmap);
        panel.Render(canvas, 1280, 0, null, data);
        Assert.True(panel.RowBodyRects[1].Height >= wrapped.Count * 16 + 12);
        var directory = Environment.GetEnvironmentVariable("DSS_TRADE_RENDER_DIR");
        if (directory is not null)
        {
            Directory.CreateDirectory(directory);
            canvas.Clear(new SKColor(2, 16, 24));
            panel.Render(canvas, 1280, 0, null, MarketData(Market() with
            {
                StockBands =
                [new("item.food-rations", StationMarketStockState.Shortage), new("item.energy-cells", StationMarketStockState.Shortage),
                 new("item.electronics", StationMarketStockState.Normal), new("item.iron-ore", StationMarketStockState.Normal),
                 new("item.steel", StationMarketStockState.Normal), new("item.water", StationMarketStockState.Normal),
                 new("item.ice", StationMarketStockState.Surplus)]
            }));
            using var image = SKImage.FromBitmap(bitmap);
            using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
            File.WriteAllBytes(Path.Combine(directory, "station-market-knowledge.png"), encoded.ToArray());
        }
    }

    // ── Resource survey presentation and real-content flow ────────────

    [Fact]
    public void Unknown_resource_field_shows_id_speed_mass_and_unknown_composition()
    {
        var data = FieldData(new(long.MaxValue, false, true));
        Assert.Equal(new[]
        {
            ("Name", "FIELD"), ("Speed", "0 km/s"),
            ("Mass", "9223372036854775807 kg"), ("Composition", "Unknown")
        }, ObjectInfoPanel.BuildLines(data));

        var (buffer, screen) = CreateScreen();
        var ship = new ObjectMotionSnapshot(PlayerShipId, 10000, 10000, 0, 0);
        var target = new ObjectMotionSnapshot("FIELD", 10000, 10060, 0, 0,
            DisplayName: "Hidden field kind", Image: "neutral.png", Survey: data.Survey);
        buffer.Update(new(1, 0, SimulationSpeed.Speed0, [ship, target], PlayerShipObjectId: PlayerShipId));
        RenderScreen(screen);
        screen.OnMouseDown(640, 420);
        var projected = screen.SelectedOrActiveObjectInfo!.Value;
        Assert.Same(data.Survey, projected.Survey);
        Assert.Equal("FIELD", projected.DisplayName);
        Assert.Equal("neutral.png", projected.Image);
        Assert.Equal(ObjectInfoPanel.BuildLines(data with { DistanceKm = 6 }), ObjectInfoPanel.BuildLines(projected));
    }

    [Fact]
    public void Unknown_flag_hides_inconsistent_resource_payload()
    {
        var data = FieldData(new(1000000, false, true, "Ice", [new("item.ice", 1000)]));
        var lines = ObjectInfoPanel.BuildLines(data);
        Assert.Equal(4, lines.Count);
        Assert.Equal(("Composition", "Unknown"), lines[3]);
        Assert.DoesNotContain(lines, l => l.Value.Contains('%') || l.Value == "Ice");
    }

    [Fact]
    public void Known_resource_field_shows_authoritative_fractions_in_stable_order()
    {
        var data = FieldData(new(1234567, true, false, "Silicate",
            [new("item.silicon", 99), new("item.iron-ore", 800), new("item.future", 101)]));
        var lines = ObjectInfoPanel.BuildLines(data);
        Assert.Equal(("Composition", "Silicate"), lines[3]);
        Assert.Equal(new[]
        {
            ("item.future", $"{10.1m:0.#}%"),
            (TradeItemPresentation.ItemDisplayName("item.iron-ore"), "80%"),
            (TradeItemPresentation.ItemDisplayName("item.silicon"), $"{9.9m:0.#}%")
        }, lines.Skip(4));
        Assert.Equal(100m, lines.Skip(4).Sum(l => decimal.Parse(l.Value.TrimEnd('%'), CultureInfo.CurrentCulture)));
        Assert.DoesNotContain(lines, l => l.Label == "Direction");
        Assert.Equal(4, ObjectInfoPanel.BuildLines(FieldData(new(1, true, false, "Iron"))).Count);
    }

    [Fact]
    public void Legacy_and_empty_panel_data_keep_existing_lines()
    {
        Assert.Equal(new[] { ("Name", "—"), ("Speed", "—"), ("Direction", "—") }, ObjectInfoPanel.BuildLines(null));
        Assert.Equal(new[] { ("Name", "Legacy"), ("Speed", "5 km/s"), ("Direction", "90°") },
            ObjectInfoPanel.BuildLines(new("OLD", "Legacy", 5, 90, "Asteroid")));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Resource_rows_fit_expanded_body_and_preserve_hit_regions(bool extended)
    {
        var config = JsonSerializer.Deserialize<StationResourceFieldConfig>(
            File.ReadAllText(Path.Combine(ClientRoot, "Data", "World", "station-resource-fields.json")))!;
        var variant = config.FieldKinds.SelectMany(k => k.Variants).First(v => v.Resources.Count == 4);
        var resources = variant.Resources.Select(r => new ResourceFractionSnapshot(r.ItemTypeId, r.Permille)).ToImmutableArray();
        if (extended) resources = resources.AddRange(Enumerable.Range(0, 12).Select(i => new ResourceFractionSnapshot($"item.{i:D2}", 1)));
        var data = FieldData(new(1000000000, true, false, variant.CompositionType, resources));
        var panel = new ObjectInfoPanel();
        using var bitmap = new SKBitmap(ScreenWidth, 1200);
        using var canvas = new SKCanvas(bitmap);
        panel.Render(canvas, ScreenWidth, 8, null, data);
        int lineCount = ObjectInfoPanel.BuildLines(data).Count;
        Assert.Equal(extended ? 20 : 8, lineCount);
        Assert.Equal(Math.Max(162f, 12f + lineCount * 16f), panel.RowBodyRects[1].Height);
        Assert.Equal(162f, panel.RowBodyRects[0].Height);
        Assert.Equal(panel.RowBodyRects[0].Bottom, panel.RowCaptionRects[1].Top);
        Assert.Equal(panel.RowBodyRects[1].Bottom, panel.BodyRect.Bottom);
        var body = panel.RowBodyRects[1];
        Assert.True(panel.OnMouseDown(body.MidX, body.Bottom - 1));
        var caption = panel.RowCaptionRects[1];
        Assert.True(panel.OnMouseDown(caption.MidX, caption.MidY));
        panel.Render(canvas, ScreenWidth, 8, null, data);
        Assert.Equal(SKRect.Empty, panel.RowBodyRects[1]);
        Assert.True(panel.IsRowOpen(0));
        panel.OnMouseDown(caption.MidX, caption.MidY);
        panel.Render(canvas, ScreenWidth, 8, null, data);
        Assert.Equal(body, panel.RowBodyRects[1]);
        var toggle = panel.HideShowButtonRect;
        panel.OnMouseDown(toggle.MidX, toggle.MidY);
        panel.Render(canvas, ScreenWidth, 8, null, data);
        Assert.All(panel.RowBodyRects, r => Assert.Equal(SKRect.Empty, r));
        panel.OnMouseDown(toggle.MidX, toggle.MidY);
        panel.Render(canvas, ScreenWidth, 8, null, data);
        Assert.Equal(body, panel.RowBodyRects[1]);
    }

    [Fact]
    public async Task Real_content_scan_and_reload_update_existing_info_panel()
    {
        string scenarioPath = Path.GetTempFileName();
        string savePath = Path.GetTempFileName();
        try
        {
            var source = ScenarioLoader.LoadFromFile(Path.Combine(ClientRoot, "Scenarios", "Undocked", "scenario.json"));
            // Seed 2: the production named survey stream's first draw is 0.8452141274908623 (< 85%).
            File.WriteAllText(scenarioPath, JsonSerializer.Serialize(source with { GameState = source.GameState with { MasterSeed = 2 } }));
            using var engine = EngineContentLoader.CreateEngineFromScenarioFile(SettingsPath, scenarioPath);
            var initial = engine.CaptureSnapshotForTests();
            var target = initial.Objects.First(o => o.Survey is { CanStructuralScan: true });
            // Place the stationary ship 6 km from this generated target for a deterministic screen click.
            var save = engine.CaptureSaveStateForTests(0, SimulationSpeed.Speed0);
            engine.LoadScenario(save with
            {
                GameState = save.GameState with
                {
                    SpaceObjects = save.GameState.SpaceObjects.Select(o => o.ObjectId == PlayerShipId
                        ? o with { PositionX = target.X, PositionY = target.Y - 60 } : o).ToArray()
                }
            }, isSave: true);
            await using var handle = new GameSessionHandle(new PanelEngineConnection(engine));
            var screen = new GameSessionScreen(handle.Buffer, new LinearMotionPredictor(), handle);
            handle.Buffer.Update(engine.CaptureSnapshotForTests());
            RenderScreen(screen);
            screen.OnMouseDown(640, 420);
            Assert.Equal(target.ObjectId, screen.SelectedObjectId);
            var unknown = ObjectInfoPanel.BuildLines(screen.SelectedOrActiveObjectInfo);
            Assert.Equal(("Composition", "Unknown"), unknown[3]);
            Assert.Equal(("Mass", $"{target.Survey!.MassKg} kg"), unknown[2]);
            Assert.Equal(5, unknown.Count);
            RenderScreen(screen);
            var scannerRow = screen.CommandsPanel.CommandPanelRows.Single(r => r.Name == "Space Control");
            if (!scannerRow.Opened)
            {
                screen.OnMouseDown(scannerRow.CaptionRect.MidX, scannerRow.CaptionRect.MidY);
                RenderScreen(screen);
            }
            var button = Assert.Single(screen.CommandsPanel.AllCommandButtons, b => b.CommandTypeId == ScannerCommandTypes.StructuralScan);
            Assert.True(button.Enabled);
            screen.OnMouseDown(button.Rect.MidX, button.Rect.MidY);
            screen.OnMouseDown(button.Rect.MidX, button.Rect.MidY);
            var busy = engine.CaptureSnapshotForTests();
            Assert.Equal(CommandReasonCodes.Busy, Assert.Single(busy.CommandResults).ReasonCode);
            handle.Buffer.Update(busy);
            RenderScreen(screen);
            Assert.False(Assert.Single(screen.CommandsPanel.AllCommandButtons, b => b.CommandTypeId == ScannerCommandTypes.StructuralScan).Enabled);
            Assert.Equal(unknown, ObjectInfoPanel.BuildLines(screen.SelectedOrActiveObjectInfo));
            // Repeated paused snapshots and a frame before due cannot reveal composition.
            Assert.False(engine.CaptureSnapshotForTests(0).Objects.Single(o => o.ObjectId == target.ObjectId).Survey!.CompositionKnown);
            Assert.False(engine.CaptureSnapshotForTests(59999).Objects.Single(o => o.ObjectId == target.ObjectId).Survey!.CompositionKnown);
            var completed = engine.CaptureSnapshotForTests(60000);
            Assert.Equal(CommandResultStatus.Executed, Assert.Single(completed.CommandResults).Status);
            handle.Buffer.Update(completed);
            RenderScreen(screen);
            var revealed = completed.Objects.Single(o => o.ObjectId == target.ObjectId);
            Assert.True(revealed.Survey!.CompositionKnown);
            Assert.Same(revealed.Survey, screen.SelectedOrActiveObjectInfo!.Value.Survey);
            Assert.Equal(revealed.Image, screen.SelectedOrActiveObjectInfo!.Value.Image);
            var known = ObjectInfoPanel.BuildLines(screen.SelectedOrActiveObjectInfo);
            Assert.Equal(("Composition", revealed.Survey.CompositionType), known[3]);
            Assert.Equal(revealed.Survey.Resources.OrderBy(r => r.ItemTypeId, StringComparer.Ordinal)
                .Select(r => (TradeItemPresentation.ItemDisplayName(r.ItemTypeId), $"{r.Permille / 10m:0.#}%")), known.Skip(4).Where(line => line.Label != "Distance"));
            Assert.False(Assert.Single(screen.CommandsPanel.AllCommandButtons, b => b.CommandTypeId == ScannerCommandTypes.StructuralScan).Enabled);
            File.WriteAllText(savePath, JsonSerializer.Serialize(engine.CaptureSaveStateForTests(60000, SimulationSpeed.Speed0)));
            using var restored = EngineContentLoader.CreateEngineFromSaveFile(SettingsPath, savePath);
            var reloadBuffer = new SnapshotBuffer();
            reloadBuffer.Update(restored.CaptureSnapshotForTests(60000));
            var reloadScreen = new GameSessionScreen(reloadBuffer, new LinearMotionPredictor());
            RenderScreen(reloadScreen);
            reloadScreen.OnMouseDown(640, 420);
            Assert.Equal(target.ObjectId, reloadScreen.SelectedObjectId);
            Assert.Equal(known, ObjectInfoPanel.BuildLines(reloadScreen.SelectedOrActiveObjectInfo));
        }
        finally
        {
            File.Delete(scenarioPath);
            File.Delete(savePath);
        }
    }

    private static readonly string ClientRoot = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client"));
    private static string SettingsPath => Path.Combine(ClientRoot, "Settings.json");
    private static ObjectInfoPanelData FieldData(AsteroidSurveySnapshot survey) =>
        new("FIELD", "Secret field kind", 0, 90, "Asteroid", Survey: survey);

    private sealed class PanelEngineConnection(SimulationEngine engine) : IGameSessionConnection
    {
        public ValueTask SendCommandAsync(PlayerCommand command, CancellationToken cancellationToken = default)
        {
            engine.ReceiveCommand(command);
            return ValueTask.CompletedTask;
        }

        public ValueTask SendDialogueCommandAsync(DialogueCommand command, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SetSimulationSpeedAsync(SimulationSpeed speed, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SetObjectInteractionStateAsync(string? activeObjectId, string? selectedObjectId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SaveAsync(string slotId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public async IAsyncEnumerable<AuthoritativeSnapshot> ReadSnapshotsAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }
    }

    [Fact]
    public void Panel_rect_is_populated_after_render()
    {
        var (buffer, screen) = CreateScreen();
        UpdateBufferWithShip(buffer, PlayerShipId);
        RenderScreen(screen);

        var panel = screen.ObjectInfoPanel;
        Assert.True(panel.CaptionRect.Width > 0);
        Assert.True(panel.CaptionRect.Height > 0);
        Assert.Equal(2, panel.RowCaptionRects.Count);
        Assert.Equal(2, panel.RowBodyRects.Count);
        Assert.All(panel.RowBodyRects, r => Assert.True(r.Width > 0 && r.Height > 0));
    }

    [Fact]
    public void Row_body_is_tall_enough_for_a_200x150_image()
    {
        var (buffer, screen) = CreateScreen();
        UpdateBufferWithShip(buffer, PlayerShipId);
        RenderScreen(screen);

        // 150px image + 6px padding above and below.
        Assert.Equal(162f, ObjectInfoPanel.RowBodyHeight);
        Assert.All(screen.ObjectInfoPanel.RowBodyRects, r => Assert.Equal(162f, r.Height));
    }

    [Fact]
    public void Panel_is_positioned_at_top_right()
    {
        var (buffer, screen) = CreateScreen();
        UpdateBufferWithShip(buffer, PlayerShipId);
        RenderScreen(screen);

        var caption = screen.ObjectInfoPanel.CaptionRect;
        Assert.True(caption.Right <= ScreenWidth);
        Assert.True(caption.Left > ScreenWidth / 2f,
            $"Panel left edge ({caption.Left}) should be in the right half of the screen");
        Assert.True(caption.Top < ScreenHeight / 4f,
            $"Panel top edge ({caption.Top}) should be near the top of the screen");
    }

    [Fact]
    public void Hide_show_toggle_collapses_and_restores_the_rows()
    {
        var (buffer, screen) = CreateScreen();
        UpdateBufferWithShip(buffer, PlayerShipId);
        RenderScreen(screen);

        var panel = screen.ObjectInfoPanel;
        var toggle = panel.HideShowButtonRect;

        screen.OnMouseDown(toggle.MidX, toggle.MidY);
        RenderScreen(screen);
        Assert.Equal(ObjectInfoPanelState.Closed, panel.State);
        Assert.All(panel.RowBodyRects, r => Assert.Equal(0f, r.Width));

        screen.OnMouseDown(toggle.MidX, toggle.MidY);
        RenderScreen(screen);
        Assert.Equal(ObjectInfoPanelState.Open, panel.State);
        Assert.All(panel.RowBodyRects, r => Assert.True(r.Width > 0));
    }

    [Fact]
    public void Row_caption_click_collapses_and_restores_just_that_row()
    {
        var (buffer, screen) = CreateScreen();
        UpdateBufferWithShip(buffer, PlayerShipId);
        RenderScreen(screen);

        var panel = screen.ObjectInfoPanel;
        var playerRowCaption = panel.RowCaptionRects[0];

        screen.OnMouseDown(playerRowCaption.MidX, playerRowCaption.MidY);
        RenderScreen(screen);

        Assert.False(panel.IsRowOpen(0));
        Assert.True(panel.IsRowOpen(1));
        Assert.Equal(0f, panel.RowBodyRects[0].Width);
        Assert.True(panel.RowBodyRects[1].Width > 0);

        screen.OnMouseDown(playerRowCaption.MidX, playerRowCaption.MidY);
        RenderScreen(screen);

        Assert.True(panel.IsRowOpen(0));
        Assert.True(panel.RowBodyRects[0].Width > 0);
    }

    [Fact]
    public void Click_inside_panel_does_not_pan_camera()
    {
        var (buffer, screen) = CreateScreen();
        UpdateBufferWithShip(buffer, PlayerShipId);
        RenderScreen(screen);

        double focusXBefore = screen.CameraFocusX;
        double focusYBefore = screen.CameraFocusY;

        var body = screen.ObjectInfoPanel.RowBodyRects[0];
        var result = screen.OnMouseDown(body.MidX, body.MidY);

        Assert.Equal(ScreenEvent.None, result);
        Assert.Equal(focusXBefore, screen.CameraFocusX);
        Assert.Equal(focusYBefore, screen.CameraFocusY);
    }

    // ── Wiring: Player Ship row content ───────────────────────────────

    [Fact]
    public void PlayerShipInfo_is_null_without_a_snapshot()
    {
        var (_, screen) = CreateScreen();
        Assert.Null(screen.PlayerShipInfo);
    }

    [Fact]
    public void PlayerShipInfo_reflects_the_current_player_ship_state()
    {
        var (buffer, screen) = CreateScreen();
        UpdateBufferWithShip(buffer, PlayerShipId, speedKmS: 15.5, direction: 45, displayName: "My Ship");
        RenderScreen(screen);

        var info = screen.PlayerShipInfo;
        Assert.NotNull(info);
        Assert.Equal(PlayerShipId, info!.Value.ObjectId);
        Assert.Equal("My Ship", info.Value.DisplayName);
        Assert.Equal(15.5, info.Value.SpeedKmS);
        Assert.Equal(45, info.Value.Direction);
    }

    [Fact]
    public void PlayerShipInfo_carries_the_resolved_image_from_the_snapshot()
    {
        var (buffer, screen) = CreateScreen();
        UpdateBufferWithShip(buffer, PlayerShipId, image: "Images/CelestialObjects/Spacecraft/ship-tetrarch-class.png");
        RenderScreen(screen);

        Assert.Equal("Images/CelestialObjects/Spacecraft/ship-tetrarch-class.png", screen.PlayerShipInfo!.Value.Image);
    }
}
