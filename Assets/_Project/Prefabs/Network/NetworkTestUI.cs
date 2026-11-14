using Unity.Netcode;
using UnityEngine;

public class NetworkTestUI : MonoBehaviour
{
    [SerializeField] private GameObject snowballPrefab;
    [SerializeField] private Vector3 snowballSpawnPos = new Vector3(0f, 1.5f, 3f);

    private void OnGUI()
    {
        if (NetworkManager.Singleton == null) return;

        GUILayout.BeginArea(new Rect(10, 10, 220, 180));

        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            if (GUILayout.Button("Start Host", GUILayout.Height(35)))
            {
                if (NetworkManager.Singleton.StartHost() && snowballPrefab != null)
                {
                    GameObject ball = Instantiate(snowballPrefab, snowballSpawnPos, Quaternion.identity);
                    ball.GetComponent<NetworkObject>().Spawn();
                }
            }

            if (GUILayout.Button("Start Client", GUILayout.Height(35)))
            {
                NetworkManager.Singleton.StartClient();
            }
        }
        else
        {
            string mode = NetworkManager.Singleton.IsHost ? "Host" : "Client";
            GUILayout.Label($"Mode: {mode}");
            GUILayout.Label($"Connected: {NetworkManager.Singleton.IsConnectedClient}");
        }

        GUILayout.EndArea();
    }
}