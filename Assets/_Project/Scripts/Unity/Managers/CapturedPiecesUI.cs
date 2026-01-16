using UnityEngine;
using UnityEngine.UI;
using Chess.Core.Models;
using Chess.Unity.ScriptableObjects;

namespace Chess.Unity.Managers
{
    public class CapturedPiecesUI : MonoBehaviour
    {
        [Header("Containers")]
        [SerializeField] private Transform _whiteCapturedContainer; // Alt Panel
        [SerializeField] private Transform _blackCapturedContainer; // Üst Panel
        
        [Header("Assets")]
        [SerializeField] private GameObject _pieceImagePrefab;
        [SerializeField] private PieceTheme _pieceTheme;

        public void AddCapturedPiece(Piece piece)
        {
            if (piece.Type == PieceType.None || piece.Type == PieceType.King) return;

            Transform targetContainer = (piece.Color == PieceColor.White) ? _whiteCapturedContainer : _blackCapturedContainer;

            GameObject newImageObj = Instantiate(_pieceImagePrefab, targetContainer);
            
            if (newImageObj.TryGetComponent<Image>(out var img))
            {
                img.sprite = _pieceTheme.GetSprite(piece.Type, piece.Color);
            }
        }

        public void ResetUI()
        {
            ClearContainer(_whiteCapturedContainer);
            ClearContainer(_blackCapturedContainer);
        }

        private void ClearContainer(Transform container)
        {
            foreach (Transform child in container) Destroy(child.gameObject);
        }
    }
}