using UnityEngine;
using TMPro;

namespace Game.Minigames.WordSearch
{
    public class WordSearchClueItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text clueText;
        [SerializeField] private GameObject foundOverlay;
        [SerializeField] private SpriteRenderer paperBg;
        [SerializeField] private float horizontalPadding = 0.5f;

        public string TargetWord { get; private set; }

        // --- THÊM THAM SỐ baseSortingOrder VÀO ĐÂY ---
        public void Initialize(string word, int baseSortingOrder)
        {
            TargetWord = word;
            clueText.text = word;

            if (foundOverlay != null)
                foundOverlay.SetActive(false);

            clueText.color = Color.black;

            // 1. CẬP NHẬT SORTING ORDER LINH HOẠT
            // 1. CẬP NHẬT SORTING ORDER LINH HOẠT
            if (paperBg != null)
            {
                paperBg.sortingOrder = baseSortingOrder;           // Tầng giấy (VD: 10)
            }

            // Lấy component Renderer của Text 3D để set Sorting Order
            if (clueText != null && clueText.TryGetComponent<Renderer>(out var textRenderer))
            {
                textRenderer.sortingOrder = baseSortingOrder + 1;  // Tầng chữ (VD: 11)
            }

            if (foundOverlay != null && foundOverlay.TryGetComponent<SpriteRenderer>(out var overlayRenderer))
            {
                overlayRenderer.sortingOrder = baseSortingOrder + 2; // Tầng mực che (VD: 12)
            }

            // 2. LOGIC TỰ ĐỘNG CO GIÃN TỜ GIẤY (Giữ nguyên)
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
            if (foundOverlay != null)
                foundOverlay.SetActive(true);
        }
    }
}