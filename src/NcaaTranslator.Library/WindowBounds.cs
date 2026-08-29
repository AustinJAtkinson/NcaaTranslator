using System.Text.Json;

namespace NcaaTranslator.Library
{
    public readonly record struct DisplayRect(int X, int Y, int Width, int Height)
    {
        public bool Contains(int x, int y) =>
            x >= X && y >= Y && x < X + Width && y < Y + Height;
    }

    public class WindowBounds
    {
        public const int DefaultWidth = 1440;
        public const int DefaultHeight = 900;
        public const int MinWidth = 1100;
        public const int MinHeight = 700;

        internal const string FileName = "Window.json";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        public static string BaseDirectory { get; set; } = AppContext.BaseDirectory;

        public int Width { get; set; } = DefaultWidth;
        public int Height { get; set; } = DefaultHeight;
        public int? Left { get; set; }
        public int? Top { get; set; }
        public bool Maximized { get; set; }

        public static string ResolvePath()
        {
            return Path.GetFullPath(Path.Combine(BaseDirectory, FileName));
        }

        public static WindowBounds Load(IEnumerable<DisplayRect>? displays = null)
        {
            var path = ResolvePath();
            WindowBounds bounds;
            if (!File.Exists(path))
            {
                bounds = CreateDefault();
            }
            else
            {
                try
                {
                    var json = File.ReadAllText(path);
                    bounds = JsonSerializer.Deserialize<WindowBounds>(json, JsonOptions) ?? CreateDefault();
                }
                catch (JsonException)
                {
                    bounds = CreateDefault();
                }
                catch (IOException)
                {
                    bounds = CreateDefault();
                }
            }

            bounds.Normalize(displays);
            return bounds;
        }

        public void Save()
        {
            var path = ResolvePath();
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(path, JsonSerializer.Serialize(this, JsonOptions));
        }

        // Maximized pixel size must not replace the restore Width/Height/Left/Top.
        public void ApplySizeChange(int width, int height, int left, int top, bool maximized)
        {
            if (maximized)
            {
                Maximized = true;
                return;
            }

            ApplyRestored(width, height, left, top);
        }

        public void ApplyMaximized()
        {
            Maximized = true;
        }

        public void ApplyRestored(int width, int height, int left, int top)
        {
            Maximized = false;
            Width = ClampWidth(width);
            Height = ClampHeight(height);
            Left = left;
            Top = top;
        }

        // Minimized is never written to Window.json.
        public void ApplyMinimized()
        {
        }

        public static WindowBounds CreateDefault() => new()
        {
            Width = DefaultWidth,
            Height = DefaultHeight,
            Left = null,
            Top = null,
            Maximized = false,
        };

        public void Normalize(IEnumerable<DisplayRect>? displays)
        {
            Width = Width <= 0 ? DefaultWidth : ClampWidth(Width);
            Height = Height <= 0 ? DefaultHeight : ClampHeight(Height);

            var hasOrigin = Left.HasValue && Top.HasValue;
            if (!hasOrigin)
            {
                Left = null;
                Top = null;
                return;
            }

            if (displays == null)
                return;

            var displayList = displays as IList<DisplayRect> ?? displays.ToList();
            if (displayList.Count == 0)
                return;

            var originOnDisplay = displayList.Any(d => d.Contains(Left!.Value, Top!.Value));
            if (originOnDisplay)
                return;

            Left = null;
            Top = null;
            if (!Maximized)
            {
                Width = DefaultWidth;
                Height = DefaultHeight;
            }
        }

        private static int ClampWidth(int width) => Math.Max(width, MinWidth);
        private static int ClampHeight(int height) => Math.Max(height, MinHeight);
    }
}
