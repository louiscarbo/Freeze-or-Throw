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

        void Awake()
        {
            startButton.onClick.AddListener(StartGame);
            joinButton.onClick.AddListener(OpenJoin);
            optionsButton.onClick.AddListener(OpenOptions);
            exitButton.onClick.AddListener(exitDialog.Open);
        }

        void StartGame()
        {
            if (SessionLauncher.Host())
            {
                menuScreens.ShowLobby();
            }
        }

        void OpenJoin()
        {
            menuScreens.ShowJoin();
        }

        void OpenOptions()
        {
            menuScreens.ShowOptions();
        }
    }
}
