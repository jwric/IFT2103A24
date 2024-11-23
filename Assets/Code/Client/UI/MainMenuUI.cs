using Code.Client.Managers;
using UnityEngine;

namespace Code.Client.UI
{
    public class MainMenuUI : MenuPage
    {

        public void OnQuitButtonClicked()
        {
            Application.Quit();
        }
        
    }
}