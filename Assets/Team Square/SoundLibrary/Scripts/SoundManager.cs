using System.Collections.Generic;
using MyBox;
using UnityEngine;

public class SoundManager : Singleton<SoundManager>
{
    [SerializeField] private SoundLibrary soundLibrary;
    [SerializeField] private AudioPlayer audioPlayerPrefab;
    
    [Header("Sound Throttling")]
    [SerializeField] private int maxSimultaneousSameSounds = 3;
    [SerializeField] private float throttleResetTime = 0.1f; // Time window to track sounds

    private List<AudioPlayer> audioPlayerPool;
    private Queue<AudioPlayer> availablePlayers;
    
    // Dedicated players for music and ambient
    private AudioPlayer currentMusicPlayer;
    private AudioPlayer currentAmbientPlayer;
    
    // Throttling system
    private Dictionary<string, List<float>> soundTimestamps;

    private void Awake()
    {
        InitializePool();
        soundTimestamps = new Dictionary<string, List<float>>();
    }

    private void InitializePool()
    {
        audioPlayerPool = new List<AudioPlayer>();
        availablePlayers = new Queue<AudioPlayer>();
    }

    private AudioPlayer CreateNewAudioPlayer()
    {
        AudioPlayer player = Instantiate(audioPlayerPrefab, transform);
        player.gameObject.SetActive(false);
        audioPlayerPool.Add(player);
        availablePlayers.Enqueue(player);

        return player;
    }

    private AudioPlayer CreateDedicatedPlayer()
    {
        AudioPlayer player = Instantiate(audioPlayerPrefab, transform);
        player.gameObject.SetActive(false);
        audioPlayerPool.Add(player);
        // Note: NOT added to availablePlayers queue!

        return player;
    }

    private AudioPlayer GetAvailablePlayer()
    {
        if (availablePlayers.Count > 0)
            return availablePlayers.Dequeue();
        
        return CreateNewAudioPlayer();
    }

    public void ReturnPlayerToPool(AudioPlayer player)
    {
        if (player == null) return;
        
        // Don't return music or ambient players to the pool
        if (player == currentMusicPlayer || player == currentAmbientPlayer)
            return;

        player.gameObject.SetActive(false);
        availablePlayers.Enqueue(player);
    }

    private bool CanPlaySound(string soundKey)
    {
        float currentTime = Time.time;
        
        // Initialize the list for this sound if it doesn't exist
        if (!soundTimestamps.ContainsKey(soundKey))
        {
            soundTimestamps[soundKey] = new List<float>();
        }
        
        List<float> timestamps = soundTimestamps[soundKey];
        
        // Remove old timestamps outside the time window
        timestamps.RemoveAll(t => currentTime - t > throttleResetTime);
        
        // Check if we've reached the limit
        if (timestamps.Count >= maxSimultaneousSameSounds)
        {
            return false;
        }
        
        // Add current timestamp
        timestamps.Add(currentTime);
        return true;
    }

    public void PlaySound(SoundKeys soundKey, float? pitchOverride = null, float volumeMultiplier = 1f)
    {
        if (soundKey == SoundKeys._None)
            return;

        AudioPlayer player = GetAvailablePlayer();
        string soundKeyString = soundKey.ToString();
        
        if (!CanPlaySound(soundKeyString)) return;
        
        
        if (player != null)
        {
            player.gameObject.SetActive(true);
            player.PlaySound(soundLibrary.Sounds[soundKeyString], pitchOverride, volumeMultiplier);
        }
    }

    public void PlayMusic(SoundKeys soundKey)
    {
        if (currentMusicPlayer != null)
        {
            currentMusicPlayer.Stop();
            currentMusicPlayer.gameObject.SetActive(false);
        }
        
        if (currentMusicPlayer == null)
            currentMusicPlayer = CreateDedicatedPlayer();
        
        currentMusicPlayer.gameObject.SetActive(true);
        currentMusicPlayer.PlaySound(soundLibrary.Sounds[soundKey.ToString()]);
    }

    public void PlayAmbient(SoundKeys soundKey)
    {
        if (currentAmbientPlayer != null)
        {
            currentAmbientPlayer.Stop();
            currentAmbientPlayer.gameObject.SetActive(false);
        }
        
        if (currentAmbientPlayer == null)
            currentAmbientPlayer = CreateDedicatedPlayer();
        
        currentAmbientPlayer.gameObject.SetActive(true);
        currentAmbientPlayer.PlaySound(soundLibrary.Sounds[soundKey.ToString()]);
    }

    public void StopMusic()
    {
        if (currentMusicPlayer != null)
        {
            currentMusicPlayer.Stop();
            currentMusicPlayer.gameObject.SetActive(false);
        }
    }

    public void StopAmbient()
    {
        if (currentAmbientPlayer != null)
        {
            currentAmbientPlayer.Stop();
            currentAmbientPlayer.gameObject.SetActive(false);
        }
    }
}