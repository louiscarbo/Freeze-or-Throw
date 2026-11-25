using Unity.Netcode;
using UnityEngine;

namespace FreezeOrThrow.Cameras
{
    public class FollowCamera : MonoBehaviour
    {
        [SerializeField] float distance = 13f;
        [SerializeField] float pitch = 55f;
        [SerializeField] float focusHeight = 1.35f;
        [SerializeField] float smoothTime = 0.15f;
        [SerializeField] Vector2 minimumFocus = new Vector2(-25f, -28f);
        [SerializeField] Vector2 maximumFocus = new Vector2(26f, 28f);

        Transform localPlayer;
        Vector3 velocity;

        void Awake()
        {
            transform.rotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        void LateUpdate()
        {
            if (localPlayer == null)
            {
                localPlayer = FindLocalPlayer();
                if (localPlayer == null)
                {
                    return;
                }
            }

            Vector3 focus = new Vector3(
                Mathf.Clamp(localPlayer.position.x, minimumFocus.x, maximumFocus.x),
                focusHeight,
                Mathf.Clamp(localPlayer.position.z, minimumFocus.y, maximumFocus.y));
            Vector3 cameraTarget = focus - transform.forward * distance;
            transform.position = Vector3.SmoothDamp(transform.position, cameraTarget, ref velocity, smoothTime);
        }

        static Transform FindLocalPlayer()
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager == null || !networkManager.IsListening || networkManager.LocalClient == null)
            {
                return null;
            }

            NetworkObject playerObject = networkManager.LocalClient.PlayerObject;
            return playerObject != null ? playerObject.transform : null;
        }
    }
}
