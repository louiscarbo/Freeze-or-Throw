using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(PlayerInputHandler))]
public class PlayerMovement : NetworkBehaviour
{
    private PlayerInputHandler _inputHandler;
    private Rigidbody _rb;

    public float moveSpeed = 5f;
    public float rotationSpeed = 60f;

    private void Awake()
    {
        _inputHandler = GetComponent<PlayerInputHandler>();
        _rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        // Offset spawn positions on the Server so players don't overlap at (0,0,0)
        if (IsServer)
        {
            transform.position = new Vector3(OwnerClientId * 3f, 2.5f, 0f);
        }

        // Only the local owner should read inputs and simulate local Rigidbody physics
        if (!IsOwner)
        {
            _inputHandler.enabled = false;
            _rb.isKinematic = true;
        }
    }

    private void FixedUpdate()
    {
        // Prevent moving other players' characters
        if (!IsOwner) return;

        if (_inputHandler.currentState == PlayerState.RollingSnowball)
        {
            // TANK CONTROLS
            Vector2 tankInput = _inputHandler.tankMoveInput;

            Vector3 moveDir = transform.forward * tankInput.y;
            _rb.MovePosition(_rb.position + moveDir * moveSpeed * Time.fixedDeltaTime);

            float rotAmount = tankInput.x * rotationSpeed * Time.fixedDeltaTime;
            Quaternion turnRot = Quaternion.Euler(0f, rotAmount, 0f);
            _rb.MoveRotation(_rb.rotation * turnRot);
        }
        else
        {
            // STD CONTROLS
            Vector2 stdInput = _inputHandler.standardMoveInput;
            Vector3 moveDir = new Vector3(stdInput.x, 0, stdInput.y).normalized;

            _rb.MovePosition(_rb.position + moveDir * moveSpeed * Time.fixedDeltaTime);

            if (moveDir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir);
                _rb.MoveRotation(Quaternion.Slerp(_rb.rotation, targetRot, 15f * Time.fixedDeltaTime));
            }
        }
    }
}