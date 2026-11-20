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

    [Header("Grab & Follow Settings")]
    [SerializeField] private float grabRange = 2.2f;          // Auto-grabs nearest snowball within this range when entering RollingSnowball
    [SerializeField] private float holdDistancePadding = 0.75f; // Extra spacing between player and snowball surface
    [SerializeField] private float followSmoothSpeed = 25f;

    [Header("Debug Visualization")]
    [SerializeField] private bool showSnowCheckGizmo = true;

    private NetworkVariable<float> networkSize = new NetworkVariable<float>(
        1f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private Rigidbody _rb;
    private PlayerMovement _holder;
    private Vector3 _lastPosition;
    private bool hasPrintedFull = false;
    private bool isCollectingSnow = false;

    public float Size => networkSize.Value;
    public bool IsFull => networkSize.Value >= maxSize;
    public bool IsHeld => _holder != null;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            networkSize.Value = minSize;
            _lastPosition = transform.position;
        }
        else if (_rb != null)
        {
            _rb.isKinematic = true;
        }

        UpdateVisualScale(networkSize.Value);
        networkSize.OnValueChanged += OnSizeChanged;
    }

    public override void OnNetworkDespawn()
    {
        networkSize.OnValueChanged -= OnSizeChanged;
    }

    private void FixedUpdate()
    {
        if (!IsServer || _rb == null) return;

        // 1. If nobody is holding this snowball, check if a nearby player just entered RollingSnowball mode
        if (_holder == null)
        {
            TryFindNearbyGrabber();
        }

        // 2. Release if the holder exited RollingSnowball mode or disconnected
        if (_holder == null || !_holder.IsRollingSnowball)
        {
            _holder = null;
            isCollectingSnow = false;
            _rb.linearVelocity = new Vector3(0f, _rb.linearVelocity.y, 0f);
            _rb.angularVelocity = Vector3.zero;
            _lastPosition = transform.position;
            return;
        }

        // 3. Position the snowball directly in front of the player (scales out as the ball grows)
        float radius = networkSize.Value * 0.5f;
        float distanceInFront = radius + holdDistancePadding;

        Vector3 targetPos = _holder.transform.position + (_holder.transform.forward * distanceInFront);
        targetPos.y = Mathf.Max(transform.position.y, radius); // Keep resting on the ground as it scales up

        Vector3 nextPos = Vector3.Lerp(_rb.position, targetPos, followSmoothSpeed * Time.fixedDeltaTime);
        Vector3 frameDisplacement = nextPos - _lastPosition;
        frameDisplacement.y = 0f;

        _rb.MovePosition(nextPos);
        _rb.linearVelocity = Vector3.zero;

        // Visually spin the snowball forward as the player moves
        float distanceMoved = frameDisplacement.magnitude;
        if (distanceMoved > 0.002f)
        {
            Vector3 rollAxis = Vector3.Cross(Vector3.up, frameDisplacement.normalized);
            float angleDegrees = (distanceMoved / Mathf.Max(0.1f, radius)) * Mathf.Rad2Deg;
            _rb.MoveRotation(Quaternion.AngleAxis(angleDegrees, rollAxis) * _rb.rotation);
        }

        _lastPosition = nextPos;

        // 4. Collect snow from the ground only while the player is actively moving the grabbed ball
        if (IsFull || SnowGridManager.Instance == null || distanceMoved <= 0.002f)
        {
            isCollectingSnow = false;
            return;
        }

        float collected = SnowGridManager.Instance.ConsumeSnowAt(
            nextPos,
            snowCollectRate * Time.fixedDeltaTime
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

    private void TryFindNearbyGrabber()
    {
        float radius = networkSize.Value * 0.5f;
        Collider[] hits = Physics.OverlapSphere(transform.position, radius + grabRange);

        float closestDist = float.MaxValue;
        PlayerMovement bestPlayer = null;

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<PlayerMovement>(out var player) && player.IsRollingSnowball)
            {
                // Make sure this player isn't already holding a different snowball
                if (IsPlayerAlreadyHoldingAnotherBall(player)) continue;

                float dist = Vector3.Distance(transform.position, player.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    bestPlayer = player;
                }
            }
        }

        if (bestPlayer != null)
        {
            _holder = bestPlayer;
            _lastPosition = transform.position;
        }
    }

    private bool IsPlayerAlreadyHoldingAnotherBall(PlayerMovement player)
    {
        Snowball[] allBalls = FindObjectsByType<Snowball>(FindObjectsSortMode.None);
        foreach (var ball in allBalls)
        {
            if (ball != this && ball._holder == player)
            {
                return true;
            }
        }
        return false;
    }

    private void OnCollisionStay(Collision collision)
    {
        if (!IsServer || _holder != null) return;

        if (collision.gameObject.TryGetComponent<PlayerMovement>(out var player) && player.IsRollingSnowball)
        {
            if (!IsPlayerAlreadyHoldingAnotherBall(player))
            {
                _holder = player;
                _lastPosition = transform.position;
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