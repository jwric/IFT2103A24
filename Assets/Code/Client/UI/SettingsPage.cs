using System;
using System.Collections;
using Code.Client.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Slider = UnityEngine.UI.Slider;
using Toggle = UnityEngine.UI.Toggle;

namespace Code.Client.UI
{
    public class SettingsPage : MenuPage
    {
        public RectTransform[] items;
        public VerticalLayoutGroup layoutGroup;
        
        public Slider MasterVolSlider;
        public Slider MusicVolSlider;
        public Slider AmbientVolSlider;
        public Slider SFXVolSlider;

        public Slider FPSSlider;
        public Toggle FullscreenToggle;
        public Toggle CSPToggle;
        public Toggle ReconcileToggle;
        public Toggle InterpolationToggle;

        public TextMeshProUGUI FPSValue;

        public TMP_InputField UsernameInput;

        public Button PrimColorButton;
        public Button SecColorButton;
        public TextMeshProUGUI PrimColorText;
        public TextMeshProUGUI SecColorText;

        public Color currentSelectingColor;
        public bool isSelectingPrimColor;
        public bool isSelectingSecColor;

        public FlexibleColorPicker ColorPicker;

        private bool hasInitialized = false;
        private Vector3[] initialPositions;

        protected override void OnAwake()
        {
            base.OnAwake();

            isSelectingPrimColor = false;
            isSelectingSecColor = false;
        }

        private void OnDisable()
        {
            isSelectingSecColor = false;
            isSelectingPrimColor = false;
            ColorPicker.gameObject.SetActive(false);
        }

        private void Start()
        {
            MasterVolSlider.value = GameManager.Instance.Settings.MasterVolume;
            MusicVolSlider.value = GameManager.Instance.Settings.MusicVolume;
            AmbientVolSlider.value = GameManager.Instance.Settings.AmbientVolume;
            SFXVolSlider.value = GameManager.Instance.Settings.SFXVolume;

            FPSSlider.value = GameManager.Instance.Settings.TargetFramerate;
            FullscreenToggle.isOn = GameManager.Instance.Settings.IsFullscreen;
            FPSValue.text = $"{GameManager.Instance.Settings.TargetFramerate} FPS";
            UsernameInput.text = GameManager.Instance.Settings.Name;
            CSPToggle.isOn = GameManager.Instance.Settings.ClientSidePrediction;
            ReconcileToggle.isOn = GameManager.Instance.Settings.ServerReconciliation;
            InterpolationToggle.isOn = GameManager.Instance.Settings.EntityInterpolation;

            MasterVolSlider.onValueChanged.AddListener(value =>
            {
                GameManager.Instance.Settings.SetMasterVolume(MasterVolSlider.value);
            });

            MusicVolSlider.onValueChanged.AddListener(value =>
            {
                GameManager.Instance.Settings.SetMusicVolume(MusicVolSlider.value);
            });

            AmbientVolSlider.onValueChanged.AddListener(value =>
            {
                GameManager.Instance.Settings.SetAmbientVolume(AmbientVolSlider.value);
            });

            SFXVolSlider.onValueChanged.AddListener(value =>
            {
                GameManager.Instance.Settings.SetSFXVolume(SFXVolSlider.value);
            });

            FPSSlider.onValueChanged.AddListener(value =>
            {
                GameManager.Instance.Settings.SetTargetFramerate((int)FPSSlider.value);
                FPSValue.text = $"{(int)FPSSlider.value} FPS";
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

            PrimColorText.text = $"{ColorUtility.ToHtmlStringRGB(GameManager.Instance.Settings.PrimaryColor)}";
            SecColorText.text = $"{ColorUtility.ToHtmlStringRGB(GameManager.Instance.Settings.SecondaryColor)}";
            PrimColorButton.image.color = GameManager.Instance.Settings.PrimaryColor;
            SecColorButton.image.color = GameManager.Instance.Settings.SecondaryColor;

            PrimColorButton.onClick.AddListener(() =>
            {
                isSelectingSecColor = false;
                if (isSelectingPrimColor)
                {
                    ColorPicker.gameObject.SetActive(false);
                    isSelectingPrimColor = false;
                }
                else
                {
                    ColorPicker.gameObject.SetActive(true);
                    isSelectingPrimColor = true;
                    ColorPicker.SetColor(GameManager.Instance.Settings.PrimaryColor);
                }
            });

            SecColorButton.onClick.AddListener(() =>
            {
                isSelectingPrimColor = false;
                if (isSelectingSecColor)
                {
                    ColorPicker.gameObject.SetActive(false);
                    isSelectingSecColor = false;
                }
                else
                {
                    ColorPicker.gameObject.SetActive(true);
                    isSelectingSecColor = true;
                    ColorPicker.SetColor(GameManager.Instance.Settings.SecondaryColor);
                }
            });

            ColorPicker.onColorChange.AddListener(color =>
            {
                if (isSelectingPrimColor)
                {
                    Debug.Log("Setting prim color");
                    GameManager.Instance.Settings.PrimaryColor = color;
                    PrimColorText.text = $"{ColorUtility.ToHtmlStringRGB(color)}";
                    PrimColorButton.image.color = color;
                }
                else if (isSelectingSecColor)
                {
                    Debug.Log("Setting sec color");
                    GameManager.Instance.Settings.SecondaryColor = color;
                    SecColorText.text = $"{ColorUtility.ToHtmlStringRGB(color)}";
                    SecColorButton.image.color = color;
                }
            });

            ColorPicker.gameObject.SetActive(false);
        }

        public override void Show()
        {
            if (!hasInitialized)
            {
                hasInitialized = true;
                initialPositions = new Vector3[items.Length];
                for (int i = 0; i < items.Length; i++)
                {
                    initialPositions[i] = items[i].anchoredPosition;
                }
            }

            foreach (var item in items)
            {
                item.gameObject.SetActive(false); // Ensure items are invisible initially
            }

            base.Show();
            StartCoroutine(SlideInItems());
        }

        private IEnumerator SlideInItems()
        {
            layoutGroup.enabled = false; // Disable the layout group to prevent interference

            float overlap = 0.05f; // Time overlap between animations
            float duration = 0.5f; // Duration of the animation for each item

            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                item.gameObject.SetActive(true); // Make item visible

                Vector3 startPos = initialPositions[i];
                startPos.x = -Screen.width;

                Vector3 targetPos = initialPositions[i];

                StartCoroutine(SlideItem(item, startPos, targetPos, duration));

                yield return new WaitForSeconds(overlap);
            }

            yield return new WaitForSeconds(duration); // Ensure all animations complete

            layoutGroup.enabled = true; // Re-enable the layout group
            layoutGroup.CalculateLayoutInputVertical(); // Force layout recalculation
        }

        private IEnumerator SlideItem(RectTransform item, Vector3 startPos, Vector3 targetPos, float duration)
        {
            item.anchoredPosition = startPos;

            float elapsedTime = 0;
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / duration);
                t = Mathf.SmoothStep(0, 1, t); // Simple easing

                item.anchoredPosition = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }

            item.anchoredPosition = targetPos;
        }
    }
}
