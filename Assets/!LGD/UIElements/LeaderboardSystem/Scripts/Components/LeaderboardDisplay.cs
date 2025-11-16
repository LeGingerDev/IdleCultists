using UnityEngine;

namespace LGD.UIElements.Leaderboards
{
    // Generic base class for leaderboard display
    public abstract class LeaderboardDisplay<T> : MonoBehaviour where T : class
    {
        public abstract void InitializeEntry(T entry, int rank);


    }
}