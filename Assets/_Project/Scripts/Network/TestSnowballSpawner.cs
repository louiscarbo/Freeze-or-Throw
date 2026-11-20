using Unity.Netcode;
using UnityEngine;

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

            Instantiate(snowballPrefab, spawnPosition, Quaternion.identity).Spawn(true);
        }
    }
}
