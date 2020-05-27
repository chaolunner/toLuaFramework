using System.Diagnostics;

public static class ProcessUtil
{
    public static Process CreateProcess(string cmd, string args, string workingDir = "")
    {
        var processStartInfo = new ProcessStartInfo(cmd);
        processStartInfo.Arguments = args;
        processStartInfo.CreateNoWindow = true;
        processStartInfo.UseShellExecute = true;
        processStartInfo.RedirectStandardError = false;
        processStartInfo.RedirectStandardInput = false;
        processStartInfo.RedirectStandardOutput = false;
        if (!string.IsNullOrEmpty(workingDir))
        {
            processStartInfo.WorkingDirectory = workingDir;
        }
        return Process.Start(processStartInfo);
    }

    public static void RunBat(string batfile, string args, string workingDir = "")
    {
        var process = CreateProcess(batfile, args, workingDir);
        process.Close();
    }
}
