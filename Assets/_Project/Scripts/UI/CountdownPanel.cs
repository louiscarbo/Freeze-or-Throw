using FreezeOrThrow.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FreezeOrThrow.UI
{
    public class CountdownPanel : MonoBehaviour
    {
        [SerializeField] MatchState matchState;
        [SerializeField] TMP_Text countdownText;
        [SerializeField] Image[] stepPills;
        [SerializeField] Sprite activeStepSprite;
        [SerializeField] Sprite idleStepSprite;

        int shownSeconds = -1;

        void Update()
        {
            int secondsLeft = Mathf.CeilToInt(matchState.CountdownSecondsLeft);
            if (secondsLeft == shownSeconds)
            {
                return;
            }

            shownSeconds = secondsLeft;
            countdownText.text = secondsLeft.ToString();
            ShowActiveStep(secondsLeft);
        }

        void ShowActiveStep(int secondsLeft)
        {
            for (int stepIndex = 0; stepIndex < stepPills.Length; stepIndex++)
            {
                bool isActive = stepPills.Length - stepIndex == secondsLeft;
                stepPills[stepIndex].sprite = isActive ? activeStepSprite : idleStepSprite;
            }
        }
    }
}
