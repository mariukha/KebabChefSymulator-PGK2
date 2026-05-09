using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Network synchronization wrapper for KitchenStation.
/// Runs alongside KitchenStation on each station GameObject.
/// Server-authoritative: clients send interaction requests via ServerRpc,
/// server processes them and broadcasts state changes via NetworkVariables.
/// </summary>
public class NetworkKitchenStation : NetworkBehaviour
{
    // Synchronized station state (Server -> All Clients)
    private NetworkVariable<bool> netIsProcessing = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private NetworkVariable<float> netProcessEndTime = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private NetworkVariable<int> netPreparedMeatServings = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private NetworkVariable<bool> netHasLavash = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private NetworkVariable<int> netAssemblyCount = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private NetworkVariable<NetworkItemState> netStationItem = new NetworkVariable<NetworkItemState>(
        NetworkItemState.Empty(), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private KitchenStation localStation;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        localStation = GetComponent<KitchenStation>();
        if (localStation == null)
        {
            Debug.LogWarning("[NetworkKitchenStation] Brak KitchenStation na obiekcie: " + gameObject.name);
        }

        // Clients listen for state changes to update visuals
        if (!IsServer)
        {
            netIsProcessing.OnValueChanged += OnProcessingStateChanged;
            netPreparedMeatServings.OnValueChanged += OnMeatServingsChanged;
            netHasLavash.OnValueChanged += OnLavashChanged;
            netAssemblyCount.OnValueChanged += OnAssemblyChanged;
            netStationItem.OnValueChanged += OnStationItemChanged;
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (!IsServer)
        {
            netIsProcessing.OnValueChanged -= OnProcessingStateChanged;
            netPreparedMeatServings.OnValueChanged -= OnMeatServingsChanged;
            netHasLavash.OnValueChanged -= OnLavashChanged;
            netAssemblyCount.OnValueChanged -= OnAssemblyChanged;
            netStationItem.OnValueChanged -= OnStationItemChanged;
        }
    }

    /// <summary>
    /// Called by PlayerInteraction when a player interacts with this station.
    /// Client sends the request to the server for authoritative processing.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void InteractServerRpc(ulong interactingClientId, NetworkItemState heldItem, ServerRpcParams rpcParams = default)
    {
        if (localStation == null)
        {
            return;
        }

        // Find the player's PlayerInteraction on the server
        NetworkPlayer networkPlayer = FindNetworkPlayerByClientId(interactingClientId);
        if (networkPlayer == null)
        {
            Debug.LogWarning("[NetworkKitchenStation] Nie znaleziono gracza: " + interactingClientId);
            return;
        }

        PlayerInteraction interaction = networkPlayer.GetComponent<PlayerInteraction>();
        if (interaction == null)
        {
            return;
        }

        // Synchronize the held item from network state to server-side PlayerInteraction
        KitchenItem clientItem = heldItem.ToKitchenItem();
        SyncHeldItemToServer(interaction, clientItem);

        // Process the interaction on the server
        localStation.Interact(interaction);

        // After interaction, sync the feedback message back to the requesting client
        string feedback = interaction.FeedbackMessage;
        if (!string.IsNullOrEmpty(feedback))
        {
            SendFeedbackClientRpc(feedback, RpcTargetSingle(interactingClientId));
        }

        // Sync updated held item back to client
        NetworkItemState updatedHeld = NetworkItemState.FromKitchenItem(interaction.HeldItem);
        SyncHeldItemBackClientRpc(updatedHeld, RpcTargetSingle(interactingClientId));

        // Update network state
        SyncStationState();
    }

    private void SyncStationState()
    {
        if (!IsServer || localStation == null)
        {
            return;
        }

        netIsProcessing.Value = localStation.IsProcessing;
        netProcessEndTime.Value = localStation.ProcessEndTime;
        netPreparedMeatServings.Value = localStation.PreparedMeatServings;
        netHasLavash.Value = localStation.HasLavash;
        netAssemblyCount.Value = localStation.AssemblyCount;
        netStationItem.Value = NetworkItemState.FromKitchenItem(localStation.StationItem);
    }

    private void Update()
    {
        if (IsServer && localStation != null)
        {
            // Sync state periodically because processing timers finish in KitchenStation.Update()
            SyncStationState();
        }
    }

    private ClientRpcParams RpcTargetSingle(ulong clientId)
    {
        return new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        };
    }

    [ClientRpc]
    private void SendFeedbackClientRpc(string message, ClientRpcParams clientRpcParams = default)
    {
        // Find local player and set feedback
        PlayerInteraction localInteraction = FindLocalPlayerInteraction();
        if (localInteraction != null)
        {
            localInteraction.SetFeedback(message);
        }
    }

    [ClientRpc]
    private void SyncHeldItemBackClientRpc(NetworkItemState itemState, ClientRpcParams clientRpcParams = default)
    {
        PlayerInteraction localInteraction = FindLocalPlayerInteraction();
        if (localInteraction == null)
        {
            return;
        }

        KitchenItem newItem = itemState.ToKitchenItem();
        if (newItem == null)
        {
            localInteraction.ClearHeldItem();
        }
        else if (!localInteraction.HasItemInHand)
        {
            localInteraction.TryReceiveItem(newItem);
        }
        else
        {
            // Replace: clear and re-receive
            localInteraction.ClearHeldItem();
            if (newItem != null)
            {
                localInteraction.TryReceiveItem(newItem);
            }
        }
    }

    private void SyncHeldItemToServer(PlayerInteraction interaction, KitchenItem clientItem)
    {
        // Ensure server-side PlayerInteraction has the same held item as the client
        interaction.ClearHeldItem();
        if (clientItem != null)
        {
            interaction.TryReceiveItem(clientItem);
        }
    }

    // === Value Changed Handlers (Client-side visual updates) ===
    // Clients only receive state to update visual color. 
    // Wait, the client's KitchenStation still has its own update loop?
    // Clients should theoretically override their local KitchenStation state or just refresh visuals.
    // For now, since localStation properties are private, we only refresh visuals.
    // Ideally, clients need these properties set so GetPrompt returns correct text.

    private void ApplyStateToLocal()
    {
        if (localStation != null && !IsServer)
        {
            localStation.SyncNetworkState(
                netIsProcessing.Value,
                netProcessEndTime.Value,
                netPreparedMeatServings.Value,
                netHasLavash.Value,
                netStationItem.Value
            );
        }
    }

    private void OnProcessingStateChanged(bool oldValue, bool newValue) => ApplyStateToLocal();
    private void OnMeatServingsChanged(int oldValue, int newValue) => ApplyStateToLocal();
    private void OnLavashChanged(bool oldValue, bool newValue) => ApplyStateToLocal();
    private void OnAssemblyChanged(int oldValue, int newValue) => ApplyStateToLocal();
    private void OnStationItemChanged(NetworkItemState oldValue, NetworkItemState newValue) => ApplyStateToLocal();

    // === Helpers ===

    private static NetworkPlayer FindNetworkPlayerByClientId(ulong clientId)
    {
        if (NetworkManager.Singleton == null)
        {
            return null;
        }

        NetworkClient client;
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out client))
        {
            return null;
        }

        if (client.PlayerObject == null)
        {
            return null;
        }

        return client.PlayerObject.GetComponent<NetworkPlayer>();
    }

    private static PlayerInteraction FindLocalPlayerInteraction()
    {
        NetworkPlayer[] players = FindObjectsByType<NetworkPlayer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (NetworkPlayer player in players)
        {
            if (player.IsOwner)
            {
                return player.GetComponent<PlayerInteraction>();
            }
        }

        return null;
    }
}
