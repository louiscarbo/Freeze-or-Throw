using FreezeOrThrow.Core;
using FreezeOrThrow.Networking;
using TMPro;
using UnityEngine;

namespace FreezeOrThrow.UI
{
    [System.Serializable]
    public class PlayerTile
    {
        [SerializeField] GameObject normalLook;
        [SerializeField] GameObject youLook;
        [SerializeField] TMP_Text normalScoreText;
        [SerializeField] TMP_Text youScoreText;

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
        [SerializeField] MatchState matchState;
        [SerializeField] TMP_Text timerText;
        [SerializeField] PlayerTile[] tiles;

        PlayerList playerList;
        int shownSeconds = -1;

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
            for (int playerSlot = 0; playerSlot < tiles.Length; playerSlot++)
            {
                tiles[playerSlot].ShowScore(matchState.ScoreOf(playerSlot));
            }
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
            int localSlot = playerList != null ? playerList.LocalSlot : 0;
            for (int playerSlot = 0; playerSlot < tiles.Length; playerSlot++)
            {
                tiles[playerSlot].ShowLook(playerSlot == localSlot);
            }
        }
    }
}
