using FreezeOrThrow.Networking;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace FreezeOrThrow.UI
{
    public class LobbyScreen : MonoBehaviour
    {
        [System.Serializable]
        class PlayerCard
        {
            public GameObject joinedContent;
            public GameObject joiningContent;
            public GameObject hostBadge;
            public GameObject youBadge;
            public TMP_Text nameText;
        }

        [SerializeField] string gameSceneName = "Multiplayer-Test";
        [SerializeField] TMP_Text codeText;
        [SerializeField] Button copyCodeButton;
        [SerializeField] TMP_Text playerCountText;
        [SerializeField] PlayerCard[] playerCards;
        [SerializeField] Button startButton;
        [SerializeField] GameObject guestWaitingPill;
        [SerializeField] TMP_Text hostNoteText;
        [SerializeField] Button leaveButton;

        PlayerList playerList;
        string shownCode = "";
        bool shownSpawned;
        bool wasConnected;
        bool leaving;

        void OnEnable()
        {
            wasConnected = false;
            leaving = false;
            copyCodeButton.onClick.AddListener(CopyCode);
            startButton.onClick.AddListener(StartMatch);
            leaveButton.onClick.AddListener(Leave);
            FindPlayerList();
            Refresh();
        }

        void OnDisable()
        {
            ReleasePlayerList();
            copyCodeButton.onClick.RemoveListener(CopyCode);
            startButton.onClick.RemoveListener(StartMatch);
            leaveButton.onClick.RemoveListener(Leave);
        }

        void Update()
        {
            if (leaving)
            {
                return;
            }

            if (HasLostConnection())
            {
                Leave();
                return;
            }

            if (playerList == null)
            {
                FindPlayerList();
            }

            bool spawned = playerList != null && playerList.IsSpawned;
            if (spawned != shownSpawned || (spawned && shownCode.Length == 0 && playerList.Code.Length > 0))
            {
                Refresh();
            }
        }

        void FindPlayerList()
        {
            playerList = FindFirstObjectByType<PlayerList>();
            if (playerList != null)
            {
                playerList.PlayersChanged += Refresh;
            }
        }

        void ReleasePlayerList()
        {
            if (playerList != null)
            {
                playerList.PlayersChanged -= Refresh;
            }

            playerList = null;
        }

        bool HasLostConnection()
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager == null)
            {
                return false;
            }

            if (!networkManager.IsListening)
            {
                return true;
            }

            if (networkManager.IsConnectedClient)
            {
                wasConnected = true;
                return false;
            }

            return wasConnected;
        }

        void Refresh()
        {
            bool spawned = playerList != null && playerList.IsSpawned;
            shownCode = spawned ? playerList.Code : "";
            shownSpawned = spawned;
            codeText.text = shownCode;
            playerCountText.text = $"{(spawned ? playerList.PlayerCount : 0)} / {PlayerList.MaxPlayers} players";

            int hostSlot = spawned ? playerList.HostSlot : -1;
            int localSlot = spawned ? playerList.LocalSlot : -1;
            for (int slot = 0; slot < playerCards.Length; slot++)
            {
                PlayerCard card = playerCards[slot];
                bool taken = spawned && playerList.IsSlotTaken(slot);
                card.joinedContent.SetActive(taken);
                card.joiningContent.SetActive(!taken);
                card.nameText.text = $"Player {slot + 1}";
                card.hostBadge.SetActive(slot == hostSlot);
                card.youBadge.SetActive(slot == localSlot && slot != hostSlot);
            }

            NetworkManager networkManager = NetworkManager.Singleton;
            bool isHost = networkManager != null && networkManager.IsHost;
            startButton.gameObject.SetActive(isHost);
            guestWaitingPill.SetActive(!isHost);
            hostNoteText.text = isHost ? "Only the host can start the match" : $"Player {hostSlot + 1} is the host";
        }

        void CopyCode()
        {
            GUIUtility.systemCopyBuffer = shownCode;
        }

        void StartMatch()
        {
            SessionLauncher.StartMatch(gameSceneName);
        }

        void Leave()
        {
            leaving = true;
            SessionLauncher.Leave();
        }
    }
}
