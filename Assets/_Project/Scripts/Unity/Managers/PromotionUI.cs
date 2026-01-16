using System;
using UnityEngine;
using UnityEngine.UI;
using Chess.Core.Models;
using Chess.Unity.ScriptableObjects;

namespace Chess.Unity.Managers
{
    public class PromotionUI : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private GameObject _panel;
        [SerializeField] private Image _queenImg, _rookImg, _bishopImg, _knightImg;
        [SerializeField] private Button _queenBtn, _rookBtn, _bishopBtn, _knightBtn;
        
        [Header("Assets")]
        [SerializeField] private PieceTheme _theme;

        private Action<PieceType> _onPieceSelected;

        private void Start()
        {
            // Panel başlangıçta kapalı
            _panel.SetActive(false);

            // Buton Dinleyicileri (Callback pattern)
            _queenBtn.onClick.AddListener(() => OnSelect(PieceType.Queen));
            _rookBtn.onClick.AddListener(() => OnSelect(PieceType.Rook));
            _bishopBtn.onClick.AddListener(() => OnSelect(PieceType.Bishop));
            _knightBtn.onClick.AddListener(() => OnSelect(PieceType.Knight));
        }

        public void Show(PieceColor color, Action<PieceType> callback)
        {
            _onPieceSelected = callback;

            // Buton ikonlarını oyuncunun rengine göre ayarla
            _queenImg.sprite = _theme.GetSprite(PieceType.Queen, color);
            _rookImg.sprite = _theme.GetSprite(PieceType.Rook, color);
            _bishopImg.sprite = _theme.GetSprite(PieceType.Bishop, color);
            _knightImg.sprite = _theme.GetSprite(PieceType.Knight, color);

            _panel.SetActive(true);
        }

        // YENİ METOD: Dışarıdan zorla kapatmak için (Örn: Restart atılınca)
        public void Hide()
        {
            _panel.SetActive(false);
        }

        private void OnSelect(PieceType type)
        {
            _panel.SetActive(false);
            _onPieceSelected?.Invoke(type); // Seçimi GameManager'a bildir
        }
    }
}