using JeffHelper.Utils;

using Serilog;

using System.Drawing;
using System.Text.RegularExpressions;

namespace JeffHelper;

public static partial class Program
{
    private const string c_input = "input";
    private const string c_output = "output";

    private const string c_defaultScope = "main";
    private const int c_defaultExportSize = 2048;
    private const bool c_defaultTrim = true;
    private static readonly int[] c_defaultSizes = [32, 64, 128, 256, 512];

    private static string s_scope = c_defaultScope;
    private static int s_exportSize = c_defaultExportSize;
    private static bool s_trim = c_defaultTrim;
    private static int[] s_sizes = c_defaultSizes;

    [GeneratedRegex(c_input)]
    private static partial Regex PathRegex();

    public static void Main()
    {
        Initialize();

        while (true)
        {
            Option();

            Log.Information("Generating images...");
            Generate(c_input);

            if (s_trim || s_sizes.Length > 0)
            {
                Log.Information("Resizing images...");
                Resize(c_output);
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

        if (!Directory.Exists(c_input))
        {
            Directory.CreateDirectory(c_input);
        }

        if (!Directory.Exists(c_output))
        {
            Directory.CreateDirectory(c_output);
        }
    }

    private static void Option()
    {
        s_scope = Ask("Enter scope, either {Classes} or {Main} (default [{Scope}]):",
            ["classes", "main", c_defaultScope],
            input =>
            {
                if (string.IsNullOrEmpty(input))
                {
                    return (true, c_defaultScope);
                }

                return (true, input);
            }
        );

        s_exportSize = Ask("Enter export size (default [{ExportSize}]):",
            [c_defaultExportSize],
            input =>
            {
                if (string.IsNullOrEmpty(input))
                {
                    return (true, c_defaultExportSize);
                }

                if (int.TryParse(input, out var size))
                {
                    return (true, size);
                }

                return (false, 0);
            }
        );

        s_trim = Ask("Trim images? (default [{Trim}]):",
            [c_defaultTrim],
            input =>
            {
                if (string.IsNullOrEmpty(input))
                {
                    return (true, c_defaultTrim);
                }

                var trim = input.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                    input.Equals("y", StringComparison.OrdinalIgnoreCase) ||
                    input.Equals("yes", StringComparison.OrdinalIgnoreCase);

                return (true, trim);
            }
        );

        s_sizes = Ask("Enter sizes as a comma-separated list (default [{Sizes}] or enter {0} for none):",
            [string.Join(',', c_defaultSizes), '0'],
            input =>
            {
                if (string.IsNullOrEmpty(input))
                {
                    return (true, c_defaultSizes);
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

        var outputPath = PathRegex().Replace(path, c_output, 1);

        ExecuteCmd.ExecuteCommand("jeff",
            $"-i {path} -o {outputPath} -S {s_scope} -R true -d true -f \"[1]\" -w {s_exportSize}",
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

        foreach (var size in s_sizes)
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

            if (s_trim)
            {
                image = image.Trim();
            }

            foreach (var size in s_sizes)
            {
                using var secondarySizeImage = image.Resize(size, size);

                var filePath = Path.Join(Path.GetDirectoryName(file), size.ToString(), Path.GetFileName(file));
                secondarySizeImage.Save(filePath);
            }

            stream.Dispose();
            image.Dispose();

            if (s_sizes.Length > 0)
            {
                File.Delete(file);
            }

            Log.Information("Image {Path} done", file);
        }
    }
}
