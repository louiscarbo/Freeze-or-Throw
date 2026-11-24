using System.Collections;
using FreezeOrThrow.Core;
using FreezeOrThrow.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FreezeOrThrow.UI
{
    [System.Serializable]
    public class PlayerTile
    {
        [SerializeField] GameObject normalLook;
        [SerializeField] GameObject youLook;
        [SerializeField] TMP_Text normalScoreText;
        [SerializeField] TMP_Text youScoreText;

        public Transform YouLook => youLook.transform;

        public TMP_Text YouScoreText => youScoreText;

        public void ShowScore(int score)
        {
            string scoreLabel = score.ToString();
            normalScoreText.text = scoreLabel;
            youScoreText.text = scoreLabel;
        }

        public void ShowLook(bool isLocalPlayer)
        {
            youLook.SetActive(isLocalPlayer);
            normalLook.SetActive(!isLocalPlayer);
        }
    }

    public class Hud : MonoBehaviour
    {
        const float PulseFromSeconds = 10f;
        const float HurryFromSeconds = 5f;
        const float PulseLength = 0.3f;
        const float PulseGrowth = 0.12f;

        const float PopupDuration = 0.8f;
        const float PopupStartHeight = 16f;
        const float PopupRise = 30f;
        const float PopupFontSize = 48f;
        static readonly Vector2 PopupSize = new Vector2(100f, 64f);

        [SerializeField] MatchState matchState;
        [SerializeField] TMP_Text timerText;
        [SerializeField] RectTransform timer;
        [SerializeField] Image timerPlank;
        [SerializeField] Sprite hurryPlankSprite;
        [SerializeField] Material hurryTimerMaterial;
        [SerializeField] PlayerTile[] tiles;

        PlayerList playerList;
        int shownSeconds = -1;
        int localSlot;
        int[] shownScores;
        bool showingHurry;
        Sprite calmPlankSprite;
        Material calmTimerMaterial;

        void Awake()
        {
            calmPlankSprite = timerPlank.sprite;
            calmTimerMaterial = timerText.fontSharedMaterial;
            shownScores = new int[tiles.Length];
            System.Array.Fill(shownScores, -1);
        }

        void OnEnable()
        {
            matchState.ScoresChanged += ShowScores;
            FindPlayerList();
            ShowScores();
            ShowLooks();
        }

        void OnDisable()
        {
            matchState.ScoresChanged -= ShowScores;
            if (playerList != null)
            {
                playerList.PlayersChanged -= ShowLooks;
            }

            playerList = null;
        }

        void Update()
        {
            if (playerList == null)
            {
                FindPlayerList();
            }

            float preciseSecondsLeft = matchState.SecondsLeft;
            PulseTimer(preciseSecondsLeft);

            int secondsLeft = Mathf.CeilToInt(preciseSecondsLeft);
            if (secondsLeft == shownSeconds)
            {
                return;
            }

            shownSeconds = secondsLeft;
            timerText.text = $"{secondsLeft / 60}:{secondsLeft % 60:00}";
            ShowHurry(matchState.HasStarted && secondsLeft <= HurryFromSeconds);
        }

        void PulseTimer(float preciseSecondsLeft)
        {
            float growth = 0f;
            if (matchState.IsPlaying && preciseSecondsLeft <= PulseFromSeconds)
            {
                float timeSinceTick = 1f - (preciseSecondsLeft - Mathf.Floor(preciseSecondsLeft));
                if (timeSinceTick < PulseLength)
                {
                    growth = PulseGrowth * Mathf.Sin(timeSinceTick / PulseLength * Mathf.PI);
                }
            }

            timer.localScale = Vector3.one * (1f + growth);
        }

        void ShowHurry(bool hurry)
        {
            if (hurry == showingHurry)
            {
                return;
            }

            showingHurry = hurry;
            timerPlank.sprite = hurry ? hurryPlankSprite : calmPlankSprite;
            timerText.fontSharedMaterial = hurry ? hurryTimerMaterial : calmTimerMaterial;
        }

        void ShowScores()
        {
            for (int playerSlot = 0; playerSlot < tiles.Length; playerSlot++)
            {
                int score = matchState.ScoreOf(playerSlot);
                tiles[playerSlot].ShowScore(score);
                if (!matchState.IsSpawned)
                {
                    continue;
                }

                if (playerSlot == localSlot && shownScores[playerSlot] >= 0 && score > shownScores[playerSlot])
                {
                    StartCoroutine(PopPlusOne(tiles[playerSlot]));
                }

                shownScores[playerSlot] = score;
            }
        }

        IEnumerator PopPlusOne(PlayerTile tile)
        {
            TMP_Text popup = Instantiate(tile.YouScoreText, tile.YouLook);
            popup.name = "PlusOne";
            popup.text = "+1";
            popup.raycastTarget = false;
            popup.enableAutoSizing = false;
            popup.fontSize = PopupFontSize;
            popup.alignment = TextAlignmentOptions.Bottom;
            popup.fontSharedMaterial = calmTimerMaterial;

            RectTransform popupRect = popup.rectTransform;
            popupRect.anchorMin = new Vector2(0.5f, 1f);
            popupRect.anchorMax = new Vector2(0.5f, 1f);
            popupRect.pivot = new Vector2(0.5f, 0f);
            popupRect.sizeDelta = PopupSize;
            popupRect.localScale = Vector3.one;

            for (float elapsed = 0f; elapsed < PopupDuration; elapsed += Time.deltaTime)
            {
                float progress = elapsed / PopupDuration;
                float easedRise = 1f - (1f - progress) * (1f - progress);
                popupRect.anchoredPosition = new Vector2(0f, PopupStartHeight + PopupRise * easedRise);
                popup.alpha = progress < 0.5f ? 1f : 1f - (progress - 0.5f) / 0.5f;
                yield return null;
            }

            Destroy(popup.gameObject);
        }

        void FindPlayerList()
        {
            playerList = FindFirstObjectByType<PlayerList>();
            if (playerList != null)
            {
                playerList.PlayersChanged += ShowLooks;
                ShowLooks();
            }
        }

        void ShowLooks()
        {
            localSlot = playerList != null ? playerList.LocalSlot : 0;
            for (int playerSlot = 0; playerSlot < tiles.Length; playerSlot++)
            {
                tiles[playerSlot].ShowLook(playerSlot == localSlot);
            }
        }
    }
}
