using System;
using System.Collections.Generic;
using Code.Client.GameStates;
using Code.Client.UI;
using Code.Server;
using Code.Shared;
using UnityEngine;

namespace Code.Client.Managers
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        
        
        // Managers
        public UIManager UIManager { get; private set; }
        public NetworkManager NetworkManager { get; private set; }
        public AudioManager AudioManager { get; private set; }
        public LoadingManager LoadingManager { get; private set; }
        
        // States
        private GameStateBase _currentState;
        private readonly Dictionary<Type, GameStateBase> _states = new();
        
        // UI Canvases
        [SerializeField]
        private MainMenuController _mainMenu;
        [SerializeField]
        private GameHUDController _gameHUD;
        [SerializeField]
        private LoadingScreen _loadingScreen;
        [SerializeField]
        private PauseMenuController _pauseMenu;
        
        // settings
        public Settings Settings;
        
        public PlayerViewPrefabs PlayerViewPrefabs;
        public AudioManagerResources AudioManagerResources;
        
        // Prefabs
        public Logic.PlayerView PlayerViewPrefab;
        public Logic.ShootEffect ShootEffectPrefab;
        public PooledParticleSystem HitParticles;
        public GameObject RewindGO;
        public CameraFollow Camera;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            Settings = new Settings();

            UIManager = new UIManager(this, _mainMenu, _loadingScreen, _pauseMenu, _gameHUD);
            NetworkManager = new NetworkManager(this);
            AudioManager = new AudioManager(this);
            LoadingManager = new LoadingManager(this);
            
            
            // Initialize states
            _states.Add(typeof(LoadingState), new LoadingState(this));
            _states.Add(typeof(MainMenuState), new MainMenuState(this));
            _states.Add(typeof(InGameState), new InGameState(this));
            
            Physics2D.simulationMode = SimulationMode2D.Script;
        }

        private void Start()
        {
            NetworkManager.Start();
            // initial state
            ChangeState<LoadingState>();
        }

        public void ChangeState<T>(object context = null) where T : GameStateBase
        {
            // dont change to same state
            if (_currentState?.GetType() == typeof(T))
            {
                return;
            }
            if (_states.TryGetValue(typeof(T), out var newState))
            {
                _currentState?.OnExit();
                _currentState = newState;
                _currentState.OnEnter(context);
            }
        }
        
        public void QuitGame()
        {
            _currentState?.OnExit();
            Application.Quit();
        }
        
        private void Update()
        {
            var dt = Time.deltaTime;

            NetworkManager.Poll();
            _currentState?.Update();
            AudioManager.Update(dt);
        }

        private void FixedUpdate()
        {
            var dt = Time.fixedDeltaTime;
            
            _currentState?.FixedUpdate();
        }
        
        private void OnDestroy()
        {
            NetworkManager.Stop();
        }
        
    }
}