using UnityEngine;

namespace Farm.Core
{
    /// <summary>
    /// Aligns a world-space object (such as a World Space Canvas or UI element) to face the camera.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Billboard : MonoBehaviour
    {
        public enum BillboardMode
        {
            /// <summary>
            /// Matches the camera's orientation directly. Prevents perspective distortion for World Space UI Canvases.
            /// </summary>
            CameraForward,

            /// <summary>
            /// Faces the camera position directly.
            /// </summary>
            LookAtCamera,

            /// <summary>
            /// Constrains rotation to the world Y-axis only, keeping the UI vertically upright.
            /// </summary>
            YAxisOnly
        }

        [Header("Camera Reference")]
        [Tooltip("Target camera to face. If unassigned, automatically resolves to Camera.main.")]
        [SerializeField] private Camera _targetCamera;

        [Header("Alignment Settings")]
        [Tooltip("Algorithm used to orient the object towards the camera.")]
        [SerializeField] private BillboardMode _mode = BillboardMode.CameraForward;

        [Tooltip("Rotates the object 180 degrees if the UI or graphic faces backward.")]
        [SerializeField] private bool _invertFacing = false;

        private Transform _cachedTransform;
        private Transform _cameraTransform;

        private void Awake()
        {
            _cachedTransform = transform;
            ResolveCamera();
        }

        private void LateUpdate()
        {
            if (_cameraTransform == null)
            {
                ResolveCamera();
                if (_cameraTransform == null) return;
            }

            UpdateAlignment();
        }

        /// <summary>
        /// Explicitly sets the target camera to follow.
        /// </summary>
        /// <param name="targetCamera">The camera to align towards.</param>
        public void SetCamera(Camera targetCamera)
        {
            _targetCamera = targetCamera;
            _cameraTransform = targetCamera != null ? targetCamera.transform : null;
        }

        private void ResolveCamera()
        {
            if (_targetCamera == null)
            {
                _targetCamera = Camera.main;
            }

            if (_targetCamera != null)
            {
                _cameraTransform = _targetCamera.transform;
            }
        }

        private void UpdateAlignment()
        {
            switch (_mode)
            {
                case BillboardMode.CameraForward:
                    Quaternion camRotation = _cameraTransform.rotation;
                    _cachedTransform.rotation = _invertFacing
                        ? camRotation * Quaternion.Euler(0f, 180f, 0f)
                        : camRotation;
                    break;

                case BillboardMode.LookAtCamera:
                    Vector3 toCamera = _cachedTransform.position - _cameraTransform.position;
                    if (toCamera.sqrMagnitude > 0.0001f)
                    {
                        Vector3 lookDir = _invertFacing ? -toCamera : toCamera;
                        _cachedTransform.rotation = Quaternion.LookRotation(lookDir, _cameraTransform.up);
                    }
                    break;

                case BillboardMode.YAxisOnly:
                    Vector3 toCameraY = _cachedTransform.position - _cameraTransform.position;
                    toCameraY.y = 0f;
                    if (toCameraY.sqrMagnitude > 0.0001f)
                    {
                        Vector3 lookDir = _invertFacing ? -toCameraY : toCameraY;
                        _cachedTransform.rotation = Quaternion.LookRotation(lookDir, Vector3.up);
                    }
                    break;
            }
        }
    }
}
