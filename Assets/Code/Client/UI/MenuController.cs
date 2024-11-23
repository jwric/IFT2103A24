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
        
        private void Awake()
        {
            _rootCanvas = GetComponent<Canvas>();
        }
        
        private void Start()
        {
            if (_firstFocusedElement != null)
            {
                EventSystem.current.SetSelectedGameObject(_firstFocusedElement);
            }
            
            if (_startPage != null)
            {
                PushPage(_startPage);
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
        
        public void Show()
        {
            gameObject.SetActive(true);
        }
        
        public bool IsTopPage(MenuPage page)
        {
            return _pageStack.Count > 0 && _pageStack.Peek() == page;
        }
        
        public bool IsInStack(MenuPage page)
        {
            return _pageStack.Contains(page);
        }
        
        public void PushPage(MenuPage page)
        {
            
            if (_pageStack.Count > 0)
            {
                var previousPage = _pageStack.Peek();
                if (previousPage.exitOnNewPage)
                {
                    previousPage.Hide();
                }
            }
            
            page.SetMenuController(this);
            page.Show();
            _pageStack.Push(page);
        }
        
        public void PopPage()
        {
            if (_pageStack.Count > 0)
            {
                var page = _pageStack.Pop();
                page.Hide();
            }
            
            if (_pageStack.Count > 0)
            {
                var currentPage = _pageStack.Peek();
                currentPage.Show();
            }
        }
        
    }
}