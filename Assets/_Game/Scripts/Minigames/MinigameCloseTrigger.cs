using UnityEngine;
using Game.Core;

namespace Game.Interactables
{
    /// <summary>
    /// Script gắn vào vật thể 3D (như cục tẩy, nút bấm) để đóng Minigame.
    /// Bắt buộc vật thể phải có Collider (BoxCollider...)
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class MinigameCloseTrigger : MonoBehaviour
    {
        [Tooltip("Minigame nào sẽ bị đóng khi bấm vào cục này?")]
        public MinigameType targetMinigame = MinigameType.Maze;

        private void OnMouseDown()
        {
            GameEvents.RaiseMinigameClosed(targetMinigame);
        }
    }
}