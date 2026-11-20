using FreezeOrThrow.Core;
using UnityEngine;

namespace FreezeOrThrow.UI
{
    public class GameScreens : MonoBehaviour
    {
        [SerializeField] MatchState matchState;
        [SerializeField] GameObject countdownPanel;
        [SerializeField] GameObject hudPanel;
        [SerializeField] GameObject rankingsPanel;

        void Update()
        {
            SetShown(countdownPanel, matchState.IsSpawned && !matchState.HasStarted);
            SetShown(hudPanel, matchState.IsPlaying);
            SetShown(rankingsPanel, matchState.HasEnded);
        }

        void SetShown(GameObject panel, bool shouldBeActive)
        {
            if (panel.activeSelf != shouldBeActive)
            {
                panel.SetActive(shouldBeActive);
            }
        }
    }
}
