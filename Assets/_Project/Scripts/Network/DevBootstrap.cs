using Unity.Netcode;
using UnityEngine;

namespace FreezeOrThrow.Networking
{
    public class DevBootstrap : MonoBehaviour
    {
        [SerializeField] NetworkManager networkManagerPrefab;

        void Start()
        {
            if (NetworkManager.Singleton == null)
            {
                Instantiate(networkManagerPrefab);
            }

            if (!NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.StartHost();
            }
        }
    }
}
