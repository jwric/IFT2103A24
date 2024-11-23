using System.Net;
using Code.Client.GameStates;
using Code.Client.Managers;
using LiteNetLib;
using TMPro;
using UnityEngine;

namespace Code.Client.UI
{
    public class ConnectionPage : MenuPage
    {

        [SerializeField]
        private TMP_InputField _ipInputField;
        [SerializeField]
        private TMP_InputField _portInputField;

        protected override void OnAwake()
        {
            base.OnAwake();
            
            // default ip
            _ipInputField.text = NetUtils.GetLocalIp(LocalAddrType.IPv4);
            // default port
            _portInputField.text = "10515";
        }

        public void OnStartGameButtonClicked()
        {
            var ip = _ipInputField.text;
            var port = int.Parse(_portInputField.text);
            GameManager.Instance.ChangeState<InGameState>(new GameStartContext { Ip = _ipInputField.text, Port = port });
        }
        
    }
}