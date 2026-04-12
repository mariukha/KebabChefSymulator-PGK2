using System;
using System.Collections.Generic;
using System.Text;

[Serializable]
public class Order
{
    public string orderId = "classic-kebab";
    public string nazwaKlienta = "Klient";
    public string nazwaZamowienia = "Klasyczny kebab";
    public List<IngredientRequirement> wymaganeSkladniki = new List<IngredientRequirement>();
    public float czasNaRealizacje = 90f;
    public float nagrodaPieniezna = 30f;

    public Order Clone()
    {
        Order copy = new Order
        {
            orderId = orderId,
            nazwaKlienta = nazwaKlienta,
            nazwaZamowienia = nazwaZamowienia,
            czasNaRealizacje = czasNaRealizacje,
            nagrodaPieniezna = nagrodaPieniezna
        };

        foreach (IngredientRequirement requirement in wymaganeSkladniki)
        {
            copy.wymaganeSkladniki.Add(new IngredientRequirement(
                requirement.ingredientKind,
                requirement.requiredState,
                requirement.quantity));
        }

        return copy;
    }

    public string BuildDescription()
    {
        StringBuilder builder = new StringBuilder();
        builder.Append(nazwaZamowienia);
        builder.Append(" dla ");
        builder.Append(nazwaKlienta);
        builder.Append(": ");

        for (int i = 0; i < wymaganeSkladniki.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            builder.Append(wymaganeSkladniki[i].ToDisplayString());
        }

        return builder.ToString();
    }
}
