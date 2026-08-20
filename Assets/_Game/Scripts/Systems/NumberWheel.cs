using UnityEngine;
using TMPro;

namespace Game.Systems.Lock
{
    public class NumberWheel : MonoBehaviour
    {
        [Header("Thực thể")]
        [Tooltip("Kéo khối trụ Cylinder vào đây")]
        public Transform wheel;

        [Tooltip("Kéo 5 Text 3D vào đây")]
        public TMP_Text[] texts = new TMP_Text[5];

        [Header("Cấu hình Lõi")]
        [Tooltip("Góc giãn cách giữa các số (Càng nhỏ số càng khít nhau)")]
        [Range(20f, 72f)]
        public float spacingAngle = 45f;
        public float rollSpeed = 15f;

        [Header("Nút điều khiển (Kéo KeyboxButton vào đây)")]
        public KeyboxButton buttonUp;
        public KeyboxButton buttonDown;

        // Số chính hiển thị ở mặt trước
        public int CurrentValue { get; private set; } = 0;

        private float radius;
        private int currentScrollIndex = 0;
        private float smoothScrollIndex = 0f;
        private Quaternion initialWheelRotation;

        private void Start()
        {
            // 1. TỰ ĐỘNG TÍNH BÁN KÍNH
            radius = (wheel.localScale.x + wheel.localScale.z) / 2f * 0.5f;
            initialWheelRotation = wheel.localRotation;

            // 2. KHỞI TẠO CHỮ SỐ CHO 5 SLOT
            for (int i = 0; i < 5; i++)
            {
                int targetRel = GetRelativeOffset(i, currentScrollIndex);

                // Quy đổi vị trí tương đối (-2, -1, 0, 1, 2) thành con số cần hiển thị
                int digit = (CurrentValue - targetRel + 10) % 10;
                texts[i].text = digit.ToString();
            }
        }

        // ĐĂNG KÝ LẮNG NGHE KHI BẬT
        private void OnEnable()
        {
            if (buttonUp != null) buttonUp.OnClicked += SpinUp;
            if (buttonDown != null) buttonDown.OnClicked += SpinDown;
        }

        // HỦY LẮNG NGHE KHI TẮT (Tránh rò rỉ bộ nhớ)
        private void OnDisable()
        {
            if (buttonUp != null) buttonUp.OnClicked -= SpinUp;
            if (buttonDown != null) buttonDown.OnClicked -= SpinDown;
        }

        // Bánh xe cuộn từ dưới lên (Số tăng)
        public void SpinUp()
        {
            CurrentValue = (CurrentValue + 1) % 10;
            currentScrollIndex--;
            UpdateHiddenDigits();
        }

        // Bánh xe cuộn từ trên xuống (Số giảm)
        public void SpinDown()
        {
            CurrentValue = (CurrentValue - 1 + 10) % 10;
            currentScrollIndex++;
            UpdateHiddenDigits();
        }

        // ==========================================
        // LOGIC LÕI: THAY SỐ Ở GÓC KHUẤT
        // ==========================================
        private void UpdateHiddenDigits()
        {
            for (int i = 0; i < 5; i++)
            {
                // Lấy vị trí tương đối của Text so với khung hình hiện tại
                int targetRel = GetRelativeOffset(i, currentScrollIndex);

                // +2 là góc khuất bên Trên, -2 là góc khuất bên Dưới
                if (targetRel == 2)
                {
                    texts[i].text = ((CurrentValue - 2 + 10) % 10).ToString();
                }
                else if (targetRel == -2)
                {
                    texts[i].text = ((CurrentValue + 2) % 10).ToString();
                }
            }
        }

        // Thuật toán nắn vòng lặp 5 phần tử
        private int GetRelativeOffset(int itemIndex, int scrollIndex)
        {
            int offset = (itemIndex - scrollIndex) % 5;
            if (offset > 2) offset -= 5;
            if (offset < -2) offset += 5;
            return offset;
        }

        private void Update()
        {
            // Nội suy để cuộn mượt mà
            smoothScrollIndex = Mathf.Lerp(smoothScrollIndex, currentScrollIndex, Time.deltaTime * rollSpeed);

            wheel.localRotation = initialWheelRotation * Quaternion.Euler(0f, smoothScrollIndex * spacingAngle, 0f);

            // 4. ĐỊNH VỊ 5 CHỮ SỐ (TEXT) TRÊN VÒNG CUNG
            for (int i = 0; i < 5; i++)
            {
                float relativePos = i - smoothScrollIndex;

                // Thuật toán Vòng Lặp Liên Tục: 
                // Giữ vị trí tương đối luôn ở mốc [-2.5, 2.5] để Text tự động dịch chuyển tức thời qua góc ẩn
                while (relativePos > 2.5f) relativePos -= 5f;
                while (relativePos < -2.5f) relativePos += 5f;

                float currentAngle = relativePos * spacingAngle;
                float rad = currentAngle * Mathf.Deg2Rad;

                // Tọa độ bám sát mặt trụ
                float y = radius * Mathf.Cos(rad);
                float z = radius * Mathf.Sin(rad);

                texts[i].transform.localPosition = new Vector3(0f, y, z);

                // Text thì vẫn xoay quanh trục X để lật theo mặt trụ
                texts[i].transform.localRotation = Quaternion.Euler(90f + currentAngle, 0f, 0f);
            }
        }

        private void OnMouseOver()
        {
            // Bắt giá trị lăn của con lăn chuột (Scroll Wheel)
            float scroll = Input.mouseScrollDelta.y;

            if (scroll < 0f)
            {
                SpinUp();
            }
            else if (scroll > 0f)
            {
                SpinDown();
            }
        }
    }
}