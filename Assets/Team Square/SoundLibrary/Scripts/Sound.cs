using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

public enum PitchMode
{
    FixPitch,
    RandomPitch
}


[Serializable]
public class Sound
{
    [HorizontalGroup, HideLabel] public AudioClip clip;
    [HorizontalGroup, LabelWidth(50)] public bool loop;
    [HorizontalGroup, Range(0, 1)] public float volume = 1f;
    [HorizontalGroup, HideLabel] public PitchMode pitchMode;
    [HorizontalGroup, HideLabel, ShowIf("@pitchMode == PitchMode.RandomPitch"), MinMaxSlider(0.5f, 3f, true)] public Vector2 randomPitchRange = new Vector2(0.9f, 1.1f);
    [HorizontalGroup, HideLabel, HideIf("@pitchMode == PitchMode.RandomPitch"), Range(0.5f, 3f)] public float fixedPitch = 1f;
    [HorizontalGroup, HideLabel] public AudioMixerGroup audioMixerGroup;

    public float GetPitch()
    {
        if (pitchMode == PitchMode.RandomPitch)
            return UnityEngine.Random.Range(randomPitchRange.x, randomPitchRange.y);
        else
            return fixedPitch;
    }

#if UNITY_EDITOR
    private static AudioSource previewAudioSource;
#endif

    [HorizontalGroup, Button]
    public void preview()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            if (clip != null)
            {
                StopAllPreviews();
                PlayClipInEditor(clip, volume);
            }
        }
#endif
    }

#if UNITY_EDITOR
    private void PlayClipInEditor(AudioClip clip, float volume)
    {
        // Create a preview audio source if it doesn't exist
        if (previewAudioSource == null)
        {
            GameObject previewObject = EditorUtility.CreateGameObjectWithHideFlags(
                "Audio Preview", 
                HideFlags.HideAndDontSave
            );
            previewAudioSource = previewObject.AddComponent<AudioSource>();
        }

        previewAudioSource.clip = clip;
        previewAudioSource.volume = volume;
        previewAudioSource.pitch = GetPitch();
        previewAudioSource.loop = false;
        previewAudioSource.Play();
    }
    
    private void StopAllPreviews()
    {
        // Stop the custom preview audio source
        if (previewAudioSource != null && previewAudioSource.isPlaying)
        {
            previewAudioSource.Stop();
        }

        // Also stop any AudioUtil previews as a fallback
        var unityEditorAssembly = typeof(AudioImporter).Assembly;
        var audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
        var method = audioUtilClass.GetMethod(
            "StopAllPreviewClips",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public
        );
        
        if (method != null)
        {
            method.Invoke(null, null);
        }
    }
#endif
}