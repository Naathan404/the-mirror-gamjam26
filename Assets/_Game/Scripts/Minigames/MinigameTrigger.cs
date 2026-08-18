using UnityEngine;
using Game.Core;

namespace Game.Interactables
{
    /// <summary>
    /// Script gắn vào vật thể 3D trong môi trường để người chơi click vào mở Minigame
    /// Yêu cầu vật thể phải có Collider (BoxCollider, SphereCollider...)
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class MinigameTrigger : MonoBehaviour
    {
        [Header("Cài đặt Tương tác")]
        [Tooltip("Chọn Minigame sẽ được mở khi click vào object này")]
        public MinigameType targetMinigame = MinigameType.Maze;

        // Hàm OnMouseDown mặc định của Unity sẽ tự động bắt sự kiện 
        // khi người chơi click chuột trái vào Collider của vật thể này.
        private void OnMouseDown()
        {
            Debug.Log($"[Interact] Người chơi vừa click vào {gameObject.name}. Đang gọi mở {targetMinigame}...");
            
            // Bắn lệnh qua Event Bus
            GameEvents.RaiseMinigameOpened(targetMinigame);
        }
    }
}