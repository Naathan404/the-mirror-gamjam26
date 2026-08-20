using System.Collections;
using UnityEngine;
using TMPro;

namespace Game.Minigames.WordSearch
{
    [RequireComponent(typeof(BoxCollider))]
    public class WordSearchClueItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text clueText;
        [SerializeField] private GameObject foundOverlay;
        [SerializeField] private SpriteRenderer paperBg;
        [SerializeField] private float horizontalPadding = 0.5f;

        [Header("Tương tác vật lý (Game Feel)")]
        [SerializeField] private float rubberBandFactor = 0.2f; // Lực cản khi kéo ra ngoài (Càng nhỏ càng nặng)
        [SerializeField] private float snapBackSpeed = 15f;     // Tốc độ dây thun bắn về biên

        public string TargetWord { get; private set; }

        private Camera mainCam;
        private BoxCollider spawnArea;
        private Vector3 dragOffset;
        private int currentBaseSortingOrder;
        private bool isDragging = false;
        private Coroutine snapBackCoroutine; // Biến lưu trữ quá trình bay về
        private static int globalTopSortingOrder = 100; // BIẾN STATIC: Dùng chung cho tất cả các giấy để biết tầng cao nhất hiện tại

        private void Start()
        {
            mainCam = Camera.main;
        }

        public void Initialize(string word, int baseSortingOrder, BoxCollider area)
        {
            if (baseSortingOrder == 10) globalTopSortingOrder = 100;

            TargetWord = word;
            clueText.text = word;
            currentBaseSortingOrder = baseSortingOrder;
            spawnArea = area;

            if (foundOverlay != null) foundOverlay.SetActive(false);
            clueText.color = Color.black;

            UpdateSortingOrder(currentBaseSortingOrder);

            if (paperBg != null && paperBg.drawMode == SpriteDrawMode.Sliced)
            {
                clueText.ForceMeshUpdate();
                float textWidth = clueText.preferredWidth;
                paperBg.size = new Vector2(textWidth + horizontalPadding, paperBg.size.y);
            }
        }

        public void MarkAsFound()
        {
            clueText.color = new Color(0.5f, 0, 0, 1f);
            if (foundOverlay != null) foundOverlay.SetActive(true);
        }
        private void OnMouseDown()
        {
            isDragging = true;

            if (snapBackCoroutine != null)
            {
                StopCoroutine(snapBackCoroutine);
            }

            // ==========================================
            // LOGIC LÊN ĐỈNH: Tờ giấy này sẽ lấy tầng cao nhất + thêm 10
            globalTopSortingOrder += 10;
            currentBaseSortingOrder = globalTopSortingOrder;

            // Cập nhật hiển thị ngay lập tức
            UpdateSortingOrder(currentBaseSortingOrder);
            // ==========================================

            Vector3 mousePos = GetMouseWorldPosition();
            dragOffset = transform.position - mousePos;
        }

        private void OnMouseDrag()
        {
            if (!isDragging) return;

            Vector3 targetPosition = GetMouseWorldPosition() + dragOffset;

            if (spawnArea != null)
            {
                Vector3 localPos = spawnArea.transform.InverseTransformPoint(targetPosition);
                Vector3 extents = spawnArea.size / 2f;
                Vector3 center = spawnArea.center;

                // 1. Tính toán vị trí biên giới cứng
                Vector3 clampedLocalPos = new Vector3(
                    Mathf.Clamp(localPos.x, center.x - extents.x, center.x + extents.x),
                    Mathf.Clamp(localPos.y, center.y - extents.y, center.y + extents.y),
                    localPos.z
                );

                // 2. Tính phần tọa độ bị kéo lố ra ngoài
                Vector3 excess = localPos - clampedLocalPos;

                // 3. Tạo hiệu ứng dây thun: Chỉ lấy 20% lực kéo lố (rubberBandFactor)
                Vector3 finalLocalPos = clampedLocalPos + (excess * rubberBandFactor);

                targetPosition = spawnArea.transform.TransformPoint(finalLocalPos);
            }

            transform.position = targetPosition;
        }

        private void OnMouseUp()
        {
            isDragging = false;

            // 4. Khi buông tay, kích hoạt hàm bắn giấy về biên
            if (spawnArea != null && gameObject.activeInHierarchy)
            {
                snapBackCoroutine = StartCoroutine(SnapBackRoutine());
            }
        }

        private IEnumerator SnapBackRoutine()
        {
            Vector3 localPos = spawnArea.transform.InverseTransformPoint(transform.position);
            Vector3 extents = spawnArea.size / 2f;
            Vector3 center = spawnArea.center;

            // Tính vị trí an toàn cần bay về
            Vector3 clampedLocalPos = new Vector3(
                Mathf.Clamp(localPos.x, center.x - extents.x, center.x + extents.x),
                Mathf.Clamp(localPos.y, center.y - extents.y, center.y + extents.y),
                localPos.z
            );

            // Nếu đã nằm an toàn bên trong biên thì không làm gì cả
            if (localPos == clampedLocalPos) yield break;

            Vector3 targetWorldPos = spawnArea.transform.TransformPoint(clampedLocalPos);

            // 5. Nội suy Lerp để bay về mượt mà (càng gần càng chậm lại)
            while (Vector3.Distance(transform.position, targetWorldPos) > 0.005f)
            {
                transform.position = Vector3.Lerp(transform.position, targetWorldPos, Time.deltaTime * snapBackSpeed);
                yield return null;
            }

            // Chốt hạ tọa độ tuyệt đối
            transform.position = targetWorldPos;
        }

        private Vector3 GetMouseWorldPosition()
        {
            if (mainCam == null) return transform.position;
            Plane paperPlane = new Plane(-transform.forward, transform.position);
            Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);

            if (paperPlane.Raycast(ray, out float distance))
            {
                return ray.GetPoint(distance);
            }
            return transform.position;
        }

        private void UpdateSortingOrder(int baseOrder)
        {
            if (paperBg != null) paperBg.sortingOrder = baseOrder;
            if (clueText != null && clueText.TryGetComponent<Renderer>(out var textRenderer)) textRenderer.sortingOrder = baseOrder + 1;
            if (foundOverlay != null && foundOverlay.TryGetComponent<SpriteRenderer>(out var overlayRenderer)) overlayRenderer.sortingOrder = baseOrder + 2;
        }
    }
}