using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Code.Client.UI
{
  [RequireComponent(typeof(Canvas))]
    [DisallowMultipleComponent]
    public class MenuController : MonoBehaviour
    {
        [SerializeField]
        private MenuPage _startPage;

        [SerializeField]
        private GameObject _firstFocusedElement;

        private Canvas _rootCanvas;

        private Stack<MenuPage> _pageStack = new Stack<MenuPage>();

        private Coroutine _visibilityAnimation;

        private bool _isAnimatingVisibility;

        private void Awake()
        {
            _rootCanvas = GetComponent<Canvas>();
            if (!_rootCanvas.TryGetComponent(out CanvasGroup canvasGroup))
            {
                canvasGroup = _rootCanvas.gameObject.AddComponent<CanvasGroup>();
            }
            canvasGroup.alpha = 0;
        }

        private void Start()
        {
            if (_firstFocusedElement != null)
            {
                EventSystem.current.SetSelectedGameObject(_firstFocusedElement);
            }

            if (_startPage != null)
            {
                PushPage(_startPage, true);
            }
        }

        public void Show()
        {
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true); // Ensure the GameObject is active
            }

            if (_visibilityAnimation != null)
            {
                StopCoroutine(_visibilityAnimation);
                // reset everything
                _rootCanvas.GetComponent<CanvasGroup>().alpha = 0;
                _rootCanvas.GetComponent<CanvasGroup>().interactable = false;
                _rootCanvas.GetComponent<CanvasGroup>().blocksRaycasts = false;
                
                _isAnimatingVisibility = false;
                Debug.Log("Stopped visibility animation for game object: " + gameObject.name);
            }

            _visibilityAnimation = StartCoroutine(AnimateVisibility(true));
        }

        public void Hide()
        {
            if (_visibilityAnimation != null)
                StopCoroutine(_visibilityAnimation);

            _visibilityAnimation = StartCoroutine(AnimateVisibility(false));
        }
        
        public void HideInstantly()
        {
            if (_visibilityAnimation != null)
                StopCoroutine(_visibilityAnimation);

            _rootCanvas.GetComponent<CanvasGroup>().alpha = 0;
            _rootCanvas.GetComponent<CanvasGroup>().interactable = false;
            _rootCanvas.GetComponent<CanvasGroup>().blocksRaycasts = false;
            gameObject.SetActive(false);
        }

        private IEnumerator AnimateVisibility(bool visible)
        {
            if (_isAnimatingVisibility)
                yield break;

            _isAnimatingVisibility = true;
            float duration = 0.5f;
            float elapsedTime = 0;
            var canvasGroup = _rootCanvas.GetComponent<CanvasGroup>();

            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;

            float startAlpha = canvasGroup.alpha;
            float targetAlpha = visible ? 1 : 0;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / duration);
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, MenuPage.EaseOutCubic(t));
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;

            if (!visible)
            {
                gameObject.SetActive(false); // Deactivate only after animation completes
            }

            _isAnimatingVisibility = false;
        }

        public bool IsTopPage(MenuPage page)
        {
            return _pageStack.Count > 0 && _pageStack.Peek() == page;
        }

        public bool IsInStack(MenuPage page)
        {
            return _pageStack.Contains(page);
        }

        public void PushPage(MenuPage page, bool instant = false)
        {
            if (_pageStack.Count > 0)
            {
                var previousPage = _pageStack.Peek();
                if (previousPage.exitOnNewPage)
                {
                    if (instant)
                        previousPage.HideInstantly();
                    else
                        previousPage.Hide();
                }
            }

            page.SetMenuController(this);
            if (instant)
                page.ShowInstantly();
            else
                page.Show();

            _pageStack.Push(page);
        }
        
        public void PushPage(MenuPage page)
        {
            PushPage(page, false);
        }

        public void PopPage(bool instant = false)
        {
            if (_pageStack.Count > 0)
            {
                var page = _pageStack.Pop();
                if (instant)
                    page.HideInstantly();
                else
                    page.Hide();
            }

            if (_pageStack.Count > 0)
            {
                var currentPage = _pageStack.Peek();
                if (instant)
                    currentPage.ShowInstantly();
                else
                    currentPage.Show();
            }
        }
        
        public void PopPage()
        {
            PopPage(false);
        }
    }
}
