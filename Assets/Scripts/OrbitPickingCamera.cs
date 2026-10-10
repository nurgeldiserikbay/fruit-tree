using UnityEngine;
using UnityEngine.EventSystems;

namespace FruitTree
{
    public sealed class OrbitPickingCamera : MonoBehaviour
    {
        public Transform target;
        public float distance = 8.2f;
        public float yaw = 25f;
        public float pitch = 13f;
        public float sensitivity = 0.23f;
        private Vector2 downPosition;
        private Vector2 lastPosition;
        private bool dragging;
        private const float TapThreshold = 18f;

        private void LateUpdate()
        {
            if (target == null) return;
            UpdatePointer();
            var rotation = Quaternion.Euler(pitch, yaw, 0);
            var focus = target.position + Vector3.up * 2.8f;
            transform.position = focus + rotation * new Vector3(0, 0, -distance);
            transform.rotation = Quaternion.LookRotation(focus - transform.position, Vector3.up);
        }

        private void UpdatePointer()
        {
            if (Input.touchCount > 1)
            {
                var a = Input.GetTouch(0);
                var b = Input.GetTouch(1);
                var previousDistance = Vector2.Distance(a.position - a.deltaPosition, b.position - b.deltaPosition);
                var currentDistance = Vector2.Distance(a.position, b.position);
                distance = Mathf.Clamp(distance - (currentDistance - previousDistance) * 0.012f, 4f, 12f);
                dragging = false;
                return;
            }
            bool isTouch = Input.touchCount == 1;
            Vector2 position = isTouch ? Input.GetTouch(0).position : (Vector2)Input.mousePosition;
            bool down = isTouch ? Input.GetTouch(0).phase == TouchPhase.Began : Input.GetMouseButtonDown(0);
            bool held = isTouch ? Input.GetTouch(0).phase == TouchPhase.Moved : Input.GetMouseButton(0);
            bool up = isTouch ? Input.GetTouch(0).phase == TouchPhase.Ended : Input.GetMouseButtonUp(0);
            if (down)
            {
                dragging = true;
                downPosition = position;
                lastPosition = position;
            }
            if (held && dragging)
            {
                var delta = position - lastPosition;
                yaw += delta.x * sensitivity;
                pitch = Mathf.Clamp(pitch - delta.y * sensitivity, -25f, 55f);
                lastPosition = position;
            }
            if (up && dragging)
            {
                if (Vector2.Distance(downPosition, position) <= TapThreshold)
                    Pick(position);
                dragging = false;
            }
#if UNITY_EDITOR
            distance = Mathf.Clamp(distance - Input.mouseScrollDelta.y * 0.45f, 4f, 12f);
#endif
        }

        private void Pick(Vector2 position)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            var ray = GetComponent<Camera>().ScreenPointToRay(position);
            if (Physics.Raycast(ray, out var hit, 30f))
            {
                var apple = hit.collider.GetComponent<Apple>();
                if (apple != null) apple.Pick();
            }
        }
    }
}
