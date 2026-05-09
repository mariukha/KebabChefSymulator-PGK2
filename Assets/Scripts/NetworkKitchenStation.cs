using UnityEngine;

/// <summary>
/// Lightweight station sync helper (plain MonoBehaviour, no NetworkObject needed).
/// On the SERVER: periodically checks for dirty state, provides data for NetworkPlayer to broadcast.
/// On the CLIENT: receives state updates from NetworkPlayer and applies to local KitchenStation.
/// Interaction routing goes through NetworkPlayer.InteractWithStationServerRpc().
/// </summary>
public class NetworkKitchenStation : MonoBehaviour
{
    private KitchenStation localStation;

    // Dirty-checking cache (server only)
    private bool lastIsProcessing;
    private float lastProcessEndTime;
    private int lastPreparedMeatServings;
    private bool lastHasLavash;
    private int lastAssemblyCount;
    private NetworkItemState lastStationItem;

    /// <summary>
    /// Unique station index, assigned at creation time by KitchenGameBootstrap.
    /// Used to match stations between server and client.
    /// </summary>
    public int StationIndex { get; set; }

    private void Start()
    {
        localStation = GetComponent<KitchenStation>();
    }

    /// <summary>
    /// Returns true if station state has changed since last snapshot.
    /// Called by NetworkPlayer on the server to decide what to broadcast.
    /// </summary>
    public bool IsStateDirty()
    {
        if (localStation == null) return false;

        if (localStation.IsProcessing != lastIsProcessing) return true;
        if (localStation.HasLavash != lastHasLavash) return true;
        if (localStation.PreparedMeatServings != lastPreparedMeatServings) return true;
        if (localStation.AssemblyCount != lastAssemblyCount) return true;
        if (Mathf.Abs(localStation.ProcessEndTime - lastProcessEndTime) > 0.01f) return true;

        NetworkItemState currentItem = NetworkItemState.FromKitchenItem(localStation.StationItem);
        if (currentItem.exists != lastStationItem.exists) return true;
        if (currentItem.ingredientKind != lastStationItem.ingredientKind) return true;
        if (currentItem.state != lastStationItem.state) return true;

        return false;
    }

    /// <summary>
    /// Captures current station state into a serializable struct for network broadcast.
    /// Updates dirty-check cache.
    /// </summary>
    public StationStateSnapshot CaptureSnapshot()
    {
        if (localStation == null) return default;

        lastIsProcessing = localStation.IsProcessing;
        lastProcessEndTime = localStation.ProcessEndTime;
        lastPreparedMeatServings = localStation.PreparedMeatServings;
        lastHasLavash = localStation.HasLavash;
        lastAssemblyCount = localStation.AssemblyCount;
        lastStationItem = NetworkItemState.FromKitchenItem(localStation.StationItem);

        return new StationStateSnapshot
        {
            stationIndex = StationIndex,
            isProcessing = lastIsProcessing,
            processEndTime = lastProcessEndTime,
            preparedMeatServings = lastPreparedMeatServings,
            hasLavash = lastHasLavash,
            assemblyCount = lastAssemblyCount,
            stationItem = lastStationItem
        };
    }

    /// <summary>
    /// Applies state received from server broadcast (client-side).
    /// </summary>
    public void ApplySnapshot(StationStateSnapshot snapshot)
    {
        if (localStation == null)
        {
            localStation = GetComponent<KitchenStation>();
        }

        if (localStation != null)
        {
            localStation.SyncNetworkState(
                snapshot.isProcessing,
                snapshot.processEndTime,
                snapshot.preparedMeatServings,
                snapshot.hasLavash,
                snapshot.stationItem
            );
        }
    }

    /// <summary>
    /// Server-side: processes interaction on this station.
    /// Called by NetworkPlayer.InteractWithStationServerRpc().
    /// </summary>
    public void ServerInteract(PlayerInteraction interaction)
    {
        if (localStation != null)
        {
            localStation.Interact(interaction);
        }
    }
}

/// <summary>
/// Compact station state for network transmission via RPC.
/// </summary>
public struct StationStateSnapshot : Unity.Netcode.INetworkSerializable
{
    public int stationIndex;
    public bool isProcessing;
    public float processEndTime;
    public int preparedMeatServings;
    public bool hasLavash;
    public int assemblyCount;
    public NetworkItemState stationItem;

    public void NetworkSerialize<T>(Unity.Netcode.BufferSerializer<T> serializer) where T : Unity.Netcode.IReaderWriter
    {
        serializer.SerializeValue(ref stationIndex);
        serializer.SerializeValue(ref isProcessing);
        serializer.SerializeValue(ref processEndTime);
        serializer.SerializeValue(ref preparedMeatServings);
        serializer.SerializeValue(ref hasLavash);
        serializer.SerializeValue(ref assemblyCount);
        serializer.SerializeValue(ref stationItem);
    }
}
