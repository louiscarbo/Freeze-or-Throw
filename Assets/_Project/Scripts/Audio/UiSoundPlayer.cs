using System.Collections.Generic;
using FreezeOrThrow.Core;
using FreezeOrThrow.Networking;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FreezeOrThrow.Audio
{
    public class UiSoundPlayer : MonoBehaviour
    {
        const string LibraryResourceName = "UiSoundLibrary";
        const int CountdownSteps = 3;
        const int WarningSeconds = 10;
        const int SliderTicksPerFullRange = 20;
        const float MinimumSecondsBetweenSliderTicks = 0.05f;
        const float SecondsBetweenSearches = 0.25f;

        static readonly HashSet<string> BackButtonNames = new HashSet<string>
        {
            "BackButton",
            "BackToHomeButton",
            "LeaveButton",
            "StayButton",
        };

        UiSoundLibrary library;
        AudioSource audioSource;

        readonly HashSet<Button> hookedButtons = new HashSet<Button>();
        readonly Dictionary<Slider, int> sliderTickBySlider = new Dictionary<Slider, int>();
        float lastSliderTickTime = float.NegativeInfinity;

        MatchState matchState;
        PlayerList playerList;
        float nextSearchTime;

        int lastCountdownStep = -1;
        int lastWarningSecond = int.MaxValue;
        int lastLocalScore = -1;
        bool wasPlaying;
        int lastPlayerCount;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void CreatePlayer()
        {
            UiSoundLibrary library = Resources.Load<UiSoundLibrary>(LibraryResourceName);
            if (library == null)
            {
                Debug.LogWarning($"No {LibraryResourceName} found in a Resources folder, UI sounds are off.");
                return;
            }

            var playerObject = new GameObject(nameof(UiSoundPlayer));
            DontDestroyOnLoad(playerObject);
            playerObject.AddComponent<UiSoundPlayer>().library = library;
        }

        void Awake()
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }

        void Start()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            HookSceneControls();
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        void HandleSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            HookSceneControls();
        }

        void HookSceneControls()
        {
            hookedButtons.RemoveWhere(button => button == null);
            foreach (Button button in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (hookedButtons.Add(button))
                {
                    AudioClip clip = BackButtonNames.Contains(button.name) ? library.back : library.buttonClick;
                    button.onClick.AddListener(() => Play(clip));
                }
            }

            var destroyedSliders = new List<Slider>();
            foreach (Slider slider in sliderTickBySlider.Keys)
            {
                if (slider == null)
                {
                    destroyedSliders.Add(slider);
                }
            }

            foreach (Slider slider in destroyedSliders)
            {
                sliderTickBySlider.Remove(slider);
            }

            foreach (Slider slider in FindObjectsByType<Slider>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!sliderTickBySlider.ContainsKey(slider))
                {
                    sliderTickBySlider[slider] = SliderTickOf(slider);
                    slider.onValueChanged.AddListener(value => TickSlider(slider));
                }
            }
        }

        int SliderTickOf(Slider slider)
        {
            return Mathf.RoundToInt(slider.normalizedValue * SliderTicksPerFullRange);
        }

        void TickSlider(Slider slider)
        {
            int sliderTick = SliderTickOf(slider);
            if (sliderTick == sliderTickBySlider[slider])
            {
                return;
            }

            sliderTickBySlider[slider] = sliderTick;
            if (Time.unscaledTime - lastSliderTickTime < MinimumSecondsBetweenSliderTicks)
            {
                return;
            }

            lastSliderTickTime = Time.unscaledTime;
            Play(library.sliderTick);
        }

        void Update()
        {
            if (Time.unscaledTime >= nextSearchTime)
            {
                nextSearchTime = Time.unscaledTime + SecondsBetweenSearches;
                FindNetworkState();
            }

            if (matchState != null && matchState.IsSpawned)
            {
                FollowMatch();
            }

            FollowPlayers();
        }

        void FindNetworkState()
        {
            if (matchState == null)
            {
                matchState = FindFirstObjectByType<MatchState>();
                lastCountdownStep = -1;
                lastWarningSecond = int.MaxValue;
                lastLocalScore = -1;
                wasPlaying = false;
            }

            if (playerList == null)
            {
                playerList = FindFirstObjectByType<PlayerList>();
            }
        }

        void FollowMatch()
        {
            int countdownStep = matchState.HasStarted ? 0 : Mathf.Min(CountdownSteps, Mathf.CeilToInt(matchState.CountdownSecondsLeft));
            if (countdownStep != lastCountdownStep)
            {
                if (countdownStep > 0)
                {
                    Play(library.countdownTick);
                }
                else if (lastCountdownStep > 0)
                {
                    Play(library.countdownGo);
                }

                lastCountdownStep = countdownStep;
            }

            if (matchState.IsPlaying)
            {
                wasPlaying = true;
                int secondsLeft = Mathf.CeilToInt(matchState.SecondsLeft);
                if (secondsLeft <= WarningSeconds && secondsLeft != lastWarningSecond)
                {
                    lastWarningSecond = secondsLeft;
                    Play(library.timerWarning);
                }
            }
            else if (wasPlaying && matchState.HasEnded)
            {
                wasPlaying = false;
                Play(library.matchEnd);
            }

            int localSlot = playerList != null && playerList.IsSpawned ? playerList.LocalSlot : 0;
            if (localSlot < 0)
            {
                return;
            }

            int localScore = matchState.ScoreOf(localSlot);
            if (lastLocalScore >= 0 && localScore > lastLocalScore)
            {
                Play(library.snowmanScored);
            }

            lastLocalScore = localScore;
        }

        void FollowPlayers()
        {
            if (playerList == null || !playerList.IsSpawned)
            {
                lastPlayerCount = 0;
                return;
            }

            int playerCount = playerList.PlayerCount;
            if (matchState == null && lastPlayerCount > 0 && playerCount > lastPlayerCount)
            {
                Play(library.playerJoined);
            }

            lastPlayerCount = playerCount;
        }

        void Play(AudioClip clip)
        {
            if (clip == null)
            {
                return;
            }

            audioSource.PlayOneShot(clip, Settings.SoundEffectsVolume);
        }
    }
}
