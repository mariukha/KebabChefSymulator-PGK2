/// \file SimplePlayerController.cs
/// \brief Plik zawierający klasę SimplePlayerController odpowiedzialną za ruch i sterowanie kamerą gracza.
/// \details Implementuje prosty kontroler pierwszoosobowy z obsługą ruchu za pomocą klawiatury,
/// obrotu kamery za pomocą myszy, grawitacji oraz blokowania wejścia podczas otwartych menu.

using UnityEngine;

/// <summary>
/// Prosty kontroler ruchu gracza w trybie pierwszoosobowym.
/// Obsługuje poruszanie się postaci za pomocą klawiszy WASD, obrót kamery za pomocą myszy,
/// grawitację, blokowanie/odblokowywanie kursora oraz przełączanie trybu okna (F11).
/// Blokuje sterowanie, gdy otwarte jest menu główne, sklep, lobby, pauza lub ustawienia.
/// </summary>
public class SimplePlayerController : MonoBehaviour
{
    /// <summary>
    /// Wysokość oczu gracza nad poziomem podłoża (w jednostkach Unity).
    /// Określa pozycję kamery względem postaci.
    /// </summary>
    private const float EyeHeight = 1.75f;

    /// <summary>
    /// Prędkość poruszania się gracza (w jednostkach Unity na sekundę).
    /// </summary>
    [Header("Ustawienia ruchu")]
    public float speed = 4.5f;

    /// <summary>
    /// Czułość myszy wpływająca na szybkość obrotu kamery.
    /// Wartość jest pobierana z <see cref="GameSettingsManager"/> podczas startu.
    /// </summary>
    public float sensitivity = 2.0f;

    /// <summary>
    /// Przyspieszenie grawitacyjne stosowane do gracza (ujemna wartość oznacza kierunek w dół).
    /// </summary>
    public float gravity = -20f;

    /// <summary>
    /// Kamera gracza używana do widoku pierwszoosobowego i obrotu.
    /// </summary>
    [Header("Referencje")]
    public Camera playerCamera;

    /// <summary>
    /// Komponent CharacterController używany do fizycznego poruszania postaci z obsługą kolizji.
    /// </summary>
    private CharacterController characterController;

    /// <summary>
    /// Bieżący kąt obrotu kamery w osi X (pochylenie w górę/dół).
    /// Wartość jest ograniczana do zakresu [-85°, 85°], aby zapobiec odwróceniu kamery.
    /// </summary>
    private float rotationX;

    /// <summary>
    /// Bieżąca prędkość pionowa gracza, uwzględniająca grawitację.
    /// </summary>
    private float verticalVelocity;

    /// <summary>
    /// Flaga określająca, czy ustawiono początkowy cel patrzenia przed inicjalizacją.
    /// </summary>
    private bool hasInitialLookTarget;

    /// <summary>
    /// Pozycja w świecie, na którą gracz powinien patrzeć przy starcie.
    /// Używana, gdy cel patrzenia jest ustawiany przed wywołaniem Start().
    /// </summary>
    private Vector3 initialLookTarget;

    /// <summary>
    /// Zbuforowana referencja do interfejsu sklepu, aby uniknąć wielokrotnego wyszukiwania.
    /// </summary>
    private ShopUI cachedShopUI;

    /// <summary>
    /// Zbuforowana referencja do interfejsu lobby, aby uniknąć wielokrotnego wyszukiwania.
    /// </summary>
    private LobbyUI cachedLobbyUI;

    /// <summary>
    /// Inicjalizuje CharacterController w metodzie Awake.
    /// Jeśli komponent nie jest obecny, tworzy go z domyślnymi parametrami
    /// (wysokość 1.8, środek na 0.9, promień 0.35).
    /// Konfiguruje także wysokość kroku i limit nachylenia terenu.
    /// </summary>
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

    /// <summary>
    /// Inicjalizacja gracza przy starcie sceny.
    /// Blokuje i ukrywa kursor, ładuje czułość myszy z ustawień gry,
    /// ustawia kamerę na wysokości oczu i opcjonalnie kieruje widok na cel początkowy.
    /// </summary>
    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (GameSettingsManager.Instance != null)
        {
            sensitivity = GameSettingsManager.Instance.MouseSensitivity;
        }

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

    /// <summary>
    /// Ustawia początkowy cel patrzenia, który zostanie zastosowany w metodzie Start().
    /// Używane, gdy cel patrzenia musi być określony przed inicjalizacją kontrolera.
    /// </summary>
    /// <param name="worldTarget">Pozycja w świecie, na którą gracz powinien patrzeć.</param>
    public void SetInitialLookTarget(Vector3 worldTarget)
    {
        initialLookTarget = worldTarget;
        hasInitialLookTarget = true;
    }

    /// <summary>
    /// Natychmiast kieruje wzrok gracza na wskazany punkt w świecie.
    /// Obraca postać w osi Y, aby patrzeć w kierunku celu, i ustawia pochylenie kamery w osi X.
    /// </summary>
    /// <param name="worldTarget">Pozycja w świecie, na którą gracz powinien skierować wzrok.</param>
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

    /// <summary>
    /// Główna pętla aktualizacji wywoływana co klatkę.
    /// Sprawdza, czy interfejsy użytkownika blokują sterowanie (sklep, lobby, menu, pauza, ustawienia).
    /// Jeśli sterowanie nie jest zablokowane, obsługuje ruch i obrót kamery.
    /// Obsługuje również klawisz Escape (blokada/odblokowanie kursora) i F11 (przełączanie trybu okna).
    /// </summary>
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
        MainMenuUI mainMenu = FindFirstObjectByType<MainMenuUI>();
        bool menuOpen = mainMenu != null && mainMenu.IsMenuOpen;
        bool pauseOpen = PauseMenuUI.Instance != null && PauseMenuUI.Instance.IsPaused;
        bool settingsOpen = SettingsMenuUI.Instance != null && SettingsMenuUI.Instance.IsOpen;
        bool inputBlocked = shopOpen || lobbyOpen || menuOpen || pauseOpen || settingsOpen;

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
            GameSettingsManager.EnsureInstance().ToggleWindowModeShortcut();
        }
    }

    /// <summary>
    /// Obsługuje ruch gracza na podstawie wejścia z klawiatury (osie Vertical i Horizontal).
    /// Stosuje normalizację wektora ruchu, grawitację oraz porusza postać za pomocą CharacterController.
    /// Resetuje prędkość pionową do małej ujemnej wartości, gdy gracz stoi na ziemi.
    /// </summary>
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

    /// <summary>
    /// Obsługuje obrót kamery i postaci na podstawie ruchu myszy.
    /// Obraca postać wokół osi Y (lewo/prawo) i kamerę wokół osi X (góra/dół).
    /// Pochylenie kamery jest ograniczone do zakresu [-85°, 85°].
    /// Obrót jest aktywny tylko gdy kursor jest zablokowany.
    /// </summary>
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

    /// <summary>
    /// Normalizuje kąt do zakresu [-180°, 180°].
    /// Używane do konwersji kątów Eulera, które mogą wykraczać poza standardowy zakres.
    /// </summary>
    /// <param name="angle">Kąt do znormalizowania w stopniach.</param>
    /// <returns>Znormalizowany kąt w zakresie [-180°, 180°].</returns>
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
