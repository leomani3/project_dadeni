using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEditor;
using Debug = UnityEngine.Debug;

public static class SteamUploader
{
    public const string ScriptsFolderName = "SteamScripts";

    public static readonly GameBuilder.Platform[] UploadablePlatforms =
    {
        GameBuilder.Platform.Windows,
        GameBuilder.Platform.MacOS
    };

    private static readonly TimeSpan StallTimeout = TimeSpan.FromMinutes(5);

    private static long _lastOutputTicks;

    public static bool CanUpload(GameBuilder.Platform platform) =>
        Array.IndexOf(UploadablePlatforms, platform) >= 0;

    public static string ScriptsDir =>
        Path.Combine(GameBuilder.ProjectRoot, GameBuilder.BuildsFolderName, ScriptsFolderName);

    public static string SteamCmdPath =>
        Path.Combine(SteamUploadSettings.ContentBuilderPath, "builder", "steamcmd.exe");

    public static string SteamBuildOutputDir =>
        Path.Combine(SteamUploadSettings.ContentBuilderPath, "output");

    public static string ContentRootFor(GameBuilder.Platform platform, GameBuilder.Options options)
    {
        string dir = GameBuilder.OutputDirFor(platform, options);
        return platform == GameBuilder.Platform.MacOS
                   ? Path.Combine(dir, GameBuilder.ExecutableName(GameBuilder.Platform.MacOS))
                   : dir;
    }

    public static string Validate(IList<GameBuilder.Platform> platforms, GameBuilder.Options options)
    {
        if (platforms == null || platforms.Count == 0)
            return "No uploadable platform selected (Linux has no depot).";

        if (string.IsNullOrWhiteSpace(SteamUploadSettings.ContentBuilderPath))
            return "Set the SDK ContentBuilder folder first.";

        if (!File.Exists(SteamCmdPath))
            return $"steamcmd.exe not found at {SteamCmdPath}.";

        if (string.IsNullOrWhiteSpace(SteamUploadSettings.Username))
            return "Set the Steam account name first.";

        if (!IsId(SteamUploadSettings.AppId))
            return "App ID must be a number.";

        foreach (GameBuilder.Platform platform in platforms)
        {
            if (!IsId(SteamUploadSettings.GetDepotId(platform)))
                return $"Depot ID for {platform} must be a number.";

            string root = ContentRootFor(platform, options);
            if (!Directory.Exists(root))
                return $"Nothing to upload for {platform}: {root} does not exist. Build it first.";
        }

        var clash = platforms.GroupBy(SteamUploadSettings.GetDepotId).FirstOrDefault(g => g.Count() > 1);
        if (clash != null)
            return $"{string.Join(" and ", clash)} share depot ID {clash.Key}. Give each platform its own depot.";

        return null;
    }

