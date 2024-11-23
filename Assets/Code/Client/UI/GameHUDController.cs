using System;
using Code.Client.GameStates;
using Code.Client.Managers;
using TMPro;
using UnityEngine;

namespace Code.Client.UI
{
    /// <summary>
    /// Encapsulates the UI logic for the game HUD.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class GameHUDController : MonoBehaviour
    {

        public Action OnRespawn;
        public Action OnMainMenu;
        
        [SerializeField]
        private CanvasGroup deathScreen;
        [SerializeField]
        private TextMeshProUGUI deathText;

        private void Awake()
        {
            HideDeathScreen();
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
        
        public void OnPauseButtonClicked()
        {
        }
        
        public void ShowDeathScreen(string message)
        {
            deathText.text = message;
            deathScreen.alpha = 1;
            deathScreen.interactable = true;
            deathScreen.blocksRaycasts = true;
        }

        public void HideDeathScreen()
        {
            deathScreen.alpha = 0;
            deathScreen.interactable = false;
            deathScreen.blocksRaycasts = false;
        }

        public void OnRespawnButtonClicked()
        {
            OnRespawn?.Invoke();
            HideDeathScreen();
        }
        
        public void OnMainMenuButtonClicked()
        {
            OnMainMenu?.Invoke();
            HideDeathScreen();
        }
    }
}