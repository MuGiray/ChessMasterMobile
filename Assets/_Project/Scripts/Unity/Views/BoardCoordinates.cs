using UnityEngine;
using TMPro; // TextMeshPro kütüphanesi

namespace Chess.Unity.Views
{
    public class BoardCoordinates : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private GameObject _labelPrefab; // İçinde TextMeshPro olan prefab
        [SerializeField] private Color _textColor = new Color(0.8f, 0.8f, 0.8f, 1f); // Hafif gri
        [SerializeField] private float _offset = 0.75f; // Tahtadan ne kadar uzak olsun?
        [SerializeField] private float _fontSize = 3.5f;

        private readonly string[] _files = { "a", "b", "c", "d", "e", "f", "g", "h" };

        private void Start()
        {
            GenerateCoordinates();
        }

        private void GenerateCoordinates()
        {
            // Temizlik (Eğer önceden oluşturulmuşsa sil)
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }

            // --- KOORDİNATLARI OLUŞTUR ---
            for (int i = 0; i < 8; i++)
            {
                // 1. HARFLER (Files - X Ekseni)
                // Alt Kenar (a, b, c...)
                CreateLabel(_files[i], new Vector3(i, -_offset, 0));
                // Üst Kenar (İsteğe bağlı, simetri için ekliyoruz)
                CreateLabel(_files[i], new Vector3(i, 7 + _offset, 0));

                // 2. RAKAMLAR (Ranks - Y Ekseni)
                // Sol Kenar (1, 2, 3...)
                CreateLabel((i + 1).ToString(), new Vector3(-_offset, i, 0));
                // Sağ Kenar
                CreateLabel((i + 1).ToString(), new Vector3(7 + _offset, i, 0));
            }
        }

        private void CreateLabel(string text, Vector3 position)
        {
            if (_labelPrefab == null) return;

            GameObject labelObj = Instantiate(_labelPrefab, transform);
            labelObj.transform.position = position;
            
            // TextMeshPro bileşenini al
            TextMeshPro tmp = labelObj.GetComponent<TextMeshPro>();
            if (tmp != null)
            {
                tmp.text = text;
                tmp.color = _textColor;
                tmp.fontSize = _fontSize;
                tmp.alignment = TextAlignmentOptions.Center;
                
                // Sorting Order (Tahtanın üstünde görünsün)
                tmp.GetComponent<MeshRenderer>().sortingOrder = 11; 
            }
        }
    }
}