using UnityEngine;

namespace Chess.Unity.Views
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class TileView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _renderer;
        public Vector2Int GridPosition { get; private set; } 

        public void Init(int x, int y)
        {
            GridPosition = new Vector2Int(x, y);
            transform.position = new Vector3(x, y, 0);
            name = $"Tile_{x}_{y}";
        }

        public void SetColor(Color color)
        {
            if (_renderer != null) _renderer.color = color;
        }

        private void Reset()
        {
            _renderer = GetComponent<SpriteRenderer>();
        }
    }
}