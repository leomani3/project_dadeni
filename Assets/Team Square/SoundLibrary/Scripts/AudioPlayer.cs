using System.Collections;
using UnityEngine;

public class AudioPlayer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AudioSource m_audioSource;
    
    private Coroutine waitCoroutine;

    private void Reset()
    {
        m_audioSource = GetComponent<AudioSource>();
    }

    public void PlaySound(Sound sound, float? pitchOverride = null, float volumeMultiplier = 1f)
    {
        Stop();
        
        m_audioSource.clip = sound.clip;
        m_audioSource.loop = sound.loop;
        m_audioSource.volume = sound.volume * volumeMultiplier;
        m_audioSource.pitch = pitchOverride ?? sound.GetPitch();
        m_audioSource.outputAudioMixerGroup = sound.audioMixerGroup;
        
        m_audioSource.Play();
        
        if (!sound.loop)
            waitCoroutine = StartCoroutine(WaitForAudioToFinish());
    }
    
    public void Stop()
    {
        if (waitCoroutine != null)
        {
            StopCoroutine(waitCoroutine);
            waitCoroutine = null;
        }
        
        if (m_audioSource.isPlaying)
            m_audioSource.Stop();
    }
    
    private IEnumerator WaitForAudioToFinish()
    {
        while (m_audioSource.isPlaying)
            yield return null;
        
        SoundManager.Instance.ReturnPlayerToPool(this);
    }
}