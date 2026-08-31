using System.Collections.Generic;
using DG.Tweening;
using Game.Effect;
using TMPro;
using UnityEngine;

namespace Game.Minigames
{
    public sealed class MastermindMinigameController : MinigameBaseController
    {
        [Header("Configs")]
        [SerializeField] private MastermindConfig _config;

        [SerializeField] private SpriteRenderer _background;

        [Header("Prefabs")]
        [SerializeField] private SpriteRenderer _slotIconPrefab;
        [SerializeField] private SymbolButton _symbolButtonPrefab;

        [Header("Layout containers")]
        [SerializeField] private Transform _guessSlotContainer;
        [SerializeField] private Transform _symbolButtonContainer;

        [SerializeField] private Collider _deleteButtonCollider;
        [SerializeField] private Collider _submitButtonCollider;

        [Header("Feedback")]
        [SerializeField] private TextMeshPro _feedbackText;

        [Header("History")]
        [SerializeField] private GuessHistoryRow _historyRowPrefab;
        [SerializeField] private int _maxVisibleHistoryRows = 4;
        [SerializeField] private Transform _historyContainer;
        [SerializeField] private float _historyRowSpacing = 0.3f;

        [Header("Layout Padding")]
        [SerializeField, Range(0f, 0.4f)] private float _horizontalPaddingRatio = 0.12f;
        [SerializeField, Range(0f, 0.4f)] private float _topPaddingRatio = 0.15f;
        [SerializeField, Range(0f, 0.4f)] private float _bottomPaddingRatio = 0.15f;

        [Header("Visual Effects - Slot")]
        [SerializeField, Range(0.1f, 1f)] private float _slotPopStartScale = 0.55f;
        [SerializeField] private float _slotPopDuration = 0.18f;
        [SerializeField, Range(0.05f, 1f)] private float _slotRemoveTargetScale = 0.2f;
        [SerializeField] private float _slotRemoveDuration = 0.14f;
        [SerializeField] private float _submittedSlotPunchScale = 0.12f;
        [SerializeField] private float _submittedSlotPunchDuration = 0.2f;
        [SerializeField] private float _submittedSlotStagger = 0.035f;

        [Header("Visual Effects - Buttons")]
        [SerializeField] private float _actionButtonPunchScale = 0.1f;
        [SerializeField] private float _actionButtonPunchDuration = 0.16f;
        [SerializeField] private float _symbolSpawnStagger = 0.045f;

        [Header("Visual Effects - Feedback")]
        [SerializeField] private float _feedbackPunchScale = 0.15f;
        [SerializeField] private float _feedbackPunchDuration = 0.22f;

        [Header("Visual Effects - History")]
        [SerializeField, Range(0.1f, 1f)] private float _historySpawnStartScale = 0.78f;
        [SerializeField] private float _historySpawnDuration = 0.2f;
        [SerializeField] private float _historyMoveDuration = 0.2f;

        private readonly List<SpriteRenderer> _guessSlotIcons = new();
        private readonly List<Vector3> _guessSlotBaseScales = new();
        private readonly List<Tween> _slotScaleTweens = new();
        private readonly List<SymbolButton> _symbolButtons = new();
        private readonly List<int> _currentGuess = new();

        private bool _isChecking = false;
        private int[] _secretCode;
        private int _currentAttempt;

        private class GuessHistoryEntry
        {
            public int[] Guess;
            public int Exact;
            public int Partial;
        }

        // Lưu toàn bộ lịch sử dữ liệu
        private readonly List<GuessHistoryEntry> _guessHistory = new();

        // Chỉ lưu những row GameObject đang hiển thị
        private readonly List<GuessHistoryRow> _historyRows = new();

        #region Base
        protected override void OnDifficultyIncrease(int minigamePassed)
        {
            _config = _difficultyConfig.GetMinigameConfig<MastermindConfig>(minigamePassed);
        }

        protected override void OnGameReset()
        {
            ClearGuessRow();
        }

