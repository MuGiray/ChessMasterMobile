using UnityEngine;

namespace Chess.Unity.Views
{
    public class TileView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _renderer;
        // Karelerin koordinat bilgisini saklayalım, tıklama olaylarında lazım olacak.
        public Vector2Int GridPosition { get; private set; } 

        public void Init(int x, int y)
        {
            GridPosition = new Vector2Int(x, y);
            transform.position = new Vector3(x, y, 0); // 2D Dünyada yerleşim
            name = $"Tile_{x}_{y}";
        }

        public void SetColor(Color color)
        {
            _renderer.color = color;
        }

        private void Reset()
        {
            // Script bir objeye eklendiğinde veya Reset dendiğinde otomatik bağlar.
            _renderer = GetComponent<SpriteRenderer>();
        }
    }
}