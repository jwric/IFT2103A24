using System;
using UnityEngine;

namespace Code.Client.Managers
{
    public class Settings
    {
        public float MasterVolume { get; set; }
        public float MusicVolume { get; set; }
        public float AmbientVolume { get; set; }
        public float SFXVolume { get; set; }
        public bool IsFullscreen { get; set; }
        public int TargetFramerate { get; set; }
        public bool ClientSidePrediction { get; set; }
        public bool ServerReconciliation { get; set; }
        public bool EntityInterpolation { get; set; }
        
        public event Action OnVolumeChanged;
        
        public string Name { get; set; }
        
        public Settings()
        {
            MasterVolume = 1;
            MusicVolume = 0.6f;
            AmbientVolume = 1;
            SFXVolume = 1;
            TargetFramerate = 60;
            IsFullscreen = false;
            ClientSidePrediction = true;
            ServerReconciliation = true;
            EntityInterpolation = true;
            
            System.Random r = new System.Random();

            Name = Environment.MachineName + " " + r.Next(100000);
        }
        
        public void SetMasterVolume(float volume)
        {
            MasterVolume = volume;
            OnVolumeChanged?.Invoke();
        }
        
        public void SetMusicVolume(float volume)
        {
            MusicVolume = volume;
            OnVolumeChanged?.Invoke();
        }
        
        public void SetAmbientVolume(float volume)
        {
            AmbientVolume = volume;
            OnVolumeChanged?.Invoke();
        }
        
        public void SetSFXVolume(float volume)
        {
            SFXVolume = volume;
            OnVolumeChanged?.Invoke();
        }
        
        public void SetFullScreen(bool isFullscreen)
        {
            IsFullscreen = isFullscreen;
            Screen.fullScreen = isFullscreen;
        }

        public void SetTargetFramerate(int targetFramerate)
        {
            TargetFramerate = targetFramerate;
            Application.targetFrameRate = targetFramerate;
        }
    }
}