        protected override void OnGameStart()
        {
            _currentAttempt = 0;
            _feedbackText.text = "";
            ClearHistory();
            GenerateSecretCode();
            SpawnSlots();
            SpawnSymbolButtons();
            ClearGuessRow();
        }

        protected override void OnGameClosed()
        {
            OnGameReset();
            ClearHistory();
            ClearContainer(_guessSlotContainer);
            ClearContainer(_symbolButtonContainer);
            _guessSlotIcons.Clear();
            _guessSlotBaseScales.Clear();
            _slotScaleTweens.Clear();
            _symbolButtons.Clear();
        }
        #endregion

        #region Utils
        private void ClearGuessRow()
        {
            _currentGuess.Clear();
            _isChecking = false;

            for (int i = 0; i < _guessSlotIcons.Count; i++)
            {
                var icon = _guessSlotIcons[i];
                if (icon == null) continue;

                if (i < _slotScaleTweens.Count && _slotScaleTweens[i] != null && _slotScaleTweens[i].IsActive())
                {
                    _slotScaleTweens[i].Kill();
                    _slotScaleTweens[i] = null;
                }

                if (i < _guessSlotBaseScales.Count)
                    icon.transform.localScale = _guessSlotBaseScales[i];

                icon.enabled = false;
            }

            if (_symbolUsesRemaining != null)
            {
                for (int i = 0; i < _symbolUsesRemaining.Length; i++)
                {
                    _symbolUsesRemaining[i] = 1;
                    _symbolButtons[i].SetInteractable(true);
                }
            }
        }

        private void ClearContainer(Transform container)
        {
            for (int i = container.childCount - 1; i >= 0; i--)
                Destroy(container.GetChild(i).gameObject);
        }

        private void RefreshHistoryLayout(bool animate = false)
        {
            Vector3 startLocalPos =
                _background.transform.InverseTransformPoint(_historyContainer.position);

            for (int i = 0; i < _historyRows.Count; i++)
            {
                if (_historyRows[i] == null)
                    continue;

                Vector3 localOffset = new Vector3(
                    0f,
                    -i * _historyRowSpacing,
                    0f
                );

                Vector3 targetLocalPos = startLocalPos + localOffset;
                Vector3 targetWorldPos = _background.transform.TransformPoint(targetLocalPos);

                if (animate)
                {
                    _historyRows[i].transform
                        .DOMove(targetWorldPos, _historyMoveDuration)
                        .SetEase(Ease.OutCubic);
                }
                else
                {
                    _historyRows[i].transform.position = targetWorldPos;
                }

                // Giữ nguyên rotation hiện tại của history row như logic cũ.
                // _historyRows[i].transform.rotation =
                //     _background.transform.rotation;
            }
        }

        private void ClearHistory()
        {
            _guessHistory.Clear();

            for (int i = _historyRows.Count - 1; i >= 0; i--)
            {
                if (_historyRows[i] != null)
                    Destroy(_historyRows[i].gameObject);
            }

            _historyRows.Clear();
        }
        #endregion

        private void Update()
        {
            if (!isPlaying || isCompleting || !isFocused || _isChecking) return;

            if (Input.GetMouseButtonDown(0))
                HandleClick();
        }

