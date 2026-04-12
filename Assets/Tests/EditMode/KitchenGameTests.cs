using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class KitchenGameTests
{
    [Test]
    public void ValidatorAcceptsDishWithExactIngredients()
    {
        object order = Create("Order");
        SetField(order, "orderId", "test");
        SetField(order, "nazwaKlienta", "Test");
        SetField(order, "nazwaZamowienia", "Test kebab");

        IList requirements = (IList)GetField(order, "wymaganeSkladniki");
        requirements.Add(CreateIngredientRequirement("Lavash", "Raw"));
        requirements.Add(CreateIngredientRequirement("Meat", "Cooked"));
        requirements.Add(CreateIngredientRequirement("Tomato", "Chopped"));

        object dish = CreateDish(
            CreatePreparedIngredient("Lavash", "Raw"),
            CreatePreparedIngredient("Meat", "Cooked"),
            CreatePreparedIngredient("Tomato", "Chopped"));

        object[] arguments = { order, dish, null };
        bool result = (bool)GetTypeByName("KitchenOrderValidator")
            .GetMethod("MatchesOrder", BindingFlags.Public | BindingFlags.Static)
            .Invoke(null, arguments);

        Assert.IsTrue(result, arguments[2] as string);
    }

    [Test]
    public void ValidatorRejectsWrongPreparationState()
    {
        object order = Create("Order");
        IList requirements = (IList)GetField(order, "wymaganeSkladniki");
        requirements.Add(CreateIngredientRequirement("Tomato", "Chopped"));

        object dish = CreateDish(CreatePreparedIngredient("Tomato", "Raw"));

        object[] arguments = { order, dish, null };
        bool result = (bool)GetTypeByName("KitchenOrderValidator")
            .GetMethod("MatchesOrder", BindingFlags.Public | BindingFlags.Static)
            .Invoke(null, arguments);

        Assert.IsFalse(result);
        StringAssert.Contains("Brakuje", arguments[2] as string);
    }

    [Test]
    public void SaveDataRoundTripPreservesProgress()
    {
        object saveData = Create("GameSaveData");
        object economy = GetField(saveData, "economy");
        object orderProgress = GetField(saveData, "orderProgress");

        SetField(economy, "currentBalance", 128f);
        SetField(economy, "totalEarned", 250f);
        SetField(orderProgress, "completedOrders", 4);
        SetField(orderProgress, "failedOrders", 1);
        SetField(orderProgress, "remainingOrderTime", 42f);

        object activeOrder = Create("OrderSaveData");
        SetField(activeOrder, "orderId", "classic");
        SetField(activeOrder, "clientName", "Adam");
        SetField(activeOrder, "orderName", "Klasyczny kebab");
        SetField(activeOrder, "timeLimit", 90f);
        SetField(activeOrder, "reward", 31f);
        ((IList)GetField(activeOrder, "requirements")).Add(CreateIngredientRequirement("Meat", "Cooked"));
        SetField(orderProgress, "activeOrder", activeOrder);

        string json = JsonUtility.ToJson(saveData);
        object restored = JsonUtility.FromJson(json, GetTypeByName("GameSaveData"));
        object restoredEconomy = GetField(restored, "economy");
        object restoredProgress = GetField(restored, "orderProgress");
        object restoredOrder = GetField(restoredProgress, "activeOrder");

        Assert.AreEqual(128f, (float)GetField(restoredEconomy, "currentBalance"));
        Assert.AreEqual(250f, (float)GetField(restoredEconomy, "totalEarned"));
        Assert.AreEqual(4, (int)GetField(restoredProgress, "completedOrders"));
        Assert.AreEqual("classic", GetField(restoredOrder, "orderId") as string);
        Assert.AreEqual(1, ((IList)GetField(restoredOrder, "requirements")).Count);
    }

    private static object CreateDish(params object[] ingredients)
    {
        object dish = Create("KitchenItem");
        SetField(dish, "itemName", "Kebab");
        SetField(dish, "ingredientKind", ParseEnum("IngredientKind", "Kebab"));
        SetField(dish, "state", ParseEnum("IngredientProcessState", "Assembled"));
        SetField(dish, "isDish", true);

        IList contents = (IList)GetField(dish, "contents");
        foreach (object ingredient in ingredients)
        {
            contents.Add(ingredient);
        }

        return dish;
    }

    private static object CreateIngredientRequirement(string ingredientKind, string processState, int quantity = 1)
    {
        object requirement = Create("IngredientRequirement");
        SetField(requirement, "ingredientKind", ParseEnum("IngredientKind", ingredientKind));
        SetField(requirement, "requiredState", ParseEnum("IngredientProcessState", processState));
        SetField(requirement, "quantity", quantity);
        return requirement;
    }

    private static object CreatePreparedIngredient(string ingredientKind, string processState)
    {
        object ingredient = Create("PreparedIngredientData");
        SetField(ingredient, "ingredientKind", ParseEnum("IngredientKind", ingredientKind));
        SetField(ingredient, "state", ParseEnum("IngredientProcessState", processState));
        return ingredient;
    }

    private static object ParseEnum(string typeName, string value)
    {
        return Enum.Parse(GetTypeByName(typeName), value);
    }

    private static object Create(string typeName)
    {
        return Activator.CreateInstance(GetTypeByName(typeName));
    }

    private static object GetField(object instance, string fieldName)
    {
        return instance.GetType().GetField(fieldName).GetValue(instance);
    }

    private static void SetField(object instance, string fieldName, object value)
    {
        instance.GetType().GetField(fieldName).SetValue(instance, value);
    }

    private static Type GetTypeByName(string typeName)
    {
        Assembly assembly = null;
        foreach (Assembly loadedAssembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (loadedAssembly.GetName().Name == "Assembly-CSharp")
            {
                assembly = loadedAssembly;
                break;
            }
        }

        Assert.IsNotNull(assembly, "Assembly-CSharp was not loaded.");
        Type resolvedType = assembly.GetType(typeName);
        Assert.IsNotNull(resolvedType, "Type " + typeName + " was not found.");
        return resolvedType;
    }
}
