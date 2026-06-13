/// \file RelayManager.cs
/// \brief Plik zawierający klasę RelayManager do zarządzania usługą Unity Relay.
/// \details Definiuje logikę inicjalizacji Unity Services, tworzenia sesji Relay (jako host),
/// dołączania do istniejącej sesji (jako klient) oraz konfiguracji transportu sieciowego
/// do komunikacji przez serwery pośredniczące Unity Relay.

using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

/// <summary>
/// Klasa singleton zarządzająca usługą Unity Relay do gry wieloosobowej przez internet.
/// </summary>
/// <remarks>
/// Unity Relay umożliwia graczom łączenie się bez konieczności otwierania portów
/// lub posiadania publicznego adresu IP. Komunikacja odbywa się przez serwery pośredniczące Unity.
/// <para>
/// Klasa odpowiada za:
/// <list type="bullet">
///   <item>Inicjalizację Unity Services i anonimowe logowanie</item>
///   <item>Tworzenie alokacji Relay i generowanie kodu dołączenia</item>
///   <item>Dołączanie do istniejącej sesji Relay za pomocą kodu</item>
///   <item>Konfigurację <see cref="UnityTransport"/> z danymi serwera Relay</item>
///   <item>Uruchamianie hosta lub klienta przez <see cref="NetworkManager"/></item>
/// </list>
/// </para>
/// Używa wzorca singleton z <see cref="Instance"/>.
/// </remarks>
public class RelayManager : MonoBehaviour
{
    /// <summary>
    /// Statyczna instancja singletona klasy <see cref="RelayManager"/>.
    /// </summary>
    /// <value>Jedyna aktywna instancja <see cref="RelayManager"/> lub <c>null</c>.</value>
    public static RelayManager Instance { get; private set; }

    /// <summary>
    /// Kod dołączenia do sesji Relay wygenerowany po utworzeniu alokacji.
    /// </summary>
    /// <value>
    /// Kod alfanumeryczny umożliwiający innym graczom dołączenie do sesji
    /// lub <see cref="string.Empty"/> jeśli sesja nie została jeszcze utworzona.
    /// </value>
    public string JoinCode { get; private set; } = string.Empty;

    /// <summary>
    /// Właściwość informująca, czy Unity Services zostały pomyślnie zainicjalizowane.
    /// </summary>
    /// <value><c>true</c> jeśli usługi są gotowe do użycia; w przeciwnym razie <c>false</c>.</value>
    public bool IsServicesInitialized { get; private set; }

    /// <summary>
    /// Ostatni komunikat błędu z operacji sieciowej.
    /// </summary>
    /// <value>Opis błędu lub <see cref="string.Empty"/> jeśli brak błędów.</value>
    public string LastError { get; private set; } = string.Empty;

