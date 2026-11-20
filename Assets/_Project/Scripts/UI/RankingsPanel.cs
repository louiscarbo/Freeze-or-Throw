using FreezeOrThrow.Core;
using FreezeOrThrow.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FreezeOrThrow.UI
{
    public class RankingsPanel : MonoBehaviour
    {
        const int PodiumPlaces = 3;

        [System.Serializable]
        class PlaceRow
        {
            public TMP_Text rankText;
            public TMP_Text nameText;
            public TMP_Text snowmenText;
            public Image pawnImage;
            public Image colourImage;
        }

        [SerializeField] MatchState matchState;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text subtitleText;
        [SerializeField] PlaceRow[] placeRows;
        [SerializeField] Sprite[] pawnSpritesByPlayer;
        [SerializeField] Sprite[] pillSpritesByPlayer;
        [SerializeField] Color[] coloursByPlayer;
        [SerializeField] Button backToHomeButton;

        void OnEnable()
        {
            int[] slotsBestFirst = SortSlotsBestFirst();
            for (int placeIndex = 0; placeIndex < placeRows.Length; placeIndex++)
            {
                ShowRow(placeRows[placeIndex], placeIndex, slotsBestFirst[placeIndex]);
            }

            ShowTitles(slotsBestFirst);
            backToHomeButton.onClick.AddListener(SessionLauncher.Leave);
        }

        void OnDisable()
        {
            backToHomeButton.onClick.RemoveListener(SessionLauncher.Leave);
        }

        int[] SortSlotsBestFirst()
        {
            int[] slots = new int[MatchState.PlayerCount];
            for (int slotIndex = 0; slotIndex < slots.Length; slotIndex++)
            {
                slots[slotIndex] = slotIndex;
            }

            for (int sortedCount = 1; sortedCount < slots.Length; sortedCount++)
            {
                int movingSlot = slots[sortedCount];
                int insertIndex = sortedCount - 1;
                while (insertIndex >= 0 && matchState.ScoreOf(slots[insertIndex]) < matchState.ScoreOf(movingSlot))
                {
                    slots[insertIndex + 1] = slots[insertIndex];
                    insertIndex--;
                }

                slots[insertIndex + 1] = movingSlot;
            }

            return slots;
        }

        void ShowRow(PlaceRow row, int placeIndex, int playerSlot)
        {
            int rank = matchState.RankOf(playerSlot);
            row.rankText.text = placeIndex < PodiumPlaces ? rank.ToString() : Ordinal(rank);
            row.nameText.text = $"Player {playerSlot + 1}";
            row.snowmenText.text = SnowmenLabel(matchState.ScoreOf(playerSlot));

            if (row.pawnImage != null)
            {
                Sprite pawnSprite = pawnSpritesByPlayer[playerSlot];
                row.pawnImage.gameObject.SetActive(pawnSprite != null);
                row.pawnImage.sprite = pawnSprite;
            }

            ShowColour(row.colourImage, placeIndex, playerSlot);
        }

        void ShowColour(Image colourImage, int placeIndex, int playerSlot)
        {
            Sprite pillSprite = placeIndex < PodiumPlaces ? pillSpritesByPlayer[playerSlot] : null;
            if (pillSprite != null)
            {
                colourImage.sprite = pillSprite;
                colourImage.color = Color.white;
                return;
            }

            colourImage.color = coloursByPlayer[playerSlot];
        }

        void ShowTitles(int[] slotsBestFirst)
        {
            int topSlot = slotsBestFirst[0];
            int leaderCount = 0;
            for (int slotIndex = 0; slotIndex < MatchState.PlayerCount; slotIndex++)
            {
                if (matchState.RankOf(slotIndex) == 1)
                {
                    leaderCount++;
                }
            }

            titleText.text = leaderCount == 1 ? $"Player {topSlot + 1} wins!" : "It's a tie!";
            subtitleText.text = $"{SnowmenLabel(matchState.ScoreOf(topSlot))} built before the clock ran out";
        }

        static string SnowmenLabel(int score)
        {
            return score == 1 ? "1 snowman" : $"{score} snowmen";
        }

        static string Ordinal(int rank)
        {
            switch (rank)
            {
                case 1:
                    return "1st";
                case 2:
                    return "2nd";
                case 3:
                    return "3rd";
                default:
                    return $"{rank}th";
            }
        }
    }
}
