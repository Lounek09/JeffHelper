using Serilog;

using System.Diagnostics;

namespace JeffHelper.Utils;

public static class ExecuteCmd
{
    public static void ExecuteCommand(string command, string args, string workingDirectory = "", bool showOutput = true)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = command,
            Arguments = args,
            WorkingDirectory = workingDirectory,
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        try
        {
            using var process = new Process
            {
                StartInfo = startInfo
            };

            if (showOutput)
            {
                process.OutputDataReceived += Process_OutputDataReceived;
                process.ErrorDataReceived += Process_ErrorDataReceived;
            }

            process.Start();

            if (showOutput)
            {
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }

            process.WaitForExit();
        }
        catch (Exception e)
        {
            Log.Error(e, "An error occured while execute {CommandName} command", command);
        }
    }

    private static void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Data))
        {
            Log.Information(e.Data);
        }
    }

    private static void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Data))
        {
            Log.Error(e.Data);
        }
    }
}
