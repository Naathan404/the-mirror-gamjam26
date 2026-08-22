using UnityEngine;
using Game.Core;
using Game.Managers;
using DG.Tweening; // Nhớ dùng DOTween

namespace Game.Interactables
{
    [RequireComponent(typeof(Collider))]
    public class LockInteractable : MonoBehaviour
    {
        [Header("Hover Effect (Game Feel)")]
        [SerializeField] private float _hoverScaleMultiplier = 1.05f; // To lên 5%
        [SerializeField] private float _hoverDuration = 0.2f;

        private Vector3 _originalScale;
        private bool _isInteracted = false;

        private void Start()
        {
            _originalScale = transform.localScale;
        }

        private void OnEnable()
        {
            // Khóa vĩnh viễn ổ khóa sau khi đã kích hoạt ending
            GameEvents.OnGameWon += LockForever;
        }

        private void OnDisable()
        {
            GameEvents.OnGameWon -= LockForever;
        }

        private void LockForever() => _isInteracted = true;

        // ================= HIỆU ỨNG HOVER =================
        private void OnMouseEnter()
        {
            // 1. Nếu chưa có chìa / Đã mở rồi / Đang Pause -> Khóa tương tác hoàn toàn
            if (!GameManager.Instance.HasRoomKey || _isInteracted || GameManager.Instance.CurrentState != GameState.Playing)
                return;

            // 2. Phóng to nhẹ ổ khóa để báo hiệu có thể click
            transform.DOScale(_originalScale * _hoverScaleMultiplier, _hoverDuration).SetEase(Ease.OutSine);
        }

        private void OnMouseExit()
        {
            if (_isInteracted) return;

            // Trả về kích thước cũ khi đưa chuột ra ngoài
            transform.DOScale(_originalScale, _hoverDuration).SetEase(Ease.InSine);
        }

        // ================= XỬ LÝ CLICK =================
        private void OnMouseDown()
        {
            if (_isInteracted || GameManager.Instance.CurrentState != GameState.Playing) return;

            // Vẫn gọi Event để GameManager kiểm tra. 
            // (Nếu chưa có chìa, GameManager có thể phát âm thanh "Cạch" báo lỗi khóa)
            GameEvents.RaiseDoorInteracted();

            

            // Nếu đã có chìa, thu nhỏ lại ngay lập tức và cấm hover tiếp
            if (GameManager.Instance.HasRoomKey)
            {
                transform.DOScale(new Vector2(0f, transform.localScale.y), _hoverDuration).OnComplete(() => gameObject.SetActive(false));
                
            }
            else
            {
                transform.DOKill();
                int r = Random.Range(-1, 1);
                r = r != 0 ? r : 1;
                transform.DOPunchRotation(new Vector3(0f, 0, r * 15f), 0.2f);
            }
        }
    }
}