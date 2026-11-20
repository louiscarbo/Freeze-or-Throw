using UnityEngine;
using UnityEngine.InputSystem;

namespace FreezeOrThrow.Core
{
    public class TestPointGiver : MonoBehaviour
    {
        [SerializeField] MatchState matchState;

        void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.digit1Key.wasPressedThisFrame) matchState.AddPoint(0);
            if (keyboard.digit2Key.wasPressedThisFrame) matchState.AddPoint(1);
            if (keyboard.digit3Key.wasPressedThisFrame) matchState.AddPoint(2);
            if (keyboard.digit4Key.wasPressedThisFrame) matchState.AddPoint(3);
        }
    }
}
