using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine.SceneManagement;

namespace FreezeOrThrow.Networking
{
    public class PlayerList : NetworkBehaviour
    {
        public const int MaxPlayers = 4;

        const ulong EmptySlot = ulong.MaxValue;

        readonly NetworkList<ulong> clientIdBySlot = new NetworkList<ulong>();
        readonly NetworkVariable<FixedString32Bytes> code = new NetworkVariable<FixedString32Bytes>();

        bool matchStarted;

        public event System.Action PlayersChanged;

        public int PlayerCount
        {
            get
            {
                int takenSlots = 0;
                for (int playerSlot = 0; playerSlot < clientIdBySlot.Count; playerSlot++)
                {
                    if (IsSlotTaken(playerSlot))
                    {
                        takenSlots++;
                    }
                }

                return takenSlots;
            }
        }

        public int HostSlot => IsSpawned ? SlotOf(NetworkManager.ServerClientId) : -1;

        public int LocalSlot => IsSpawned ? SlotOf(NetworkManager.LocalClientId) : -1;

        public string Code => IsSpawned ? code.Value.ToString() : "";

        public override void OnNetworkSpawn()
        {
            clientIdBySlot.OnListChanged += HandleListChanged;
            code.OnValueChanged += HandleCodeChanged;
            if (IsServer)
            {
                for (int playerSlot = 0; playerSlot < MaxPlayers; playerSlot++)
                {
                    clientIdBySlot.Add(EmptySlot);
                }

                code.Value = GameCode.Encode(GameCode.LocalAddress());
                AssignSlot(NetworkManager.ServerClientId);
                foreach (ulong clientId in NetworkManager.ConnectedClientsIds)
                {
                    AssignSlot(clientId);
                }

                NetworkManager.OnClientConnectedCallback += AssignSlot;
                NetworkManager.OnClientDisconnectCallback += FreeSlot;
                NetworkManager.SceneManager.OnLoadEventCompleted += HandleLoadEventCompleted;
            }

            PlayersChanged?.Invoke();
        }

        public override void OnNetworkDespawn()
        {
            clientIdBySlot.OnListChanged -= HandleListChanged;
            code.OnValueChanged -= HandleCodeChanged;
            if (IsServer && NetworkManager != null)
            {
                NetworkManager.OnClientConnectedCallback -= AssignSlot;
                NetworkManager.OnClientDisconnectCallback -= FreeSlot;
                if (NetworkManager.SceneManager != null)
                {
                    NetworkManager.SceneManager.OnLoadEventCompleted -= HandleLoadEventCompleted;
                }
            }
        }

        public bool IsSlotTaken(int playerSlot)
        {
            return playerSlot >= 0 && playerSlot < clientIdBySlot.Count && clientIdBySlot[playerSlot] != EmptySlot;
        }

        public int SlotOf(ulong clientId)
        {
            for (int playerSlot = 0; playerSlot < clientIdBySlot.Count; playerSlot++)
            {
                if (clientIdBySlot[playerSlot] == clientId)
                {
                    return playerSlot;
                }
            }

            return -1;
        }

        void AssignSlot(ulong clientId)
        {
            if (SlotOf(clientId) >= 0)
            {
                return;
            }

            int freeSlot = SlotOf(EmptySlot);
            if (freeSlot < 0)
            {
                NetworkManager.DisconnectClient(clientId);
                return;
            }

            clientIdBySlot[freeSlot] = clientId;
        }

        void FreeSlot(ulong clientId)
        {
            int leavingSlot = SlotOf(clientId);
            if (leavingSlot < 0)
            {
                return;
            }

            if (matchStarted)
            {
                clientIdBySlot[leavingSlot] = EmptySlot;
                return;
            }

            for (int playerSlot = leavingSlot; playerSlot < clientIdBySlot.Count - 1; playerSlot++)
            {
                clientIdBySlot[playerSlot] = clientIdBySlot[playerSlot + 1];
            }

            clientIdBySlot[clientIdBySlot.Count - 1] = EmptySlot;
        }

        void HandleLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
        {
            matchStarted = true;
        }

        void HandleListChanged(NetworkListEvent<ulong> change)
        {
            PlayersChanged?.Invoke();
        }

        void HandleCodeChanged(FixedString32Bytes previousCode, FixedString32Bytes newCode)
        {
            PlayersChanged?.Invoke();
        }
    }
}
