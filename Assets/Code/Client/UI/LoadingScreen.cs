using Code.Client.UI.Components;
using TMPro;
using UnityEngine;

namespace Code.Client.UI
{
    [RequireComponent(typeof(Canvas))]
    public class LoadingScreen : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _messageText;
        [SerializeField] private ProgressBar _progressBar;
        
        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void SetMessage(string message)
        {
            _messageText.text = message;
        }
        
        public void SetProgress(float progress)
        {
            _progressBar.SetProgress(progress);
        }
    }
}