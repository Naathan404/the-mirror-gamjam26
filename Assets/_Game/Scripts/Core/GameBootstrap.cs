using UnityEngine;

namespace Game.Core
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            GameEvents.ClearAllListeners();
        }
    }
}