using UnityEngine;

public struct PlayerInputData
{
    public Vector2 moveDirection;
    public bool isCreatingSnowball; // E
    public bool isDroppingOrGrabbing;  // Q
    public bool isThrowingOrPushing;    // Space
}