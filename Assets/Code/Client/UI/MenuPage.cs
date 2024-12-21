using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace Code.Client.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class MenuPage : MonoBehaviour
    {
        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;
        private Coroutine _currentAnimation;

        protected MenuController _menuController;

        public bool exitOnNewPage = true;
        public float animationDuration = 0.5f;
        public Func<float, float> easingFunction = EaseOutCubic;

        public void SetMenuController(MenuController menuController)
        {
            _menuController = menuController;
        }

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();

            OnAwake();
            HideInstantly();
        }

        protected virtual void OnAwake() { }

        public void HideInstantly()
        {
            _canvasGroup.alpha = 0;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }
        
        public void ShowInstantly()
        {
            _canvasGroup.alpha = 1;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;
        }

        public void Hide()
        {
            if (_currentAnimation != null)
                StopCoroutine(_currentAnimation);

            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
            _currentAnimation = StartCoroutine(AnimateAlpha(0, () =>
            {

            }));
        }

        public virtual void Show()
        {
            if (_currentAnimation != null)
                StopCoroutine(_currentAnimation);

            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;
            _currentAnimation = StartCoroutine(AnimateAlpha(1, () =>
            {

            }));
        }
        
        public bool HasFinishedAnimation()
        {
            return _currentAnimation == null;
        }

        private IEnumerator AnimateAlpha(float targetAlpha, Action onComplete)
        {
            float startAlpha = _canvasGroup.alpha;
            float elapsedTime = 0;

            while (elapsedTime < animationDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / animationDuration);
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, easingFunction(t));
                yield return null;
            }

            _canvasGroup.alpha = targetAlpha;
            onComplete?.Invoke();
        }


        public static float EaseOutCubic(float t)
        {
            return 1 - Mathf.Pow(1 - t, 3);
        }

        public static float EaseInOutQuad(float t)
        {
            return t < 0.5f ? 2 * t * t : 1 - Mathf.Pow(-2 * t + 2, 2) / 2;
        }

        public static float Linear(float t)
        {
            return t;
        }
    }
}