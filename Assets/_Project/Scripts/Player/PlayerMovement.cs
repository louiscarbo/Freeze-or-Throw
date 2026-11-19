using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(PlayerInputHandler))]
public class PlayerMovement : NetworkBehaviour
{
    private PlayerInputHandler _inputHandler;
    private Rigidbody _rb;

    [Header("Movement Speeds")]
    public float moveSpeed = 6f;
    public float rollingMoveSpeed = 3.8f; // Slower speed while grabbing/rolling a snowball
    public float rotationSpeed = 15f;

    // Network-synced rolling state so the Server / Snowball.cs knows when this player is in RollingSnowball mode
    private readonly NetworkVariable<bool> _netIsRollingSnowball = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    public bool IsRollingSnowball => _netIsRollingSnowball.Value;

    private void Awake()
    {
        _inputHandler = GetComponent<PlayerInputHandler>();
        _rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            transform.position = new Vector3(OwnerClientId * 3f, 2.5f, 0f);
        }

        if (!IsOwner)
        {
            _inputHandler.enabled = false;
            _rb.isKinematic = true;
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        bool isRollingNow = _inputHandler.currentState == PlayerState.RollingSnowball;
        if (_netIsRollingSnowball.Value != isRollingNow)
        {
            _netIsRollingSnowball.Value = isRollingNow;
        }
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;

        // Yield movement control while swinging on a grapple pole
        if (TryGetComponent<GrappleHook>(out var grapple) && grapple.IsSwinging) return;

        // Use the exact same directional input in both modes; only change the speed
        Vector2 moveInput = _inputHandler.standardMoveInput;
        if (moveInput.sqrMagnitude < 0.001f)
        {
            // Fallback in case PlayerInputHandler only populates tankMoveInput during RollingSnowball
            moveInput = _inputHandler.tankMoveInput;
        }

        Vector3 moveDir = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
        float activeSpeed = IsRollingSnowball ? rollingMoveSpeed : moveSpeed;

        _rb.MovePosition(_rb.position + moveDir * activeSpeed * Time.fixedDeltaTime);

        if (moveDir != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            _rb.MoveRotation(Quaternion.Slerp(_rb.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime));
        }
    }
}