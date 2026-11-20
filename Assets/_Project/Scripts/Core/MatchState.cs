using Unity.Netcode;

namespace FreezeOrThrow.Core
{
    public class MatchState : NetworkBehaviour
    {
        public const int PlayerCount = 4;
        const double MatchDurationSeconds = 420;

        readonly NetworkVariable<double> endServerTime = new NetworkVariable<double>();
        readonly NetworkList<int> scores = new NetworkList<int>();

        public event System.Action ScoresChanged;

        public override void OnNetworkSpawn()
        {
            scores.OnListChanged += HandleScoresChanged;
            if (IsServer)
            {
                endServerTime.Value = NetworkManager.ServerTime.Time + MatchDurationSeconds;
                for (int playerSlot = 0; playerSlot < PlayerCount; playerSlot++)
                {
                    scores.Add(0);
                }
            }

            ScoresChanged?.Invoke();
        }

        public override void OnNetworkDespawn()
        {
            scores.OnListChanged -= HandleScoresChanged;
        }

        void HandleScoresChanged(NetworkListEvent<int> change)
        {
            ScoresChanged?.Invoke();
        }

        public float SecondsLeft
        {
            get
            {
                if (!IsSpawned)
                {
                    return 0f;
                }

                return (float)System.Math.Max(0, endServerTime.Value - NetworkManager.ServerTime.Time);
            }
        }

        public bool HasEnded => IsSpawned && SecondsLeft <= 0f;

        public int ScoreOf(int playerSlot)
        {
            return playerSlot < scores.Count ? scores[playerSlot] : 0;
        }

        public void AddPoint(int playerSlot)
        {
            if (!IsServer || HasEnded)
            {
                return;
            }

            scores[playerSlot] += 1;
        }
    }
}
