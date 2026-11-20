using Unity.Netcode;
using UnityEngine;

namespace FreezeOrThrow.Networking
{
    [RequireComponent(typeof(NetworkManager))]
    public class PlayerListSpawner : MonoBehaviour
    {
        [SerializeField] PlayerList playerListPrefab;

        NetworkManager networkManager;

        void Awake()
        {
            networkManager = GetComponent<NetworkManager>();
            networkManager.OnServerStarted += SpawnPlayerList;
        }

        void OnDestroy()
        {
            if (networkManager != null)
            {
                networkManager.OnServerStarted -= SpawnPlayerList;
            }
        }

        void SpawnPlayerList()
        {
            PlayerList playerList = Instantiate(playerListPrefab);
            playerList.NetworkObject.Spawn(destroyWithScene: false);
        }
    }
}
