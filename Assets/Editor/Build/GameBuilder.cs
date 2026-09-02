using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

public static class GameBuilder
{
    public enum Platform
    {
        Windows,
        MacOS,
        Linux
    }

    public struct Options
    {
        public string Name;
        public string Version;
        public bool Development;

        public static Options Default => new Options
        {
            Name        = DefaultBuildName,
            Version     = PlayerSettings.bundleVersion,
            Development = false
        };
    }

    private const string SteamAppIdFile = "steam_appid.txt";

    public const string BuildsFolderName = "Builds";

    private static readonly string[] DoNotShipSuffixes =
    {
        "_BurstDebugInformation_DoNotShip",
        "_BackUpThisFolder_ButDontShipItWithYourGame"
    };

    [MenuItem("Tools/Build/Windows", false, 20)]
    private static void MenuBuildWindows() => Build(Options.Default, Platform.Windows);

    [MenuItem("Tools/Build/macOS", false, 21)]
    private static void MenuBuildMac() => Build(Options.Default, Platform.MacOS);

    [MenuItem("Tools/Build/Linux", false, 22)]
    private static void MenuBuildLinux() => Build(Options.Default, Platform.Linux);

    [MenuItem("Tools/Build/Windows + macOS", false, 40)]
    private static void MenuBuildDesktop() => Build(Options.Default, Platform.Windows, Platform.MacOS);

    [MenuItem("Tools/Build/All Platforms", false, 41)]
    private static void MenuBuildAll() => Build(Options.Default, Platform.Windows, Platform.MacOS, Platform.Linux);

    public static bool Build(Options options, params Platform[] platforms)
    {
        if (platforms == null || platforms.Length == 0)
        {
            Debug.LogError("[Build] No platform selected.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(options.Version))
            options.Version = PlayerSettings.bundleVersion;

        if (PlayerSettings.bundleVersion != options.Version)
        {
            PlayerSettings.bundleVersion = options.Version;
            AssetDatabase.SaveAssets();
            Debug.Log($"[Build] PlayerSettings.bundleVersion set to {options.Version}.");
        }

        string[] scenes = EditorBuildSettings.scenes
                                             .Where(s => s.enabled)
                                             .Select(s => s.path)
                                             .ToArray();

        if (scenes.Length == 0)
        {
            Debug.LogError("[Build] No enabled scenes in Build Settings.");
            return false;
        }

        Debug.Log(IsDevelopmentConfig
                      ? $"[Build] GameConfig.developmentBuild is ON -> {SteamAppIdFile} will be included."
                      : $"[Build] GameConfig.developmentBuild is OFF -> {SteamAppIdFile} will NOT be included.");

        BuildTarget originalTarget = EditorUserBuildSettings.activeBuildTarget;
        var  results       = new List<string>();
        bool allSucceeded  = true;
        var  totalTimer    = Stopwatch.StartNew();

        try
        {
            foreach (Platform platform in platforms)
            {
                bool ok = BuildSingle(platform, scenes, options, out string detail);
                allSucceeded &= ok;
                results.Add($"  {(ok ? "OK  " : "FAIL")} {platform,-8} {detail}");

                if (!ok)
                    break;
            }
        }
        finally
        {
            if (EditorUserBuildSettings.activeBuildTarget != originalTarget)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, originalTarget);
        }

        totalTimer.Stop();
        string summary = $"[Build] {(allSucceeded ? "SUCCESS" : "FAILED")} in {FormatTime(totalTimer.Elapsed)}\n"
                       + string.Join("\n", results);

        if (allSucceeded) Debug.Log(summary);
        else              Debug.LogError(summary);

        return allSucceeded;
    }

    public static bool IsDevelopmentConfig
    {
        get
        {
            GameConfig config = GameConfig.Instance;
            return config != null && config.debuggingSettings != null && config.debuggingSettings.developmentBuild;
        }
    }

    public static string FolderNameFor(Platform platform, Options options)
    {
        string version = string.IsNullOrWhiteSpace(options.Version) ? PlayerSettings.bundleVersion : options.Version;
        string name    = Sanitize(options.Name);
        if (name.Length == 0)
            name = DefaultBuildName;

        string folder = $"{name}_{version.Replace('.', '_')}";
        return options.Development ? folder + "_dev" : folder;
    }

    public static string RootFor(Platform platform) =>
        Path.Combine(ProjectRoot, BuildsFolderName, PlatformFolder(platform));

    public static string OutputDirFor(Platform platform, Options options) =>
        Path.Combine(RootFor(platform), FolderNameFor(platform, options));

    public static string PlatformFolder(Platform platform) => platform switch
    {
        Platform.Windows => "WINDOWS",
        Platform.MacOS   => "MACOS",
        Platform.Linux   => "LINUX",
        _                => platform.ToString().ToUpperInvariant()
    };

    private static bool BuildSingle(Platform platform, string[] scenes, Options options, out string detail)
    {
        BuildTarget target = ToBuildTarget(platform);

        string outputDir = OutputDirFor(platform, options);
        string location  = Path.Combine(outputDir, ExecutableName(platform));

        try
        {
            if (Directory.Exists(outputDir))
                Directory.Delete(outputDir, true);
            Directory.CreateDirectory(outputDir);
        }
        catch (Exception e)
        {
            detail = $"cannot write to {outputDir}: {e.Message}";
            return false;
        }

        Debug.Log($"[Build] {platform} -> {outputDir}");

        if (EditorUserBuildSettings.activeBuildTarget != target)
        {
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, target))
            {
                detail = "could not switch active build target (build module installed?)";
                return false;
            }
        }

        EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Player;

        if (platform == Platform.MacOS)
            TrySetMacUniversalArchitecture();

        BuildOptions buildOptions = BuildOptions.None;
        if (options.Development)
            buildOptions |= BuildOptions.Development | BuildOptions.AllowDebugging;

        var playerOptions = new BuildPlayerOptions
        {
            scenes           = scenes,
            locationPathName = location,
            target           = target,
            targetGroup      = BuildTargetGroup.Standalone,
            options          = buildOptions
        };

        BuildReport  report  = BuildPipeline.BuildPlayer(playerOptions);
        BuildSummary summary = report.summary;

        if (summary.result != BuildResult.Succeeded)
        {
            detail = $"{summary.totalErrors} error(s), see log above";
            return false;
        }

        long freed = CleanOutput(outputDir, platform, options);
        CopySteamAppId(outputDir, platform);

        string sizeInfo = $"{summary.totalSize / (1024f * 1024f):F0} MB in {FormatTime(summary.totalTime)}";
        if (freed > 0)
            sizeInfo += $", stripped {freed / (1024f * 1024f):F1} MB";

        detail = $"{outputDir} - {sizeInfo}";
        return true;
    }

    private static long CleanOutput(string outputDir, Platform platform, Options options)
    {
        long freed = 0;

        foreach (string dir in Directory.GetDirectories(outputDir))
        {
            if (DoNotShipSuffixes.Any(suffix => dir.EndsWith(suffix, StringComparison.Ordinal)))
                freed += DeleteDirectory(dir);
        }

        if (options.Development)
            return freed;

        switch (platform)
        {
            case Platform.Windows:
                freed += DeleteFile(Path.Combine(outputDir, "D3D12", "d3d12SDKLayers.dll"));
                freed += DeleteFile(Path.Combine(outputDir, "WinPixEventRuntime.dll"));
                break;

            case Platform.Linux:
                foreach (string file in Directory.GetFiles(outputDir, "*.debug", SearchOption.AllDirectories))
                    freed += DeleteFile(file);
                break;

            case Platform.MacOS:
                foreach (string dir in Directory.GetDirectories(outputDir, "*.dSYM", SearchOption.AllDirectories))
                    freed += DeleteDirectory(dir);
                break;
        }

        if (freed > 0)
            Debug.Log($"[Build] Stripped {freed / (1024f * 1024f):F1} MB from {Path.GetFileName(outputDir)}.");

        return freed;
    }

    private static void CopySteamAppId(string outputDir, Platform platform)
    {
        if (!IsDevelopmentConfig)
            return;

        string source = Path.Combine(ProjectRoot, SteamAppIdFile);
        if (!File.Exists(source))
        {
            Debug.LogWarning($"[Build] {SteamAppIdFile} not found at project root - skipped.");
            return;
        }

        File.Copy(source, Path.Combine(outputDir, SteamAppIdFile), true);

        if (platform == Platform.MacOS)
        {
            string macExecDir = Path.Combine(outputDir, ExecutableName(Platform.MacOS), "Contents", "MacOS");
            if (Directory.Exists(macExecDir))
                File.Copy(source, Path.Combine(macExecDir, SteamAppIdFile), true);
        }
    }

    private static long DeleteFile(string path)
    {
        if (!File.Exists(path))
            return 0;

        long size = new FileInfo(path).Length;
        File.Delete(path);
        return size;
    }

    private static long DeleteDirectory(string path)
    {
        if (!Directory.Exists(path))
            return 0;

        long size = Directory.GetFiles(path, "*", SearchOption.AllDirectories)
                             .Sum(f => new FileInfo(f).Length);
        Directory.Delete(path, true);
        return size;
    }

    internal static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;

    private static string SafeProductName => new string(PlayerSettings.productName
                                                        .Where(c => !char.IsWhiteSpace(c))
                                                        .ToArray());

    public static string DefaultBuildName => SafeProductName;

    public static string Sanitize(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        char[] invalid = Path.GetInvalidFileNameChars();
        return new string(name.Where(c => !char.IsWhiteSpace(c) && Array.IndexOf(invalid, c) < 0).ToArray());
    }

    private static BuildTarget ToBuildTarget(Platform platform) => platform switch
    {
        Platform.Windows => BuildTarget.StandaloneWindows64,
        Platform.MacOS   => BuildTarget.StandaloneOSX,
        Platform.Linux   => BuildTarget.StandaloneLinux64,
        _                => throw new ArgumentOutOfRangeException(nameof(platform))
    };

    public static string ExecutableName(Platform platform) => platform switch
    {
        Platform.Windows => PlayerSettings.productName + ".exe",
        Platform.MacOS   => SafeProductName + ".app",
        Platform.Linux   => SafeProductName + ".x86_64",
        _                => throw new ArgumentOutOfRangeException(nameof(platform))
    };

    private static void TrySetMacUniversalArchitecture()
    {
        try
        {
            Type settingsType = AppDomain.CurrentDomain.GetAssemblies()
                                         .Select(a => a.GetType("UnityEditor.OSXStandalone.UserBuildSettings"))
                                         .FirstOrDefault(t => t != null);

            PropertyInfo property = settingsType?.GetProperty("architecture", BindingFlags.Public | BindingFlags.Static);
            if (property == null)
                return;

            object universal = Enum.Parse(property.PropertyType, "x64ARM64");
            property.SetValue(null, universal);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Build] Could not force a universal macOS binary, using the project default: {e.Message}");
        }
    }

    private static string FormatTime(TimeSpan span) => $"{(int)span.TotalMinutes}m{span.Seconds:00}s";

    public static void BuildFromCommandLine()
    {
        string[] args = Environment.GetCommandLineArgs();

        Options options = Options.Default;
        options.Version     = GetArgValue(args, "-buildVersion") ?? PlayerSettings.bundleVersion;
        options.Name        = GetArgValue(args, "-buildName")    ?? DefaultBuildName;
        options.Development = args.Contains("-buildDev");

        string platformArg = GetArgValue(args, "-buildPlatforms") ?? "windows,macos";
        var platforms = new List<Platform>();

        foreach (string token in platformArg.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
        {
            switch (token.Trim().ToLowerInvariant())
            {
                case "windows":
                case "win":   platforms.Add(Platform.Windows); break;
                case "macos":
                case "osx":
                case "mac":   platforms.Add(Platform.MacOS);   break;
                case "linux": platforms.Add(Platform.Linux);   break;
                default:      Debug.LogWarning($"[Build] Unknown platform '{token}', ignored."); break;
            }
        }

        bool success = Build(options, platforms.ToArray());
        EditorApplication.Exit(success ? 0 : 1);
    }

    private static string GetArgValue(string[] args, string key)
    {
        int index = Array.IndexOf(args, key);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }
}
