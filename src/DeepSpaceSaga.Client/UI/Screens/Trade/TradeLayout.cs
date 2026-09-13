using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.Trade;

/// <summary>Local geometry below the unchanged 1600×60 station toolbar.</summary>
public sealed class TradeLayout
{
    public const float PanelWidth = 1600, PanelHeight = 800;
    public const float BodyStartY = 80, BodyLineHeight = 28;
    public const int VisibleRows = 8;
    public const float RowHeight = 50;
    public static float PanelLeft(int width) => (width - PanelWidth) / 2f;
    public static float PanelTop(int height) => (height - PanelHeight) / 2f;
    public static bool IsInsidePanel(float x, float y, int width, int height) =>
        SKRect.Create(PanelLeft(width), PanelTop(height), PanelWidth, PanelHeight).Contains(x, y);

    internal static readonly SKRect MarketTab = new(20, 80, 185, 126);
    internal static readonly SKRect FuelTab = new(197, 80, 380, 126);
    internal static readonly SKRect Catalog = new(20, 142, 1000, 744);
    internal static readonly SKRect Detail = new(1020, 80, 1580, 744);
    internal static readonly SKRect Search = new(36, 158, 478, 202);
    internal static readonly SKRect SearchClear = new(438, 158, 478, 202);
    internal static readonly SKRect All = new(490, 158, 620, 202);
    internal static readonly SKRect Resources = new(630, 158, 802, 202);
    internal static readonly SKRect Goods = new(812, 158, 984, 202);
    internal static readonly SKRect CargoOnly = new(36, 211, 360, 244);
    internal static readonly SKRect Rows = new(36, 286, 970, 686);
    internal static readonly SKRect Scroll = new(978, 286, 988, 686);
    internal static readonly SKRect[] Headers = [new(36, 250, 478, 282), new(478, 250, 635, 282), new(635, 250, 802, 282), new(802, 250, 970, 282)];
    internal static readonly SKRect Buy = new(1040, 190, 1300, 228);
    internal static readonly SKRect Sell = new(1300, 190, 1560, 228);
    internal static readonly SKRect Module = new(1040, 262, 1560, 304);
    internal static readonly SKRect Minus = new(1040, 336, 1090, 378);
    internal static readonly SKRect Quantity = new(1100, 336, 1440, 378);
    internal static readonly SKRect Plus = new(1450, 336, 1500, 378);
    internal static readonly SKRect Max = new(1510, 336, 1560, 378);
    internal static readonly SKRect[] Presets = [new(1040, 390, 1205, 426), new(1217, 390, 1382, 426), new(1394, 390, 1560, 426)];
    internal static readonly SKRect Slider = new(1040, 439, 1560, 463);
    internal static readonly SKRect Confirm = new(1040, 665, 1560, 711);
    internal static readonly SKRect History = new(20, 757, 285, 788);
    internal static SKRect Row(int index) => new(Rows.Left, Rows.Top + index * RowHeight, Rows.Right, Rows.Top + (index + 1) * RowHeight);
    internal static SKRect ModuleOption(int index) => new(1040, 306 + index * 40, 1560, 346 + index * 40);
}
