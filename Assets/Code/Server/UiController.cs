using System;
using System.Collections;
using System.Linq;
using System.Net;
using Code.Server;
using LiteNetLib;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Newtonsoft.Json;

namespace Code.Client
{
    public class SystemServerInitRequest
    {
        public string IpAddress { get; set; }
        public int Port { get; set; }
        public string ServerName { get; set; }
        public string GameVersion { get; set; }
    }
    
    public class UiController : MonoBehaviour
    {
        [SerializeField] private GameObject _uiObject;
        [SerializeField] private ServerLogic _serverLogic;
        [SerializeField] private InputField _ipField;
        [SerializeField] private InputField _portField;

        private IPAddress _bindAddress = IPAddress.Parse(NetUtils.GetLocalIp(LocalAddrType.IPv4));
        private int _serverPort = 10515;
        bool _isServerMode = false;

        private void Awake()
        {
            ParseCommandLineArgs();

            _ipField.text = _bindAddress.ToString();
            _portField.text = _serverPort.ToString();
            _ipField.onEndEdit.AddListener(s => IPAddress.TryParse(s, out _bindAddress));
            _portField.onEndEdit.AddListener(s => int.TryParse(s, out _serverPort));
        }
        
        private void Start()
        {
            if (_isServerMode)
            {
                Application.targetFrameRate = 60;
                
                var token = Environment.GetEnvironmentVariable("SYSTEM_SERVER_TOKEN");

                Console.WriteLine($"Starting in server mode on port {_serverPort}");
                if (string.IsNullOrEmpty(token))
                {
                    Console.WriteLine("Server token not found");
                }
                else
                {
                    Console.WriteLine($"Server token: {token}");
                }
                
                // Send server init request
                var request = new SystemServerInitRequest
                {
                    IpAddress = _bindAddress.ToString(),
                    Port = _serverPort,
                    ServerName = Application.productName,
                    GameVersion = Application.version
                };
                StartCoroutine(PostRequest("http://localhost:5000/srv/systemserver/init", request, token));

                OnHostClick();
            }
        }
        
        
        IEnumerator PostRequest(string url, SystemServerInitRequest request, string token)
        {
            string jsonData = JsonConvert.SerializeObject(request);
            
            UnityWebRequest uwr = UnityWebRequest.Post(url, jsonData, "application/json");
            uwr.SetRequestHeader("Authorization", $"Bearer {token}");
            yield return uwr.SendWebRequest();

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Debug.Log("Error While Sending: " + uwr.error);
            }
        }

        private void ParseCommandLineArgs()
        {
            string[] args = Environment.GetCommandLineArgs();

            // Check for specific server arguments
            if (args.Contains("-server"))
            {
                _isServerMode = true;

                // Port argument
                int portIndex = Array.IndexOf(args, "-port");
                if (portIndex >= 0 && portIndex < args.Length - 1 && int.TryParse(args[portIndex + 1], out int port))
                {
                    _serverPort = port;
                }

                // IP/bind address argument
                int addressIndex = Array.IndexOf(args, "-ip");
                if (addressIndex >= 0 && addressIndex < args.Length - 1)
                {
                    _bindAddress = IPAddress.TryParse(args[addressIndex + 1], out IPAddress address) ? address : _bindAddress;
                }
            }
        }
        
        public void OnHostClick()
        {
            _serverLogic.enabled = true;
            _serverLogic.StartServer(_bindAddress, _serverPort);
            _uiObject.SetActive(false);
        }
    }
}
