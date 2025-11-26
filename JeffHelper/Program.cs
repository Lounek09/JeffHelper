using JeffHelper.Utils;

using Serilog;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

using System.Text.RegularExpressions;

namespace JeffHelper;

public static partial class Program
{
    private const string c_input = "input";
    private const string c_output = "output";

    private const string c_defaultScope = "main";
    private const int c_defaultExportSize = 2048;
    private const bool c_defaultTrim = true;
    private static readonly IReadOnlyList<int> s_defaultSizes = [32, 64, 128, 256, 512];
    private const bool c_square = true;

    private static string s_scope = c_defaultScope;
    private static int s_exportSize = c_defaultExportSize;
    private static bool s_trim = c_defaultTrim;
    private static IReadOnlyList<int> s_sizes = s_defaultSizes;
    private static bool s_square = c_square;

    [GeneratedRegex(c_input)]
    private static partial Regex PathRegex();

    public static int Main()
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();

        Directory.CreateDirectory(c_input);
        Directory.CreateDirectory(c_output);

    Retry:
        AskOptions();

        Log.Information("Generating images...");
        Generate();

        if (s_trim || s_sizes.Count > 0)
        {
            Log.Information("Resizing images...");
            Resize();
        }

        Log.Information("Done, press {Key} to quit or any other key to continue", 'q');
        if (Console.ReadKey().Key == ConsoleKey.Q)
        {
            return 0;
        }

        goto Retry;
    }

    /// <summary>
    /// Asks the user for options.
    /// </summary>
    private static void AskOptions()
    {
        s_scope = Question.Ask("Enter scope, either {Classes} or {Main} (default [{Scope}]):",
            ["classes", "main", s_scope],
            s_scope,
            input => (true, input)
        );

        s_exportSize = Question.Ask("Enter export size (default [{ExportSize}]):",
            [s_exportSize],
            s_exportSize,
            input =>
            {
                if (int.TryParse(input, out var size))
                {
                    return (true, size);
                }

                return (false, 0);
            }
        );

        s_trim = Question.Ask("Trim images? (default [{Trim}]):",
            [s_trim],
            s_trim,
            input => (true, Answer.IsYes(input))
        );

        s_sizes = Question.Ask("Enter sizes as a comma-separated list (default [{Sizes}]), enter {Zero} for none:",
            [string.Join(',', s_sizes), '0'],
            s_sizes,
            input =>
            {
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

        if (s_sizes.Count > 0)
        {
            s_square = Question.Ask("Square images output? (default [{Square}]):",
                [s_square],
                s_square,
                input => (true, Answer.IsYes(input))
            );
        }
    }

    /// <summary>
    /// Generates images from the input path using Jeff and saves them to the output path.
    /// </summary>
    /// <param name="path">The path to generate images for.</param>
    private static void Generate(string path = c_input)
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

        ExecuteCmd.Execute("jeff", $"-i {path} -o {outputPath} -S {s_scope} -R true -d true -f \"[1]\" -w {s_exportSize}");

        Log.Information("Swf files in {Path} successfully generated to {OutputPath}", path, outputPath);
    }

    /// <summary>
    /// Trims and resizes images from the output path.
    /// </summary>
    /// <param name="path">The path to resize images for.</param>
    private static void Resize(string path = c_output)
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
            Directory.CreateDirectory(sizePath);
        }

        foreach (var file in files)
        {
            var directory = Path.GetDirectoryName(file);
            var fileName = Path.GetFileName(file);

            using var image = Image.Load<Rgba32>(file);
            var isModified = false;

            if (s_trim)
            {
                image.Trim();
                isModified = true;
            }

            if (s_sizes.Count > 0)
            {
                foreach (var size in s_sizes)
                {
                    var width = size;
                    var height = size;

                    if (!s_square)
                    {
                        if (image.Width > image.Height)
                        {
                            height = (int)(size * (image.Height / (double)image.Width));
                        }
                        else if (image.Height > image.Width)
                        {
                            width = (int)(size * (image.Width / (double)image.Height));
                        }
                    }

                    using var resizedImage = image.CreateResizedCopy(width, height);

                    var filePath = Path.Join(directory, size.ToString(), fileName);
                    resizedImage.Save(filePath);
                }

                File.Delete(file);
            }
            else if (isModified)
            {
                image.Save(file);
            }

            Log.Information("Image {Path} resized", file);
        }
    }
}
