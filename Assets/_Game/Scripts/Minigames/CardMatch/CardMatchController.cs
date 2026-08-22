using UnityEngine;
using Game.Core;
using System.Collections;
using System.Collections.Generic;

namespace Game.Minigames.CardMatch
{
    public class CardMatchController : MinigameBaseController
    {
        [Header("Tham chiếu Card Match")]
        public CardMatchConfig config;
        public CardItem cardPrefab;
        public Transform gridContainer;
        [Tooltip("Kéo object Paper BG vào đây")]
        public Renderer paperBackground;

        private List<CardItem> allCards = new List<CardItem>();
        private CardItem firstCard;
        private CardItem secondCard;
        private int pairsMatched = 0;
        private int totalPairs;
        private bool isInputLocked = false;

        protected override void OnGameStart()
        {
            ClearGrid();

            // 1. Nếu số lẻ (VD 9 ô), totalPairs sẽ làm tròn xuống còn 4 cặp (8 lá).
            int totalCards = config.rows * config.columns;
            totalPairs = totalCards / 2;
            pairsMatched = 0;

            int availableImages = config.cardFaceSprites.Length;
            if (availableImages == 0)
            {
                Debug.LogError("[CardMatch] Thư viện ảnh đang trống rỗng! Vui lòng thêm ít nhất 1 ảnh vào Config.");
                return;
            }

            // Sinh ID cho các cặp bài (tổng cộng bằng totalPairs * 2)
            List<int> cardIDs = new List<int>();
            for (int i = 0; i < totalPairs; i++)
            {
                int imageID = i % availableImages;
                cardIDs.Add(imageID);
                cardIDs.Add(imageID);
            }
            ShuffleList(cardIDs);

            float totalWidth = (config.columns * config.cardSize.x) + ((config.columns - 1) * config.spacing.x);
            float totalHeight = (config.rows * config.cardSize.y) + ((config.rows - 1) * config.spacing.y);

            // ==========================================
            // THUẬT TOÁN ĐO GIẤY BẤT TỬ
            // ==========================================
            gridContainer.localScale = Vector3.one;

            if (paperBackground is SpriteRenderer sr && sr.sprite != null)
            {
                Vector2 extents = sr.sprite.bounds.extents;
                Vector3[] corners = new Vector3[4]
                {
            new Vector3(-extents.x, -extents.y, 0),
            new Vector3(extents.x, -extents.y, 0),
            new Vector3(-extents.x, extents.y, 0),
            new Vector3(extents.x, extents.y, 0)
                };

                float minX = float.MaxValue, maxX = float.MinValue;
                float minY = float.MaxValue, maxY = float.MinValue;

                foreach (Vector3 corner in corners)
                {
                    Vector3 worldPt = sr.transform.TransformPoint(corner);
                    Vector3 localPt = gridContainer.InverseTransformPoint(worldPt);

                    if (localPt.x < minX) minX = localPt.x;
                    if (localPt.x > maxX) maxX = localPt.x;
                    if (localPt.y < minY) minY = localPt.y;
                    if (localPt.y > maxY) maxY = localPt.y;
                }

                float paperWidth = (maxX - minX) * config.paperPadding;
                float paperHeight = (maxY - minY) * config.paperPadding;

                float scaleX = paperWidth / totalWidth;
                float scaleY = paperHeight / totalHeight;

                float finalScale = Mathf.Min(scaleX, scaleY);
                gridContainer.localScale = new Vector3(finalScale, finalScale, 1f);
            }
            else
            {
                Debug.LogWarning("[CardMatch] PaperBg không phải SpriteRenderer hoặc chưa có hình!");
            }
            // ==========================================

            float startX = -totalWidth / 2f + config.cardSize.x / 2f;
            float startY = totalHeight / 2f - config.cardSize.y / 2f;

            // Vẫn duyệt qua toàn bộ số ô trên lưới
            for (int i = 0; i < totalCards; i++)
            {
                // LOGIC CHẶN SỐ LẺ: Nếu i vượt quá số bài đang có, bỏ qua không sinh bài nữa
                // Nó sẽ tự động để lại ô trống ở vị trí cuối cùng của lưới
                if (i >= cardIDs.Count)
                {
                    continue;
                }

                int row = i / config.columns;
                int col = i % config.columns;

                float posX = startX + col * (config.cardSize.x + config.spacing.x);
                float posY = startY - row * (config.cardSize.y + config.spacing.y);

                CardItem newCard = Instantiate(cardPrefab, gridContainer);
                newCard.transform.localPosition = new Vector3(posX, posY, 0f);

                int imageID = cardIDs[i];
                newCard.Initialize(
                    imageID,
                    config.cardFaceSprites[imageID],
                    config.cardBackSprite,
                    config.flipAnimationDuration
                );

                newCard.OnCardClicked += HandleCardClicked;
                allCards.Add(newCard);
            }

            StartCoroutine(PreviewRoutine());
        }

