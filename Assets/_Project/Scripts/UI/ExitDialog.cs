using UnityEngine;
using UnityEngine.UI;

namespace FreezeOrThrow.UI
{
    /// <summary>The "Leaving the cabin?" overlay on top of the Home screen.</summary>
    public class ExitDialog : MonoBehaviour
    {
        [SerializeField] Button stayButton;
        [SerializeField] Button exitGameButton;
        [SerializeField] GoodbyeScreen goodbyeScreen;

        void Awake()
        {
            stayButton.onClick.AddListener(Close);
            exitGameButton.onClick.AddListener(ExitGame);
        }

        public void Open()
        {
            gameObject.SetActive(true);
        }

        void Close()
        {
            gameObject.SetActive(false);
        }

        void ExitGame()
        {
            Close();
            goodbyeScreen.Show();
        }
    }
}
