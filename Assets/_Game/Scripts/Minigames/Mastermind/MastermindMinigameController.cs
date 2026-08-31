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


        [Header("Layout Padding")]
        [SerializeField, Range(0f, 0.4f)] private float _horizontalPaddingRatio = 0.12f;
        [SerializeField, Range(0f, 0.4f)] private float _topPaddingRatio = 0.15f;
        [SerializeField, Range(0f, 0.4f)] private float _bottomPaddingRatio = 0.15f;

        private List<SpriteRenderer> _guessSlotIcons = new();
        private List<SymbolButton> _symbolButtons = new();
        private List<int> _currentGuess = new();

        private bool _isChecking = false;
        private int[] _secretCode;
        private int _currentAttempt;

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
            GenerateSecretCode();
            SpawnSlots();
            SpawnSymbolButtons();
            ClearGuessRow();
        }

        protected override void OnGameClosed()
        {
            OnGameReset();
            ClearContainer(_guessSlotContainer);
            ClearContainer(_symbolButtonContainer);
            _guessSlotIcons.Clear();
            _symbolButtons.Clear();
        }
        #endregion

        #region Utils
        private void ClearGuessRow()
        {
            _currentGuess.Clear();
            _isChecking = false;
            foreach (var icon in _guessSlotIcons) icon.enabled = false;

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
                DeleteSymbol();
                return;
            }

            if (_submitButtonCollider != null && hit.collider == _submitButtonCollider)
            {
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
            _guessSlotIcons[lastIdx].enabled = false;

            if (_symbolUsesRemaining != null)
            {
                _symbolUsesRemaining[removedSymbolId]++;
                _symbolButtons[removedSymbolId].SetInteractable(true); 
            }
        }

        private void SubmitSymbol()
        {
            if (_currentGuess.Count < _config.CodeLength)
            {
                // chưa nnhapaj đủ ký tự thì không cho ấn nút đoán    
                AudioController.Instance.PlaySFX(SoundName.Lazors_Rotate);
                Camera.main.transform.DOShakePosition(0.2f, 0.2f);
                return;
            }

            _isChecking = true;
            AudioController.Instance.PlaySFX(SoundName.ButtonClick);

            var (exact, partial) = EvaluateGuess(_currentGuess.ToArray(), _secretCode);
            _feedbackText.text = $"● {exact} \t\t ○ {partial}";

            if (exact == _config.CodeLength)
            {
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

        private (int exact, int partial) EvaluateGuess(int[] guess, int[] secret)
        {
            int exact = 0;
            var secretCount = new Dictionary<int, int>();
            var guessCount = new Dictionary<int, int>();

            for (int i = 0; i < guess.Length; i++)
            {
                if (guess[i] == secret[i]) { exact++; continue; }
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
            _guessSlotIcons[index].sprite = _config.SymbolSprites[symbolId];
            _guessSlotIcons[index].enabled = true;
        } 
        #endregion
    }
}