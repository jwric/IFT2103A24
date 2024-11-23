using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Code
{
    public class Launcher : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI serverText;
        [SerializeField] private TextMeshProUGUI clientText;
        
        // Names of the server and client scenes
        [SerializeField] private string serverSceneName = "SampleScene";
        [SerializeField] private string clientSceneName = "ClientScene";

        bool isServer = false;
        
        bool hasChosen = false;
        
        void Start()
        {
            // Display options in the console (or implement a UI for selection)
            Debug.Log("Press 'S' to launch the Server");
            Debug.Log("Press 'C' to launch the Client");
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.S) && !hasChosen)
            {
                StartCoroutine(LaunchServer());
                hasChosen = true;
            }
            else if (Input.GetKeyDown(KeyCode.C) && !hasChosen)
            {
                StartCoroutine(LaunchClient());
                hasChosen = true;
            }
        }

        IEnumerator LaunchServer()
        {
            isServer = true;
            Debug.Log("Launching Server...");
            serverText.color = Color.green;
            yield return new WaitForSeconds(1);
            
            var scene = SceneManager.LoadSceneAsync(serverSceneName, LoadSceneMode.Single);

            yield return scene;
        }

        IEnumerator LaunchClient()
        {
            Debug.Log("Launching Client...");
            clientText.color = Color.green;
            yield return new WaitForSeconds(1);
            
            var scene = SceneManager.LoadSceneAsync(clientSceneName, LoadSceneMode.Single);

            yield return scene;
        }
    }
}