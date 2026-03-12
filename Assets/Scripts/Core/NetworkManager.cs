using System;
using System.Collections.Generic;
using UnityEngine;

namespace SubmarineCoop.Core
{
    /// <summary>
    /// Abstraction layer for networking. Provides an interface that can be swapped
    /// between Local (same machine), Netcode for GameObjects, Photon, or Mirror.
    /// </summary>
    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private NetworkMode networkMode = NetworkMode.Local;

        private INetworkService _networkService;

        public bool IsHost => _networkService?.IsHost ?? true;
        public bool IsConnected => _networkService?.IsConnected ?? true;
        public int LocalPlayerId => _networkService?.LocalPlayerId ?? 0;

        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<int> OnPlayerJoined;
        public event Action<int> OnPlayerLeft;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeNetworkService();
        }

        private void InitializeNetworkService()
        {
            switch (networkMode)
            {
                case NetworkMode.Local:
                    _networkService = new LocalNetworkService();
                    break;
                // Future: case NetworkMode.NetcodeForGameObjects: ...
                // Future: case NetworkMode.PhotonFusion: ...
                default:
                    _networkService = new LocalNetworkService();
                    break;
            }

            _networkService.OnConnected += () => OnConnected?.Invoke();
            _networkService.OnDisconnected += () => OnDisconnected?.Invoke();
            _networkService.OnPlayerJoined += (id) => OnPlayerJoined?.Invoke(id);
            _networkService.OnPlayerLeft += (id) => OnPlayerLeft?.Invoke(id);

            _networkService.Initialize();
            Debug.Log($"[NetworkManager] Initialized with mode: {networkMode}");
        }

        public void HostSession()
        {
            _networkService.Host();
        }

        public void JoinSession(string sessionId = "")
        {
            _networkService.Join(sessionId);
        }

        public void LeaveSession()
        {
            _networkService.Leave();
        }

        /// <summary>
        /// Send a network message to all players (or specific player).
        /// </summary>
        public void SendMessage(string channel, byte[] data, int targetPlayerId = -1)
        {
            _networkService.SendMessage(channel, data, targetPlayerId);
        }

        /// <summary>
        /// Register a handler for incoming network messages on a channel.
        /// </summary>
        public void RegisterHandler(string channel, Action<int, byte[]> handler)
        {
            _networkService.RegisterHandler(channel, handler);
        }

        /// <summary>
        /// Spawn a networked object (instantiated on all clients).
        /// </summary>
        public GameObject SpawnNetworkObject(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            return _networkService.SpawnObject(prefab, position, rotation);
        }

        private void OnDestroy()
        {
            _networkService?.Shutdown();
        }
    }

    public enum NetworkMode
    {
        Local,
        NetcodeForGameObjects,
        PhotonFusion,
        Mirror
    }

    /// <summary>
    /// Interface for network service implementations.
    /// </summary>
    public interface INetworkService
    {
        bool IsHost { get; }
        bool IsConnected { get; }
        int LocalPlayerId { get; }

        event Action OnConnected;
        event Action OnDisconnected;
        event Action<int> OnPlayerJoined;
        event Action<int> OnPlayerLeft;

        void Initialize();
        void Host();
        void Join(string sessionId);
        void Leave();
        void Shutdown();
        void SendMessage(string channel, byte[] data, int targetPlayerId = -1);
        void RegisterHandler(string channel, Action<int, byte[]> handler);
        GameObject SpawnObject(GameObject prefab, Vector3 position, Quaternion rotation);
    }

    /// <summary>
    /// Local network service for same-machine testing. Both players exist locally.
    /// All messages are delivered immediately to local handlers.
    /// </summary>
    public class LocalNetworkService : INetworkService
    {
        public bool IsHost => true;
        public bool IsConnected => _isConnected;
        public int LocalPlayerId => 0;

        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<int> OnPlayerJoined;
        public event Action<int> OnPlayerLeft;

        private bool _isConnected;
        private Dictionary<string, List<Action<int, byte[]>>> _handlers = new();
        private int _nextPlayerId = 0;

        public void Initialize()
        {
            _isConnected = true;
            _nextPlayerId = 0;
            OnConnected?.Invoke();
            Debug.Log("[LocalNetworkService] Initialized - local mode active");
        }

        public void Host()
        {
            _isConnected = true;
            OnPlayerJoined?.Invoke(_nextPlayerId++);
            Debug.Log("[LocalNetworkService] Hosting local session");
        }

        public void Join(string sessionId)
        {
            _isConnected = true;
            OnPlayerJoined?.Invoke(_nextPlayerId++);
            Debug.Log("[LocalNetworkService] Joined local session");
        }

        public void Leave()
        {
            _isConnected = false;
            OnDisconnected?.Invoke();
        }

        public void Shutdown()
        {
            _isConnected = false;
        }

        public void SendMessage(string channel, byte[] data, int targetPlayerId = -1)
        {
            // Local mode: deliver immediately to all registered handlers
            if (_handlers.TryGetValue(channel, out var handlers))
            {
                foreach (var handler in handlers)
                {
                    handler.Invoke(LocalPlayerId, data);
                }
            }
        }

        public void RegisterHandler(string channel, Action<int, byte[]> handler)
        {
            if (!_handlers.ContainsKey(channel))
            {
                _handlers[channel] = new List<Action<int, byte[]>>();
            }
            _handlers[channel].Add(handler);
        }

        public GameObject SpawnObject(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            // Local mode: just instantiate normally
            return GameObject.Instantiate(prefab, position, rotation);
        }
    }
}
