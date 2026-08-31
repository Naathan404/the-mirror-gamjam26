using UnityEngine;
using DG.Tweening;

namespace Game.Minigames.Laser
{
    public class LaserCell : MonoBehaviour
    {
        [Header("Loại ô")]
        public LaserCellType cellType = LaserCellType.Empty;

        [Header("Grid Pos")]
        public int gridX;
        public int gridY;

        [Header("Cell Status")]
        public LaserDirection gunFacing = LaserDirection.Right;      // chỉ có ý nghĩa nếu là Gun
        public MirrorOrientation mirrorOrientation = MirrorOrientation.Slash; // chỉ có ý nghĩa nếu là Mirror

        [Header("Visual")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Sprite _emptySprite;
        [SerializeField] private Sprite _gunSprite;
        [SerializeField] private Sprite _bulbOffSprite;
        [SerializeField] private Sprite _bulbOnSprite;
        [SerializeField] private Sprite _stoneSprite;
        [SerializeField] private Sprite _mirrorSprite; // vẽ sẵn dạng '/' ở góc 0 độ, xoay 90 độ để ra '\'

        [Header("Visual FX - Additive")]
        [SerializeField] private float _spawnDuration = 0.18f;
        [SerializeField, Range(0f, 1f)] private float _spawnStartAlpha = 0.15f;
        [SerializeField] private float _rotatePunchScale = 0.08f;
        [SerializeField] private float _rotatePunchDuration = 0.16f;
        [SerializeField] private float _beamPunchScale = 0.06f;
        [SerializeField] private float _beamPunchDuration = 0.12f;
        [SerializeField] private float _bulbPunchScale = 0.16f;
        [SerializeField] private float _bulbPunchDuration = 0.22f;
        [SerializeField] private float _blockedPunchScale = 0.12f;
        [SerializeField] private float _blockedPunchDuration = 0.20f;
        [SerializeField] private Color _rotateFlashColor = new Color(1f, 0.92f, 0.55f, 1f);
        [SerializeField] private Color _beamFlashColor = Color.white;
        [SerializeField] private Color _blockedFlashColor = new Color(1f, 0.45f, 0.25f, 1f);

        public bool IsLightUp = false;

        private bool _isRotating = false;

        private Color _baseColor = Color.white;
        private Vector3 _baseVisualScale = Vector3.one;
        private Tween _scaleTween;
        private Tween _colorTween;
        private Sequence _spawnSequence;

        public bool IsLit { get; private set; }
        public bool IsRotatable => cellType == LaserCellType.Gun || cellType == LaserCellType.Mirror;

        public System.Action<LaserCell> OnCellRotated;

        public void Setup(LaserCellType type, int x, int y)
        {
            cellType = type;
            gridX = x;
            gridY = y;
            IsLit = false;
            ApplyVisual();
            CacheVisualState();
        }

        public void SetGunFacing(LaserDirection dir)
        {
            gunFacing = dir;
            transform.localRotation = Quaternion.Euler(0, 0, GetVisualAngle());
        }

        public void SetMirrorOrientation(MirrorOrientation orientation)
        {
            mirrorOrientation = orientation;
            transform.localRotation = Quaternion.Euler(0, 0, GetVisualAngle());
        }

        #region Interact
        public void TryRotate()
        {
            if (!IsRotatable || _isRotating) return;

            if (cellType == LaserCellType.Gun)
                gunFacing = LaserDirectionUtil.RotateClockwise(gunFacing);
            else if (cellType == LaserCellType.Mirror)
                mirrorOrientation = mirrorOrientation == MirrorOrientation.Slash
                    ? MirrorOrientation.Backslash
                    : MirrorOrientation.Slash;

            AudioController.Instance.PlaySFX(SoundName.Lazors_Rotate);
            _isRotating = true;

            // GIỮ NGUYÊN effect xoay đang có.
            transform.DOLocalRotate(new Vector3(0, 0, GetVisualAngle()), 0.15f)
                .SetEase(Ease.OutBack)
                .OnComplete(() => _isRotating = false);

            // VFX bổ sung, chỉ tác động scale/color.
            PlayRotateFeedback();

            OnCellRotated?.Invoke(this);
        }
        #endregion

        #region Visual
        public void SetLit(bool lit)
        {
            bool justTurnedOn = lit && !IsLit;
            IsLit = lit;

            if (_spriteRenderer != null && cellType == LaserCellType.Bulb)
                _spriteRenderer.sprite = lit ? _bulbOnSprite : _bulbOffSprite;

            if (justTurnedOn && cellType == LaserCellType.Bulb)
                PlayBulbLightEffect();
        }

        public void ResetLitVisual()
        {
            if (cellType != LaserCellType.Bulb) return;

            SetLit(false);
            RestoreVisualState();
        }

        /// <summary>
        /// Cell spawn fade/pop. Không đụng position/rotation/collider.
        /// Nếu SpriteRenderer nằm cùng root với collider thì chỉ fade, không scale root.
        /// </summary>
        public void PlaySpawnEffect(float delay = 0f)
        {
            if (_spriteRenderer == null) return;

            _spawnSequence?.Kill();
            _colorTween?.Kill();
            _scaleTween?.Kill();

            Transform visualTransform = GetVisualTransform();
            bool canScaleVisualOnly = visualTransform != transform;

            Color startColor = _baseColor;
            startColor.a *= _spawnStartAlpha;
            _spriteRenderer.color = startColor;

            if (canScaleVisualOnly)
                visualTransform.localScale = _baseVisualScale * 0.82f;

            _spawnSequence = DOTween.Sequence()
                .SetDelay(delay)
                .Append(_spriteRenderer.DOColor(_baseColor, _spawnDuration).SetEase(Ease.OutQuad));

            if (canScaleVisualOnly)
                _spawnSequence.Join(
                    visualTransform.DOScale(_baseVisualScale, _spawnDuration)
                        .SetEase(Ease.OutBack));
        }

        /// <summary>
        /// Feedback khi laser đi qua cell.
        /// </summary>
        public void PlayBeamPassEffect()
        {
            if (_spriteRenderer == null || cellType == LaserCellType.Bulb) return;

            PunchVisual(_beamPunchScale, _beamPunchDuration, _beamFlashColor);
        }

        /// <summary>
        /// Impact riêng khi tia đập vào Stone.
        /// </summary>
        public void PlayBlockedEffect()
        {
            if (_spriteRenderer == null) return;

            PunchVisual(_blockedPunchScale, _blockedPunchDuration, _blockedFlashColor);
        }

        /// <summary>
        /// Nhịp nhẹ khi hoàn thành minigame.
        /// </summary>
        public void PlaySuccessEffect(float delay = 0f)
        {
            if (_spriteRenderer == null) return;

            DOVirtual.DelayedCall(delay, () =>
            {
                if (this == null || _spriteRenderer == null) return;
                PunchVisual(_bulbPunchScale * 0.75f, _bulbPunchDuration, Color.white);
            });
        }

        private void PlayRotateFeedback()
        {
            if (_spriteRenderer == null) return;

            PunchVisual(_rotatePunchScale, _rotatePunchDuration, _rotateFlashColor);
        }

        private void PlayBulbLightEffect()
        {
            if (_spriteRenderer == null) return;

            PunchVisual(_bulbPunchScale, _bulbPunchDuration, Color.white);
        }

        private void PunchVisual(float scaleAmount, float duration, Color flashColor)
        {
            Transform visualTransform = GetVisualTransform();

            _scaleTween?.Kill(true);
            _colorTween?.Kill(true);

            _scaleTween = visualTransform
                .DOPunchScale(_baseVisualScale * scaleAmount, duration, 5, 0.55f);

            Color targetFlash = new Color(
                _baseColor.r * flashColor.r,
                _baseColor.g * flashColor.g,
                _baseColor.b * flashColor.b,
                _baseColor.a);

            _colorTween = _spriteRenderer
                .DOColor(targetFlash, duration * 0.35f)
                .SetLoops(2, LoopType.Yoyo)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    if (_spriteRenderer != null)
                        _spriteRenderer.color = _baseColor;
                });
        }

