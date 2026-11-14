using Unity.Netcode;
using UnityEngine;

public class NetworkTestUI : MonoBehaviour
{
    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 200, 150));

        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            if (GUILayout.Button("Start Host"))
                NetworkManager.Singleton.StartHost();

            if (GUILayout.Button("Start Client"))
                NetworkManager.Singleton.StartClient();
        }
        else
        {
            GUILayout.Label(NetworkManager.Singleton.IsHost ? "Mode: Host" : "Mode: Client");
        }

        GUILayout.EndArea();
    }
}