        private IEnumerator PreviewRoutine()
        {
            isInputLocked = true; // Khóa không cho người chơi click

            // Đợi người chơi ghi nhớ
            yield return new WaitForSeconds(config.previewTime);

            // Úp toàn bộ bài xuống
            foreach (var card in allCards)
            {
                card.FlipDown();
            }

            // Chờ hiệu ứng úp bài chạy xong rồi mới mở khóa cho chơi
            yield return new WaitForSeconds(config.flipAnimationDuration);
            isInputLocked = false;
        }

        protected override void OnGameReset()
        {
            if (isPlaying)
            {
                StopAllCoroutines(); // Dừng mọi hoạt động lật/xem trước đang dang dở
                OnGameStart();
            }
        }

        private void HandleCardClicked(CardItem clickedCard)
        {
            if (!isPlaying || isInputLocked) return;

            clickedCard.FlipUp();

            if (firstCard == null)
            {
                firstCard = clickedCard;
            }
            else
            {
                secondCard = clickedCard;
                StartCoroutine(CheckMatchRoutine());
            }
        }

        private IEnumerator CheckMatchRoutine()
        {
            isInputLocked = true; // Khóa không cho bấm lá thứ 3

            // Chờ lá thứ 2 lật ngửa hẳn ra
            yield return new WaitForSeconds(config.flipAnimationDuration);

            if (firstCard.CardID == secondCard.CardID)
            {
                AudioController.Instance.PlaySFX(SoundName.Card_Matched);
                firstCard.SetMatched();
                secondCard.SetMatched();
                pairsMatched++;

                if (pairsMatched >= totalPairs)
                {
                    CompleteMinigame();
                }
                else
                {
                    isInputLocked = false;
                }
            }
            else
            {
                yield return new WaitForSeconds(config.delayBeforeFlipBack);
                firstCard.FlipDown();
                secondCard.FlipDown();

                yield return new WaitForSeconds(config.flipAnimationDuration);
                isInputLocked = false;
            }

            firstCard = null;
            secondCard = null;
        }

        private void ClearGrid()
        {
            StopAllCoroutines();
            foreach (var card in allCards)
            {
                if (card != null)
                {
                    card.OnCardClicked -= HandleCardClicked;
                    Destroy(card.gameObject);
                }
            }
            allCards.Clear();
            firstCard = null;
            secondCard = null;
            isInputLocked = false;
        }

        protected override void OnGameClosed() => ClearGrid();

        private void ShuffleList<T>(List<T> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                T temp = list[i];
                int randomIndex = UnityEngine.Random.Range(i, list.Count);
                list[i] = list[randomIndex];
                list[randomIndex] = temp;
            }
        }

        protected override void OnDifficultyIncrease(int minigamePassed)
        {
            config = _difficultyConfig.GetMinigameConfig<CardMatchConfig>(minigamePassed);
        }
    }
}