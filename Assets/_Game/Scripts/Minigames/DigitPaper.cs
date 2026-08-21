using System.Collections;
using UnityEngine;
using TMPro;
using Game.Core;

namespace Game.Minigames
{
    [RequireComponent(typeof(BoxCollider))]
    public class DigitPaper : MonoBehaviour
    {
        [SerializeField] private TMP_Text digitText;
        [SerializeField] private SpriteRenderer paperBg;

        [Header("Tương tác vật lý (Game Feel)")]
        [SerializeField] private float rubberBandFactor = 0.2f;
        [SerializeField] private float snapBackSpeed = 15f;

        private Camera mainCam;
        private BoxCollider spawnArea;
        private BoxCollider paperCollider;
        private Vector3 dragOffset;
        private int currentBaseSortingOrder;
        private bool isDragging = false;
        private Coroutine snapBackCoroutine;

        private static int globalTopSortingOrder = 100;

        private void Awake()
        {
            paperCollider = GetComponent<BoxCollider>();
        }

        private void Start()
        {
            mainCam = Camera.main;
            GameEvents.OnMinigameOpened += HidePaper;
            GameEvents.OnMinigameClosed += ShowPaper;
        }

        private void OnDestroy()
        {
            GameEvents.OnMinigameOpened -= HidePaper;
            GameEvents.OnMinigameClosed -= ShowPaper;
        }

        public void Initialize(string digit, int baseSortingOrder, BoxCollider area, Color paperColor)
        {
            if (digitText != null) digitText.text = digit;
            spawnArea = area;

            // Đổi màu mảnh giấy (Yêu cầu ảnh Sprite gốc phải là màu Trắng/Xám)
            if (paperBg != null) paperBg.color = paperColor;

            globalTopSortingOrder += 10;
            currentBaseSortingOrder = globalTopSortingOrder;
            UpdateSortingOrder(currentBaseSortingOrder);
        }

        private void HidePaper(MinigameType _)
        {
            if (paperBg != null) paperBg.enabled = false;
            if (digitText != null) digitText.enabled = false;
            if (paperCollider != null) paperCollider.enabled = false;
            isDragging = false;
        }

        private void ShowPaper(MinigameType _)
        {
            if (paperBg != null) paperBg.enabled = true;
            if (digitText != null) digitText.enabled = true;
            if (paperCollider != null) paperCollider.enabled = true;
        }

        private void OnMouseDown()
        {
            isDragging = true;
            globalTopSortingOrder += 10;
            currentBaseSortingOrder = globalTopSortingOrder;
            UpdateSortingOrder(currentBaseSortingOrder + 100);
            AudioController.Instance.PlaySFX(SoundName.Pick_Clue);

            if (snapBackCoroutine != null) StopCoroutine(snapBackCoroutine);

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
                Vector3 areaExtents = spawnArea.size / 2f;
                Vector3 center = spawnArea.center;

                Vector2 paperExtents = paperBg != null ? paperBg.size / 2f : Vector2.one * 0.5f;

                float limitX = Mathf.Max(0, areaExtents.x - paperExtents.x);
                float limitY = Mathf.Max(0, areaExtents.y - paperExtents.y);

                Vector3 clampedLocalPos = new Vector3(
                    Mathf.Clamp(localPos.x, center.x - limitX, center.x + limitX),
                    Mathf.Clamp(localPos.y, center.y - limitY, center.y + limitY),
                    localPos.z
                );

                Vector3 excess = localPos - clampedLocalPos;
                Vector3 finalLocalPos = clampedLocalPos + (excess * rubberBandFactor);

                targetPosition = spawnArea.transform.TransformPoint(finalLocalPos);
            }

            transform.position = targetPosition;
        }

        private void OnMouseUp()
        {
            isDragging = false;
            UpdateSortingOrder(currentBaseSortingOrder);

            if (spawnArea != null && gameObject.activeInHierarchy)
            {
                snapBackCoroutine = StartCoroutine(SnapBackRoutine());
            }
        }

        private IEnumerator SnapBackRoutine()
        {
            Vector3 localPos = spawnArea.transform.InverseTransformPoint(transform.position);
            Vector3 areaExtents = spawnArea.size / 2f;
            Vector3 center = spawnArea.center;

            Vector2 paperExtents = paperBg != null ? paperBg.size / 2f : Vector2.one * 0.5f;

            float limitX = Mathf.Max(0, areaExtents.x - paperExtents.x);
            float limitY = Mathf.Max(0, areaExtents.y - paperExtents.y);

            Vector3 clampedLocalPos = new Vector3(
                Mathf.Clamp(localPos.x, center.x - limitX, center.x + limitX),
                Mathf.Clamp(localPos.y, center.y - limitY, center.y + limitY),
                localPos.z
            );

            if (localPos == clampedLocalPos) yield break;

            Vector3 targetWorldPos = spawnArea.transform.TransformPoint(clampedLocalPos);

            while (Vector3.Distance(transform.position, targetWorldPos) > 0.005f)
            {
                transform.position = Vector3.Lerp(transform.position, targetWorldPos, Time.deltaTime * snapBackSpeed);
                yield return null;
            }
            transform.position = targetWorldPos;
        }

        private Vector3 GetMouseWorldPosition()
        {
            if (mainCam == null) return transform.position;
            Plane paperPlane = new Plane(-transform.forward, transform.position);
            Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
            if (paperPlane.Raycast(ray, out float distance)) return ray.GetPoint(distance);
            return transform.position;
        }

        private void UpdateSortingOrder(int baseOrder)
        {
            if (paperBg != null) paperBg.sortingOrder = baseOrder;
            if (digitText != null && digitText.TryGetComponent<Renderer>(out var textRenderer)) textRenderer.sortingOrder = baseOrder + 1;
        }
    }
}