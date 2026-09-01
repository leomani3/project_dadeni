#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public class ScreenshotWindow : EditorWindow
{
    private int screenshotWidth = 1920;
    private int screenshotHeight = 1080;
    private string screenshotName = "screenshot";
    private string screenshotPath = "Assets/Screenshots/";
    private int superSize = 1;

    [MenuItem("Window/Screenshot Tool")]
    public static void ShowWindow()
    {
        GetWindow<ScreenshotWindow>("Screenshot Tool");
    }

    private void OnGUI()
    {
        GUILayout.Label("Paramètres de Screenshot", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();

        // Dimensions
        GUILayout.Label("Dimensions", EditorStyles.boldLabel);
        screenshotWidth = EditorGUILayout.IntField("Largeur (pixels)", screenshotWidth);
        screenshotHeight = EditorGUILayout.IntField("Hauteur (pixels)", screenshotHeight);

        EditorGUILayout.Space();

        // Super Size (multiplier la résolution)
        GUILayout.Label("Qualité", EditorStyles.boldLabel);
        superSize = EditorGUILayout.IntSlider("Super Size", superSize, 1, 4);
        EditorGUILayout.HelpBox("Multiplie la résolution pour une meilleure qualité. 1 = résolution normale, 2 = 2x, etc.", MessageType.Info);

        EditorGUILayout.Space();

        // Nom et chemin
        GUILayout.Label("Fichier", EditorStyles.boldLabel);
        screenshotName = EditorGUILayout.TextField("Nom du fichier", screenshotName);
        
        EditorGUILayout.BeginHorizontal();
        screenshotPath = EditorGUILayout.TextField("Chemin de sauvegarde", screenshotPath);
        if (GUILayout.Button("Parcourir", GUILayout.Width(80)))
        {
            SelectFolder();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Bouton de capture
        if (GUILayout.Button("Prendre un Screenshot", GUILayout.Height(40)))
        {
            TakeScreenshot();
        }

        EditorGUILayout.Space();

        // Informations
        GUILayout.Label("Informations", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox($"Résolution finale: {screenshotWidth * superSize}x{screenshotHeight * superSize} pixels", MessageType.None);
        EditorGUILayout.HelpBox($"Dossier: {Path.GetFullPath(screenshotPath)}", MessageType.None);
    }

    private void SelectFolder()
    {
        string selectedPath = EditorUtility.OpenFolderPanel("Sélectionner le dossier de sauvegarde", screenshotPath, "");
        
        if (!string.IsNullOrEmpty(selectedPath))
        {
            // Convertir le chemin absolu en chemin relatif au projet si possible
            if (selectedPath.StartsWith(Application.dataPath))
            {
                screenshotPath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
            }
            else
            {
                screenshotPath = selectedPath;
            }
        }
    }

    private void TakeScreenshot()
    {
        // Créer le dossier s'il n'existe pas
        if (!Directory.Exists(screenshotPath))
        {
            Directory.CreateDirectory(screenshotPath);
        }

        // Créer un nom de fichier unique
        string fileName = $"{screenshotName}_{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";
        string fullPath = Path.Combine(screenshotPath, fileName);

        // Prendre le screenshot avec les paramètres définis
        ScreenCapture.CaptureScreenshot(fullPath, superSize);

        EditorUtility.DisplayDialog("Succès", $"Screenshot sauvegardé:\n{fullPath}", "OK");
        Debug.Log($"Screenshot pris: {fullPath}");
    }
}
#endif