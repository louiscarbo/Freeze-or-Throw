using UnityEngine;

namespace FreezeOrThrow.Music
{
    [CreateAssetMenu(menuName = "Freeze or Throw/Music Library")]
    public class MusicLibrary : ScriptableObject
    {
        public AudioClip menuTrack;
        public AudioClip arenaTrack;
    }
}
