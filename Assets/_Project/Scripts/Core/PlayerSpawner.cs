using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FreezeOrThrow.Core
{
    public class PlayerSpawner : NetworkBehaviour
    {
        [SerializeField] NetworkObject playerPrefab;

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                return;
            }

            NetworkManager.OnClientConnectedCallback += SpawnPlayer;
            if (NetworkManager.ConnectedClientsIds.Count > 1)
            {
                NetworkManager.SceneManager.OnLoadEventCompleted += SpawnPlayersAfterEveryoneLoaded;
                return;
            }

            SpawnMissingPlayers();
        }

        public override void OnNetworkDespawn()
        {
            if (!IsServer || NetworkManager == null)
            {
                return;
            }

            NetworkManager.OnClientConnectedCallback -= SpawnPlayer;
            if (NetworkManager.SceneManager != null)
            {
                NetworkManager.SceneManager.OnLoadEventCompleted -= SpawnPlayersAfterEveryoneLoaded;
            }
        }

        void SpawnPlayersAfterEveryoneLoaded(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
        {
            NetworkManager.SceneManager.OnLoadEventCompleted -= SpawnPlayersAfterEveryoneLoaded;
            SpawnMissingPlayers();
        }

        void SpawnMissingPlayers()
        {
            foreach (ulong clientId in NetworkManager.ConnectedClientsIds)
            {
                SpawnPlayer(clientId);
            }
        }

        void SpawnPlayer(ulong clientId)
        {
            if (NetworkManager.ConnectedClients[clientId].PlayerObject != null)
            {
                return;
            }

            Instantiate(playerPrefab).SpawnAsPlayerObject(clientId, true);
        }
    }
}
