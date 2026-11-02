using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FreezeOrThrow.UI
{
    /// <summary>
    /// Shown after "Exit game". The game quits once the delay runs out, and a click before that
    /// goes back to the cabin, which is what the hint on the screen promises.
    /// </summary>
    public class GoodbyeScreen : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField, Min(0f)] float secondsBeforeQuit = 3f;

        public void Show()
        {
            gameObject.SetActive(true);
            StartCoroutine(QuitAfterDelay());
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // Deactivating the object also stops the coroutine, which cancels the quit.
            gameObject.SetActive(false);
        }

        IEnumerator QuitAfterDelay()
        {
            yield return new WaitForSecondsRealtime(secondsBeforeQuit);
            Quit();
        }

        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
