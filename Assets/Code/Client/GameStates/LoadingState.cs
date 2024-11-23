using System;
using System.Collections;
using System.Collections.Generic;
using Code.Client.Managers;
using UnityEngine;

namespace Code.Client.GameStates
{
    public class LoadingState : GameStateBase
    {
        public LoadingState(GameManager gameManager) : base(gameManager)
        {
        }

        public override void OnEnter(object context = null)
        {
            GameManager.UIManager.ShowLoadingScreen();            
            
            // load game data
            var tasks = new List<LoadingTask>
            {
                new("Loading game data...", LoadGameData)
            };
            GameManager.LoadingManager.StartLoading(tasks, OnLoadingComplete);
        }
        
        private void OnLoadingComplete(LoadingResult result)
        {
            GameManager.ChangeState<MainMenuState>();
        }
        
        
        private IEnumerator LoadGameData(Action<bool, string> callback)
        {
            // Load game data
            GameManager.UIManager.UpdateLoadingProgress(0.1f);
            for (var i = 0; i < 10; i++)
            {
                yield return new WaitForSeconds(0.1f);
                GameManager.UIManager.UpdateLoadingProgress(0.1f * (i + 1));
                GameManager.UIManager.UpdateLoadingMessage($"Loading game data... {i + 1}/10");
            }
        }
    }
}