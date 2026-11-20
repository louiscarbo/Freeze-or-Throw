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

        [SerializeField] Lobby lobby;
        [SerializeField] string gameSceneName = "Multiplayer-Test";
        [SerializeField] TMP_Text codeText;
        [SerializeField] Button copyCodeButton;
        [SerializeField] TMP_Text playerCountText;
        [SerializeField] PlayerCard[] playerCards;
        [SerializeField] Button startButton;
        [SerializeField] GameObject guestWaitingPill;
        [SerializeField] TMP_Text hostNoteText;
        [SerializeField] Button leaveButton;

        string shownCode = "";
        bool shownSpawned;
        bool wasConnected;
        bool leaving;

        void OnEnable()
        {
            wasConnected = false;
            leaving = false;
            lobby.PlayersChanged += Refresh;
            copyCodeButton.onClick.AddListener(CopyCode);
            startButton.onClick.AddListener(StartMatch);
            leaveButton.onClick.AddListener(Leave);
            Refresh();
        }

        void OnDisable()
        {
            lobby.PlayersChanged -= Refresh;
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

            bool spawned = lobby.IsSpawned;
            if (spawned != shownSpawned || (spawned && shownCode.Length == 0 && lobby.Code.Length > 0))
            {
                Refresh();
            }
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
            shownCode = lobby.Code;
            shownSpawned = lobby.IsSpawned;
            codeText.text = shownCode;
            playerCountText.text = $"{lobby.PlayerCount} / {Lobby.MaxPlayers} players";

            int hostSlot = lobby.HostSlot;
            int localSlot = lobby.LocalSlot;
            for (int slot = 0; slot < playerCards.Length; slot++)
            {
                PlayerCard card = playerCards[slot];
                bool taken = lobby.IsSlotTaken(slot);
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
            GUIUtility.systemCopyBuffer = lobby.Code;
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
