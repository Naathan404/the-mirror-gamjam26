using System.Collections;
using UnityEngine;
using Game.Core;
using DG.Tweening;

namespace Game.Interactables
{
    [RequireComponent(typeof(Collider))]
    public class KeyInteractable : MonoBehaviour
    {
        [Header("Hiệu ứng nhặt đồ")]
        [Tooltip("Khoảng cách chìa khóa bay lên")]
        public float floatUpDistance = 1.5f;

        [Tooltip("Độ phóng to của chìa khóa (Ví dụ: 2 = to gấp đôi)")]
        public float scaleUpMultiplier = 2.5f;

        [Tooltip("Thời gian bay và phóng to")]
        public float floatDuration = 0.8f;

        private bool isCollected = false;

        private void OnMouseDown()
        {
            if (isCollected) return;
            isCollected = true;

            // Khóa không cho bấm đúp
            GetComponent<Collider>().enabled = false;

            StartCoroutine(CollectRoutine());
        }

        private IEnumerator CollectRoutine()
        {
            // 1. Bắn sự kiện ngay lập tức (Để Keybox giật và đóng ngăn kéo luôn)
            GameEvents.RaiseKeyCollected();
            GameEvents.RaiseLockUnlocked();
            Debug.Log("[Key] Đã nhặt được chìa khóa phòng!");

            // 2. Chìa khóa bay vút lên trên (Trục Y)
            transform.DOMoveY(transform.position.y + floatUpDistance, floatDuration).SetEase(Ease.OutCubic);

            // 3. Hiệu ứng MỚI: Phóng to dần ra trong lúc bay
            Vector3 targetScale = transform.localScale * scaleUpMultiplier;
            transform.DOScale(targetScale, floatDuration).SetEase(Ease.OutCubic);

            // Đợi hiệu ứng chạy xong
            yield return new WaitForSeconds(floatDuration);

            // 4. Biến mất
            gameObject.SetActive(false);
        }
    }
}