using Code.Client.Managers;
using UnityEngine;

namespace Code.Client.UI
{
    /// <summary>
    /// Encapsulates the UI logic for the game HUD.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class GameHUDController : MonoBehaviour
    {
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
    }
}