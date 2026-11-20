using FreezeOrThrow.Core;
using TMPro;
using UnityEngine;

namespace FreezeOrThrow.UI
{
    public class Hud : MonoBehaviour
    {
        [SerializeField] MatchState matchState;
        [SerializeField] TMP_Text timerText;
        [SerializeField] TMP_Text[] scoreTexts;

        int shownSeconds = -1;

        void OnEnable()
        {
            matchState.ScoresChanged += ShowScores;
            ShowScores();
        }

        void OnDisable()
        {
            matchState.ScoresChanged -= ShowScores;
        }

        void Update()
        {
            int secondsLeft = Mathf.CeilToInt(matchState.SecondsLeft);
            if (secondsLeft == shownSeconds)
            {
                return;
            }

            shownSeconds = secondsLeft;
            timerText.text = $"{secondsLeft / 60}:{secondsLeft % 60:00}";
        }

        void ShowScores()
        {
            for (int playerSlot = 0; playerSlot < scoreTexts.Length; playerSlot++)
            {
                scoreTexts[playerSlot].text = matchState.ScoreOf(playerSlot).ToString();
            }
        }
    }
}
