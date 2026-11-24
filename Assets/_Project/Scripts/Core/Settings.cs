using UnityEngine;

namespace FreezeOrThrow.Core
{
    public static class Settings
    {
        const string MusicVolumeKey = "Settings.MusicVolume";
        const string SoundEffectsVolumeKey = "Settings.SoundEffectsVolume";

        const float DefaultMusicVolume = 0.6f;
        const float DefaultSoundEffectsVolume = 0.8f;

        public static float MusicVolume
        {
            get { return PlayerPrefs.GetFloat(MusicVolumeKey, DefaultMusicVolume); }
            set { Save(MusicVolumeKey, value); }
        }

        public static float SoundEffectsVolume
        {
            get { return PlayerPrefs.GetFloat(SoundEffectsVolumeKey, DefaultSoundEffectsVolume); }
            set { Save(SoundEffectsVolumeKey, value); }
        }

        static void Save(string key, float volume)
        {
            PlayerPrefs.SetFloat(key, Mathf.Clamp01(volume));
            PlayerPrefs.Save();
        }
    }
}
