using Code.Client.UI;
using UnityEngine;

namespace Code.Client.Managers
{
    
    public class UIManager
    {
        private GameManager gameManager;
        
        private MainMenuController _mainMenu;
        private LoadingScreen _loadingScreen;
        private PauseMenuController _pauseMenu;
        private GameHUDController _gameHUDController;
        
        public MainMenuController MainMenuController => _mainMenu;
        public PauseMenuController PauseMenuController => _pauseMenu;
        public GameHUDController GameHUDController => _gameHUDController;
        public LoadingScreen LoadingScreen => _loadingScreen;

        public UIManager(GameManager gameManager, MainMenuController mainMenu, LoadingScreen loadingScreen, PauseMenuController pauseMenu, GameHUDController gameHUDController)
        {
            this.gameManager = gameManager;

            // Initialize UI
            _mainMenu = mainMenu;
            _loadingScreen = loadingScreen;
            _pauseMenu = pauseMenu;
            _gameHUDController = gameHUDController;
            
            _mainMenu.Hide();
            _loadingScreen.Hide();
            _pauseMenu.Hide();
            _gameHUDController.Hide();
        }

        public void ShowMainMenu()
        {
            _mainMenu.Show();
        }

        public void HideMainMenu()
        {
            _mainMenu.Hide();
        }

        public void ShowLoadingScreen()
        {
            _loadingScreen.Show();
        }

        public void UpdateLoadingMessage(string message)
        {
            _loadingScreen.SetMessage(message);
        }

        public void UpdateLoadingProgress(float progress)
        {
            _loadingScreen.SetProgress(progress);
        }

        public void HideLoadingScreen()
        {
            _loadingScreen.Hide();
        }

        public void ShowGameHUD()
        {
            _gameHUDController.Show();
        }

        public void HideGameHUD()
        {
            _gameHUDController.Hide();
        }
        
        public void ShowPauseMenu()
        {
            _pauseMenu.Show();
        }
        
        public void HidePauseMenu()
        {
            _pauseMenu.Hide();
        }
    }
}