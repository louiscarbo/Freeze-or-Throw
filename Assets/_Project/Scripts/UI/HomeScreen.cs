using FreezeOrThrow.Networking;
using UnityEngine;
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
        [SerializeField] MenuScreens menuScreens;
        [SerializeField] string gameSceneName = "Game";
        [SerializeField] string testHostAddress = "127.0.0.1";

        void Awake()
        {
            startButton.onClick.AddListener(StartGame);
            joinButton.onClick.AddListener(OpenJoin);
            optionsButton.onClick.AddListener(OpenOptions);
            exitButton.onClick.AddListener(exitDialog.Open);
        }

        void StartGame()
        {
            SessionLauncher.Host(gameSceneName);
        }

        void OpenJoin()
        {
            SessionLauncher.Join(testHostAddress);
        }

        void OpenOptions()
        {
            menuScreens.ShowOptions();
        }
    }
}
