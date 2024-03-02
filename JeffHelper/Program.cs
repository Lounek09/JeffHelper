using JeffHelper.Utils;

using Serilog;

using System.Drawing;

namespace JeffHelper;

public static class Program
{
    public const string INPUT_DIR = "input";
    public const string OUTPUT_DIR = "output";
    public const string JEFF_DIR = "Jeff";
    public const string JEFF_REPO = "git@github.com:H3r3zy/Jeff.git";

    private static int _mainSize = 256;
    private static int[] _secondarySizes = [32, 64, 128, 512];

    public static void Main()
    {
        Initialize();
        Option();

        Log.Information("Generating images...");
        Generate(INPUT_DIR);

        Log.Information("Resizing images...");
        Resize(OUTPUT_DIR);

        Log.Information("Press any key to exit...");
        Console.ReadKey();
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

        if (!Directory.Exists(JEFF_DIR))
        {
            ExecuteCmd.ExecuteCommand("git", $"clone {JEFF_REPO}");
            ExecuteCmd.ExecuteCommand("cmd", "/c npm install", JEFF_DIR);
        }
    }

    private static void Option()
    {
        _mainSize = Ask("Enter main size (default [{MainSize}]):",
            [_mainSize],
            input =>
            {
                if (string.IsNullOrEmpty(input))
                {
                    return (true, _mainSize);
                }

                if (int.TryParse(input, out var size))
                {
                    return (true, size);
                }

                return (false, 0);
            }
        );

        _secondarySizes = Ask("Enter secondary sizes as a comma-separated list (default [{SecondarySizes}] or enter '{None}' for none):",
            [string.Join(',', _secondarySizes), 0],
            input =>
            {
                if (string.IsNullOrEmpty(input))
                {
                    return (true, _secondarySizes);
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

    private static T Ask<T>(string messageTemplace, object[] templateParameters, Func<string?, (bool, T)> validator)
    {
        Log.Information(messageTemplace, templateParameters);
        var input = Console.ReadLine();

        var (Success, Value) = validator(input);
        if (Success)
        {
            return Value;
        }

        Log.Warning("Invalid input");
        return Ask(messageTemplace, templateParameters, validator);
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

        var outputPath = path.Replace(INPUT_DIR + Path.DirectorySeparatorChar, OUTPUT_DIR + Path.DirectorySeparatorChar);

        ExecuteCmd.ExecuteCommand("node",
            $"Jeff/bin/jeff -i {path} -o {outputPath} -R true -d true -f \"[1]\" -w 2000",
            string.Empty,
            false);

        Log.Information("Images from {Path} successfully generated", path);
    }

    private static void Resize(string path)
    {
        foreach (var directory in Directory.GetDirectories(path))
        {
            Resize(directory);
        }

        var files = Directory.GetFiles(path, "*.png");
        if (files.Length == 0)
        {
            return;
        }

        foreach (var size in _secondarySizes)
        {
            var sizePath = path + Path.DirectorySeparatorChar + size;
            if (!Directory.Exists(sizePath))
            {
                Directory.CreateDirectory(sizePath);
            }
        }

        foreach (var file in files)
        {
            FileStream stream = new(file, FileMode.Open);
            var image = Image.FromStream(stream);
            stream.Dispose();

            var trimedImage = image.Trim();

            var mainSizeImage = trimedImage.Resize(_mainSize, _mainSize);
            File.Delete(file);
            mainSizeImage.Save(file);

            foreach (var size in _secondarySizes)
            {
                var secondarySizeImage = trimedImage.Resize(size, size);
                secondarySizeImage.Save(Path.GetDirectoryName(file) + Path.DirectorySeparatorChar + size + Path.DirectorySeparatorChar + Path.GetFileName(file));
            }

            Log.Information("Image {Path} successfully resized", file);
        }
    }
}
