using System.Runtime.CompilerServices;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.InputSystem.Users;

[RequireComponent(typeof(Rigidbody), typeof(PlayerInputHandler))]
public class PlayerMovement : MonoBehaviour
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

    private void FixedUpdate()
    {
        if (_inputHandler.currentState == PlayerState.RollingSnowball)
        {
            // TANK CONTROLS
            Vector2 tankInput = _inputHandler.tankMoveInput;

            Vector3 moveDir = transform.forward*tankInput.y;
            _rb.MovePosition(_rb.position+moveDir*moveSpeed*Time.fixedDeltaTime);

            float rotAmount = tankInput.x*rotationSpeed*Time.fixedDeltaTime;
            Quaternion turnRot = Quaternion.Euler(0f, rotAmount, 0f);
            _rb.MoveRotation(_rb.rotation*turnRot);
        } else
        {
            // STD CONTROLS
            Vector2 stdInput = _inputHandler.standardMoveInput;
            Vector3 moveDir = new Vector3(stdInput.x, 0, stdInput.y).normalized;

            _rb.MovePosition(_rb.position+moveDir*moveSpeed*Time.fixedDeltaTime);

            if (moveDir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir);
                _rb.MoveRotation(Quaternion.Slerp(_rb.rotation, targetRot, 15f*Time.fixedDeltaTime));
            }
        }
    }
}
