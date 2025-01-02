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

        [SerializeField]
        private TextMeshProUGUI healthText;

        private Coroutine currentTween;
        private const float fadeDuration = 0.5f; // Duration of the fade animation in seconds

        private void Awake()
        {
            HideDeathScreenInstant();
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
            deathScreen.interactable = true;
            deathScreen.blocksRaycasts = true;

            // Start fade-in animation
            if (currentTween != null)
                StopCoroutine(currentTween);
            currentTween = StartCoroutine(FadeCanvasGroup(deathScreen, 0, 1, fadeDuration));
        }

        public void HideDeathScreen()
        {
            deathScreen.interactable = false;
            deathScreen.blocksRaycasts = false;

            // Start fade-out animation
            if (currentTween != null)
                StopCoroutine(currentTween);
            currentTween = StartCoroutine(FadeCanvasGroup(deathScreen, 1, 0, fadeDuration));
        }

        private void HideDeathScreenInstant()
        {
            deathScreen.alpha = 0;
            deathScreen.interactable = false;
            deathScreen.blocksRaycasts = false;
        }

        private System.Collections.IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float startAlpha, float endAlpha, float duration)
        {
            float elapsedTime = 0;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / duration;

                // Apply easing (smooth step)
                t = t * t * (3f - 2f * t);

                canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
                yield return null;
            }

            // Ensure the final alpha is set
            canvasGroup.alpha = endAlpha;
        }

        public void UpdateHealth(int health)
        {
            health = Mathf.Max(0, health);
            healthText.text = $"Health: {health}";
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
