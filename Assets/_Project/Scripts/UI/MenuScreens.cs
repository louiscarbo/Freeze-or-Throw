using UnityEngine;

namespace FreezeOrThrow.UI
{
    public class MenuScreens : MonoBehaviour
    {
        [SerializeField] GameObject homeScreen;
        [SerializeField] GameObject optionsScreen;
        [SerializeField] GameObject joinScreen;
        [SerializeField] GameObject lobbyScreen;

        public void ShowHome()
        {
            Show(homeScreen);
        }

        public void ShowOptions()
        {
            Show(optionsScreen);
        }

        public void ShowJoin()
        {
            Show(joinScreen);
        }

        public void ShowLobby()
        {
            Show(lobbyScreen);
        }

        void Show(GameObject screen)
        {
            homeScreen.SetActive(screen == homeScreen);
            optionsScreen.SetActive(screen == optionsScreen);
            joinScreen.SetActive(screen == joinScreen);
            lobbyScreen.SetActive(screen == lobbyScreen);
        }
    }
}
