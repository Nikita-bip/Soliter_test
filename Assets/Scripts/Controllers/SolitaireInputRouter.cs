using UnityEngine;

namespace TestTask.Solitaire.Controllers
{
    public sealed class SolitaireInputRouter : MonoBehaviour
    {
        private SolitaireController _controller;

        public void Initialize(SolitaireController controller)
        {
            _controller = controller;
        }

        private void Update()
        {
            if (_controller == null || _controller.IsBusy)
            {
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                _controller.TryHandlePointer(Input.mousePosition);
                return;
            }

            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                _controller.TryHandlePointer(Input.GetTouch(0).position);
            }
        }
    }
}
