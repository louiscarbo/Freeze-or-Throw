using UnityEngine;

namespace FreezeOrThrow.Core
{
    public static class Settings
    {
        const string MusicVolumeKey = "Settings.MusicVolume";
        const string SoundEffectsVolumeKey = "Settings.SoundEffectsVolume";
        const string VoiceChatVolumeKey = "Settings.VoiceChatVolume";

        const float DefaultMusicVolume = 0.6f;
        const float DefaultSoundEffectsVolume = 0.8f;
        const float DefaultVoiceChatVolume = 0.7f;

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

        public static float VoiceChatVolume
        {
            get { return PlayerPrefs.GetFloat(VoiceChatVolumeKey, DefaultVoiceChatVolume); }
            set { Save(VoiceChatVolumeKey, value); }
        }

        static void Save(string key, float volume)
        {
            PlayerPrefs.SetFloat(key, Mathf.Clamp01(volume));
            PlayerPrefs.Save();
        }
    }
}
