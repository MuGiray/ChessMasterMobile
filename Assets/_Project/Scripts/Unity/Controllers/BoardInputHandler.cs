using UnityEngine;
using UnityEngine.EventSystems;
using Chess.Unity.Managers;
using CoreVector2Int = Chess.Core.Models.Vector2Int;

namespace Chess.Unity.Controllers
{
    public class BoardInputHandler : MonoBehaviour
    {
        private Camera _mainCamera;

        private void Start()
        {
            _mainCamera = Camera.main;
        }

        private void Update()
        {
            // UI Tıklamalarını Engelle
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            if (Input.touchCount > 0 && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)) return;

            if (Input.GetMouseButtonDown(0))
            {
                HandleInput(Input.mousePosition);
            }
            else if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                HandleInput(Input.GetTouch(0).position);
            }
        }

        private void HandleInput(Vector3 screenPosition)
        {
            Vector3 worldPosition = _mainCamera.ScreenToWorldPoint(screenPosition);
            
            int x = Mathf.RoundToInt(worldPosition.x);
            int y = Mathf.RoundToInt(worldPosition.y);

            if (x >= 0 && x < 8 && y >= 0 && y < 8)
            {
                GameManager.Instance.OnSquareSelected(new CoreVector2Int(x, y));
            }
            else
            {
                GameManager.Instance.DeselectPiece();
            }
        }
    }
}