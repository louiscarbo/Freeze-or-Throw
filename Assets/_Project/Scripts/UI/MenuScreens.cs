using UnityEngine;

namespace FreezeOrThrow.UI
{
    public class MenuScreens : MonoBehaviour
    {
        [SerializeField] GameObject homeScreen;
        [SerializeField] GameObject optionsScreen;

        public void ShowHome()
        {
            Show(homeScreen);
        }

        public void ShowOptions()
        {
            Show(optionsScreen);
        }

        void Show(GameObject screen)
        {
            homeScreen.SetActive(screen == homeScreen);
            optionsScreen.SetActive(screen == optionsScreen);
        }
    }
}
