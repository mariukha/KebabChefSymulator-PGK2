using UnityEngine;

public class SimplePlayerController : MonoBehaviour
{
    private const float EyeHeight = 1.75f;

    [Header("Ustawienia ruchu")]
    public float speed = 4.5f;
    public float sensitivity = 2.0f;
    public float gravity = -20f;

    [Header("Referencje")]
    public Camera playerCamera;

    private CharacterController characterController;
    private float rotationX;
    private float verticalVelocity;
    private bool hasInitialLookTarget;
    private Vector3 initialLookTarget;
    private ShopUI cachedShopUI;
    private LobbyUI cachedLobbyUI;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            characterController = gameObject.AddComponent<CharacterController>();
            characterController.height = 1.8f;
            characterController.center = new Vector3(0f, 0.9f, 0f);
            characterController.radius = 0.35f;
        }

        characterController.stepOffset = 0.06f;
        characterController.slopeLimit = 45f;
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
        }

        if (playerCamera != null)
        {
            playerCamera.transform.localPosition = new Vector3(0f, EyeHeight, 0f);
            rotationX = NormalizeAngle(playerCamera.transform.localEulerAngles.x);
            rotationX = Mathf.Clamp(rotationX, -85f, 85f);
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
        }

        if (hasInitialLookTarget)
        {
            SetLookAt(initialLookTarget);
        }
    }

    public void SetInitialLookTarget(Vector3 worldTarget)
    {
        initialLookTarget = worldTarget;
        hasInitialLookTarget = true;
    }

    public void SetLookAt(Vector3 worldTarget)
    {
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
        }

        Vector3 eyePosition = transform.position + new Vector3(0f, EyeHeight, 0f);
        Vector3 direction = worldTarget - eyePosition;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Vector3 flatDirection = Vector3.ProjectOnPlane(direction.normalized, Vector3.up);
        if (flatDirection.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(flatDirection.normalized, Vector3.up);
        }

        rotationX = -Mathf.Asin(Mathf.Clamp(direction.normalized.y, -1f, 1f)) * Mathf.Rad2Deg;
        rotationX = Mathf.Clamp(rotationX, -85f, 85f);

        if (playerCamera != null)
        {
            playerCamera.transform.localPosition = new Vector3(0f, EyeHeight, 0f);
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
        }
    }

    private void Update()
    {
        if (cachedShopUI == null)
        {
            cachedShopUI = FindFirstObjectByType<ShopUI>();
        }

        if (cachedLobbyUI == null)
        {
            cachedLobbyUI = FindFirstObjectByType<LobbyUI>();
        }

        bool shopOpen = cachedShopUI != null && cachedShopUI.IsShopOpen;
        bool lobbyOpen = cachedLobbyUI != null && cachedLobbyUI.IsLobbyOpen;
        bool inputBlocked = shopOpen || lobbyOpen;

        if (!inputBlocked)
        {
            HandleMovement();
            HandleRotation();
        }

        if (Input.GetKeyDown(KeyCode.Escape) && !inputBlocked)
        {
            bool shouldUnlock = Cursor.lockState == CursorLockMode.Locked;
            Cursor.lockState = shouldUnlock ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = shouldUnlock;
        }

        if (Input.GetKeyDown(KeyCode.F11))
        {
            if (Screen.fullScreen)
            {
                Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
            }
            else
            {
                Resolution current = Screen.currentResolution;
                Screen.SetResolution(current.width, current.height, FullScreenMode.FullScreenWindow);
            }
        }
    }

    private void HandleMovement()
    {
        float moveForward = Input.GetAxisRaw("Vertical");
        float moveSide = Input.GetAxisRaw("Horizontal");

        Vector3 move = (transform.forward * moveForward) + (transform.right * moveSide);
        move = Vector3.ClampMagnitude(move, 1f);

        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        verticalVelocity += gravity * Time.deltaTime;
        move.y = verticalVelocity / speed;

        characterController.Move(move * speed * Time.deltaTime);
    }

    private void HandleRotation()
    {
        if (playerCamera == null || Cursor.lockState != CursorLockMode.Locked)
        {
            return;
        }

        float mouseX = Input.GetAxis("Mouse X") * sensitivity * 100f * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity * 100f * Time.deltaTime;

        transform.Rotate(Vector3.up * mouseX);

        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, -85f, 85f);
        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
    }

    private float NormalizeAngle(float angle)
    {
        while (angle > 180f)
        {
            angle -= 360f;
        }

        while (angle < -180f)
        {
            angle += 360f;
        }

        return angle;
    }
}
