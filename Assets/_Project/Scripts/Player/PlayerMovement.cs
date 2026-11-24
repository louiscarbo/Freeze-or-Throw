using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider), typeof(PlayerInputHandler))]
public class PlayerMovement : NetworkBehaviour
{
    private PlayerInputHandler _inputHandler;
    private Rigidbody _rb;
    private Collider _playerCollider;

    [Header("Movement Speeds")]
    public float moveSpeed = 6f;
    public float rollingMoveSpeed = 3.8f; // Slower speed while grabbing/rolling a snowball
    public float rotationSpeed = 15f;

    [Header("Tank Controls")]
    public bool useTankControlsWhileRolling = false;
    public float tankTurnSpeed = 140f;  // degrees per sec

    [Header("Snowball Grab")]
    public float grabRange = 2.2f;

    // Network-synced rolling state so the Server / Snowball.cs knows when this player is in RollingSnowball mode
    // private readonly NetworkVariable<bool> _netIsRollingSnowball = new NetworkVariable<bool>(
    //     false,
    //     NetworkVariableReadPermission.Everyone,
    //     NetworkVariableWritePermission.Owner
    // );

    // Server-authoritative reference to the snowball this player is currently rolling
    private readonly NetworkVariable<NetworkObjectReference> _heldSnowballRef = new NetworkVariable<NetworkObjectReference>(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public bool IsRollingSnowball => _heldSnowballRef.Value.TryGet(out _);

    private void Awake()
    {
        _inputHandler = GetComponent<PlayerInputHandler>();
        _rb = GetComponent<Rigidbody>();
        _playerCollider = GetComponent<Collider>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            transform.position = new Vector3(OwnerClientId * 3f, 2.5f, 0f);
        }

        if (IsOwner)
        {
            _inputHandler.OnGrabDropPressed += HandleGrabDropPressed;
            _inputHandler.OnCreatePressed += HandleCreatePressed;
        }
        else
        {
            _inputHandler.enabled = false;
            _rb.isKinematic = true;
        }

        _heldSnowballRef.OnValueChanged += OnHeldSnowballChanged;

        // apply collision ignore if this player was already holding a ball when we spawned
        ApplyCollisionIgnore(_heldSnowballRef.Value, true);
    }

    public override void OnNetworkDespawn()
    {
        _heldSnowballRef.OnValueChanged -= OnHeldSnowballChanged;

        if (IsOwner)
        {
            _inputHandler.OnGrabDropPressed -= HandleGrabDropPressed;
            _inputHandler.OnCreatePressed -= HandleCreatePressed;
        }

        if (IsServer && _heldSnowballRef.Value.TryGet(out NetworkObject ballObj) &&
            ballObj.TryGetComponent<Snowball>(out var ball))
        {
            ball.ReleaseHolder(this);
            _heldSnowballRef.Value = default;
        }
    }
    
    private void HandleGrabDropPressed()
    {
        if (!IsOwner) return;
        RequestGrabOrDropRpc();
    }

    private void HandleCreatePressed()
    {
        //TODO: create Snowball
    }

    [Rpc(SendTo.Server)]
    private void RequestGrabOrDropRpc()
    {
        // if already holding snowball, drop it
        if (_heldSnowballRef.Value.TryGet(out NetworkObject heldObj))
        {
            if (heldObj.TryGetComponent<Snowball>(out var heldBall))
            {
                heldBall.ReleaseHolder(this);
            }
            _heldSnowballRef.Value = default;
            return;
        }

        // else, find closest snowball in range
        Collider[] hits = Physics.OverlapSphere(transform.position, grabRange);
        Snowball closestBall = null;
        float closestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<Snowball>(out var ball) && !ball.IsHeld)
            {
                // measure from surface of snowball to account for snowball size
                float dist = Vector3.Distance(transform.position, ball.transform.position) - ball.Size * 0.5f;
                if (dist <= grabRange && dist < closestDist)
                {
                    closestDist = dist;
                    closestBall = ball;
                }
            }
        }

        if (closestBall != null && closestBall.TryAssignHolder(this))
        {
            _heldSnowballRef.Value = closestBall.NetworkObject;
        }
    }

    // to be called usually by server systems (freeze penalty, ring-out, steal power-up, etc)
    public void ForceReleaseSnowballOnServer()
    {
        if (!IsServer) return;

        if (_heldSnowballRef.Value.TryGet(out NetworkObject heldObj) &&
            heldObj.TryGetComponent<Snowball>(out var heldBall))
        {
            heldBall.ReleaseHolder(this);
        }
        _heldSnowballRef.Value = default;
    }

    private void OnHeldSnowballChanged(NetworkObjectReference prev, NetworkObjectReference curr)
    {
        // re enable collision with old snowball, ignore collision with newly held snowball
        ApplyCollisionIgnore(prev, false);
        ApplyCollisionIgnore(curr, true);

        if (IsOwner)
        {
            bool holding = curr.TryGet(out _);
            _inputHandler.SetState(holding ? PlayerState.RollingSnowball : PlayerState.EmptyHanded);
        }
    }

    private void ApplyCollisionIgnore(NetworkObjectReference ballRef, bool ignore)
    {
        if (_playerCollider == null) return;

        if (ballRef.TryGet(out NetworkObject ballObj) && ballObj.TryGetComponent<Collider>(out var ballCol))
        {
            Physics.IgnoreCollision(_playerCollider, ballCol, ignore);
        }
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;

        // Yield movement control while swinging on a grapple pole
        if (TryGetComponent<GrappleHook>(out var grapple) && grapple.IsSwinging) return;

        // tank controls (optional)
        if (IsRollingSnowball && useTankControlsWhileRolling)
        {
            Vector2 tankInput = _inputHandler.tankMoveInput;
            float turn = tankInput.x * tankTurnSpeed * Time.fixedDeltaTime;
            Quaternion nextRot = _rb.rotation * Quaternion.Euler(0f, turn, 0f);
            _rb.MoveRotation(nextRot);

            Vector3 forwardMove = (nextRot * Vector3.forward) * (tankInput.y * rollingMoveSpeed * Time.fixedDeltaTime);
            _rb.MovePosition(_rb.position + forwardMove);
            
            return;
        }

        // Use the exact same directional input in both modes; only change the speed
        Vector2 moveInput = IsRollingSnowball ? _inputHandler.tankMoveInput : _inputHandler.standardMoveInput;
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