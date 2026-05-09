using Unity.Collections;
using Unity.Netcode;

public struct NetworkItemState : INetworkSerializable
{
    public FixedString64Bytes itemName;
    public IngredientKind ingredientKind;
    public IngredientProcessState state;
    public bool isDish;
    public bool exists;

    public int contentCount;
    public IngredientKind content0Kind;
    public IngredientProcessState content0State;
    public IngredientKind content1Kind;
    public IngredientProcessState content1State;
    public IngredientKind content2Kind;
    public IngredientProcessState content2State;
    public IngredientKind content3Kind;
    public IngredientProcessState content3State;
    public IngredientKind content4Kind;
    public IngredientProcessState content4State;
    public IngredientKind content5Kind;
    public IngredientProcessState content5State;
    public IngredientKind content6Kind;
    public IngredientProcessState content6State;
    public IngredientKind content7Kind;
    public IngredientProcessState content7State;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref itemName);
        serializer.SerializeValue(ref ingredientKind);
        serializer.SerializeValue(ref state);
        serializer.SerializeValue(ref isDish);
        serializer.SerializeValue(ref exists);
        serializer.SerializeValue(ref contentCount);

        if (contentCount > 0) { serializer.SerializeValue(ref content0Kind); serializer.SerializeValue(ref content0State); }
        if (contentCount > 1) { serializer.SerializeValue(ref content1Kind); serializer.SerializeValue(ref content1State); }
        if (contentCount > 2) { serializer.SerializeValue(ref content2Kind); serializer.SerializeValue(ref content2State); }
        if (contentCount > 3) { serializer.SerializeValue(ref content3Kind); serializer.SerializeValue(ref content3State); }
        if (contentCount > 4) { serializer.SerializeValue(ref content4Kind); serializer.SerializeValue(ref content4State); }
        if (contentCount > 5) { serializer.SerializeValue(ref content5Kind); serializer.SerializeValue(ref content5State); }
        if (contentCount > 6) { serializer.SerializeValue(ref content6Kind); serializer.SerializeValue(ref content6State); }
        if (contentCount > 7) { serializer.SerializeValue(ref content7Kind); serializer.SerializeValue(ref content7State); }
    }

    public static NetworkItemState Empty()
    {
        return new NetworkItemState { exists = false };
    }

    public static NetworkItemState FromKitchenItem(KitchenItem item)
    {
        if (item == null)
        {
            return Empty();
        }

        NetworkItemState netState = new NetworkItemState
        {
            itemName = new FixedString64Bytes(item.itemName ?? "Skladnik"),
            ingredientKind = item.ingredientKind,
            state = item.state,
            isDish = item.isDish,
            exists = true,
            contentCount = 0
        };

        if (item.contents != null && item.isDish)
        {
            netState.contentCount = UnityEngine.Mathf.Min(item.contents.Count, 8);
            for (int i = 0; i < netState.contentCount; i++)
            {
                SetContent(ref netState, i, item.contents[i].ingredientKind, item.contents[i].state);
            }
        }

        return netState;
    }

    public KitchenItem ToKitchenItem()
    {
        if (!exists)
        {
            return null;
        }

        KitchenItem item = new KitchenItem
        {
            itemName = itemName.ToString(),
            ingredientKind = ingredientKind,
            state = state,
            isDish = isDish
        };

        for (int i = 0; i < contentCount; i++)
        {
            GetContent(this, i, out IngredientKind cKind, out IngredientProcessState cState);
            item.contents.Add(new PreparedIngredientData(cKind, cState));
        }

        return item;
    }

    private static void SetContent(ref NetworkItemState s, int index, IngredientKind kind, IngredientProcessState pState)
    {
        switch (index)
        {
            case 0: s.content0Kind = kind; s.content0State = pState; break;
            case 1: s.content1Kind = kind; s.content1State = pState; break;
            case 2: s.content2Kind = kind; s.content2State = pState; break;
            case 3: s.content3Kind = kind; s.content3State = pState; break;
            case 4: s.content4Kind = kind; s.content4State = pState; break;
            case 5: s.content5Kind = kind; s.content5State = pState; break;
            case 6: s.content6Kind = kind; s.content6State = pState; break;
            case 7: s.content7Kind = kind; s.content7State = pState; break;
        }
    }

    private static void GetContent(NetworkItemState s, int index, out IngredientKind kind, out IngredientProcessState pState)
    {
        switch (index)
        {
            case 0: kind = s.content0Kind; pState = s.content0State; break;
            case 1: kind = s.content1Kind; pState = s.content1State; break;
            case 2: kind = s.content2Kind; pState = s.content2State; break;
            case 3: kind = s.content3Kind; pState = s.content3State; break;
            case 4: kind = s.content4Kind; pState = s.content4State; break;
            case 5: kind = s.content5Kind; pState = s.content5State; break;
            case 6: kind = s.content6Kind; pState = s.content6State; break;
            case 7: kind = s.content7Kind; pState = s.content7State; break;
            default: kind = IngredientKind.Meat; pState = IngredientProcessState.Raw; break;
        }
    }
}
