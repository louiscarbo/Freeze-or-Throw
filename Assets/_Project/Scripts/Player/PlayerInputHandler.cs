using System;
using UnityEngine;

public enum PlayerState
{
    EmptyHanded,
    HoldingFreshSnowball,
    RollingSnowball
}

public class PlayerInputHandler : MonoBehaviour
{
    private PlayerControls controls;
    public PlayerState currentState { get; private set; } = PlayerState.EmptyHanded;

    //IMP: events for PlayerMovement and interaction script to subscribe to
    public event Action OnGrabDropPressed;
    public event Action OnCreatePressed;
    public event Action OnPushThrowPressed;

    // standard inputs
    public Vector2 standardMoveInput => controls != null && controls.Standard.enabled
        ? controls.Standard.Move.ReadValue<Vector2>() : Vector2.zero;
    public Vector2 aimPosition => controls != null && controls.Standard.enabled
        ? controls.Standard.Aim.ReadValue<Vector2>() : Vector2.zero;

    // rolling inputs
    public Vector2 tankMoveInput => controls != null && controls.Rolling.enabled
        ? controls.Rolling.TankMove.ReadValue<Vector2>() : Vector2.zero;

    private void Awake()
    {
        controls = new PlayerControls();

        // STANDARD MAP BINDINGS
        controls.Standard.Create.performed += _ => OnCreatePressed?.Invoke();
        controls.Standard.GrabDrop.performed += _ => OnGrabDropPressed?.Invoke();
        controls.Standard.PushThrow.performed += _ => OnPushThrowPressed?.Invoke();

        // ROLLING MAP BINDINGS
        controls.Rolling.Drop.performed += _ => OnGrabDropPressed?.Invoke();
        controls.Rolling.ThrowForward.performed += _ => OnPushThrowPressed?.Invoke();
    }

    public void SetState(PlayerState newState)
    {
        if (currentState == newState) return;

        currentState = newState;
        Debug.Log($"Player State changed to: {newState}");

        if (newState == PlayerState.RollingSnowball)
        {
            controls.Standard.Disable();
            controls.Rolling.Enable();
        } else
        {
            controls.Standard.Enable();
            controls.Rolling.Disable();
        }
    }

    private void OnEnable() => controls.Standard.Enable();   // start with standard controls
    private void OnDisable() => controls.Disable();
}
