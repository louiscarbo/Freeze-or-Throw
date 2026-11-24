using UnityEngine;

namespace FreezeOrThrow.Audio
{
    [CreateAssetMenu(menuName = "Freeze or Throw/UI Sound Library")]
    public class UiSoundLibrary : ScriptableObject
    {
        public AudioClip buttonClick;
        public AudioClip back;
        public AudioClip sliderTick;
        public AudioClip countdownTick;
        public AudioClip countdownGo;
        public AudioClip snowmanScored;
        public AudioClip timerWarning;
        public AudioClip matchEnd;
        public AudioClip playerJoined;
    }
}
