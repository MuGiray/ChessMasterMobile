using UnityEngine;
using System.Collections; // Coroutine için gerekli
using CoreVector2Int = Chess.Core.Models.Vector2Int;

namespace Chess.Unity.Views
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class PieceView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _renderer;

        private void Reset()
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        public void Init(Chess.Core.Models.PieceType type, Chess.Core.Models.PieceColor color, Sprite sprite)
        {
            _renderer.sprite = sprite;
            name = $"{color}_{type}";
        }

        // ESKİ METOD: public void SetPosition(CoreVector2Int coords) { ... }
        // YENİ METOD: Animasyonlu hareket
        public void MoveTo(CoreVector2Int coords)
        {
            StopAllCoroutines(); // Önceki hareket bitmediyse durdur
            StartCoroutine(AnimateMove(new Vector3(coords.x, coords.y, -1)));
        }

        // İlk oluşumda animasyonsuz koymak için (Setup)
        public void SetPositionImmediate(CoreVector2Int coords)
        {
            transform.position = new Vector3(coords.x, coords.y, -1);
        }

        private IEnumerator AnimateMove(Vector3 targetPos)
        {
            Vector3 startPos = transform.position;
            float duration = 0.2f; // 200ms sürede git (Hızlı ve akıcı)
            float elapsed = 0f;

            while (elapsed < duration)
            {
                transform.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.position = targetPos; // Tam yerine oturt
        }
    }
}