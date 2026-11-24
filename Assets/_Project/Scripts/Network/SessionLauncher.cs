using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FreezeOrThrow.Networking
{
    public static class SessionLauncher
    {
        const string MenuSceneName = "Menu";

        public static bool Host()
        {
            return NetworkManager.Singleton.StartHost();
        }

        public static void StartMatch(string gameSceneName)
        {
            if (!NetworkManager.Singleton.IsServer)
            {
                return;
            }

            NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
        }

        public static bool Join(string hostAddress)
        {
            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.ConnectionData.Address = hostAddress;
            return NetworkManager.Singleton.StartClient();
        }

        public static void Leave()
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            networkManager.Shutdown();
            Object.DestroyImmediate(networkManager.gameObject);
            UnityEngine.SceneManagement.SceneManager.LoadScene(MenuSceneName);
        }
    }
}
