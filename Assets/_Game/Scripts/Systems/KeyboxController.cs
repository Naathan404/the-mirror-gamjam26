using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game.Core;
using DG.Tweening;
using Game.Managers;
using Game.Effect;
using KeyCode = Game.Core.KeyCode;
using Game.Utils;

namespace Game.Systems.Lock
{

    public class KeyboxController : MonoBehaviour
    {
        public class ShapeDigit
        {
            public KeyShape Shape;
            public int Digit;    
        }

        [Header("Button Refs")]
        public NumberWheel[] digitWheels = new NumberWheel[4];
        public KeyboxButton submitButton;

        [Header("Animation")]
        [Tooltip("Object chứa toàn bộ hình ảnh hộp khóa")]
        public Transform boxTransform;
        public float scrambleTime = 1f; // Thời gian quay số loạn xạ sau khi đóng

        [Header("Key Drawer")]
        public Transform drawerTransform;
        public float drawerOpenTargetX = 3f;
        public float drawerMoveDuration = 0.8f;

        [Header("KeyCode")]
        [SerializeField] private List<SpriteRenderer> _allKeyShapes = new List<SpriteRenderer>(4);
        [SerializeField] private Sprite _squareSprite;
        [SerializeField] private Sprite _crossSprite;
        [SerializeField] private Sprite _cirleSprite;
        [SerializeField] private Sprite _triangleSprite;

        public Collider roomKeyCollider;

        private List<KeyCode> targetPasscode = new List<KeyCode>(4);
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

        private void SetupTargetPasscode(Dictionary<MinigameType, KeyCode> minigameDigitMap)
        {
            if (minigameDigitMap == null)
            {
                Debug.LogError("[KeyboxController] Passcode setup failed because minigame digit map is null.");
                return;
            }

            targetPasscode.Clear();

            AddPasscodeDigit(minigameDigitMap, MinigameType.Maze);
            AddPasscodeDigit(minigameDigitMap, MinigameType.CardMatch);
            AddPasscodeDigit(minigameDigitMap, MinigameType.Wires);
            AddPasscodeDigit(minigameDigitMap, MinigameType.WordSearch);

            if (targetPasscode.Count < GameConstants.NUMBER_OF_MINIGAMES)
            {
                Debug.LogError($"[KeyboxController] Passcode setup failed. Expected {GameConstants.NUMBER_OF_MINIGAMES} digits but received {targetPasscode.Count}.");
                return;
            }

            targetPasscode = ShuffleHelper.Shuffle(targetPasscode);
            UpdateKeyShapeSprites();
        }

        private void AddPasscodeDigit(Dictionary<MinigameType, KeyCode> minigameDigitMap, MinigameType minigameType)
        {
            if (minigameDigitMap.TryGetValue(minigameType, out KeyCode keyCode))
            {
                targetPasscode.Add(keyCode);
            }
        }

        private void UpdateKeyShapeSprites()
        {
            if (_allKeyShapes == null)
            {
                Debug.LogError("[KeyboxController] Key shape sprite renderers are not assigned.");
                return;
            }

            int count = Mathf.Min(targetPasscode.Count, _allKeyShapes.Count);

            for (int i = 0; i < count; i++)
            {
                if (_allKeyShapes[i] == null)
                {
                    continue;
                }

                _allKeyShapes[i].sprite = GetShapeSprite(targetPasscode[i].Shape);
            }
        }

        private Sprite GetShapeSprite(KeyShape shape)
        {
            if (shape == KeyShape.Square)
            {
                return _squareSprite;
            }

            if (shape == KeyShape.Cross)
            {
                return _crossSprite;
            }

            if (shape == KeyShape.Circle)
            {
                return _cirleSprite;
            }

            if (shape == KeyShape.Triangle)
            {
                return _triangleSprite;
            }

            return null;
        }

        private void TryOpenBox()
        {
            if (isUnlocked || isProcessing)
            {
                return;
            }

            if (targetPasscode.Count < GameConstants.NUMBER_OF_MINIGAMES)
            {
                Debug.LogError($"[KeyboxController] Cannot open box because passcode is incomplete. Expected {GameConstants.NUMBER_OF_MINIGAMES} digits but has {targetPasscode.Count}.");
                return;
            }

            if (GameManager.Instance.MinigamePassed < GameConstants.NUMBER_OF_MINIGAMES)
            {
#if UNITY_EDITOR
                Debug.Log("Chưa giải đủ Minigame");
#endif
                StartCoroutine(ScrambleOnFailRoutine());
                FilterController.Instance.FlashScreen(Color.white, 0.25f);
                return;
            }

            int digitCount = Mathf.Min(GameConstants.NUMBER_OF_MINIGAMES, digitWheels.Length);
            bool isCorrect = true;
            for (int i = 0; i < digitCount; i++)
            {
                if (digitWheels[i] != null && digitWheels[i].CurrentValue != targetPasscode[i].Digit)
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
                FilterController.Instance.FlashScreen(Color.white, 0.25f);
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