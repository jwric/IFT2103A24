using Code.Client.Managers;
using TMPro;
using UnityEngine;
using Slider = UnityEngine.UI.Slider;
using Toggle = UnityEngine.UI.Toggle;

namespace Code.Client.UI
{
    public class SettingsPage : MenuPage
    {
        
        public Slider FPSSlider;
        public Toggle FullscreenToggle;
        public Toggle CSPToggle;
        public Toggle ReconcileToggle;
        public Toggle InterpolationToggle;

        public TextMeshProUGUI FPSValue;
        
        public TMP_InputField UsernameInput;
        
        protected override void OnAwake()
        {
            base.OnAwake();
            
        
        }

        private void Start()
        {
            FPSSlider.value = GameManager.Instance.Settings.TargetFramerate;
            FullscreenToggle.isOn = GameManager.Instance.Settings.IsFullscreen;
            FPSValue.text = $"{GameManager.Instance.Settings.TargetFramerate} FPS";
            UsernameInput.text = GameManager.Instance.Settings.Name;
            CSPToggle.isOn = GameManager.Instance.Settings.ClientSidePrediction;
            ReconcileToggle.isOn = GameManager.Instance.Settings.ServerReconciliation;
            InterpolationToggle.isOn = GameManager.Instance.Settings.EntityInterpolation;
            
            
            FPSSlider.onValueChanged.AddListener(value =>
            {
                GameManager.Instance.Settings.SetTargetFramerate((int) FPSSlider.value);
                FPSValue.text = $"{(int) FPSSlider.value} FPS";
            });
            
            FullscreenToggle.onValueChanged.AddListener(value =>
            {
                GameManager.Instance.Settings.SetFullScreen(FullscreenToggle.isOn);
            });
            
            UsernameInput.onEndEdit.AddListener(value =>
            {
                GameManager.Instance.Settings.Name = UsernameInput.text;
            });
            
            CSPToggle.onValueChanged.AddListener(value =>
            {
                GameManager.Instance.Settings.ClientSidePrediction = CSPToggle.isOn;
            });
            
            ReconcileToggle.onValueChanged.AddListener(value =>
            {
                GameManager.Instance.Settings.ServerReconciliation = ReconcileToggle.isOn;
            });
            
            InterpolationToggle.onValueChanged.AddListener(value =>
            {
                GameManager.Instance.Settings.EntityInterpolation = InterpolationToggle.isOn;
            });
        }
    }
}