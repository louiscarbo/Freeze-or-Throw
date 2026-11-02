using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FreezeOrThrow.UI
{
    /// <summary>The first screen of the Menu scene.</summary>
    public class HomeScreen : MonoBehaviour
    {
        [SerializeField] Button startButton;
        [SerializeField] Button joinButton;
        [SerializeField] Button optionsButton;
        [SerializeField] Button exitButton;
        [SerializeField] ExitDialog exitDialog;
        [SerializeField] string gameSceneName = "Game";

        void Awake()
        {
            startButton.onClick.AddListener(StartGame);
            joinButton.onClick.AddListener(OpenJoin);
            optionsButton.onClick.AddListener(OpenOptions);
            exitButton.onClick.AddListener(exitDialog.Open);
        }

        void StartGame()
        {
            SceneManager.LoadScene(gameSceneName);
        }

        // Join needs Photon Fusion (December) and Options is the next screen to build.
        void OpenJoin()
        {
            Debug.Log("Join game screen is not built yet.");
        }

        void OpenOptions()
        {
            Debug.Log("Options screen is not built yet.");
        }
    }
}
