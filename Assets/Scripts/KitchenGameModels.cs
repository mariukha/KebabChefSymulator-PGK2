using System;
using System.Collections.Generic;
using System.Text;

public enum IngredientKind
{
    Meat,
    Tomato,
    Onion,
    Lettuce,
    GarlicSauce,
    Lavash,
    Kebab
}

public enum IngredientProcessState
{
    Raw,
    Chopped,
    Cooked,
    Assembled
}

public enum KitchenStationType
{
    IngredientSource,
    CuttingBoard,
    Grill,
    Assembly,
    Delivery
}

[Serializable]
public class IngredientRequirement
{
    public IngredientKind ingredientKind = IngredientKind.Meat;
    public IngredientProcessState requiredState = IngredientProcessState.Raw;
    public int quantity = 1;

    public IngredientRequirement()
    {
    }

    public IngredientRequirement(
        IngredientKind ingredientKind,
        IngredientProcessState requiredState,
        int quantity = 1)
    {
        this.ingredientKind = ingredientKind;
        this.requiredState = requiredState;
        this.quantity = quantity;
    }

    public string ToDisplayString()
    {
        string formattedName = KitchenNaming.FormatIngredient(ingredientKind, requiredState);

        if (quantity <= 1)
        {
            return formattedName;
        }

        return quantity + "x " + formattedName;
    }
}

[Serializable]
public class PreparedIngredientData
{
    public IngredientKind ingredientKind = IngredientKind.Meat;
    public IngredientProcessState state = IngredientProcessState.Raw;

    public PreparedIngredientData()
    {
    }

    public PreparedIngredientData(IngredientKind ingredientKind, IngredientProcessState state)
    {
        this.ingredientKind = ingredientKind;
        this.state = state;
    }

    public string ToDisplayString()
    {
        return KitchenNaming.FormatIngredient(ingredientKind, state);
    }
}

[Serializable]
public class KitchenItem
{
    public string itemName;
    public IngredientKind ingredientKind = IngredientKind.Meat;
    public IngredientProcessState state = IngredientProcessState.Raw;
    public bool isDish;
    public float estimatedValue;
    public List<PreparedIngredientData> contents = new List<PreparedIngredientData>();

    public static KitchenItem FromIngredient(IngredientData data)
    {
        return new KitchenItem
        {
            itemName = data != null ? data.DisplayName : "Skladnik",
            ingredientKind = data != null ? data.typSkladnika : IngredientKind.Tomato,
            state = data != null ? data.stanPoczatkowy : IngredientProcessState.Raw,
            estimatedValue = data != null ? data.wartoscSprzedazy : 0f
        };
    }

    public KitchenItem Clone()
    {
        KitchenItem copy = new KitchenItem
        {
            itemName = itemName,
            ingredientKind = ingredientKind,
            state = state,
            isDish = isDish,
            estimatedValue = estimatedValue
        };

        foreach (PreparedIngredientData ingredient in contents)
        {
            copy.contents.Add(new PreparedIngredientData(ingredient.ingredientKind, ingredient.state));
        }

        return copy;
    }

    public string BuildSummary()
    {
        if (!isDish)
        {
            return KitchenNaming.FormatIngredient(ingredientKind, state);
        }

        StringBuilder builder = new StringBuilder();
        builder.Append(itemName);
        builder.Append(": ");

        for (int i = 0; i < contents.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            builder.Append(contents[i].ToDisplayString());
        }

        return builder.ToString();
    }
}

public static class KitchenOrderValidator
{
    public static bool MatchesOrder(Order order, KitchenItem item, out string failureReason)
    {
        if (order == null)
        {
            failureReason = "Brak aktywnego zamowienia.";
            return false;
        }

        if (item == null || !item.isDish)
        {
            failureReason = "Klient oczekuje gotowego kebaba.";
            return false;
        }

        Dictionary<string, int> deliveredMap = BuildIngredientCountMap(item.contents);
        Dictionary<string, int> requiredMap = BuildRequirementCountMap(order.wymaganeSkladniki);

        foreach (KeyValuePair<string, int> requirement in requiredMap)
        {
            int deliveredAmount;
            deliveredMap.TryGetValue(requirement.Key, out deliveredAmount);
            if (deliveredAmount < requirement.Value)
            {
                failureReason = "Brakuje skladnika lub ma zly stan przygotowania.";
                return false;
            }
        }

        foreach (KeyValuePair<string, int> delivered in deliveredMap)
        {
            int requiredAmount;
            requiredMap.TryGetValue(delivered.Key, out requiredAmount);
            if (delivered.Value > requiredAmount)
            {
                failureReason = "Kebab zawiera nadmiarowe skladniki.";
                return false;
            }
        }

        failureReason = string.Empty;
        return true;
    }

    private static Dictionary<string, int> BuildIngredientCountMap(List<PreparedIngredientData> ingredients)
    {
        Dictionary<string, int> map = new Dictionary<string, int>();
        foreach (PreparedIngredientData ingredient in ingredients)
        {
            string key = GetKey(ingredient.ingredientKind, ingredient.state);
            if (!map.ContainsKey(key))
            {
                map[key] = 0;
            }

            map[key]++;
        }

        return map;
    }

    private static Dictionary<string, int> BuildRequirementCountMap(List<IngredientRequirement> requirements)
    {
        Dictionary<string, int> map = new Dictionary<string, int>();
        foreach (IngredientRequirement requirement in requirements)
        {
            string key = GetKey(requirement.ingredientKind, requirement.requiredState);
            if (!map.ContainsKey(key))
            {
                map[key] = 0;
            }

            map[key] += requirement.quantity;
        }

        return map;
    }

    private static string GetKey(IngredientKind kind, IngredientProcessState state)
    {
        return kind + "|" + state;
    }
}

public static class KitchenNaming
{
    public static string GetIngredientLabel(IngredientKind kind)
    {
        switch (kind)
        {
            case IngredientKind.Meat:
                return "Mieso";
            case IngredientKind.Tomato:
                return "Pomidor";
            case IngredientKind.Onion:
                return "Cebula";
            case IngredientKind.Lettuce:
                return "Salata";
            case IngredientKind.GarlicSauce:
                return "Sos czosnkowy";
            case IngredientKind.Lavash:
                return "Lawasz";
            case IngredientKind.Kebab:
                return "Kebab";
            default:
                return kind.ToString();
        }
    }

    public static string FormatIngredient(IngredientKind kind, IngredientProcessState state)
    {
        string ingredientName = GetIngredientLabel(kind);

        if (state == IngredientProcessState.Raw && kind != IngredientKind.Meat)
        {
            return ingredientName;
        }

        return ingredientName + " (" + GetProcessLabel(state) + ")";
    }

    public static string GetProcessLabel(IngredientProcessState state)
    {
        switch (state)
        {
            case IngredientProcessState.Raw:
                return "surowy";
            case IngredientProcessState.Chopped:
                return "pokrojony";
            case IngredientProcessState.Cooked:
                return "upieczony";
            case IngredientProcessState.Assembled:
                return "zlozony";
            default:
                return state.ToString();
        }
    }
}
