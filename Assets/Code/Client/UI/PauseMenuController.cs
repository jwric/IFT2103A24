using System;
using Code.Client.GameStates;
using Code.Client.Managers;
using UnityEngine;

namespace Code.Client.UI
{
    public class PauseMenuController : MenuController
    {
        public Action OnResume;
        public Action OnMainMenu;
        public Action OnQuit;
        
        public void OnResumeButtonClicked()
        {
            OnResume?.Invoke();
        }

        public void OnMainMenuButtonClicked()
        {
            OnMainMenu?.Invoke();
        }
        
        public void OnQuitButtonClicked()
        {
            OnQuit?.Invoke();
        }
    }
}