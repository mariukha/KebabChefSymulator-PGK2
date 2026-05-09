using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class RelayManager : MonoBehaviour
{
    public static RelayManager Instance { get; private set; }

    public string JoinCode { get; private set; } = string.Empty;

    public bool IsServicesInitialized { get; private set; }

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

    public async Task InitializeServices()
    {
        if (IsServicesInitialized)
        {
            return;
        }

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
            LastError = "Blad inicjalizacji: " + e.Message;
            Debug.LogError("[RelayManager] " + LastError);
        }
    }

    public async Task<string> CreateRelay(int maxPlayers = 3)
    {
        try
        {
            await InitializeServices();

            if (!IsServicesInitialized)
            {
                return null;
            }

            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers);

            JoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            Debug.Log("[RelayManager] Relay utworzony. Kod: " + JoinCode);

            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

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

            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            Debug.Log("[RelayManager] Dolaczono do Relay. Kod: " + joinCode);

            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );

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
