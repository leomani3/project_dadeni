using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class BuildWindow : EditorWindow
{
    private const string PrefPrefix = "Dadeni_Build_";

    private static readonly GameBuilder.Platform[] AllPlatforms =
    {
        GameBuilder.Platform.Windows,
        GameBuilder.Platform.MacOS,
        GameBuilder.Platform.Linux
    };

    private string _name;
    private string _version;
    private bool   _development;

    private readonly Dictionary<GameBuilder.Platform, bool> _enabled = new Dictionary<GameBuilder.Platform, bool>();

    private string _contentBuilderPath;
    private string _steamUser;
    private string _appId;
    private bool   _previewOnly;
    private bool   _steamFoldout;

    private readonly Dictionary<GameBuilder.Platform, string> _depotIds = new Dictionary<GameBuilder.Platform, string>();

    private Vector2 _scroll;

    [MenuItem("Tools/Build/Build Window %&b", false, 0)]
    private static void Open()
    {
        var window = GetWindow<BuildWindow>(true, "Build " + PlayerSettings.productName);
        window.minSize = new Vector2(560, 380);
        window.Show();
    }

    private void OnEnable()
    {
        _name        = EditorPrefs.GetString(PrefPrefix + "Name", GameBuilder.DefaultBuildName);
        _version     = EditorPrefs.GetString(PrefPrefix + "Version", PlayerSettings.bundleVersion);
        _development = EditorPrefs.GetBool(PrefPrefix + "Dev", false);

        foreach (GameBuilder.Platform platform in AllPlatforms)
        {
            _enabled[platform] = EditorPrefs.GetBool(PrefPrefix + "Enabled_" + platform,
                                                     platform != GameBuilder.Platform.Linux);
        }

        _contentBuilderPath = SteamUploadSettings.ContentBuilderPath;
        _steamUser          = SteamUploadSettings.Username;
        _appId              = SteamUploadSettings.AppId;
        _previewOnly        = SteamUploadSettings.PreviewOnly;
        _steamFoldout       = EditorPrefs.GetBool(PrefPrefix + "SteamFoldout", false);

        foreach (GameBuilder.Platform platform in SteamUploader.UploadablePlatforms)
            _depotIds[platform] = SteamUploadSettings.GetDepotId(platform);
    }

    private void SavePrefs()
    {
        EditorPrefs.SetString(PrefPrefix + "Name", _name);
        EditorPrefs.SetString(PrefPrefix + "Version", _version);
        EditorPrefs.SetBool(PrefPrefix + "Dev", _development);

        foreach (GameBuilder.Platform platform in AllPlatforms)
            EditorPrefs.SetBool(PrefPrefix + "Enabled_" + platform, _enabled[platform]);

        SteamUploadSettings.ContentBuilderPath = _contentBuilderPath;
        SteamUploadSettings.Username           = _steamUser;
        SteamUploadSettings.AppId              = _appId;
        SteamUploadSettings.PreviewOnly        = _previewOnly;
        EditorPrefs.SetBool(PrefPrefix + "SteamFoldout", _steamFoldout);

        foreach (GameBuilder.Platform platform in SteamUploader.UploadablePlatforms)
            SteamUploadSettings.SetDepotId(platform, _depotIds[platform]);
    }

    private void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        EditorGUILayout.Space();
        EditorGUI.BeginChangeCheck();

        using (new EditorGUILayout.HorizontalScope())
        {
            _name = EditorGUILayout.TextField("Build name", _name);

            using (new EditorGUI.DisabledScope(GameBuilder.Sanitize(_name) == GameBuilder.DefaultBuildName))
            {
                if (GUILayout.Button("Reset", GUILayout.Width(50)))
                {
                    _name = GameBuilder.DefaultBuildName;
                    GUI.FocusControl(null);
                }
            }
        }

        _version = EditorGUILayout.TextField("Version", _version);
        EditorGUILayout.LabelField(" ", "Written to Player Settings when you build.", EditorStyles.miniLabel);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Platforms", EditorStyles.boldLabel);

        foreach (GameBuilder.Platform platform in AllPlatforms)
            DrawPlatformRow(platform);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Options", EditorStyles.boldLabel);
        _development = EditorGUILayout.ToggleLeft("Unity development build (keeps debug files)", _development);

        if (EditorGUI.EndChangeCheck())
            SavePrefs();

        DrawSteamAppIdStatus();

        EditorGUILayout.Space();

        var platforms = SelectedPlatforms();
        using (new EditorGUI.DisabledScope(platforms.Count == 0 || string.IsNullOrWhiteSpace(_version)))
        {
            if (GUILayout.Button($"Build {platforms.Count} platform(s)", GUILayout.Height(32)))
                RunBuild(platforms);
        }

        EditorGUILayout.Space();
        DrawSteamSection(platforms);

        EditorGUILayout.EndScrollView();
    }

    private void DrawSteamSection(List<GameBuilder.Platform> platforms)
    {
        EditorGUI.BeginChangeCheck();

        _steamFoldout = EditorGUILayout.Foldout(_steamFoldout, "Upload to Steam", true, EditorStyles.foldoutHeader);
        if (!_steamFoldout)
        {
            if (EditorGUI.EndChangeCheck())
                SavePrefs();
            return;
        }

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                _contentBuilderPath = EditorGUILayout.TextField("SDK ContentBuilder", _contentBuilderPath);

                if (GUILayout.Button("...", GUILayout.Width(30)))
                {
                    string picked = EditorUtility.OpenFolderPanel("ContentBuilder folder in the Steamworks SDK",
                                                                  _contentBuilderPath, string.Empty);
                    if (!string.IsNullOrEmpty(picked))
                    {
                        _contentBuilderPath = picked;
                        GUI.FocusControl(null);
                        SavePrefs();
                    }
                }
            }

            _steamUser = EditorGUILayout.TextField("Account name", _steamUser);
            _appId     = EditorGUILayout.TextField("App ID", _appId);

            foreach (GameBuilder.Platform platform in SteamUploader.UploadablePlatforms)
                _depotIds[platform] = EditorGUILayout.TextField($"Depot ID ({platform})", _depotIds[platform]);

            _previewOnly = EditorGUILayout.ToggleLeft("Preview only (validate, publish nothing)", _previewOnly);
        }

        if (EditorGUI.EndChangeCheck())
            SavePrefs();

        var uploadable = platforms.Where(SteamUploader.CanUpload).ToList();

        if (platforms.Contains(GameBuilder.Platform.Linux))
            EditorGUILayout.HelpBox("Linux has no depot and is never uploaded.", MessageType.None);

        string problem = SteamUploader.Validate(uploadable, CurrentOptions());
        if (problem != null)
            EditorGUILayout.HelpBox(problem, MessageType.Warning);

        using (new EditorGUI.DisabledScope(problem != null))
        {
            string label = _previewOnly
                               ? $"Preview upload of {uploadable.Count} depot(s)"
                               : $"Upload {uploadable.Count} depot(s) to Steam";

            if (GUILayout.Button(label, GUILayout.Height(28)))
                RunUpload(uploadable);
        }

        EditorGUILayout.LabelField(
            "steamcmd reuses the session from one interactive login - no password is stored here.",
            EditorStyles.miniLabel);
    }

    private void DrawPlatformRow(GameBuilder.Platform platform)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                _enabled[platform] = EditorGUILayout.ToggleLeft(Label(platform), _enabled[platform], GUILayout.Width(160));

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Open", GUILayout.Width(45)))
                {
                    string dir = GameBuilder.RootFor(platform);
                    Directory.CreateDirectory(dir);
                    EditorUtility.RevealInFinder(dir);
                }
            }

            if (_enabled[platform])
                EditorGUILayout.LabelField(GameBuilder.OutputDirFor(platform, CurrentOptions()), EditorStyles.miniLabel);
        }
    }

    private void DrawSteamAppIdStatus()
    {
        bool devConfig = GameBuilder.IsDevelopmentConfig;
        EditorGUILayout.HelpBox(
            devConfig
                ? "GameConfig.developmentBuild is ON: steam_appid.txt will be included in the build."
                : "GameConfig.developmentBuild is OFF: steam_appid.txt will not be included.",
            devConfig ? MessageType.Warning : MessageType.None);

        if (GUILayout.Button("Select GameConfig", EditorStyles.miniButton))
        {
            var config = Resources.Load<GameConfig>("GameConfig");
            if (config != null)
                Selection.activeObject = config;
        }
    }

    private static string Label(GameBuilder.Platform platform) => platform switch
    {
        GameBuilder.Platform.Windows => "Windows (x64)",
        GameBuilder.Platform.MacOS   => "macOS (universal)",
        GameBuilder.Platform.Linux   => "Linux (x64)",
        _                            => platform.ToString()
    };

    private List<GameBuilder.Platform> SelectedPlatforms()
    {
        var platforms = new List<GameBuilder.Platform>();
        foreach (GameBuilder.Platform platform in AllPlatforms)
        {
            if (_enabled[platform])
                platforms.Add(platform);
        }
        return platforms;
    }

    private GameBuilder.Options CurrentOptions() => new GameBuilder.Options
    {
        Name        = _name,
        Version     = _version,
        Development = _development
    };

    private void RunBuild(List<GameBuilder.Platform> platforms)
    {
        SavePrefs();
        GameBuilder.Options options = CurrentOptions();

        EditorApplication.delayCall += () => GameBuilder.Build(options, platforms.ToArray());
    }

    private void RunUpload(List<GameBuilder.Platform> platforms)
    {
        SavePrefs();
        GameBuilder.Options options = CurrentOptions();

        if (!_previewOnly)
        {
            string depots = string.Join("\n", platforms.Select(
                                p => $"  {p} - depot {SteamUploadSettings.GetDepotId(p)}\n      {SteamUploader.ContentRootFor(p, options)}"));

            bool confirmed = EditorUtility.DisplayDialog(
                "Upload to Steam",
                $"This uploads to app {_appId} as {_steamUser}:\n\n{depots}\n\n"
                + "The build appears in Steamworks but is not set live on any branch.",
                "Upload", "Cancel");

            if (!confirmed)
                return;
        }

        bool preview = _previewOnly;
        EditorApplication.delayCall += () => SteamUploader.Upload(platforms, options, preview);
    }
}
