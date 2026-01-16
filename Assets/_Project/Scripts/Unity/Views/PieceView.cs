using UnityEngine;
using System.Collections;
using CoreVector2Int = Chess.Core.Models.Vector2Int;

namespace Chess.Unity.Views
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class PieceView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _renderer;
        private const float ANIMATION_DURATION = 0.2f;

        private void Reset()
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        public void Init(Chess.Core.Models.PieceType type, Chess.Core.Models.PieceColor color, Sprite sprite)
        {
            _renderer.sprite = sprite;
            name = $"{color}_{type}";
        }

        public void SetPositionImmediate(CoreVector2Int coords)
        {
            transform.position = new Vector3(coords.x, coords.y, -1);
        }

        public void MoveTo(CoreVector2Int coords)
        {
            StopAllCoroutines();
            StartCoroutine(AnimateMove(new Vector3(coords.x, coords.y, -1)));
        }

        private IEnumerator AnimateMove(Vector3 targetPos)
        {
            Vector3 startPos = transform.position;
            float elapsed = 0f;

            while (elapsed < ANIMATION_DURATION)
            {
                transform.position = Vector3.Lerp(startPos, targetPos, elapsed / ANIMATION_DURATION);
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.position = targetPos;
        }
    }
}