using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(SphereCollider), typeof(Rigidbody))]
public class Snowball : NetworkBehaviour
{
    [Header("Size Settings")]
    [SerializeField] private float minSize = 1f;
    [SerializeField] private float maxSize = 5f;
    [SerializeField] private float snowCollectRate = 25f;
    [SerializeField] private float snowToSizeRatio = 0.02f;

    [Header("Push Settings")]
    [SerializeField] private float pushStrength = 6f;

    [Header("Debug Visualization")]
    [SerializeField] private bool showSnowCheckGizmo = true;

    // Synced size across all connected players (starts at 1)
    private NetworkVariable<float> networkSize = new NetworkVariable<float>(
        1f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private Rigidbody _rb;
    private bool hasPrintedFull = false;
    private bool isCollectingSnow = false;

    public float Size => networkSize.Value;
    public bool IsFull => networkSize.Value >= maxSize;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            networkSize.Value = minSize;
        }
        else if (_rb != null)
        {
            // Clients only receive position updates via NetworkTransform; they do not simulate local physics
            _rb.isKinematic = true;
        }

        UpdateVisualScale(networkSize.Value);
        networkSize.OnValueChanged += OnSizeChanged;
    }

    public override void OnNetworkDespawn()
    {
        networkSize.OnValueChanged -= OnSizeChanged;
    }

    private void Update()
    {
        // Only the Server/Host calculates snow collection and size growth
        if (!IsServer || IsFull || SnowGridManager.Instance == null)
        {
            isCollectingSnow = false;
            return;
        }

        // Collect snow from the ground under the snowball
        float collected = SnowGridManager.Instance.ConsumeSnowAt(
            transform.position,
            snowCollectRate * Time.deltaTime
        );

        isCollectingSnow = collected > 0f;

        if (isCollectingSnow)
        {
            float newSize = Mathf.Min(maxSize, networkSize.Value + (collected * snowToSizeRatio));
            networkSize.Value = newSize;

            if (newSize >= maxSize && !hasPrintedFull)
            {
                hasPrintedFull = true;
                Debug.Log("I AM FULL");
            }
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        // Only the Server/Host simulates snowball physics
        if (!IsServer || _rb == null) return;

        if (collision.gameObject.CompareTag("Player") || collision.gameObject.GetComponent<PlayerMovement>() != null)
        {
            // Push direction from the player toward the center of the snowball (flattened on Y)
            Vector3 pushDir = transform.position - collision.transform.position;
            pushDir.y = 0f;

            if (pushDir.sqrMagnitude > 0.001f)
            {
                pushDir.Normalize();
                _rb.AddForce(pushDir * pushStrength, ForceMode.Acceleration);
            }
        }
    }

    private void OnSizeChanged(float previousValue, float newValue)
    {
        UpdateVisualScale(newValue);

        if (newValue >= maxSize && !hasPrintedFull)
        {
            hasPrintedFull = true;
            Debug.Log("I AM FULL");
        }
    }

    private void UpdateVisualScale(float currentSize)
    {
        // Scales the sphere mesh and SphereCollider proportionally
        transform.localScale = Vector3.one * currentSize;
    }

    private void OnDrawGizmos()
    {
        if (!showSnowCheckGizmo || !Application.isPlaying) return;

        Gizmos.color = isCollectingSnow ? Color.green : Color.red;
        Vector3 groundPoint = transform.position - Vector3.up * (transform.localScale.y * 0.5f);
        Gizmos.DrawWireSphere(groundPoint, 0.35f);
    }
}