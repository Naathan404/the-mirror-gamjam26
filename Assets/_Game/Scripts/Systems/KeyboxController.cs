using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game.Core;

namespace Game.Systems.Lock
{
    public class KeyboxController : MonoBehaviour
    {
        [Header("Tham chiếu Cơ khí")]
        public NumberWheel[] digitWheels = new NumberWheel[4];
        public KeyboxButton submitButton;

        [Header("Hiệu ứng Keybox (Game Feel)")]
        [Tooltip("Object chứa toàn bộ hình ảnh hộp khóa (thường là chính nó)")]
        public Transform boxTransform;
        public float joltScale = 1.05f; // Phóng to 5% khi giật
        public float joltSpeed = 20f;
        public float scrambleTime = 0.2f; // Tốc độ xoay loạn xạ khi nhập sai

        private int[] targetPasscode = new int[4];
        private bool isUnlocked = false;
        private bool isProcessing = false; // Cờ khóa hệ thống khi đang chạy hiệu ứng

        private Vector3 originalScale;
        private Coroutine joltCoroutine;

        private void Start()
        {
            if (boxTransform == null) boxTransform = transform;
            originalScale = boxTransform.localScale;
        }

        private void OnEnable()
        {
            GameEvents.OnPasscodeGenerated += SetupTargetPasscode;
            if (submitButton != null) submitButton.OnClicked += TryOpenBox;
        }

        private void OnDisable()
        {
            GameEvents.OnPasscodeGenerated -= SetupTargetPasscode;
            if (submitButton != null) submitButton.OnClicked -= TryOpenBox;
        }

        private void SetupTargetPasscode(Dictionary<MinigameType, int> minigameDigitMap)
        {
            if (minigameDigitMap.TryGetValue(MinigameType.Maze, out int d0)) targetPasscode[0] = d0;
            if (minigameDigitMap.TryGetValue(MinigameType.CardMatch, out int d1)) targetPasscode[1] = d1;
            if (minigameDigitMap.TryGetValue(MinigameType.Wires, out int d2)) targetPasscode[2] = d2;
            if (minigameDigitMap.TryGetValue(MinigameType.WordSearch, out int d3)) targetPasscode[3] = d3;

            Debug.Log($"[Keybox] Đã nhận mã bí mật: {targetPasscode[0]}{targetPasscode[1]}{targetPasscode[2]}{targetPasscode[3]}");
        }

        private void TryOpenBox()
        {
            if (isUnlocked || isProcessing) return;

            bool isCorrect = true;
            for (int i = 0; i < 4; i++)
            {
                if (digitWheels[i] != null && digitWheels[i].CurrentValue != targetPasscode[i])
                {
                    isCorrect = false;
                    break;
                }
            }

            if (isCorrect)
            {
                Debug.Log("[Keybox] 🎉 MẬT MÃ CHÍNH XÁC! Hộp mở ra!");
                isUnlocked = true;

                // Giật nhẹ hộp khóa báo hiệu thành công
                TriggerJoltEffect();

                // Ẩn toàn bộ nút bấm / Khóa tương tác
                SetInteractable(false);

                // TODO: Bổ sung code thả chìa khóa rớt ra tại đây (nếu có)

                GameEvents.RaiseLockUnlocked();
            }
            else
            {
                Debug.LogWarning("[Keybox] ❌ Sai mã! Đang reset bảng số...");
                StartCoroutine(ScrambleOnFailRoutine());
            }
        }

        // ==========================================
        // HIỆU ỨNG 1: GIẬT KEYBOX (PUNCH SCALE)
        // ==========================================
        private void TriggerJoltEffect()
        {
            if (joltCoroutine != null) StopCoroutine(joltCoroutine);
            joltCoroutine = StartCoroutine(JoltRoutine());
        }

        private IEnumerator JoltRoutine()
        {
            Vector3 targetScale = originalScale * joltScale;

            // Phóng to cực nhanh (Tạo lực đập)
            while (Vector3.Distance(boxTransform.localScale, targetScale) > 0.001f)
            {
                boxTransform.localScale = Vector3.Lerp(boxTransform.localScale, targetScale, Time.deltaTime * joltSpeed * 2f);
                yield return null;
            }

            // Thu nhỏ về nguyên bản từ từ (Tạo độ đàn hồi)
            while (Vector3.Distance(boxTransform.localScale, originalScale) > 0.001f)
            {
                boxTransform.localScale = Vector3.Lerp(boxTransform.localScale, originalScale, Time.deltaTime * joltSpeed);
                yield return null;
            }

            boxTransform.localScale = originalScale;
        }

        // ==========================================
        // HIỆU ỨNG 2: QUAY SỐ LOẠN XẠ KHI NHẬP SAI (SCRAMBLE)
        // ==========================================
        private IEnumerator ScrambleOnFailRoutine()
        {
            isProcessing = true;

            // Tắt toàn bộ Collider để người chơi không bấm hay lăn chuột phá bĩnh lúc máy đang xoay
            SetInteractable(false);

            TriggerJoltEffect(); // Rung giật báo sai mã

            float duration = scrambleTime; // Thời gian xoay loạn xạ
            float elapsed = 0f;

            // Vòng lặp xoay số tít thò lò
            while (elapsed < duration)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (digitWheels[i] != null)
                    {
                        // 50% quay lên, 50% quay xuống
                        if (Random.value > 0.5f) digitWheels[i].SpinUp();
                        else digitWheels[i].SpinDown();
                    }
                }

                // Tốc độ xoay loạn (0.05 giây đổi số 1 lần -> Rất nhanh!)
                yield return new WaitForSeconds(0.05f);
                elapsed += 0.05f;
            }

            // Mở lại các Collider để người chơi nhập mã mới
            SetInteractable(true);
            isProcessing = false;
        }

        // ==========================================
        // HÀM HỖ TRỢ: KHÓA/MỞ TƯƠNG TÁC
        // ==========================================
        private void SetInteractable(bool state)
        {
            // Quét và Bật/Tắt toàn bộ Collider nằm bên trong Keybox
            Collider[] colliders = GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
            {
                col.enabled = state;
            }
        }
    }
}