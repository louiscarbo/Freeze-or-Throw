using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FreezeOrThrow.Networking
{
    public class TestSnowballSpawner : NetworkBehaviour
    {
        [SerializeField] NetworkObject snowballPrefab;
        [SerializeField] Vector3 spawnPosition = new Vector3(0f, 1.5f, 3f);

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                return;
            }

            if (NetworkManager.ConnectedClientsIds.Count > 1)
            {
                NetworkManager.SceneManager.OnLoadEventCompleted += SpawnSnowballAfterEveryoneLoaded;
                return;
            }

            SpawnSnowball();
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer && NetworkManager != null && NetworkManager.SceneManager != null)
            {
                NetworkManager.SceneManager.OnLoadEventCompleted -= SpawnSnowballAfterEveryoneLoaded;
            }
        }

        void SpawnSnowballAfterEveryoneLoaded(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
        {
            NetworkManager.SceneManager.OnLoadEventCompleted -= SpawnSnowballAfterEveryoneLoaded;
            SpawnSnowball();
        }

        void SpawnSnowball()
        {
            Instantiate(snowballPrefab, spawnPosition, Quaternion.identity).Spawn(true);
        }
    }
}
