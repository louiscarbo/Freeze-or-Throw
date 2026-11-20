using Unity.Netcode;
using UnityEngine;

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
            foreach (ulong clientId in NetworkManager.ConnectedClientsIds)
            {
                SpawnPlayer(clientId);
            }
        }

        public override void OnNetworkDespawn()
        {
            NetworkManager.OnClientConnectedCallback -= SpawnPlayer;
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
