using Unity.Collections;
using Unity.Netcode;

namespace FreezeOrThrow.Networking
{
    public class Lobby : NetworkBehaviour
    {
        public const int MaxPlayers = 4;

        const ulong EmptySlot = ulong.MaxValue;

        readonly NetworkList<ulong> clientIdBySlot = new NetworkList<ulong>();
        readonly NetworkVariable<FixedString32Bytes> code = new NetworkVariable<FixedString32Bytes>();

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
            int slot = SlotOf(clientId);
            if (slot >= 0)
            {
                clientIdBySlot[slot] = EmptySlot;
            }
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
