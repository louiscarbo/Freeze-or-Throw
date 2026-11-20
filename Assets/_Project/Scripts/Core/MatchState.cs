using Unity.Netcode;
using UnityEngine;

namespace FreezeOrThrow.Core
{
    public class MatchState : NetworkBehaviour
    {
        public const int PlayerCount = 4;

        [SerializeField] double countdownSeconds = 3;
        [SerializeField] double matchDurationSeconds = 420;

        readonly NetworkVariable<double> startServerTime = new NetworkVariable<double>();
        readonly NetworkVariable<double> endServerTime = new NetworkVariable<double>();
        readonly NetworkList<int> scores = new NetworkList<int>();

        public event System.Action ScoresChanged;

        public override void OnNetworkSpawn()
        {
            scores.OnListChanged += HandleScoresChanged;
            if (IsServer)
            {
                startServerTime.Value = NetworkManager.ServerTime.Time + countdownSeconds;
                endServerTime.Value = startServerTime.Value + matchDurationSeconds;
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

                double untilEnd = endServerTime.Value - NetworkManager.ServerTime.Time;
                double matchLength = endServerTime.Value - startServerTime.Value;
                return (float)System.Math.Max(0, System.Math.Min(untilEnd, matchLength));
            }
        }

        public float CountdownSecondsLeft
        {
            get
            {
                if (!IsSpawned)
                {
                    return 0f;
                }

                return (float)System.Math.Max(0, startServerTime.Value - NetworkManager.ServerTime.Time);
            }
        }

        public bool HasStarted => IsSpawned && CountdownSecondsLeft <= 0f;

        public bool HasEnded => IsSpawned && SecondsLeft <= 0f;

        public bool IsPlaying => HasStarted && !HasEnded;

        public int ScoreOf(int playerSlot)
        {
            return playerSlot < scores.Count ? scores[playerSlot] : 0;
        }

        public void AddPoint(int playerSlot)
        {
            if (!IsServer || !IsPlaying)
            {
                return;
            }

            scores[playerSlot] += 1;
        }

        public int RankOf(int playerSlot)
        {
            int ownScore = ScoreOf(playerSlot);
            int rank = 1;
            for (int otherSlot = 0; otherSlot < PlayerCount; otherSlot++)
            {
                if (ScoreOf(otherSlot) > ownScore)
                {
                    rank++;
                }
            }

            return rank;
        }
    }
}
