using UnityEngine;
using Unity.Cinemachine;
using Sirenix.OdinInspector;
using UnityEngine.InputSystem;
using Utils.UI;

[RequireComponent(typeof(CinemachineCamera))]
public class CameraController : MyBox.Singleton<CameraController>
{
    [Header("Pan Settings")]
    [SerializeField] private float panSpeed = 0.5f;

    [Header("Zoom Settings")]
    [SerializeField] private float zoomSpeed = 5f;
    [SerializeField] private float zoomSmoothness = 10f;
    [SerializeField] private float minCameraDistance = 5f;
    [SerializeField] private float maxCameraDistance = 50f;
    [SerializeField] private float m_defaultZooom = 20f;

    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 100f;

    [Header("Boundary Settings")]
    [SerializeField] private float boundaryRadius = 100f;

    [TitleGroup("Anim Settings")]
    [SerializeField] private float m_resetAnimDuration = 1f;
    [SerializeField] private float m_resetAnimZoom = 100f;
    [SerializeField] private AnimationCurve m_resetAnimCurve;

    #region Variables
    private bool isControlling = true;

    private CinemachineCamera virtualCamera;
    private CinemachineComponentBase componentBase;
    private CinemachinePositionComposer composer;

    private Transform followTarget;
    private Vector2 lastMousePosition;
    private float targetCameraDistance;
    #endregion

    protected void Awake()
    {
        virtualCamera = GetComponent<CinemachineCamera>();
        componentBase = virtualCamera.GetCinemachineComponent(CinemachineCore.Stage.Body);
        composer = componentBase as CinemachinePositionComposer;

        if (virtualCamera.Follow == null)
            Debug.LogWarning("CameraController: CinemachineCamera has no Follow target assigned!");

        followTarget = virtualCamera.Follow;
    }

    private void Update()
    {
        if (!isControlling) return;
        HandlePanning();
        HandleZoom();
        HandleRotation();
    }

    public void SetControl(bool enable)
    {
        isControlling = enable;

        if (enable)
        {
            lastMousePosition = Mouse.current.position.ReadValue();
        }
    }

    private void HandlePanning()
    {
        if (followTarget == null) return;

        // Middle mouse pan
        if (Mouse.current.middleButton.wasPressedThisFrame)
        {
            lastMousePosition = Mouse.current.position.ReadValue();
        }

        if (Mouse.current.middleButton.isPressed)
        {
            Vector3 delta = Mouse.current.position.ReadValue() - lastMousePosition;
        
            Vector3 right = transform.right;
            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;

            Vector3 move = (-right * delta.x - forward * delta.y) * panSpeed * Time.deltaTime * PlayerPrefs.GetFloat("CameraSensitivity", 0.5f);
            Vector3 newPosition = followTarget.position + move;

            newPosition = ApplyBoundaryConstraint(newPosition);
            followTarget.position = newPosition;
            lastMousePosition = Mouse.current.position.ReadValue();
        }

        // WASD pan (physical key position, cross-layout)
        Vector3 right2 = transform.right;
        Vector3 forward2 = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;

        Vector3 keyboardInput = Vector3.zero;
        if (Keyboard.current[Key.W].isPressed) keyboardInput += forward2;
        if (Keyboard.current[Key.S].isPressed) keyboardInput -= forward2;
        if (Keyboard.current[Key.A].isPressed) keyboardInput -= right2;
        if (Keyboard.current[Key.D].isPressed) keyboardInput += right2;

        if (keyboardInput != Vector3.zero)
        {
            Vector3 keyboardMove = keyboardInput * panSpeed * 10f * Time.deltaTime * PlayerPrefs.GetFloat("CameraSensitivity", 0.5f);
            Vector3 newPosition = ApplyBoundaryConstraint(followTarget.position + keyboardMove);
            followTarget.position = newPosition;
        }
    }

    private Vector3 ApplyBoundaryConstraint(Vector3 position)
    {
        Vector3 flatPosition = new Vector3(position.x, 0f, position.z);
        Vector3 flatCenter = Vector3.zero;
        float distanceFromCenter = Vector3.Distance(flatPosition, flatCenter);
        
        if (distanceFromCenter <= boundaryRadius)
            return position;
        
        Vector3 directionFromCenter = (flatPosition - flatCenter).normalized;
        Vector3 clampedFlatPosition = flatCenter + directionFromCenter * boundaryRadius;
        
        return new Vector3(clampedFlatPosition.x, position.y, clampedFlatPosition.z);
    }

    private void HandleZoom()
    {
        if (composer == null) return;

        if (UIManager.Instance != null && UIManager.Instance.IsOverUI) return;

        float scroll = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            targetCameraDistance -= scroll * zoomSpeed;
            targetCameraDistance = Mathf.Clamp(targetCameraDistance, minCameraDistance, maxCameraDistance);
        }
        
        composer.CameraDistance = Mathf.Lerp(
            composer.CameraDistance, 
            targetCameraDistance, 
            zoomSmoothness * Time.deltaTime
        );
    }

    private void HandleRotation()
    {
        if (Mouse.current.middleButton.wasPressedThisFrame)
        {
            lastMousePosition = Mouse.current.position.ReadValue();
        }

        if (Mouse.current.rightButton.wasPressedThisFrame )
        {
            Vector3 delta = Mouse.current.position.ReadValue() - lastMousePosition;
            float rotationY = delta.x * rotationSpeed * Time.deltaTime * PlayerPrefs.GetFloat("CameraSensitivity", 0.5f);

            transform.Rotate(Vector3.up, rotationY, Space.World);

            lastMousePosition = Mouse.current.position.ReadValue();
        }
    }
}