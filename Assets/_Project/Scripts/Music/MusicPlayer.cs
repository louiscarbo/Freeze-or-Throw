using FreezeOrThrow.Core;
using UnityEngine;

namespace FreezeOrThrow.Music
{
    public class MusicPlayer : MonoBehaviour
    {
        const string LibraryResourceName = "MusicLibrary";
        const float CrossFadeSeconds = 1f;
        const float SecondsBetweenSearches = 0.25f;

        MusicLibrary library;
        AudioSource incomingSource;
        AudioSource outgoingSource;
        float crossFadeProgress = 1f;
        float nextSearchTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void CreatePlayer()
        {
            MusicLibrary library = Resources.Load<MusicLibrary>(LibraryResourceName);
            if (library == null)
            {
                Debug.LogWarning($"No {LibraryResourceName} found in a Resources folder, music is off.");
                return;
            }

            var playerObject = new GameObject(nameof(MusicPlayer));
            DontDestroyOnLoad(playerObject);
            playerObject.AddComponent<MusicPlayer>().library = library;
        }

        void Awake()
        {
            incomingSource = CreateSource();
            outgoingSource = CreateSource();
        }

        AudioSource CreateSource()
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.volume = 0f;
            return source;
        }

        void Update()
        {
            if (Time.unscaledTime >= nextSearchTime)
            {
                nextSearchTime = Time.unscaledTime + SecondsBetweenSearches;
                AudioClip wantedTrack = FindFirstObjectByType<MatchState>() != null ? library.arenaTrack : library.menuTrack;
                if (wantedTrack != incomingSource.clip)
                {
                    SwitchTo(wantedTrack);
                }
            }

            if (crossFadeProgress < 1f)
            {
                crossFadeProgress = Mathf.Min(1f, crossFadeProgress + Time.unscaledDeltaTime / CrossFadeSeconds);
                if (crossFadeProgress >= 1f)
                {
                    outgoingSource.Stop();
                }
            }

            float musicVolume = Settings.MusicVolume;
            incomingSource.volume = crossFadeProgress * musicVolume;
            outgoingSource.volume = (1f - crossFadeProgress) * musicVolume;
        }

        void SwitchTo(AudioClip track)
        {
            (incomingSource, outgoingSource) = (outgoingSource, incomingSource);
            crossFadeProgress = outgoingSource.isPlaying ? 0f : 1f;
            incomingSource.clip = track;
            incomingSource.Play();
        }
    }
}
