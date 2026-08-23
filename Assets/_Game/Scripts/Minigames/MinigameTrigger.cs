using UnityEngine;
using Game.Core;

namespace Game.Interactables
{
    [RequireComponent(typeof(Collider))]
    public class MinigameTrigger : MonoBehaviour
    {
        [Header("Cài đặt Tương tác")]
        [Tooltip("Chọn Minigame sẽ được mở khi click vào object này")]
        public MinigameType targetMinigame = MinigameType.Maze;

        private void OnMouseDown()
        {
            Debug.Log($"[Interact] Người chơi vừa click vào {gameObject.name}. Đang gọi mở {targetMinigame}...");
            
            AudioController.Instance.PlaySFX(SoundName.Minigame_Open);
            GameEvents.RaiseMinigameOpened(targetMinigame);
        }
    }
}