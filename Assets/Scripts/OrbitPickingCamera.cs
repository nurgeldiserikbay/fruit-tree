using System;
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
        public const float MinimumCameraHeight = .85f;
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

        private void Start()
        {
            FitTreeInPortrait();
        }

        /// <summary>Frames the entire tree at launch regardless of mobile aspect ratio.</summary>
        public void FitTreeInPortrait()
        {
            if (target == null) return;
            if (viewCamera == null) viewCamera = GetComponent<Camera>();
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            float aspect = Mathf.Max(.35f, viewCamera.aspect);
            float verticalTan = Mathf.Tan(viewCamera.fieldOfView * Mathf.Deg2Rad * .5f);
            float horizontalTan = verticalTan * aspect;
            float radial = Mathf.Max(bounds.extents.x, bounds.extents.z);
            // Perspective framing must also account for the closest side
            // of a deep canopy. A simple radius / tan(FOV) fit clips the
            // front branches even though the crown center fits.
            float framedDistance = Mathf.Max(
                radial + radial / horizontalTan,
                radial + bounds.extents.y / verticalTan);
            // Keep 12% margin for orbit changes and wind sway. Do not cap
            // the computed framing at 15m: large mature trees need more room.
            framedDistance = Mathf.Clamp(framedDistance * 1.12f, 7f, 25f);
            wantedFocusHeight = Mathf.Clamp(bounds.center.y, 1.4f, 4.1f);
            focusHeight = wantedFocusHeight;
            wantedDistance = framedDistance;
            distance = framedDistance;
            ApplyCameraTransform(); // Home backdrop must be framed before input is disabled.
        }

        /// <summary>
        /// An optional, player-initiated last-fruit guide. It never harvests fruit
        /// or disables manual 360-degree orbit after the camera reaches the target.
        /// </summary>
        public void LookTowardFruit(Vector3 worldPosition)
        {
            if (target == null) return;
            Vector3 offset = worldPosition - target.position;
            Vector2 radial = new Vector2(offset.x, offset.z);
            if (radial.sqrMagnitude > .0001f)
                wantedYaw = Mathf.Atan2(-offset.x, -offset.z) * Mathf.Rad2Deg;
            wantedFocusHeight = Mathf.Clamp(offset.y, 1.4f, 4.1f);
            // Move gently closer if the player is far away. Keep user freedom to zoom.
            wantedDistance = Mathf.Min(wantedDistance, 8.5f);
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
            // Pitch below this angle would move the camera under the meadow.
            // Enforce on both the target and the smoothed actual camera pose.
            float safePitch = GroundSafeMinimumPitch(distance, focusHeight);
            pitch = Mathf.Max(pitch, safePitch);
            wantedPitch = Mathf.Max(wantedPitch,
                GroundSafeMinimumPitch(wantedDistance, wantedFocusHeight));
        }

        private void LateUpdate() { ApplyCameraTransform(); }

        /// <summary>
        /// One source of truth for orbit geometry, shared with the Editor's
        /// portrait fruit-reachability audit. View controls never need a
        /// separate, approximate camera model.
        /// </summary>
        public static Vector3 OrbitPosition(Vector3 pivot, float orbitDistance,
            float orbitYaw, float orbitPitch, float orbitFocusHeight)
        {
            Vector3 focus = pivot + Vector3.up * orbitFocusHeight;
            Quaternion rotation = Quaternion.Euler(orbitPitch, orbitYaw, 0f);
            return focus + rotation * (Vector3.back * orbitDistance);
        }

        /// <summary>
        /// True only when a point can be aimed at within the tighter 9:20
        /// portrait view, with a small margin around the screen edges.
        /// Does not test wood or other physical occluders.
        /// </summary>
        public static bool IsInsidePortraitView(Vector3 cameraPosition,
            Vector3 lookAt, Vector3 worldPoint, float verticalFov = 47f,
            float aspectRatio = .45f)
        {
            Vector3 aim = lookAt - cameraPosition;
            if (aim.sqrMagnitude < .000001f ||
                verticalFov <= 0f || verticalFov >= 180f || aspectRatio <= 0f)
                return false;
            Quaternion rotation = Quaternion.LookRotation(aim, Vector3.up);
            Vector3 local = Quaternion.Inverse(rotation) * (worldPoint - cameraPosition);
            if (local.z <= .001f) return false;
            float verticalLimit = Mathf.Tan(verticalFov * Mathf.Deg2Rad * .5f) *
                local.z * .95f;
            return Mathf.Abs(local.y) <= verticalLimit &&
                Mathf.Abs(local.x) <= verticalLimit * aspectRatio;
        }

        /// <summary>Minimum downward pitch that keeps the lens over the lawn.</summary>
        public static float GroundSafeMinimumPitch(float orbitDistance, float orbitFocusHeight)
        {
            float ratio = (MinimumCameraHeight - orbitFocusHeight) /
                Mathf.Max(2.8f, orbitDistance);
            return Mathf.Asin(Mathf.Clamp(ratio, -1f, 1f)) * Mathf.Rad2Deg;
        }

        private void ApplyCameraTransform()
        {
            if (target == null) return;
            pitch = Mathf.Max(pitch, GroundSafeMinimumPitch(distance, focusHeight));
            Vector3 focus = target.position + Vector3.up * focusHeight;
            transform.position = OrbitPosition(target.position, distance,
                yaw, pitch, focusHeight);
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
                    wantedDistance = Mathf.Clamp(wantedDistance - (newGap - oldGap) * .009f, 2.8f, 18f);
                    // A two-finger vertical slide can inspect upper or lower branches.
                    float verticalSlide = (a.deltaPosition.y + b.deltaPosition.y) * .5f;
                    wantedFocusHeight = Mathf.Clamp(wantedFocusHeight - verticalSlide * .005f, 1.4f, 4.1f);
                }
                pinching = true;
                // The old one-finger gesture must never finish as a tap after
                // zooming: its pointer-down event predates the pinch.
                tracking = false;
                activeFinger = -1;
                moved = true;
                return;
            }
            var touch = Input.GetTouch(0);
            if (pinching)
            {
                // One finger remains on the screen after pinch. Start a fresh
                // drag at its current position, not the stale pre-pinch point.
                // Mark it moved so lifting it cannot accidentally pick fruit.
                pinching = false;
                tracking = false;
                activeFinger = -1;
                if (CanResumeOrbitAfterPinch(touch.phase))
                {
                    activeFinger = touch.fingerId;
                    Begin(touch.position);
                    moved = true;
                }
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
                if (touch.phase == TouchPhase.Canceled)
                {
                    tracking = false;
                    activeFinger = -1;
                }
            }
        }

        /// <summary>
        /// A surviving finger may continue camera rotation, but an ended or
        /// canceled pointer cannot start a drag or trigger a fruit pick.
        /// Kept pure so Editor tests can validate mobile touch transitions.
        /// </summary>
        public static bool CanResumeOrbitAfterPinch(TouchPhase phase)
        {
            return phase != TouchPhase.Ended && phase != TouchPhase.Canceled;
        }

        private void OnDisable()
        {
            // Home/menu transitions must not retain a stale gesture or pick it
            // when the user returns to the orchard.
            tracking = false;
            pinching = false;
            activeFinger = -1;
            moved = false;
        }

        private void MouseInput()
        {
            var pos = (Vector2)Input.mousePosition;
            if (Input.GetMouseButtonDown(0)) Begin(pos);
            if (Input.GetMouseButton(0) && tracking) Move(pos);
            if (Input.GetMouseButtonUp(0) && tracking) End(pos, -1);
            if (Input.mouseScrollDelta.y != 0)
                wantedDistance = Mathf.Clamp(wantedDistance - Input.mouseScrollDelta.y * .5f, 2.8f, 18f);
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

        /// <summary>
        /// Converts a small finger-sized screen margin into a bounded world
        /// radius. The cap prevents a tap on one branch from taking distant fruit.
        /// </summary>
        public static float TouchAssistRadius(int screenHeight, float verticalFov, float depth)
        {
            float fov = Mathf.Clamp(verticalFov, 20f, 90f) * Mathf.Deg2Rad;
            float unitsPerPixel = 2f * Mathf.Max(1f, depth) *
                Mathf.Tan(fov * .5f) / Mathf.Max(1, screenHeight);
            return Mathf.Clamp(unitsPerPixel * 14f, .035f, .12f);
        }

        // Transparent pixels of an optional foliage atlas are not occluders.
        // Opaque leaf pixels remain blocking, so touching foliage cannot
        // harvest a fruit that is visually hidden behind it.
        public static bool IsTransparentFoliageHit(RaycastHit hit)
        {
            if (hit.collider == null || hit.collider.name != "Merged Leaves")
                return false;
            var renderer = hit.collider.GetComponent<MeshRenderer>();
            Material material = renderer != null ? renderer.sharedMaterial : null;
            if (material == null || !material.HasProperty("_AlphaCutoff") ||
                material.GetFloat("_AlphaCutoff") <= 0f)
                return false;
            Texture2D texture = material.mainTexture as Texture2D;
            if (texture == null || !texture.isReadable)
                return false; // Conservative: do not pick hidden fruit.
            return texture.GetPixelBilinear(hit.textureCoord.x,
                hit.textureCoord.y).a < material.GetFloat("_AlphaCutoff");
        }

        private static bool HasVisibleFruitLine(Vector3 origin,
            Collider fruitCollider, float maxDistance)
        {
            Vector3 towardsFruit = fruitCollider.bounds.center - origin;
            float length = towardsFruit.magnitude;
            if (length <= .001f || length > maxDistance) return false;
            var sight = new Ray(origin, towardsFruit / length);
            RaycastHit[] hits = Physics.RaycastAll(sight, length + .15f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (RaycastHit hit in hits)
            {
                if (IsTransparentFoliageHit(hit)) continue;
                return hit.collider == fruitCollider;
            }
            return false;
        }

        private void TryPick(Vector2 point)
        {
            Ray ray = viewCamera.ScreenPointToRay(point);
            const float maxDistance = 30f;
            bool previousBackfaces = Physics.queriesHitBackfaces;
            try
            {
                Physics.queriesHitBackfaces = true;
                // A tap is infrequent; sorting all hits allows transparent
                // leaf texels to be skipped while respecting opaque blockers.
                RaycastHit[] hits = Physics.RaycastAll(ray, maxDistance,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                for (int i = 0; i < hits.Length; i++)
                {
                    if (IsTransparentFoliageHit(hits[i])) continue;
                    Apple fruit = hits[i].collider.GetComponentInParent<Apple>();
                    if (fruit != null) fruit.Pick();
                    return;
                }

                float depth = target == null ? distance :
                    Vector3.Distance(viewCamera.transform.position, target.position);
                float radius = TouchAssistRadius(Screen.height,
                    viewCamera.fieldOfView, depth);
                if (Physics.SphereCast(ray, radius, out RaycastHit hit, maxDistance,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    // Touch assist cannot go through an opaque leaf or branch.
                    Apple fruit = hit.collider.GetComponentInParent<Apple>();
                    // A wide sphere can graze a fruit hidden just behind a
                    // nearby leaf. Confirm a clear central line of sight before
                    // letting touch assistance harvest anything.
                    if (fruit != null && HasVisibleFruitLine(ray.origin,
                        hit.collider, maxDistance))
                        fruit.Pick();
                }
            }
            finally
            {
                Physics.queriesHitBackfaces = previousBackfaces;
            }
        }
    }
}