        private Transform GetVisualTransform()
        {
            return _spriteRenderer != null ? _spriteRenderer.transform : transform;
        }

        private void CacheVisualState()
        {
            if (_spriteRenderer == null) return;

            _baseColor = _spriteRenderer.color;
            _baseVisualScale = GetVisualTransform().localScale;
        }

        private void RestoreVisualState()
        {
            if (_spriteRenderer == null) return;

            _scaleTween?.Kill();
            _colorTween?.Kill();
            _spawnSequence?.Kill();

            GetVisualTransform().localScale = _baseVisualScale;
            _spriteRenderer.color = _baseColor;
        }

        private float GetVisualAngle()
        {
            if (cellType == LaserCellType.Gun)
            {
                switch (gunFacing)
                {
                    case LaserDirection.Up: return 90f;
                    case LaserDirection.Right: return 0f;
                    case LaserDirection.Down: return -90f;
                    case LaserDirection.Left: return 180f;
                }
            }
            else if (cellType == LaserCellType.Mirror)
            {
                return mirrorOrientation == MirrorOrientation.Slash ? 0f : 90f;
            }
            return 0f;
        }

        private void ApplyVisual()
        {
            if (_spriteRenderer != null)
            {
                switch (cellType)
                {
                    case LaserCellType.Empty: _spriteRenderer.sprite = _emptySprite; break;
                    case LaserCellType.Gun: _spriteRenderer.sprite = _gunSprite; break;
                    case LaserCellType.Bulb: _spriteRenderer.sprite = _bulbOffSprite; break;
                    case LaserCellType.Stone: _spriteRenderer.sprite = _stoneSprite; break;
                    case LaserCellType.Mirror: _spriteRenderer.sprite = _mirrorSprite; break;
                }
            }

            transform.localRotation = Quaternion.Euler(0, 0, GetVisualAngle());
        }

        private void OnDestroy()
        {
            _scaleTween?.Kill();
            _colorTween?.Kill();
            _spawnSequence?.Kill();
        }

        #endregion
    }
}
