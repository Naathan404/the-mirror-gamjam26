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

        public bool IsLightUp = false;

        private bool _isRotating = false;

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
            transform.DOLocalRotate(new Vector3(0, 0, GetVisualAngle()), 0.15f)
                .SetEase(Ease.OutBack)
                .OnComplete(() => _isRotating = false);
            
            OnCellRotated?.Invoke(this);
        }
        #endregion

        #region Visual
        public void SetLit(bool lit)
        {
            IsLit = lit;
            if (_spriteRenderer != null && cellType == LaserCellType.Bulb)
                _spriteRenderer.sprite = lit ? _bulbOnSprite : _bulbOffSprite;
        }

        public void ResetLitVisual()
        {
            if (cellType == LaserCellType.Bulb) SetLit(false);
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

        #endregion
    }
}