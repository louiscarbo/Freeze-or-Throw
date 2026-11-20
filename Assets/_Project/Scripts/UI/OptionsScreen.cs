using FreezeOrThrow.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FreezeOrThrow.UI
{
    public class OptionsScreen : MonoBehaviour
    {
        [SerializeField] Slider musicSlider;
        [SerializeField] Slider soundEffectsSlider;
        [SerializeField] Slider voiceChatSlider;
        [SerializeField] TMP_Text musicValueText;
        [SerializeField] TMP_Text soundEffectsValueText;
        [SerializeField] TMP_Text voiceChatValueText;
        [SerializeField] Button fullscreenButton;
        [SerializeField] Button windowedButton;
        [SerializeField] RectTransform windowModeSelector;
        [SerializeField] TMP_Text fullscreenLabel;
        [SerializeField] TMP_Text windowedLabel;
        [SerializeField] Image fullscreenIcon;
        [SerializeField] Image windowedIcon;
        [SerializeField] Color selectedColor = Color.white;
        [SerializeField] Color unselectedColor = Color.gray;
        [SerializeField] Material selectedLabelMaterial;
        [SerializeField] Material unselectedLabelMaterial;
        [SerializeField] Button backButton;
        [SerializeField] MenuScreens menuScreens;

        void OnEnable()
        {
            musicSlider.SetValueWithoutNotify(Settings.MusicVolume);
            soundEffectsSlider.SetValueWithoutNotify(Settings.SoundEffectsVolume);
            voiceChatSlider.SetValueWithoutNotify(Settings.VoiceChatVolume);
            ShowPercentage(musicValueText, musicSlider.value);
            ShowPercentage(soundEffectsValueText, soundEffectsSlider.value);
            ShowPercentage(voiceChatValueText, voiceChatSlider.value);
            ShowWindowMode(Screen.fullScreen);

            musicSlider.onValueChanged.AddListener(ChangeMusicVolume);
            soundEffectsSlider.onValueChanged.AddListener(ChangeSoundEffectsVolume);
            voiceChatSlider.onValueChanged.AddListener(ChangeVoiceChatVolume);
            fullscreenButton.onClick.AddListener(ChooseFullscreen);
            windowedButton.onClick.AddListener(ChooseWindowed);
            backButton.onClick.AddListener(GoBack);
        }

        void OnDisable()
        {
            musicSlider.onValueChanged.RemoveListener(ChangeMusicVolume);
            soundEffectsSlider.onValueChanged.RemoveListener(ChangeSoundEffectsVolume);
            voiceChatSlider.onValueChanged.RemoveListener(ChangeVoiceChatVolume);
            fullscreenButton.onClick.RemoveListener(ChooseFullscreen);
            windowedButton.onClick.RemoveListener(ChooseWindowed);
            backButton.onClick.RemoveListener(GoBack);
        }

        void ChangeMusicVolume(float volume)
        {
            Settings.MusicVolume = volume;
            ShowPercentage(musicValueText, volume);
        }

        void ChangeSoundEffectsVolume(float volume)
        {
            Settings.SoundEffectsVolume = volume;
            ShowPercentage(soundEffectsValueText, volume);
        }

        void ChangeVoiceChatVolume(float volume)
        {
            Settings.VoiceChatVolume = volume;
            ShowPercentage(voiceChatValueText, volume);
        }

        void ChooseFullscreen()
        {
            Screen.fullScreen = true;
            ShowWindowMode(true);
        }

        void ChooseWindowed()
        {
            Screen.fullScreen = false;
            ShowWindowMode(false);
        }

        void GoBack()
        {
            menuScreens.ShowHome();
        }

        void ShowPercentage(TMP_Text valueText, float volume)
        {
            valueText.text = Mathf.RoundToInt(volume * 100f) + "%";
        }

        void ShowWindowMode(bool fullscreen)
        {
            var chosenSegment = (RectTransform)(fullscreen ? fullscreenButton : windowedButton).transform;
            windowModeSelector.anchoredPosition = chosenSegment.anchoredPosition;
            ShowSegment(fullscreenLabel, fullscreenIcon, fullscreen);
            ShowSegment(windowedLabel, windowedIcon, !fullscreen);
        }

        void ShowSegment(TMP_Text label, Image icon, bool selected)
        {
            label.color = selected ? selectedColor : unselectedColor;
            label.fontSharedMaterial = selected ? selectedLabelMaterial : unselectedLabelMaterial;
            icon.color = selected ? selectedColor : unselectedColor;
        }
    }
}
