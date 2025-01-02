using System;
using System.Collections;
using System.Collections.Generic;
using Code.Client.Managers;
using UnityEngine;

namespace Code.Client.GameStates
{
    public class MainMenuState : GameStateBase
    {
        public MainMenuState(GameManager gameManager) : base(gameManager) { }

        public override void OnEnter(object context = null)
        {
            // play music
            GameManager.AudioManager.PlayMusic("Home");
            
            var tasks = new List<LoadingTask>
            {
                new("Cleaning up game session...", CleanupGameSession),
            };

            GameManager.LoadingManager.StartLoading(tasks, OnLoadingComplete);
        }

        public override void OnExit()
        {
            Debug.Log("Exiting main menu state...");
            GameManager.UIManager.HideMainMenu();
        }

        private void OnLoadingComplete(LoadingResult result)
        {
            GameManager.UIManager.ShowMainMenu();
        }

        private IEnumerator CleanupGameSession(Action<bool, string> callback)
        {
            Debug.Log("Cleaning up game session...");
            yield return new WaitForSeconds(1f); // Simulate cleanup
            Debug.Log("Game session cleaned up.");
        }
    }
}