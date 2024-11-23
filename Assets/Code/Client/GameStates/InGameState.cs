using System;
using System.Collections;
using System.Collections.Generic;
using Code.Client.Managers;
using LiteNetLib;
using UnityEngine;

namespace Code.Client.GameStates
{
    public class InGameState : GameStateBase
    {
        
        public bool IsReady { get; private set; }
        
        private string ip;
        private int port;
        
        // game state ---->
        private Logic.ClientLogic _clientLogic;
        private bool _isPaused;
        // game state <----

        public InGameState(GameManager gameManager) : base(gameManager)
        {
        }

        public override void OnEnter(object context = null)
        {
            if (context is not GameStartContext startContext) return;
            
            ip = startContext.Ip;
            port = startContext.Port;
            GameManager.UIManager.ShowLoadingScreen();

            var tasks = new List<LoadingTask>
            {
                new("Connecting to server...", ConnectToServerTask)
            };
                
            GameManager.LoadingManager.StartLoading(tasks, OnLoadingComplete);
        }
        
        private IEnumerator ConnectToServerTask(Action<bool, string> onResult)
        {
            bool hasFinished = false;
            bool connSuccess = false;
            string message = "Connecting...";

            // Attempt to connect asynchronously
            yield return GameManager.NetworkManager.ConnectAsync(ip, port, (success, resultMessage) =>
            {
                hasFinished = true;
                connSuccess = success;
                message = resultMessage;
            });

            // Wait until the connection finishes
            while (!hasFinished)
            {
                yield return null;
            }

            // Report the result
            if (connSuccess)
            {
                Debug.Log("Connection successful!");
                onResult?.Invoke(true, message);
            }
            else
            {
                Debug.LogError("Connection failed.");
                onResult?.Invoke(false, message);
            }
        }

        public void OnDisconnect(DisconnectInfo info)
        {
            GameManager.ChangeState<MainMenuState>();
        }
        
        private void OnLoadingComplete(LoadingResult result)
        {
            Debug.Log($"Loading complete {result.Success}");
            if (result.Success)
            {
                IsReady = true;
                
                // subscribe to pause menu events
                var pauseMenu = GameManager.UIManager.PauseMenuController;
                pauseMenu.OnResume = TogglePause;
                pauseMenu.OnQuit = () =>
                {
                    GameManager.QuitGame();
                };
                pauseMenu.OnMainMenu = () =>
                {
                    TogglePause();
                    GameManager.ChangeState<MainMenuState>();
                };
                
                // show game HUD
                GameManager.UIManager.ShowGameHUD();
                

                

                var hudController = GameManager.UIManager.GameHUDController;

                // sub to disconnect event
                GameManager.NetworkManager.OnDisconnect += OnDisconnect;
                
                // todo: this is a horrible way to manage prefabs, but it's a quick patch for now
                var playerViewPrefab = GameManager.ClientPlayerViewPrefab;
                var remotePlayerViewPrefab = GameManager.RemotePlayerViewPrefab;
                var ShootPrefab = GameManager.ShootEffectPrefab;
                var RewindPrefab = GameManager.RewindGO;
                var camera = GameManager.Camera;
                _clientLogic = new Logic.ClientLogic();
                _clientLogic.Init(camera, playerViewPrefab, remotePlayerViewPrefab, ShootPrefab, RewindPrefab, hudController);
                
                // sub to death screen events
                hudController.OnRespawn = () =>
                {
                    _clientLogic.Destroy();
                    _clientLogic.Init(camera, playerViewPrefab, remotePlayerViewPrefab, ShootPrefab, RewindPrefab, hudController);
                    
                };
                
                hudController.OnMainMenu = () =>
                {
                    GameManager.ChangeState<MainMenuState>();
                };
            }
            else
            {
                GameManager.ChangeState<MainMenuState>();
            }
        }

        public override void OnExit()
        {
            GameManager.UIManager.HideGameHUD();
            
            IsReady = false;
            
            // unregister pause menu events
            var pauseMenu = GameManager.UIManager.PauseMenuController;
            pauseMenu.OnResume = null;
            pauseMenu.OnQuit = null;
            pauseMenu.OnMainMenu = null;
            
            // unregister death screen events
            var hudController = GameManager.UIManager.GameHUDController;
            hudController.OnRespawn = null;
            hudController.OnMainMenu = null;
            
            // unsubscribe from disconnect event
            GameManager.NetworkManager.OnDisconnect -= OnDisconnect;
            
            // cleanup game state
            _clientLogic?.Destroy();
            _clientLogic = null;

            // cleanup
            GameManager.NetworkManager.Disconnect();
         
        }

        public override void Update()
        {

            // Handle game-specific updates
            
            // pause menu check
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                TogglePause();
            }
            
            if (IsReady)
            {
                _clientLogic.Update();
            }
        }
        
        private void TogglePause()
        {
            _isPaused = !_isPaused;
            
            // Show/hide pause menu
            if (_isPaused)
            {
                GameManager.UIManager.ShowPauseMenu();
            }
            else
            {
                GameManager.UIManager.HidePauseMenu();
            }
            Time.timeScale = _isPaused ? 0 : 1;
        }

        public override void FixedUpdate()
        {
            if (IsReady)
            {
                _clientLogic.FixedUpdate();
            }
            // Handle game-specific physics updates
        }
    }

    public class GameStartContext
    {
        public string Ip { get; set; }
        public int Port { get; set; }
    }

    public class DisconnectContext { }
}