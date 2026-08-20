using System; // Bổ sung thư viện này để dùng Action
using System.Collections;
using UnityEngine;

namespace Game.Systems.Lock
{
    [RequireComponent(typeof(Collider))]
    public class KeyboxButton : MonoBehaviour
    {
        [Header("Hiệu ứng Vật lý")]
        public Transform buttonMesh;
        public Vector3 pushAxis = Vector3.back;
        public float pressDepth = 0.03f;
        public float pressSpeed = 15f;

        // SỰ KIỆN DUY NHẤT: Bắn ra khi nút bị click
        public event Action OnClicked;

        private Vector3 originalLocalPos;
        private Coroutine pressCoroutine;
        private Transform targetTransform;

        private void Start()
        {
            targetTransform = buttonMesh != null ? buttonMesh : transform;
            originalLocalPos = targetTransform.localPosition;
        }

        private void OnMouseDown()
        {
            if (pressCoroutine != null) StopCoroutine(pressCoroutine);
            pressCoroutine = StartCoroutine(PressAnimationRoutine());

            // Bất kỳ ai đang đăng ký lắng nghe sự kiện này đều sẽ được thông báo
            OnClicked?.Invoke();
        }

        private IEnumerator PressAnimationRoutine()
        {
            Vector3 pressedPos = originalLocalPos + pushAxis.normalized * pressDepth;

            while (Vector3.Distance(targetTransform.localPosition, pressedPos) > 0.001f)
            {
                targetTransform.localPosition = Vector3.Lerp(targetTransform.localPosition, pressedPos, Time.deltaTime * pressSpeed * 2f);
                yield return null;
            }

            while (Vector3.Distance(targetTransform.localPosition, originalLocalPos) > 0.001f)
            {
                targetTransform.localPosition = Vector3.Lerp(targetTransform.localPosition, originalLocalPos, Time.deltaTime * pressSpeed);
                yield return null;
            }
            targetTransform.localPosition = originalLocalPos;
        }
    }
}