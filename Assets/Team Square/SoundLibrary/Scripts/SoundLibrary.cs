using System.IO;
using System.Text;
using Sirenix.OdinInspector;
using UnityEngine;
using Utils;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "SoundLibrary", menuName = "Sound Library")]
public class SoundLibrary : ScriptableObject
{
    [SerializeField] private SerializableDictionary<string, Sound> m_sounds = new SerializableDictionary<string, Sound>();
    
    public SerializableDictionary<string, Sound> Sounds => m_sounds;
    
    private const string ENUM_FILE_PATH = "Assets/Team Square/SoundLibrary/Scripts/SoundKeys.cs";
    
    [Button]
    private void GenerateKeys()
    {
        // Ensure the directory exists
        string directory = Path.GetDirectoryName(ENUM_FILE_PATH);
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);
    
        StringBuilder sb = new StringBuilder();
    
        // Start enum declaration
        string indent = "";
        sb.AppendLine($"{indent}public enum SoundKeys");
        sb.AppendLine($"{indent}{{");
    
        // Add None as first value
        sb.AppendLine($"{indent}    _None,");
    
        // Add enum values
        int index = 0;
        int totalCount = m_sounds.Count;
    
        foreach (var kvp in m_sounds)
        {
            string cleanValue = kvp.Key.Trim().Replace(" ", "");
            sb.Append($"{indent}    {cleanValue}");
        
            if (index < totalCount - 1)
                sb.Append(",");
        
            sb.AppendLine();
            index++;
        }
    
        // Close enum
        sb.AppendLine($"{indent}}}");
    
        // Write to file (creates if doesn't exist, overwrites if it does)
        File.WriteAllText(ENUM_FILE_PATH, sb.ToString());
    
#if UNITY_EDITOR
        AssetDatabase.Refresh();
#endif
    }
}