        #region Inputs
        private void HandleClick()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 100f)) return;

            if (_deleteButtonCollider != null && hit.collider == _deleteButtonCollider)
            {
                AudioController.Instance.PlaySFX(SoundName.Lazors_Rotate);
                PlayActionButtonPunch(_deleteButtonCollider);
                DeleteSymbol();
                return;
            }

            if (_submitButtonCollider != null && hit.collider == _submitButtonCollider)
            {
                PlayActionButtonPunch(_submitButtonCollider);
                SubmitSymbol();
                return;
            }

            if (hit.collider.TryGetComponent(out SymbolButton symbolButton) && symbolButton.gameObject.activeSelf)
            {
                AudioController.Instance.PlaySFX(SoundName.Button3DClick);
                OnSymbolButtonClicked(symbolButton);
            }
        }
        #endregion

        #region Core logic
        private void SpawnSlots()
        {
            ClearContainer(_guessSlotContainer);
            _guessSlotIcons.Clear();
            _guessSlotBaseScales.Clear();
            _slotScaleTweens.Clear();

            float spriteWidth = _background.sprite.bounds.size.x;
            float spriteHeight = _background.sprite.bounds.size.y;

            float sideMargin = spriteWidth * _horizontalPaddingRatio;
            float usableWidth = spriteWidth - sideMargin * 2f;
            float spacing = _config.CodeLength > 1 ? usableWidth / (_config.CodeLength - 1) : 0f;
            float startX = -usableWidth / 2f;

            float topLocalY = spriteHeight / 2f - spriteHeight * _topPaddingRatio;

            for (int i = 0; i < _config.CodeLength; i++)
            {
                var icon = Instantiate(_slotIconPrefab, _guessSlotContainer);
                Vector3 localOffset = new Vector3(startX + spacing * i, topLocalY, 0f);
                icon.transform.position = _background.transform.TransformPoint(localOffset);
                icon.transform.rotation = _background.transform.rotation;
                icon.enabled = false;

                _guessSlotIcons.Add(icon);
                _guessSlotBaseScales.Add(icon.transform.localScale);
                _slotScaleTweens.Add(null);
            }
        }

        private int[] _symbolUsesRemaining;

        private void SpawnSymbolButtons()
        {
            ClearContainer(_symbolButtonContainer);
            _symbolButtons.Clear();

            float spriteWidth = _background.sprite.bounds.size.x;
            float spriteHeight = _background.sprite.bounds.size.y;

            float sideMargin = spriteWidth * _horizontalPaddingRatio;
            float usableWidth = spriteWidth - sideMargin * 2f;
            float spacing = _config.SymbolCount > 1 ? usableWidth / (_config.SymbolCount - 1) : 0f;
            float startX = -usableWidth / 2f;

            float bottomLocalY = -spriteHeight / 2f + spriteHeight * _bottomPaddingRatio;

            for (int i = 0; i < _config.SymbolCount; i++)
            {
                var btn = Instantiate(_symbolButtonPrefab, _symbolButtonContainer);
                Vector3 localOffset = new Vector3(startX + spacing * i, bottomLocalY, 0f);
                btn.transform.position = _background.transform.TransformPoint(localOffset);
                btn.transform.rotation = _background.transform.rotation;
                btn.Init(i, _config.SymbolSprites[i]);
                btn.PlaySpawnEffect(i * _symbolSpawnStagger);
                _symbolButtons.Add(btn);
            }

            // Nếu không cho lặp, mỗi symbol chỉ dùng được 1 lần cho tới khi bị xoá ra khỏi guess
            if (!_config.AllowDuplicateSymbols)
            {
                _symbolUsesRemaining = new int[_config.SymbolCount];
                for (int i = 0; i < _symbolUsesRemaining.Length; i++)
                    _symbolUsesRemaining[i] = 1;
            }
            else
            {
                _symbolUsesRemaining = null; // không giới hạn
            }
        }

        private void GenerateSecretCode()
        {
            _secretCode = new int[_config.CodeLength];

            if (_config.AllowDuplicateSymbols)
            {
                for (int i = 0; i < _config.CodeLength; i++)
                    _secretCode[i] = UnityEngine.Random.Range(0, _config.SymbolCount);
            }
            else
            {
                // Fisher-Yates
                var pool = new List<int>();
                for (int i = 0; i < _config.SymbolCount; i++) pool.Add(i);

                for (int i = pool.Count - 1; i > 0; i--)
                {
                    int j = UnityEngine.Random.Range(0, i + 1);
                    (pool[i], pool[j]) = (pool[j], pool[i]);
                }

                for (int i = 0; i < _config.CodeLength; i++)
                    _secretCode[i] = pool[i];
            }
        }

        private void OnSymbolButtonClicked(SymbolButton btn)
        {
            if (_currentGuess.Count >= _config.CodeLength) return;

            if (_symbolUsesRemaining != null)
            {
                if (_symbolUsesRemaining[btn.SymbolId] <= 0) return; // đã dùng hết, chặn click
                _symbolUsesRemaining[btn.SymbolId]--;
                if (_symbolUsesRemaining[btn.SymbolId] <= 0)
                    btn.SetInteractable(false); // đổi màu xám / disable collider
            }

            _currentGuess.Add(btn.SymbolId);
            UpdateSlotIcon(_currentGuess.Count - 1, btn.SymbolId);
        }

        private void DeleteSymbol()
        {
            if (_currentGuess.Count <= 0)
            {
                return;
            }

            int lastIdx = _currentGuess.Count - 1;
            int removedSymbolId = _currentGuess[lastIdx];

            _currentGuess.RemoveAt(lastIdx);

            PlaySlotRemoveEffect(lastIdx);

            // Logic cũ vẫn là ẩn slot ngay lập tức.
            _guessSlotIcons[lastIdx].enabled = false;

            if (_symbolUsesRemaining != null)
            {
                _symbolUsesRemaining[removedSymbolId]++;
                _symbolButtons[removedSymbolId].SetInteractable(true);
                _symbolButtons[removedSymbolId].PlayRestoreEffect();
            }
        }

        private void SubmitSymbol()
        {
            if (_currentGuess.Count < _config.CodeLength)
            {
                AudioController.Instance.PlaySFX(SoundName.Lazors_Rotate);
                Camera.main.transform.DOShakePosition(0.2f, 0.2f);
                PlayGuessSlotsInvalidEffect();
                return;
            }

            _isChecking = true;
            AudioController.Instance.PlaySFX(SoundName.ButtonClick);

            var guessArray = _currentGuess.ToArray();
            var (exact, partial) = EvaluateGuess(guessArray, _secretCode);
            _feedbackText.text = $"● {exact} \t\t ○ {partial}";

            PlaySubmittedGuessEffect();
            PlayFeedbackEffect();
            AddHistoryRow(guessArray, exact, partial);

            if (exact == _config.CodeLength)
            {
                PlaySuccessEffect();
                CompleteMinigame();
                return;
            }

            _currentAttempt++;

            if (_currentAttempt >= _config.MaxAttempts)
            {
                OnFailed();
                return;
            }

            AudioController.Instance.PlaySFX(SoundName.ButtonClick);
            FilterController.Instance.FlashScreen(Color.white);

            ClearGuessRow();
        }

        private void AddHistoryRow(int[] guess, int exact, int partial)
        {
            var entry = new GuessHistoryEntry
            {
                Guess = (int[])guess.Clone(),
                Exact = exact,
                Partial = partial
            };

            _guessHistory.Add(entry);

            var row = Instantiate(_historyRowPrefab, _historyContainer);
            row.transform.position = _historyContainer.position;

            row.Setup(
                entry.Guess,
                _config.SymbolSprites,
                entry.Exact,
                entry.Partial
            );

            Vector3 rowBaseScale = row.transform.localScale;
            row.transform.localScale = rowBaseScale * _historySpawnStartScale;

            _historyRows.Add(row);

            while (_historyRows.Count > _maxVisibleHistoryRows)
            {
                var oldestRow = _historyRows[0];
                _historyRows.RemoveAt(0);

                if (oldestRow != null)
                    Destroy(oldestRow.gameObject);
            }

            RefreshHistoryLayout(true);

            row.transform
                .DOScale(rowBaseScale, _historySpawnDuration)
                .SetEase(Ease.OutBack);
        }

        private (int exact, int partial) EvaluateGuess(int[] guess, int[] secret)
        {
            int exact = 0;
            var secretCount = new Dictionary<int, int>();
            var guessCount = new Dictionary<int, int>();

            for (int i = 0; i < guess.Length; i++)
            {
                if (guess[i] == secret[i])
                {
                    exact++;
                    continue;
                }

                secretCount[secret[i]] = secretCount.GetValueOrDefault(secret[i]) + 1;
                guessCount[guess[i]] = guessCount.GetValueOrDefault(guess[i]) + 1;
            }

            int partial = 0;
            foreach (var kv in guessCount)
                partial += Mathf.Min(kv.Value, secretCount.GetValueOrDefault(kv.Key));

            return (exact, partial);
        }
        #endregion

        #region Visual
        private void UpdateSlotIcon(int index, int symbolId)
        {
            var icon = _guessSlotIcons[index];
            Vector3 baseScale = _guessSlotBaseScales[index];

            if (_slotScaleTweens[index] != null && _slotScaleTweens[index].IsActive())
                _slotScaleTweens[index].Kill();

            icon.sprite = _config.SymbolSprites[symbolId];
            icon.enabled = true;

            icon.transform.localScale = baseScale * _slotPopStartScale;
            _slotScaleTweens[index] = icon.transform
                .DOScale(baseScale, _slotPopDuration)
                .SetEase(Ease.OutBack);
        }


        private void PlaySlotRemoveEffect(int index)
        {
            if (index < 0 || index >= _guessSlotIcons.Count) return;

            var source = _guessSlotIcons[index];
            if (source == null || !source.enabled) return;

            var ghost = Instantiate(_slotIconPrefab, _guessSlotContainer);
            ghost.sprite = source.sprite;
            ghost.enabled = true;
            ghost.transform.position = source.transform.position;
            ghost.transform.rotation = source.transform.rotation;
            ghost.transform.localScale = index < _guessSlotBaseScales.Count
                ? _guessSlotBaseScales[index]
                : source.transform.localScale;

            Vector3 targetScale = ghost.transform.localScale * _slotRemoveTargetScale;
            ghost.transform
                .DOScale(targetScale, _slotRemoveDuration)
                .SetEase(Ease.InBack)
                .OnComplete(() =>
                {
                    if (ghost != null)
                        Destroy(ghost.gameObject);
                });
        }

        private void PlayActionButtonPunch(Collider buttonCollider)
        {
            if (buttonCollider == null) return;

            buttonCollider.transform.DOPunchScale(
                Vector3.one * _actionButtonPunchScale,
                _actionButtonPunchDuration,
                5,
                0.5f
            );
        }

        private void PlayFeedbackEffect()
        {
            if (_feedbackText == null) return;

            _feedbackText.transform.DOPunchScale(
                Vector3.one * _feedbackPunchScale,
                _feedbackPunchDuration,
                6,
                0.5f
            );
        }

        private void PlaySubmittedGuessEffect()
        {
            for (int i = 0; i < _guessSlotIcons.Count; i++)
            {
                if (_guessSlotIcons[i] == null || !_guessSlotIcons[i].enabled)
                    continue;

                if (_slotScaleTweens[i] != null && _slotScaleTweens[i].IsActive())
                    _slotScaleTweens[i].Kill();

                _slotScaleTweens[i] = _guessSlotIcons[i].transform
                    .DOPunchScale(
                        Vector3.one * _submittedSlotPunchScale,
                        _submittedSlotPunchDuration,
                        5,
                        0.5f
                    )
                    .SetDelay(i * _submittedSlotStagger);
            }
        }

        private void PlayGuessSlotsInvalidEffect()
        {
            if (_guessSlotContainer == null) return;

            _guessSlotContainer.DOPunchRotation(
                new Vector3(0f, 0f, 4f),
                0.2f,
                8,
                0.6f
            );
        }

        private void PlaySuccessEffect()
        {
            for (int i = 0; i < _guessSlotIcons.Count; i++)
            {
                if (_guessSlotIcons[i] == null || !_guessSlotIcons[i].enabled)
                    continue;

                if (_slotScaleTweens[i] != null && _slotScaleTweens[i].IsActive())
                    _slotScaleTweens[i].Kill();

                _slotScaleTweens[i] = _guessSlotIcons[i].transform
                    .DOPunchScale(Vector3.one * 0.18f, 0.28f, 6, 0.45f)
                    .SetDelay(i * 0.04f);
            }
        }
        #endregion
    }
}
