using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

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

    // standard inputs
    public Vector2 standardMoveInput {get; private set; }
    public Vector2 aimPosition { get; private set; }

    // rolling inputs
    public Vector2 tankMoveInput { get; private set; }

    private void Awake()
    {
        controls = new PlayerControls();

        // STANDARD MAP BINDINGS
        controls.Standard.Move.performed += ctx =>
        {
            standardMoveInput = ctx.ReadValue<Vector2>();

            if (currentState == PlayerState.HoldingFreshSnowball && standardMoveInput.sqrMagnitude > 0.1f)
            {
                SetState(PlayerState.RollingSnowball);
            }
        };
        controls.Standard.Move.canceled += ctx => standardMoveInput = Vector2.zero;
        controls.Standard.Aim.performed += ctx => aimPosition = ctx.ReadValue<Vector2>();

        controls.Standard.Create.performed += ctx =>
        {
            if (currentState == PlayerState.EmptyHanded)
            {
                // TODO: Instantiate snowball
                SetState(PlayerState.HoldingFreshSnowball);
            }
        };

        controls.Standard.GrabDrop.performed += ctx => HandleStandardGrabDrop();
        controls.Standard.PushThrow.performed += ctx => HandleStandardPush();

        // ROLLING MAP BINDINGS
        controls.Rolling.TankMove.performed += ctx => tankMoveInput = ctx.ReadValue<Vector2>();
        controls.Rolling.TankMove.canceled += ctx => tankMoveInput = Vector2.zero;

        controls.Rolling.Drop.performed += ctx =>
        {
            // TODO: Drop snowball on ground logic
            SetState(PlayerState.EmptyHanded);
        };

        controls.Rolling.ThrowForward.performed += ctx =>
        {
            // TODO: Push snowball forward logic
            SetState(PlayerState.EmptyHanded);
        };
    }

    private void SetState(PlayerState newState)
    {
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

    private void HandleStandardGrabDrop()
    {
        if (currentState == PlayerState.EmptyHanded)
        {
            // TODO: Grab nearby snowball logic
            // If successful: SetState(PlayerState.RollingSnowball);
        } else if (currentState == PlayerState.HoldingFreshSnowball)
        {
            // TODO: Destroy snowball logic
            SetState(PlayerState.EmptyHanded);
        }
    }
    private void HandleStandardPush()
    {
        if (currentState == PlayerState.EmptyHanded)
        {
            // TODO: Push player/object logic
        } else if (currentState == PlayerState.HoldingFreshSnowball)
        {
            // TODO: Throw snowball at AimPosition logic
            SetState(PlayerState.EmptyHanded);
        }
    }

    private void OnEnable() => controls.Standard.Enable();   // start with standard controls
    private void OnDisable() => controls.Disable();
}
