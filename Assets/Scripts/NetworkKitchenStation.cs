using UnityEngine;

public class NetworkKitchenStation : MonoBehaviour
{
    private KitchenStation localStation;

    private bool lastIsProcessing;
    private float lastRemainingProcessTime;
    private int lastPreparedMeatServings;
    private bool lastHasLavash;
    private int lastAssemblyCount;
    private NetworkItemState lastStationItem;

    public int StationIndex { get; set; }

    private void Start()
    {
        localStation = GetComponent<KitchenStation>();
    }

    public bool IsStateDirty()
    {
        if (localStation == null) return false;

        if (localStation.IsProcessing != lastIsProcessing) return true;
        if (localStation.HasLavash != lastHasLavash) return true;
        if (localStation.PreparedMeatServings != lastPreparedMeatServings) return true;
        if (localStation.AssemblyCount != lastAssemblyCount) return true;

        float currentRemaining = localStation.IsProcessing
            ? Mathf.Max(0f, localStation.ProcessEndTime - Time.time)
            : 0f;
        if (Mathf.Abs(currentRemaining - lastRemainingProcessTime) > 0.5f) return true;

        NetworkItemState currentItem = NetworkItemState.FromKitchenItem(localStation.StationItem);
        if (currentItem.exists != lastStationItem.exists) return true;
        if (currentItem.ingredientKind != lastStationItem.ingredientKind) return true;
        if (currentItem.state != lastStationItem.state) return true;

        return false;
    }

    public StationStateSnapshot CaptureSnapshot()
    {
        if (localStation == null) return default;

        lastIsProcessing = localStation.IsProcessing;
        lastRemainingProcessTime = localStation.IsProcessing
            ? Mathf.Max(0f, localStation.ProcessEndTime - Time.time)
            : 0f;
        lastPreparedMeatServings = localStation.PreparedMeatServings;
        lastHasLavash = localStation.HasLavash;
        lastAssemblyCount = localStation.AssemblyCount;
        lastStationItem = NetworkItemState.FromKitchenItem(localStation.StationItem);

        StationStateSnapshot snapshot = new StationStateSnapshot
        {
            stationIndex = StationIndex,
            isProcessing = lastIsProcessing,
            remainingProcessTime = lastRemainingProcessTime,
            preparedMeatServings = lastPreparedMeatServings,
            hasLavash = lastHasLavash,
            assemblyCount = lastAssemblyCount,
            stationItem = lastStationItem
        };

        localStation.WriteAssemblyToSnapshot(ref snapshot);

        return snapshot;
    }

    public void ApplySnapshot(StationStateSnapshot snapshot)
    {
        if (localStation == null)
        {
            localStation = GetComponent<KitchenStation>();
        }

        if (localStation != null)
        {
            localStation.SyncNetworkState(snapshot);
        }
    }

    public void ServerInteract(PlayerInteraction interaction)
    {
        if (localStation != null)
        {
            localStation.Interact(interaction);
        }
    }
}

public struct StationStateSnapshot : Unity.Netcode.INetworkSerializable
{
    public int stationIndex;
    public bool isProcessing;
    public float remainingProcessTime;
    public int preparedMeatServings;
    public bool hasLavash;
    public int assemblyCount;
    public NetworkItemState stationItem;

    public IngredientKind assembly0Kind;
    public IngredientProcessState assembly0State;
    public IngredientKind assembly1Kind;
    public IngredientProcessState assembly1State;
    public IngredientKind assembly2Kind;
    public IngredientProcessState assembly2State;
    public IngredientKind assembly3Kind;
    public IngredientProcessState assembly3State;
    public IngredientKind assembly4Kind;
    public IngredientProcessState assembly4State;
    public IngredientKind assembly5Kind;
    public IngredientProcessState assembly5State;
    public IngredientKind assembly6Kind;
    public IngredientProcessState assembly6State;
    public IngredientKind assembly7Kind;
    public IngredientProcessState assembly7State;

    public void SetAssemblySlot(int index, IngredientKind kind, IngredientProcessState state)
    {
        switch (index)
        {
            case 0: assembly0Kind = kind; assembly0State = state; break;
            case 1: assembly1Kind = kind; assembly1State = state; break;
            case 2: assembly2Kind = kind; assembly2State = state; break;
            case 3: assembly3Kind = kind; assembly3State = state; break;
            case 4: assembly4Kind = kind; assembly4State = state; break;
            case 5: assembly5Kind = kind; assembly5State = state; break;
            case 6: assembly6Kind = kind; assembly6State = state; break;
            case 7: assembly7Kind = kind; assembly7State = state; break;
        }
    }

    public void GetAssemblySlot(int index, out IngredientKind kind, out IngredientProcessState state)
    {
        switch (index)
        {
            case 0: kind = assembly0Kind; state = assembly0State; break;
            case 1: kind = assembly1Kind; state = assembly1State; break;
            case 2: kind = assembly2Kind; state = assembly2State; break;
            case 3: kind = assembly3Kind; state = assembly3State; break;
            case 4: kind = assembly4Kind; state = assembly4State; break;
            case 5: kind = assembly5Kind; state = assembly5State; break;
            case 6: kind = assembly6Kind; state = assembly6State; break;
            case 7: kind = assembly7Kind; state = assembly7State; break;
            default: kind = IngredientKind.Meat; state = IngredientProcessState.Raw; break;
        }
    }

    public void NetworkSerialize<T>(Unity.Netcode.BufferSerializer<T> serializer) where T : Unity.Netcode.IReaderWriter
    {
        serializer.SerializeValue(ref stationIndex);
        serializer.SerializeValue(ref isProcessing);
        serializer.SerializeValue(ref remainingProcessTime);
        serializer.SerializeValue(ref preparedMeatServings);
        serializer.SerializeValue(ref hasLavash);
        serializer.SerializeValue(ref assemblyCount);
        serializer.SerializeValue(ref stationItem);

        int clampedCount = Mathf.Min(assemblyCount, 8);
        if (clampedCount > 0) { serializer.SerializeValue(ref assembly0Kind); serializer.SerializeValue(ref assembly0State); }
        if (clampedCount > 1) { serializer.SerializeValue(ref assembly1Kind); serializer.SerializeValue(ref assembly1State); }
        if (clampedCount > 2) { serializer.SerializeValue(ref assembly2Kind); serializer.SerializeValue(ref assembly2State); }
        if (clampedCount > 3) { serializer.SerializeValue(ref assembly3Kind); serializer.SerializeValue(ref assembly3State); }
        if (clampedCount > 4) { serializer.SerializeValue(ref assembly4Kind); serializer.SerializeValue(ref assembly4State); }
        if (clampedCount > 5) { serializer.SerializeValue(ref assembly5Kind); serializer.SerializeValue(ref assembly5State); }
        if (clampedCount > 6) { serializer.SerializeValue(ref assembly6Kind); serializer.SerializeValue(ref assembly6State); }
        if (clampedCount > 7) { serializer.SerializeValue(ref assembly7Kind); serializer.SerializeValue(ref assembly7State); }
    }
}