    /// <summary>
    /// Metoda Awake wywoływana przy tworzeniu obiektu.
    /// Implementuje wzorzec singleton — niszczy duplikaty.
    /// </summary>
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            return;
        }

        Destroy(gameObject);
    }

    /// <summary>
    /// Asynchronicznie inicjalizuje Unity Services i loguje użytkownika anonimowo.
    /// </summary>
    /// <returns>Task reprezentujący operację asynchroniczną.</returns>
    /// <remarks>
    /// Jeśli usługi są już zainicjalizowane, metoda natychmiast powraca.
    /// W przypadku błędu ustawia <see cref="LastError"/> i <see cref="IsServicesInitialized"/> na <c>false</c>.
    /// Logowanie anonimowe jest wymagane przez Unity Relay do identyfikacji gracza.
    /// </remarks>
    public async Task InitializeServices()
    {
        if (IsServicesInitialized)
        {
            return;
        }

        LastError = string.Empty;

        try
        {
            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            IsServicesInitialized = true;
            Debug.Log("[RelayManager] Unity Services zainicjalizowane. Zalogowano anonimowo.");
        }
        catch (Exception e)
        {
            IsServicesInitialized = false;
            LastError = "Blad inicjalizacji: " + e.Message;
            Debug.LogError("[RelayManager] " + LastError);
        }
    }

    /// <summary>
    /// Asynchronicznie tworzy nową sesję Relay i uruchamia hosta.
    /// </summary>
    /// <param name="maxPlayers">Maksymalna liczba graczy mogących dołączyć (nie licząc hosta). Domyślnie 3.</param>
    /// <returns>
    /// Kod dołączenia (<see cref="JoinCode"/>) jeśli operacja się powiodła;
    /// <c>null</c> w przypadku błędu.
    /// </returns>
    /// <remarks>
    /// Wykonuje następujące kroki:
    /// <list type="number">
    ///   <item>Inicjalizuje Unity Services (jeśli jeszcze nie zainicjalizowane)</item>
    ///   <item>Tworzy alokację Relay na serwerze Unity</item>
    ///   <item>Generuje kod dołączenia dla innych graczy</item>
    ///   <item>Konfiguruje <see cref="UnityTransport"/> z danymi serwera Relay</item>
    ///   <item>Rejestruje handler prefabu i uruchamia hosta</item>
    /// </list>
    /// W przypadku błędu ustawia <see cref="LastError"/> z opisem problemu.
    /// </remarks>
    /// <exception cref="Exception">Przechwytywany wewnętrznie — błędy są zapisywane w <see cref="LastError"/>.</exception>
    public async Task<string> CreateRelay(int maxPlayers = 3)
    {
        LastError = string.Empty;

        try
        {
            await InitializeServices();

            if (!IsServicesInitialized)
            {
                if (string.IsNullOrEmpty(LastError))
                {
                    LastError = "Unity Services nie zostaly zainicjalizowane.";
                }

                return null;
            }

            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers);

            JoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            Debug.Log("[RelayManager] Relay utworzony. Kod: " + JoinCode);

            UnityTransport transport = GetTransport();
            if (transport == null)
            {
                return null;
            }

            transport.SetRelayServerData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            if (NetworkSetup.Instance == null)
            {
                LastError = "NetworkSetup nie istnieje.";
                return null;
            }

            NetworkSetup.Instance.RegisterPrefabHandler();
            bool started = NetworkManager.Singleton.StartHost();

            if (started)
            {
                Debug.Log("[RelayManager] Host uruchomiony przez Relay. Kod: " + JoinCode);
                return JoinCode;
            }
            else
            {
                LastError = "Nie udalo sie uruchomic hosta.";
                return null;
            }
        }
        catch (Exception e)
        {
            LastError = "Blad tworzenia Relay: " + e.Message;
            Debug.LogError("[RelayManager] " + LastError);
            return null;
        }
    }

    /// <summary>
    /// Asynchronicznie dołącza do istniejącej sesji Relay jako klient.
    /// </summary>
    /// <param name="joinCode">Kod dołączenia do sesji (otrzymany od hosta).</param>
    /// <returns>
    /// <c>true</c> jeśli połączenie powiodło się;
    /// <c>false</c> w przypadku błędu.
    /// </returns>
    /// <remarks>
    /// Wykonuje następujące kroki:
    /// <list type="number">
    ///   <item>Inicjalizuje Unity Services (jeśli jeszcze nie zainicjalizowane)</item>
    ///   <item>Waliduje podany kod dołączenia</item>
    ///   <item>Dołącza do alokacji Relay na serwerze Unity</item>
    ///   <item>Konfiguruje <see cref="UnityTransport"/> z danymi serwera Relay (w tym danymi hosta)</item>
    ///   <item>Rejestruje handler prefabu i uruchamia klienta</item>
    /// </list>
    /// Kod dołączenia jest automatycznie normalizowany (przycinany i konwertowany na wielkie litery).
    /// W przypadku błędu ustawia <see cref="LastError"/> z opisem problemu.
    /// </remarks>
    /// <exception cref="RelayServiceException">Przechwytywany — nieprawidłowy kod daje specjalny komunikat błędu.</exception>
    /// <exception cref="Exception">Przechwytywany — ogólne błędy połączenia.</exception>
    public async Task<bool> JoinRelay(string joinCode)
    {
        LastError = string.Empty;

        try
        {
            await InitializeServices();

            if (!IsServicesInitialized)
            {
                if (string.IsNullOrEmpty(LastError))
                {
                    LastError = "Unity Services nie zostaly zainicjalizowane.";
                }

                return false;
            }

            if (string.IsNullOrWhiteSpace(joinCode))
            {
                LastError = "Kod pokoju jest pusty.";
                return false;
            }

            joinCode = joinCode.Trim().ToUpperInvariant();

            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            Debug.Log("[RelayManager] Dolaczono do Relay. Kod: " + joinCode);

            UnityTransport transport = GetTransport();
            if (transport == null)
            {
                return false;
            }

            transport.SetRelayServerData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );

            if (NetworkSetup.Instance == null)
            {
                LastError = "NetworkSetup nie istnieje.";
                return false;
            }

            NetworkSetup.Instance.RegisterPrefabHandler();
            bool started = NetworkManager.Singleton.StartClient();

            if (started)
            {
                Debug.Log("[RelayManager] Klient polaczony przez Relay.");
                return true;
            }
            else
            {
                LastError = "Nie udalo sie polaczyc jako klient.";
                return false;
            }
        }
        catch (RelayServiceException e)
        {
            if (e.Message.Contains("not found") || e.Message.Contains("invalid"))
            {
                LastError = "Nieprawidlowy kod pokoju.";
            }
            else
            {
                LastError = "Blad Relay: " + e.Message;
            }

            Debug.LogError("[RelayManager] " + LastError);
            return false;
        }
        catch (Exception e)
        {
            LastError = "Blad polaczenia: " + e.Message;
            Debug.LogError("[RelayManager] " + LastError);
            return false;
        }
    }

    /// <summary>
    /// Pobiera komponent <see cref="UnityTransport"/> z <see cref="NetworkManager"/>.
    /// </summary>
    /// <returns>
    /// Komponent <see cref="UnityTransport"/> lub <c>null</c> jeśli nie został znaleziony.
    /// </returns>
    /// <remarks>
    /// W przypadku braku NetworkManager lub UnityTransport ustawia odpowiedni komunikat w <see cref="LastError"/>.
    /// </remarks>
    private UnityTransport GetTransport()
    {
        if (NetworkManager.Singleton == null)
        {
            LastError = "NetworkManager nie istnieje.";
            return null;
        }

        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport == null)
        {
            LastError = "UnityTransport nie istnieje.";
        }

        return transport;
    }

    /// <summary>
    /// Metoda wywoływana przy niszczeniu obiektu.
    /// Czyści referencję singletona.
    /// </summary>
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
