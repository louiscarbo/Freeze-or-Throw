using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FreezeOrThrow.Cameras
{
    public class FollowCamera : MonoBehaviour
    {
        [SerializeField] float distance = 13f;
        [SerializeField] float closestDistance = 10f;
        [SerializeField] float farthestDistance = 17f;
        [SerializeField] float closestPitch = 45f;
        [SerializeField] float farthestPitch = 60f;
        [SerializeField] float distancePerScrollStep = 1f;
        [SerializeField] float zoomSmoothTime = 0.2f;
        [SerializeField] float focusHeight = 1.35f;
        [SerializeField] float smoothTime = 0.15f;
        [SerializeField] Vector2 minimumFocus = new Vector2(-25f, -28f);
        [SerializeField] Vector2 maximumFocus = new Vector2(26f, 28f);

        Transform localPlayer;
        Vector3 currentFocus;
        Vector3 focusVelocity;
        float targetDistance;
        float currentDistance;
        float distanceVelocity;
        float currentPitch;
        float pitchVelocity;

        void Awake()
        {
            targetDistance = Mathf.Clamp(distance, closestDistance, farthestDistance);
            currentPitch = transform.eulerAngles.x;
            transform.rotation = Quaternion.Euler(currentPitch, 0f, 0f);
            float heightAboveFocus = transform.position.y - focusHeight;
            currentDistance = heightAboveFocus / Mathf.Max(Mathf.Sin(currentPitch * Mathf.Deg2Rad), 0.1f);
            currentFocus = transform.position + transform.forward * currentDistance;
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

            targetDistance = Mathf.Clamp(
                targetDistance - ReadScrollSteps() * distancePerScrollStep,
                closestDistance,
                farthestDistance);
            float zoomPosition = Mathf.InverseLerp(closestDistance, farthestDistance, targetDistance);
            float targetPitch = Mathf.Lerp(closestPitch, farthestPitch, zoomPosition);
            currentDistance = Mathf.SmoothDamp(currentDistance, targetDistance, ref distanceVelocity, zoomSmoothTime);
            currentPitch = Mathf.SmoothDamp(currentPitch, targetPitch, ref pitchVelocity, zoomSmoothTime);

            Vector3 focus = new Vector3(
                Mathf.Clamp(localPlayer.position.x, minimumFocus.x, maximumFocus.x),
                focusHeight,
                Mathf.Clamp(localPlayer.position.z, minimumFocus.y, maximumFocus.y));
            currentFocus = Vector3.SmoothDamp(currentFocus, focus, ref focusVelocity, smoothTime);

            transform.rotation = Quaternion.Euler(currentPitch, 0f, 0f);
            transform.position = currentFocus - transform.forward * currentDistance;
        }

        static float ReadScrollSteps()
        {
            Mouse mouse = Mouse.current;
            return mouse != null ? mouse.scroll.ReadValue().y : 0f;
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
