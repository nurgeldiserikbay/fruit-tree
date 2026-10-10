using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace FruitTree
{
    [RequireComponent(typeof(Camera))]
    public sealed class OrbitPickingCamera : MonoBehaviour
    {
        public Transform target;
        [Range(3.1f, 12f)] public float distance = 8.4f;
        public float yaw = 20f;
        public float pitch = 13f;
        public float focusHeight = 2.65f;
        public float sensitivity = .2f;
        private float wantedYaw, wantedPitch, wantedDistance, wantedFocusHeight;
        private Vector2 pointerDown, lastPointer;
        private int activeFinger = -1;
        private bool pointerOnUI;
        private bool tracking, moved, pinching;
        private Camera viewCamera;
        private float tapThreshold;
        private readonly List<RaycastResult> uiHits = new List<RaycastResult>(8);

        private void Awake()
        {
            viewCamera = GetComponent<Camera>();
            viewCamera.nearClipPlane = .06f;
            wantedYaw = yaw; wantedPitch = pitch; wantedDistance = distance;
            wantedFocusHeight = focusHeight;
            tapThreshold = Mathf.Max(18f, Screen.dpi > 0 ? Screen.dpi * .13f : 18f);
        }

        private void Update()
        {
            if (target == null || Time.timeScale == 0) return;
            if (Input.touchCount > 0) TouchInput();
            else if (Application.isMobilePlatform)
            {
                // If both fingers are lifted simultaneously we still leave pinch mode.
                pinching = false;
                tracking = false;
                activeFinger = -1;
            }
            else
            {
                pinching = false;
                MouseInput();
            }
            yaw = Mathf.LerpAngle(yaw, wantedYaw, 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime));
            pitch = Mathf.Lerp(pitch, wantedPitch, 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime));
            distance = Mathf.Lerp(distance, wantedDistance, 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime));
            focusHeight = Mathf.Lerp(focusHeight, wantedFocusHeight,
                1f - Mathf.Exp(-10f * Time.unscaledDeltaTime));
        }

        private void LateUpdate()
        {
            if (target == null) return;
            Vector3 focus = target.position + Vector3.up * focusHeight;
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            transform.position = focus + rot * (Vector3.back * distance);
            transform.rotation = Quaternion.LookRotation(focus - transform.position, Vector3.up);
        }

        private void TouchInput()
        {
            if (Input.touchCount >= 2)
            {
                var a = Input.GetTouch(0);
                var b = Input.GetTouch(1);
                float oldGap = Vector2.Distance(a.position - a.deltaPosition, b.position - b.deltaPosition);
                float newGap = Vector2.Distance(a.position, b.position);
                if (pinching)
                {
                    wantedDistance = Mathf.Clamp(wantedDistance - (newGap - oldGap) * .009f, 3.1f, 11.5f);
                    // A two-finger vertical slide can inspect upper or lower branches.
                    float verticalSlide = (a.deltaPosition.y + b.deltaPosition.y) * .5f;
                    wantedFocusHeight = Mathf.Clamp(wantedFocusHeight - verticalSlide * .005f, 1.4f, 4.1f);
                }
                pinching = true;
                tracking = false;
                return;
            }
            var touch = Input.GetTouch(0);
            if (pinching)
            {
                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) pinching = false;
                return;
            }
            if (touch.phase == TouchPhase.Began)
            {
                activeFinger = touch.fingerId;
                Begin(touch.position);
            }
            else if (tracking && touch.fingerId == activeFinger)
            {
                if (touch.phase == TouchPhase.Moved) Move(touch.position);
                if (touch.phase == TouchPhase.Ended)
                {
                    End(touch.position, activeFinger);
                    activeFinger = -1;
                }
                if (touch.phase == TouchPhase.Canceled) tracking = false;
            }
        }

        private void MouseInput()
        {
            var pos = (Vector2)Input.mousePosition;
            if (Input.GetMouseButtonDown(0)) Begin(pos);
            if (Input.GetMouseButton(0) && tracking) Move(pos);
            if (Input.GetMouseButtonUp(0) && tracking) End(pos, -1);
            if (Input.mouseScrollDelta.y != 0)
                wantedDistance = Mathf.Clamp(wantedDistance - Input.mouseScrollDelta.y * .5f, 3.1f, 11.5f);
            if (Input.GetKey(KeyCode.LeftShift) && Input.GetMouseButton(1))
                wantedFocusHeight = Mathf.Clamp(wantedFocusHeight + Input.GetAxis("Mouse Y") * .18f, 1.4f, 4.1f);
        }

        private void Begin(Vector2 position)
        {
            pointerOnUI = OverUI(position, activeFinger);
            tracking = !pointerOnUI;
            moved = false;
            pointerDown = lastPointer = position;
        }

        private void Move(Vector2 position)
        {
            Vector2 delta = position - lastPointer;
            if (Vector2.Distance(pointerDown, position) > tapThreshold) moved = true;
            if (moved)
            {
                wantedYaw += delta.x * sensitivity;
                wantedPitch = Mathf.Clamp(wantedPitch - delta.y * sensitivity, -20f, 58f);
            }
            lastPointer = position;
        }

        private void End(Vector2 position, int fingerId)
        {
            if (!moved && !pointerOnUI && Vector2.Distance(pointerDown, position) <= tapThreshold && !OverUI(position, fingerId))
                TryPick(position);
            tracking = false;
        }

        private bool OverUI(Vector2 position, int fingerId)
        {
            // Explicit hit testing is reliable on the touch-end frame, even when
            // EventSystem's cached finger state has already been removed.
            if (EventSystem.current == null) return false;
            var data = new PointerEventData(EventSystem.current) { position = position };
            uiHits.Clear();
            EventSystem.current.RaycastAll(data, uiHits);
            return uiHits.Count > 0;
        }

        private void TryPick(Vector2 point)
        {
            Ray ray = viewCamera.ScreenPointToRay(point);
            if (Physics.Raycast(ray, out RaycastHit hit, 30f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                var apple = hit.collider.GetComponentInParent<Apple>();
                if (apple != null) apple.Pick();
            }
        }
    }
}
