using UnityEngine;
using UnityEngine.Tilemaps;

namespace Game.Minigames.Maze
{
    public class MazeRenderer : MonoBehaviour
    {
        [Header("References")]
        public Tilemap mazeTilemap;
        public Transform paperQuad;

        [Header("Tile Palette (0 to 15)")]
        public TileBase[] wallTiles = new TileBase[16];

        [Header("Markers")]
        public GameObject endMarkerPrefab;
        private GameObject spawnedEndMarker;

        public void RenderMaze(MazeData data)
        {
            mazeTilemap.ClearAllTiles();

            int innerWidth = data.Width;
            int innerHeight = data.Height;

            // Kích thước thực tế trên mặt giấy (cộng thêm 2 ô viền cho 2 bên)
            int padWidth = innerWidth + 2;
            int padHeight = innerHeight + 2;

            // 1. VẼ MÊ CUNG KÈM VIỀN ĐỆM
            for (int x = 0; x < padWidth; x++)
            {
                for (int y = 0; y < padHeight; y++)
                {
                    int tileIndex = -1;
                    tileIndex = (x == 0, x == padWidth - 1, y == 0, y == padHeight - 1) switch
                    {
                        (true, _, true, _) => 9,   // Góc Trái - Dưới
                        (true, _, _, true) => 10,  // Góc Trái - Trên
                        (_, true, true, _) => 5,   // Góc Phải - Dưới
                        (_, true, _, true) => 6,   // Góc Phải - Trên
                        (true, _, _, _) => 3,      // Nguyên cột Viền Trái
                        (_, true, _, _) => 3,      // Nguyên cột Viền Phải
                        (_, _, true, _) => 12,     // Nguyên hàng Viền Dưới
                        (_, _, _, true) => 12,     // Nguyên hàng Viền Trên
                        _ => data.Grid[x - 1, y - 1] // Lõi mê cung
                    };

                    // Đặt gạch
                    if (tileIndex >= 0 && tileIndex < 16 && wallTiles[tileIndex] != null)
                    {
                        mazeTilemap.SetTile(new Vector3Int(x, y, 0), wallTiles[tileIndex]);
                    }
                }
            }

            // 2. ÉP SCALE VÀ ĐỊNH VỊ WORLD SPACE (Dùng kích thước padWidth, padHeight)
            if (paperQuad != null)
            {
                Transform gridTransform = mazeTilemap.layoutGrid.transform;
                float paperWidth = paperQuad.localScale.x;
                float paperHeight = paperQuad.localScale.y;

                // Scale lưới nhỏ lại đủ để chứa cả lớp viền
                gridTransform.localScale = new Vector3(paperWidth / padWidth, paperHeight / padHeight, 1f);
                gridTransform.rotation = paperQuad.rotation;

                Vector3 alignBottomLeft = (-paperQuad.right * paperWidth / 2f) + (-paperQuad.up * paperHeight / 2f);
                Vector3 floatAbovePaper = -paperQuad.forward * 0.3f;

                gridTransform.position = paperQuad.position + alignBottomLeft + floatAbovePaper;
            }

            // 3. ĐẶT END MARKER
            if (endMarkerPrefab != null)
            {
                Vector3 endWorldPos = GetWorldPosition(data.EndPos);
                if (spawnedEndMarker == null)
                {
                    spawnedEndMarker = Instantiate(endMarkerPrefab, endWorldPos, paperQuad.rotation, mazeTilemap.layoutGrid.transform);
                }
                else
                {
                    spawnedEndMarker.transform.position = endWorldPos;
                }
            }
        }

        public Vector3 GetWorldPosition(Vector2Int gridPos)
        {
            // BÍ QUYẾT: Dịch tọa độ (+1, +1) vì lõi mê cung đã bị đẩy vào trong 1 ô
            // Giúp Player và Controller vẫn hoạt động đúng tọa độ ảo (0,0) mà không biết có lớp viền!
            return mazeTilemap.GetCellCenterWorld(new Vector3Int(gridPos.x + 1, gridPos.y + 1, 0));
        }
    }
}