    private static bool IsId(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.Trim().All(char.IsDigit);

    public static string WriteScripts(IList<GameBuilder.Platform> platforms, GameBuilder.Options options, bool preview)
    {
        Directory.CreateDirectory(ScriptsDir);

        string appId = SteamUploadSettings.AppId.Trim();
        var depotEntries = new List<string>();

        foreach (GameBuilder.Platform platform in platforms)
        {
            string depotId    = SteamUploadSettings.GetDepotId(platform).Trim();
            string depotPath  = Path.Combine(ScriptsDir, $"depot_{depotId}.vdf");
            File.WriteAllText(depotPath, DepotScript(depotId, ContentRootFor(platform, options)), Encoding.UTF8);
            depotEntries.Add($"\t\t\"{depotId}\"\t\"{depotPath}\"");
        }

        string appPath = Path.Combine(ScriptsDir, $"app_{appId}.vdf");
        File.WriteAllText(appPath, AppScript(appId, Description(options), depotEntries, preview), Encoding.UTF8);
        return appPath;
    }

    private static string Description(GameBuilder.Options options)
    {
        string name    = GameBuilder.Sanitize(options.Name);
        string version = string.IsNullOrWhiteSpace(options.Version) ? "?" : options.Version.Trim();
        string desc    = $"{(name.Length == 0 ? GameBuilder.DefaultBuildName : name)} {version}";

        if (options.Development)
            desc += " (dev)";

        return Escape(desc);
    }

    private static string AppScript(string appId, string desc, List<string> depotEntries, bool preview)
    {
        var script = new StringBuilder();
        script.AppendLine("\"appbuild\"");
        script.AppendLine("{");
        script.AppendLine($"\t\"appid\" \"{appId}\"");
        script.AppendLine($"\t\"desc\" \"{desc}\"");
        script.AppendLine($"\t\"buildoutput\" \"{Escape(SteamBuildOutputDir)}\"");
        script.AppendLine("\t\"contentroot\" \"\"");
        script.AppendLine("\t\"setlive\" \"\"");
        script.AppendLine($"\t\"preview\" \"{(preview ? "1" : "0")}\"");
        script.AppendLine("\t\"local\" \"\"");
        script.AppendLine("\t\"depots\"");
        script.AppendLine("\t{");
        foreach (string entry in depotEntries)
            script.AppendLine(entry);
        script.AppendLine("\t}");
        script.AppendLine("}");
        return script.ToString();
    }

    private static string DepotScript(string depotId, string contentRoot)
    {
        var script = new StringBuilder();
        script.AppendLine("\"DepotBuildConfig\"");
        script.AppendLine("{");
        script.AppendLine($"\t\"DepotID\" \"{depotId}\"");
        script.AppendLine($"\t\"contentroot\" \"{Escape(contentRoot)}\"");
        script.AppendLine("\t\"FileMapping\"");
        script.AppendLine("\t{");
        script.AppendLine("\t\t\"LocalPath\" \"*\"");
        script.AppendLine("\t\t\"DepotPath\" \".\"");
        script.AppendLine("\t\t\"recursive\" \"1\"");
        script.AppendLine("\t}");
        script.AppendLine("\t\"FileExclusion\" \"*.pdb\"");
        script.AppendLine("}");
        return script.ToString();
    }

    private static string Escape(string value) => value.Replace("\"", string.Empty);

    public static bool Upload(IList<GameBuilder.Platform> platforms, GameBuilder.Options options, bool preview)
    {
        string problem = Validate(platforms, options);
        if (problem != null)
        {
            Debug.LogError($"[Steam] {problem}");
            return false;
        }

        string appScript;
        try
        {
            Directory.CreateDirectory(SteamBuildOutputDir);
            appScript = WriteScripts(platforms, options, preview);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Steam] Could not write the build scripts: {e.Message}");
            return false;
        }

        foreach (GameBuilder.Platform platform in platforms)
            Debug.Log($"[Steam] depot {SteamUploadSettings.GetDepotId(platform)} <- {ContentRootFor(platform, options)}");

        Debug.Log($"[Steam] {(preview ? "Preview (nothing is published)" : "Uploading")} app "
                + $"{SteamUploadSettings.AppId} as {SteamUploadSettings.Username} using {appScript}");

        return Run(appScript, preview);
    }

