using UnityEngine;

namespace FreezeOrThrow.UI
{
    public class SteadySpin : MonoBehaviour
    {
        [SerializeField] float degreesPerSecond = 6f;

        void Update()
        {
            transform.Rotate(0f, 0f, degreesPerSecond * Time.deltaTime);
        }
    }
}
