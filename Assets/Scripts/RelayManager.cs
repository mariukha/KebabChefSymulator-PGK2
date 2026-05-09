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
/// Менеджер Unity Relay — позволяет подключаться через интернет без VPN и проброса портов.
/// 
/// Принцип работы:
///   1. Хост вызывает CreateRelay() → получает код комнаты (например "ABCD12")
///   2. Хост говорит код другу
///   3. Друг вызывает JoinRelay("ABCD12") → подключается через серверы Unity
/// 
/// Трафик идёт через Relay-серверы Unity, поэтому NAT/firewall не мешает.
/// Бесплатный тариф: до 50 одновременных подключений.
/// </summary>
public class RelayManager : MonoBehaviour
{
    public static RelayManager Instance { get; private set; }

    /// <summary>
    /// Код комнаты, сгенерированный при создании хоста.
    /// Другие игроки вводят этот код чтобы подключиться.
    /// </summary>
    public string JoinCode { get; private set; } = string.Empty;

    /// <summary>
    /// Состояние инициализации Unity Services.
    /// </summary>
    public bool IsServicesInitialized { get; private set; }

    /// <summary>
    /// Текст последней ошибки (для отображения в UI).
    /// </summary>
    public string LastError { get; private set; } = string.Empty;

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
    /// Инициализация Unity Gaming Services (однократно).
    /// Нужна перед использованием Relay.
    /// Авторизация анонимная — не требует аккаунта от игроков.
    /// </summary>
    public async Task InitializeServices()
    {
        if (IsServicesInitialized)
        {
            return;
        }

        try
        {
            // Инициализация Unity Services (Relay, Authentication)
            await UnityServices.InitializeAsync();

            // Анонимная авторизация — игрокам не нужен аккаунт
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            IsServicesInitialized = true;
            Debug.Log("[RelayManager] Unity Services zainicjalizowane. Zalogowano anonimowo.");
        }
        catch (Exception e)
        {
            LastError = "Blad inicjalizacji: " + e.Message;
            Debug.LogError("[RelayManager] " + LastError);
        }
    }

    /// <summary>
    /// ХОСТ: создаёт Relay-комнату и запускает сервер.
    /// Возвращает код комнаты (join code) или null при ошибке.
    /// Максимум 3 дополнительных игрока (хост + 3 = 4 всего).
    /// </summary>
    public async Task<string> CreateRelay(int maxPlayers = 3)
    {
        try
        {
            await InitializeServices();

            if (!IsServicesInitialized)
            {
                return null;
            }

            // Создаём Relay-аллокацию на серверах Unity
            // maxPlayers = количество ДОПОЛНИТЕЛЬНЫХ игроков (без хоста)
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers);

            // Получаем код комнаты — короткий текст для передачи друзьям
            JoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            Debug.Log("[RelayManager] Relay utworzony. Kod: " + JoinCode);

            // Настраиваем UnityTransport для работы через Relay
            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            // Запускаем хост
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
    /// КЛИЕНТ: подключается к существующей комнате по коду.
    /// Код — это текст вроде "ABCD12", полученный от хоста.
    /// </summary>
    public async Task<bool> JoinRelay(string joinCode)
    {
        try
        {
            await InitializeServices();

            if (!IsServicesInitialized)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(joinCode))
            {
                LastError = "Kod pokoju jest pusty.";
                return false;
            }

            joinCode = joinCode.Trim().ToUpperInvariant();

            // Подключаемся к Relay-комнате по коду
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            Debug.Log("[RelayManager] Dolaczono do Relay. Kod: " + joinCode);

            // Настраиваем UnityTransport для работы через Relay
            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );

            // Запускаем клиент
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

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
