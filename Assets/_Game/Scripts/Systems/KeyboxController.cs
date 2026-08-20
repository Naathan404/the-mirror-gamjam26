using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game.Core;
using DG.Tweening;

namespace Game.Systems.Lock
{
    public class KeyboxController : MonoBehaviour
    {
        [Header("Tham chiếu Cơ khí")]
        public NumberWheel[] digitWheels = new NumberWheel[4];
        public KeyboxButton submitButton;

        [Header("Hiệu ứng Keybox (DOTween)")]
        [Tooltip("Object chứa toàn bộ hình ảnh hộp khóa")]
        public Transform boxTransform;
        public float scrambleTime = 1f; // Thời gian quay số loạn xạ sau khi đóng

        [Header("Phần thưởng (Cơ quan ẩn)")]
        public Transform drawerTransform;
        public float drawerOpenTargetX = 3f;
        public float drawerMoveDuration = 0.8f;

        [Tooltip("Kéo Collider của chiếc chìa khóa vào đây")]
        public Collider roomKeyCollider;

        private int[] targetPasscode = new int[4];
        private bool isUnlocked = false;
        private bool isProcessing = false;

        private void Start()
        {
            if (boxTransform == null) boxTransform = transform;

            if (drawerTransform != null)
            {
                Vector3 pos = drawerTransform.localPosition;
                drawerTransform.localPosition = new Vector3(0f, pos.y, pos.z);
            }

            // Chìa khóa vẫn hiện hình (đi theo ngăn kéo), nhưng KHÔNG THỂ click
            if (roomKeyCollider != null) roomKeyCollider.enabled = false;
        }

        private void OnEnable()
        {
            GameEvents.OnPasscodeGenerated += SetupTargetPasscode;
            GameEvents.OnKeyCollected += HandleKeyCollected; // Nghe sự kiện mất chìa khóa

            if (submitButton != null) submitButton.OnClicked += TryOpenBox;
        }

        private void OnDisable()
        {
            GameEvents.OnPasscodeGenerated -= SetupTargetPasscode;
            GameEvents.OnKeyCollected -= HandleKeyCollected;

            if (submitButton != null) submitButton.OnClicked -= TryOpenBox;
        }

        private void SetupTargetPasscode(Dictionary<MinigameType, int> minigameDigitMap)
        {
            // (Giữ nguyên logic lấy Passcode như cũ)
            if (minigameDigitMap.TryGetValue(MinigameType.Maze, out int d0)) targetPasscode[0] = d0;
            if (minigameDigitMap.TryGetValue(MinigameType.CardMatch, out int d1)) targetPasscode[1] = d1;
            if (minigameDigitMap.TryGetValue(MinigameType.Wires, out int d2)) targetPasscode[2] = d2;
            if (minigameDigitMap.TryGetValue(MinigameType.WordSearch, out int d3)) targetPasscode[3] = d3;
        }

        private void TryOpenBox()
        {
            if (isUnlocked || isProcessing) return;

            bool isCorrect = true;
            for (int i = 0; i < 4; i++)
            {
                if (digitWheels[i] != null && digitWheels[i].CurrentValue != targetPasscode[i])
                {
                    isCorrect = false; break;
                }
            }

            if (isCorrect)
            {
                isUnlocked = true;
                SetInteractable(false); // Khóa không cho bấm số nữa
                StartCoroutine(OpenDrawerRoutine());
            }
            else
            {
                StartCoroutine(ScrambleOnFailRoutine());
            }
        }

        // ==========================================
        // CHUỖI HIỆU ỨNG MỞ HỘP
        // ==========================================
        private IEnumerator OpenDrawerRoutine()
        {
            isProcessing = true;

            // 1. Lắc nhẹ hộp (y hệt minigame)
            boxTransform.DOShakePosition(0.4f, new Vector3(0.1f, 0.1f, 0f), 20, 90f, false, true);
            yield return new WaitForSeconds(0.5f); // Đợi lắc xong

            // 2. Đẩy ngăn kéo ra mượt mà bằng DOTween
            if (drawerTransform != null)
            {
                drawerTransform.DOLocalMoveX(drawerOpenTargetX, drawerMoveDuration).SetEase(Ease.OutCubic);
                yield return new WaitForSeconds(drawerMoveDuration);
            }

            // 3. Mở khóa Collider để người chơi có thể bấm nhặt chìa
            if (roomKeyCollider != null) roomKeyCollider.enabled = true;

            isProcessing = false;
        }

        // ==========================================
        // CHUỖI HIỆU ỨNG ĐÓNG HỘP KHI MẤT CHÌA
        // ==========================================
        private void HandleKeyCollected()
        {
            // Chỉ chạy hiệu ứng đóng nếu hộp này đã được mở
            if (isUnlocked && drawerTransform != null && drawerTransform.localPosition.x > 0.1f)
            {
                StartCoroutine(CloseAndScrambleRoutine());
            }
        }

        private IEnumerator CloseAndScrambleRoutine()
        {
            isProcessing = true;

            // 1. Lắc nhẹ hộp lần nữa
            boxTransform.DOShakePosition(0.4f, new Vector3(0.05f, 0.05f, 0f), 20, 90f, false, true);
            yield return new WaitForSeconds(0.5f);

            // 2. Kéo ngăn kéo vào lại X = 0
            if (drawerTransform != null)
            {
                drawerTransform.DOLocalMoveX(0f, drawerMoveDuration).SetEase(Ease.InCubic);
                yield return new WaitForSeconds(drawerMoveDuration);
            }

            // 3. Chạy vòng lặp xoay số loạn xạ
            float elapsed = 0f;
            while (elapsed < scrambleTime)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (digitWheels[i] != null)
                    {
                        if (Random.value > 0.5f) digitWheels[i].SpinUp();
                        else digitWheels[i].SpinDown();
                    }
                }
                yield return new WaitForSeconds(0.05f);
                elapsed += 0.05f;
            }

            // Xong việc, hộp vĩnh viễn khóa lại (isUnlocked = true, interactable = false)
        }

        // ==========================================
        // HIỆU ỨNG SAI MÃ (SCRAMBLE)
        // ==========================================
        private IEnumerator ScrambleOnFailRoutine()
        {
            isProcessing = true;
            SetInteractable(false);

            // Lắc hộp báo lỗi
            boxTransform.DOShakePosition(0.4f, new Vector3(0.1f, 0f, 0.1f), 20, 90f, false, true);

            float elapsed = 0f;
            while (elapsed < scrambleTime)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (digitWheels[i] != null)
                    {
                        if (Random.value > 0.5f) digitWheels[i].SpinUp();
                        else digitWheels[i].SpinDown();
                    }
                }
                yield return new WaitForSeconds(0.05f);
                elapsed += 0.05f;
            }

            SetInteractable(true);
            isProcessing = false;
        }

        private void SetInteractable(bool state)
        {
            Collider[] colliders = GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
            {
                // Tránh tắt nhầm Collider của chìa khóa lúc đang quét
                if (col != roomKeyCollider)
                {
                    col.enabled = state;
                }
            }
        }
    }
}