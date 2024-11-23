using System;
using UnityEngine;

namespace Code.Client.Managers
{
    public class Settings
    {
        public bool IsFullscreen { get; set; }
        public int TargetFramerate { get; set; }
        public bool ClientSidePrediction { get; set; }
        public bool ServerReconciliation { get; set; }
        public bool EntityInterpolation { get; set; }
        
        public string Name { get; set; }
        
        public Settings()
        {
            TargetFramerate = 60;
            IsFullscreen = false;
            ClientSidePrediction = true;
            ServerReconciliation = true;
            EntityInterpolation = true;
            
            System.Random r = new System.Random();

            Name = Environment.MachineName + " " + r.Next(100000);
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