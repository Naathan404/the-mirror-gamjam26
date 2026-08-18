using UnityEngine;
using System.Collections;

namespace Game.Minigames.Maze
{
    public class MazePlayer : MonoBehaviour
    {
        [Header("Cài đặt Di chuyển")]
        [Tooltip("Tốc độ trượt của nét bút (càng lớn càng nhanh)")]
        public float moveSpeed = 5f;

        // Vị trí hiện tại trên lưới Data
        public Vector2Int CurrentGridPos { get; private set; }

        // Cờ kiểm tra xem bút có đang trượt dở không (để chặn spam phím)
        public bool IsMoving { get; private set; }

        /// <summary>
        /// Khởi tạo vị trí ban đầu khi bắt đầu game hoặc khi bị Reset
        /// </summary>
        public void Initialize(Vector2Int startGridPos, Vector3 startWorldPos)
        {
            CurrentGridPos = startGridPos;
            transform.position = startWorldPos;

            // Xoay nhân vật nằm bẹp xuống giấy giống như EndMarker
            transform.rotation = transform.parent != null ? transform.parent.rotation : Quaternion.identity;

            IsMoving = false;
        }

        /// <summary>
        /// Lệnh di chuyển sang ô mới (Gọi từ MazeController)
        /// </summary>
        public void MoveTo(Vector2Int newGridPos, Vector3 targetWorldPos)
        {
            if (IsMoving) return;

            CurrentGridPos = newGridPos;
            StartCoroutine(SmoothMoveRoutine(targetWorldPos));
        }

        /// <summary>
        /// Coroutine giúp nét bút trượt mượt mà thay vì giật cục (teleport)
        /// </summary>
        private IEnumerator SmoothMoveRoutine(Vector3 targetPos)
        {
            IsMoving = true;

            // Dùng Vector3.MoveTowards để di chuyển đều đặn
            while (Vector3.Distance(transform.position, targetPos) > 0.001f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);

                // Đợi frame tiếp theo rồi chạy tiếp vòng lặp
                yield return null;
            }

            // Đảm bảo đến đích chính xác 100%
            transform.position = targetPos;
            IsMoving = false;
        }
    }
}