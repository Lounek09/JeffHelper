using JeffHelper.Utils;

using Serilog;

using System.Drawing;
using System.Text.RegularExpressions;

namespace JeffHelper;

public static partial class Program
{
    private const string INPUT_DIR = "input";
    private const string OUTPUT_DIR = "output";
    private const string JEFF_DIR = "Jeff";
    private const string JEFF_REPO = "git@github.com:H3r3zy/Jeff.git";

    private const string _defaultScope = "main";
    private const int _defaultExportSize = 2048;
    private const bool _defaultTrim = true;
    private static readonly int[] _defaultSizes = [32, 64, 128, 256, 512];

    private static string _scope = _defaultScope;
    private static int _exportSize = _defaultExportSize;
    private static bool _trim = _defaultTrim;
    private static int[] _sizes = _defaultSizes;

    [GeneratedRegex(INPUT_DIR)]
    private static partial Regex PathRegex();

    public static void Main()
    {
        Initialize();

        while (true)
        {
            Option();

            Log.Information("Generating images...");
            Generate(INPUT_DIR);

            if (_trim || _sizes.Length > 0)
            {
                Log.Information("Resizing images...");
                Resize(OUTPUT_DIR);
            }

            Log.Information("Done, press {Q} to quit or any other key to continue", 'q');
            var key = Console.ReadKey();
            if (key.Key is ConsoleKey.Q)
            {
                break;
            }
        }
    }

    private static void Initialize()
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();

        if (!Directory.Exists(INPUT_DIR))
        {
            Directory.CreateDirectory(INPUT_DIR);
        }

        if (!Directory.Exists(OUTPUT_DIR))
        {
            Directory.CreateDirectory(OUTPUT_DIR);
        }
    }

    private static void Option()
    {
        _scope = Ask("Enter scope, either {Classes} or {Main} (default [{Scope}]):",
            ["classes", "main", _defaultScope],
            input =>
            {
                if (string.IsNullOrEmpty(input))
                {
                    return (true, _defaultScope);
                }

                return (true, input);
            }
        );

        _exportSize = Ask("Enter export size (default [{ExportSize}]):",
            [_defaultExportSize],
            input =>
            {
                if (string.IsNullOrEmpty(input))
                {
                    return (true, _defaultExportSize);
                }

                if (int.TryParse(input, out var size))
                {
                    return (true, size);
                }

                return (false, 0);
            }
        );

        _trim = Ask("Trim images? (default [{Trim}]):",
            [_defaultTrim],
            input =>
            {
                if (string.IsNullOrEmpty(input))
                {
                    return (true, _defaultTrim);
                }

                var trim = input.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                    input.Equals("y", StringComparison.OrdinalIgnoreCase) ||
                    input.Equals("yes", StringComparison.OrdinalIgnoreCase);

                return (true, trim);
            }
        );

        _sizes = Ask("Enter sizes as a comma-separated list (default [{Sizes}] or enter {0} for none):",
            [string.Join(',', _defaultSizes), '0'],
            input =>
            {
                if (string.IsNullOrEmpty(input))
                {
                    return (true, _defaultSizes);
                }

                if (input == "0")
                {
                    return (true, []);
                }

                try
                {
                    return (true, input.Split(',').Select(int.Parse).ToArray());
                }
                catch
                {
                    return (false, []); 
                }
            }
        );
    }

    private static T Ask<T>(string messageTemplate, object[] templateParameters, Func<string?, (bool, T)> validator)
    {
        Log.Information(messageTemplate, templateParameters);
        var input = Console.ReadLine();

        var (success, value) = validator(input);
        if (success)
        {
            return value;
        }

        Log.Warning("Invalid input");
        return Ask(messageTemplate, templateParameters, validator);
    }

    private static void Generate(string path)
    {
        foreach (var directory in Directory.GetDirectories(path))
        {
            Generate(directory);
        }

        if (Directory.GetFiles(path, "*.swf").Length == 0)
        {
            return;
        }

        var outputPath = PathRegex().Replace(path, OUTPUT_DIR, 1);

        ExecuteCmd.ExecuteCommand("jeff",
            $"-i {path} -o {outputPath} -S {_scope} -R true -d true -f \"[1]\" -w {_exportSize}",
            string.Empty);

        Log.Information("Directory {Path} done", path);
    }

    private static void Resize(string path)
    {
        foreach (var directory in Directory.GetDirectories(path))
        {
            Resize(directory);
        }

        var files = Directory.GetFiles(path);
        if (files.Length == 0)
        {
            return;
        }

        foreach (var size in _sizes)
        {
            var sizePath = Path.Join(path, size.ToString());
            if (!Directory.Exists(sizePath))
            {
                Directory.CreateDirectory(sizePath);
            }
        }

        foreach (var file in files)
        {
            FileStream stream = new(file, FileMode.Open);
            var image = Image.FromStream(stream);

            if (_trim)
            {
                image = image.Trim();
            }

            foreach (var size in _sizes)
            {
                using var secondarySizeImage = image.Resize(size, size);

                var filePath = Path.Join(Path.GetDirectoryName(file), size.ToString(), Path.GetFileName(file));
                secondarySizeImage.Save(filePath);
            }

            stream.Dispose();
            image.Dispose();

            if (_sizes.Length > 0)
            {
                File.Delete(file);
            }

            Log.Information("Image {Path} done", file);
        }
    }
}
