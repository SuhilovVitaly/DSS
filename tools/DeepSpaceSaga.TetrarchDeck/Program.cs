namespace DeepSpaceSaga.TetrarchDeck;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        bool verify = args.Contains("--verify", StringComparer.Ordinal);
        try
        {
            string root = AppContext.BaseDirectory;
            string? output = null;
            string? frameDirectory = null;
            int? monitor = null;
            for (int i = 0; i < args.Length; i += 2)
            {
                if (i + 1 >= args.Length) throw new ArgumentException("Пропущено значение " + args[i]);
                switch (args[i])
                {
                    case "--content": root = Path.GetFullPath(args[i + 1]); break;
                    case "--verify": output = Path.GetFullPath(args[i + 1]); break;
                    case "--frames" when verify: frameDirectory = Path.GetFullPath(args[i + 1]); break;
                    case "--monitor" when int.TryParse(args[i + 1], out int index): monitor = index; break;
                    default: throw new ArgumentException("Неизвестный параметр " + args[i]);
                }
            }
            var scenarios = Content.Scenarios(root);
            var types = Content.ModuleTypes(root);
            if (scenarios.Length == 0) throw new InvalidDataException("В сценариях не найден корабль класса «Тетрарх».");
            if (verify)
            {
                DeckChecks.Run(root, scenarios, types, output!, frameDirectory);
                return 0;
            }

            var screens = Screen.AllScreens;
            long Area(Screen screen) => (long)screen.Bounds.Width * screen.Bounds.Height;
            if (monitor is < 0 || monitor >= screens.Length) throw new ArgumentOutOfRangeException("--monitor");
            var target = monitor.HasValue ? screens[monitor.Value] : screens.MinBy(Area)!;
            if (Environment.GetEnvironmentVariable("DSS_VALIDATION_SMALL_MONITOR") == "1"
                && (!monitor.HasValue || Area(target) >= screens.Max(Area)))
                throw new InvalidOperationException("Для оконной проверки нужен явно выбранный меньший дисплей.");
            using var window = new Form
            {
                Text = "Тетрарх · Палуба | Deep Space Saga",
                MinimumSize = new Size(800, 580),
                StartPosition = FormStartPosition.Manual,
                BackColor = Ink.Space
            };
            var area = target.WorkingArea;
            int width = Math.Min(1480, area.Width), height = Math.Min(910, area.Height);
            window.Bounds = new(area.X + (area.Width - width) / 2, area.Y + (area.Height - height) / 2, width, height);
            var view = new DeckView(root, scenarios, types, Random.Shared) { Dock = DockStyle.Fill };
            window.Controls.Add(view);
            window.Shown += (_, _) => view.Start();
            Application.Run(window);
            return 0;
        }
        catch (Exception ex)
        {
            if (verify || Environment.GetEnvironmentVariable("DSS_VALIDATION_SMALL_MONITOR") == "1") Console.Error.WriteLine(ex);
            else MessageBox.Show(ex.Message, "Тетрарх", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }
    }
}
