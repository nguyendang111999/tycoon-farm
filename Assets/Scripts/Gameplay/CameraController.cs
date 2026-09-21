using Farm.Core;
using UnityEngine;

namespace Farm.Gameplay
{
    /// <summary>
    /// Panning camera controller: 1:1 ground-plane dragging with inertia and boundary clamping.
    /// Supports both mouse and single-finger touch input while ignoring UI interactions.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CameraController : MonoBehaviour
    {
        [Header("Ground Projection")]
        [Tooltip("World Y height of the ground plane where touches/clicks project.")]
        [SerializeField] private float _groundY = 0f;

        [Header("Limits")]
        [Tooltip("Area limits within which the camera's ground look-at point (or position) can move.")]
        [SerializeField] private Vector2 _minBounds = new Vector2(-20f, -20f);
        [SerializeField] private Vector2 _maxBounds = new Vector2(20f, 20f);

        [Header("Inertia & Feel")]
        [Tooltip("How quickly velocity bleeds off when releasing a drag (lower = longer glide).")]
        [Range(1f, 20f)]
        [SerializeField] private float _damping = 7f;
        [Tooltip("Maximum velocity the camera can reach from a fast flick.")]
        [SerializeField] private float _maxSpeed = 50f;
        [Tooltip("Screen pixels required to register before starting a drag (prevents accidental camera shifts during taps).")]
        [SerializeField] private float _dragThresholdPixels = 10f;

        private Camera _camera;
        private Plane _groundPlane;

        private bool _isDragging;
        private bool _hasExceededThreshold;
        private Vector3 _dragStartScreenPos;
        private Vector3 _dragStartGroundWorldPos;
        private Vector3 _velocity;
        private Vector3 _lastFrameTargetPos;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _groundPlane = new Plane(Vector3.up, new Vector3(0f, _groundY, 0f));
        }

        private void LateUpdate()
        {
            HandleInput();
            ApplyMotion();
        }

        private void HandleInput()
        {
            // 1. Check for pointer down (touch or mouse)
            if (PointerInput.TryGetDown(out Vector3 pointerScreenPos))
            {
                // Disregard clicks initiated over UI elements
                if (PointerInput.IsOverUI()) return;

                if (TryGetGroundIntersection(pointerScreenPos, out Vector3 worldHit))
                {
                    _isDragging = true;
                    _hasExceededThreshold = false;
                    _dragStartScreenPos = pointerScreenPos;
                    _dragStartGroundWorldPos = worldHit;
                    _velocity = Vector3.zero;
                    _lastFrameTargetPos = transform.position;
                }
            }

            // 2. Handle active dragging
            if (_isDragging)
            {
                if (PointerInput.TryGetPosition(out Vector3 currentScreenPos))
                {
                    if (!_hasExceededThreshold)
                    {
                        if (Vector3.Distance(currentScreenPos, _dragStartScreenPos) >= _dragThresholdPixels)
                        {
                            _hasExceededThreshold = true;
                        }
                    }

                    if (_hasExceededThreshold && TryGetGroundIntersection(currentScreenPos, out Vector3 currentGroundPos))
                    {
                        // 1:1 Tracking: Offset the camera by the distance between the anchor and current drag point
                        Vector3 delta = _dragStartGroundWorldPos - currentGroundPos;
                        Vector3 targetPos = transform.position + delta;
                        targetPos = ClampToBounds(targetPos);

                        if (Time.deltaTime > 0f)
                        {
                            _velocity = (targetPos - transform.position) / Time.deltaTime;
                            _velocity = Vector3.ClampMagnitude(_velocity, _maxSpeed);
                        }

                        transform.position = targetPos;
                        _lastFrameTargetPos = targetPos;
                    }
                }

                // 3. Check for pointer release
                if (PointerInput.TryGetUp())
                {
                    _isDragging = false;
                }
            }
        }

        private void ApplyMotion()
        {
            // Apply residual momentum and damping when not actively dragging
            if (!_isDragging && _velocity.sqrMagnitude > 0.001f)
            {
                Vector3 newPos = transform.position + _velocity * Time.deltaTime;
                Vector3 clampedPos = ClampToBounds(newPos);

                // Stop momentum if hitting a boundary wall
                if (Mathf.Approximately(newPos.x, clampedPos.x) == false) _velocity.x = 0f;
                if (Mathf.Approximately(newPos.z, clampedPos.z) == false) _velocity.z = 0f;

                transform.position = clampedPos;
                _velocity = Vector3.Lerp(_velocity, Vector3.zero, _damping * Time.deltaTime);
            }
        }

        private bool TryGetGroundIntersection(Vector3 screenPos, out Vector3 hitPoint)
        {
            Ray ray = _camera.ScreenPointToRay(screenPos);
            if (_groundPlane.Raycast(ray, out float enter))
            {
                hitPoint = ray.GetPoint(enter);
                return true;
            }

            hitPoint = Vector3.zero;
            return false;
        }

        private Vector3 ClampToBounds(Vector3 position)
        {
            position.x = Mathf.Clamp(position.x, _minBounds.x, _maxBounds.x);
            position.z = Mathf.Clamp(position.z, _minBounds.y, _maxBounds.y);
            return position;
        }

        private void OnDrawGizmosSelected()
        {
            // Visual preview of the boundary box in the Scene view
            Gizmos.color = Color.cyan;
            Vector3 center = new Vector3(
                (_minBounds.x + _maxBounds.x) * 0.5f,
                transform.position.y,
                (_minBounds.y + _maxBounds.y) * 0.5f
            );
            Vector3 size = new Vector3(
                Mathf.Abs(_maxBounds.x - _minBounds.x),
                1f,
                Mathf.Abs(_maxBounds.y - _minBounds.y)
            );
            Gizmos.DrawWireCube(center, size);
        }
    }
}