using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Code.Client.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class MenuPage : MonoBehaviour
    {
        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;
        
        protected MenuController _menuController;
        
        public void SetMenuController(MenuController menuController)
        {
            _menuController = menuController;
        }

        public bool exitOnNewPage = true;
        
        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();
            
            OnAwake();
            Hide();
        }
        
        protected virtual void OnAwake() { }

        public void Hide()
        {
            _canvasGroup.alpha = 0;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }
        
        public void Show()
        {
            _canvasGroup.alpha = 1;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;
        }
    }
}