    private static bool Run(string appScript, bool preview)
    {
        var output = new List<string>();
        var timer  = Stopwatch.StartNew();
        bool stalled   = false;
        bool cancelled = false;

        var startInfo = new ProcessStartInfo
        {
            FileName               = SteamCmdPath,
            WorkingDirectory       = SteamUploadSettings.ContentBuilderPath,
            UseShellExecute        = false,
            CreateNoWindow         = true,
            RedirectStandardOutput = true,
            RedirectStandardError  = true
        };

        startInfo.Arguments = $"+login {SteamUploadSettings.Username.Trim()} "
                            + $"+run_app_build \"{appScript}\" +quit";

        try
        {
            using (var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true })
            {
                void Capture(string line)
                {
                    if (line == null)
                        return;

                    Interlocked.Exchange(ref _lastOutputTicks, DateTime.UtcNow.Ticks);
                    lock (output)
                        output.Add(line);
                }

                process.OutputDataReceived += (_, e) => Capture(e.Data);
                process.ErrorDataReceived  += (_, e) => Capture(e.Data);

                Interlocked.Exchange(ref _lastOutputTicks, DateTime.UtcNow.Ticks);
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                while (!process.WaitForExit(500))
                {
                    var sinceOutput = DateTime.UtcNow - new DateTime(Interlocked.Read(ref _lastOutputTicks));
                    if (sinceOutput > StallTimeout)
                    {
                        stalled = true;
                        try { process.Kill(); } catch { }
                        break;
                    }

                    cancelled = EditorUtility.DisplayCancelableProgressBar(
                        "Steam",
                        $"{(preview ? "Previewing" : "Uploading")} - {timer.Elapsed:mm\\:ss}",
                        (float)(timer.Elapsed.TotalSeconds % 10.0 / 10.0));

                    if (cancelled)
                    {
                        try { process.Kill(); } catch { }
                        break;
                    }
                }

                process.WaitForExit();
                timer.Stop();

                bool succeeded = !stalled && !cancelled && process.ExitCode == 0;
                Report(output, succeeded, stalled, cancelled, timer.Elapsed, preview);
                return succeeded;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[Steam] Could not run steamcmd: {e.Message}");
            return false;
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    private static void Report(List<string> output, bool succeeded, bool stalled, bool cancelled,
                               TimeSpan elapsed, bool preview)
    {
        string logPath = Path.Combine(ScriptsDir, "steamcmd.log");
        try
        {
            lock (output)
                File.WriteAllLines(logPath, output);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Steam] Could not write {logPath}: {e.Message}");
        }

        if (stalled)
        {
            Debug.LogError($"[Steam] steamcmd stopped responding for {StallTimeout.TotalMinutes:0} minutes and was "
                         + "killed. It was almost certainly waiting on a password or Steam Guard prompt it cannot "
                         + "show here. Run this once in a terminal, complete the login, then try again:\n"
                         + $"\"{SteamCmdPath}\" +login {SteamUploadSettings.Username.Trim()}");
            return;
        }

        if (cancelled)
        {
            Debug.LogWarning($"[Steam] Cancelled after {elapsed:mm\\:ss}, steamcmd was killed. A partially "
                           + $"transferred build is discarded by Steam, nothing was published. Log: {logPath}");
            return;
        }

        string[] tail;
        lock (output)
            tail = output.Where(l => !string.IsNullOrWhiteSpace(l)).Reverse().Take(25).Reverse().ToArray();

        string summary = $"[Steam] {(succeeded ? "SUCCESS" : "FAILED")} in {elapsed:mm\\:ss}"
                       + (succeeded && preview ? " (preview - nothing was published)" : string.Empty)
                       + $"\nFull log: {logPath}\n"
                       + string.Join("\n", tail);

        if (!succeeded)
        {
            Debug.LogError(summary);
            return;
        }

        Debug.Log(summary);

        bool marker = tail.Any(l => l.IndexOf("Successfully finished", StringComparison.OrdinalIgnoreCase) >= 0);
        if (!marker)
            Debug.LogWarning("[Steam] steamcmd exited cleanly but did not report a finished appbuild. "
                           + $"Check {logPath} and the Steamworks build list before setting anything live.");
    }
}

public static class SteamUploadSettings
{
    private const string PrefPrefix = "Dadeni_Steam_";

    public static string ContentBuilderPath
    {
        get => EditorPrefs.GetString(PrefPrefix + "ContentBuilder", string.Empty);
        set => EditorPrefs.SetString(PrefPrefix + "ContentBuilder", value ?? string.Empty);
    }

    public static string Username
    {
        get => EditorPrefs.GetString(PrefPrefix + "User", string.Empty);
        set => EditorPrefs.SetString(PrefPrefix + "User", value ?? string.Empty);
    }

    public static string AppId
    {
        get => EditorPrefs.GetString(PrefPrefix + "AppId", string.Empty);
        set => EditorPrefs.SetString(PrefPrefix + "AppId", value ?? string.Empty);
    }

    public static string GetDepotId(GameBuilder.Platform platform) =>
        EditorPrefs.GetString(PrefPrefix + "Depot_" + platform, string.Empty);

    public static void SetDepotId(GameBuilder.Platform platform, string depotId) =>
        EditorPrefs.SetString(PrefPrefix + "Depot_" + platform, depotId ?? string.Empty);

    public static bool PreviewOnly
    {
        get => EditorPrefs.GetBool(PrefPrefix + "Preview", true);
        set => EditorPrefs.SetBool(PrefPrefix + "Preview", value);
    }
}
