using UnityEngine;
using UnityEngine.InputSystem;

public class DummyPusher : MonoBehaviour
{
    public float speed = 5f;

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current == null) return;

        HandleMove();
        HandleHold();
    }

    private void HandleMove()
    {
        float moveX = 0f;
        float moveZ = 0f;
        if (Keyboard.current.dKey.isPressed) moveX += 1f;
        if (Keyboard.current.aKey.isPressed) moveX -= 1f;
        if (Keyboard.current.wKey.isPressed) moveZ += 1f;
        if (Keyboard.current.sKey.isPressed) moveZ -= 1f;

        Vector3 move = new Vector3(moveX, 0, moveZ).normalized;
        transform.Translate(move*speed*Time.deltaTime);
    }

    private void HandleHold()
    {
        if (Keyboard.current.qKey.isPressed)
        {
            Snowball snowball = FindAnyObjectByType<Snowball>();
            PlayerFreezeHandler frzHandler = gameObject.GetComponent<PlayerFreezeHandler>();
            frzHandler.CurrentHeldRollball = snowball;
            snowball.StartRolling(1);
        }
    }
}
