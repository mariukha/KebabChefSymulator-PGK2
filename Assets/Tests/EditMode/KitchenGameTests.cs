/// \file KitchenGameTests.cs
/// \brief Plik zawierający testy jednostkowe EditMode dla logiki gry kuchennej (symulator kebaba).
/// \details Testy obejmują walidację zamówień, serializację/deserializację danych zapisu,
/// system ulepszeń sklepu, nazewnictwo składników w języku polskim, klonowanie obiektów
/// oraz poprawność definicji typów wyliczeniowych. Testy korzystają z refleksji,
/// aby uzyskać dostęp do typów z assembly głównego projektu (Assembly-CSharp).

using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Klasa testów jednostkowych EditMode dla systemu kuchni w grze Kebab Chef Symulator.
/// </summary>
/// <remarks>
/// Testy wykorzystują refleksję do dynamicznego tworzenia i manipulowania obiektami
/// z głównego assembly gry (Assembly-CSharp). Dzięki temu testy mogą weryfikować
/// logikę biznesową bez bezpośredniej zależności kompilacyjnej od testowanych typów.
/// Klasa zawiera testy walidatora zamówień, serializacji danych zapisu, systemu
/// ulepszeń, nazewnictwa po polsku oraz integralności typów wyliczeniowych.
/// </remarks>
public class KitchenGameTests
{
    /// <summary>
    /// Weryfikuje, że walidator akceptuje danie zawierające dokładnie wymagane składniki.
    /// </summary>
    /// <remarks>
    /// Tworzy zamówienie z trzema wymaganiami (Lavash surowy, Mięso upieczone, Pomidor pokrojony)
    /// i danie z dokładnie tymi samymi składnikami. Oczekuje, że metoda
    /// <c>KitchenOrderValidator.MatchesOrder</c> zwróci <c>true</c>.
    /// </remarks>
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

    /// <summary>
    /// Weryfikuje, że walidator odrzuca danie ze składnikiem w nieprawidłowym stanie przygotowania.
    /// </summary>
    /// <remarks>
    /// Zamówienie wymaga pokrojonego pomidora, ale danie zawiera surowy pomidor.
    /// Oczekuje się, że walidator zwróci <c>false</c>, a komunikat błędu będzie zawierał słowo "Brakuje".
    /// </remarks>
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

    /// <summary>
    /// Weryfikuje, że dane zapisu gry zachowują postęp po serializacji i deserializacji JSON.
    /// </summary>
    /// <remarks>
    /// Tworzy obiekt <c>GameSaveData</c> z ustawionymi wartościami ekonomii (saldo, zarobki)
    /// oraz postępem zamówień (ukończone, nieudane, pozostały czas, aktywne zamówienie).
    /// Serializuje do JSON za pomocą <c>JsonUtility</c>, a następnie deserializuje i porównuje
    /// przywrócone wartości z oryginalnymi.
    /// </remarks>
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

    /// <summary>
    /// Weryfikuje, że dane zapisu sklepu zachowują informacje o ulepszeniach po serializacji i deserializacji.
    /// </summary>
    /// <remarks>
    /// Tworzy obiekt <c>ShopSaveData</c> z dwiema pozycjami ulepszeń (GrillSpeed i RewardBonus)
    /// o różnych poziomach. Sprawdza, czy po cyklu JSON round-trip wszystkie pola są poprawnie zachowane,
    /// w tym liczba zakupionych ulepszeń, typy i poziomy poszczególnych wpisów.
    /// </remarks>
    [Test]
    public void ShopSaveDataRoundTripPreservesUpgrades()
    {
        object shopData = Create("ShopSaveData");
        SetField(shopData, "totalUpgradesPurchased", 3);

        IList levels = (IList)GetField(shopData, "upgradeLevels");
        object entry = Create("UpgradeLevelEntry");
        SetField(entry, "upgradeType", "GrillSpeed");
        SetField(entry, "level", 2);
        levels.Add(entry);

        object entry2 = Create("UpgradeLevelEntry");
        SetField(entry2, "upgradeType", "RewardBonus");
        SetField(entry2, "level", 1);
        levels.Add(entry2);

        string json = JsonUtility.ToJson(shopData);
        object restored = JsonUtility.FromJson(json, GetTypeByName("ShopSaveData"));

        Assert.AreEqual(3, (int)GetField(restored, "totalUpgradesPurchased"));
        IList restoredLevels = (IList)GetField(restored, "upgradeLevels");
        Assert.AreEqual(2, restoredLevels.Count);
        Assert.AreEqual("GrillSpeed", GetField(restoredLevels[0], "upgradeType") as string);
        Assert.AreEqual(2, (int)GetField(restoredLevels[0], "level"));
        Assert.AreEqual("RewardBonus", GetField(restoredLevels[1], "upgradeType") as string);
        Assert.AreEqual(1, (int)GetField(restoredLevels[1], "level"));
    }

    /// <summary>
    /// Weryfikuje, że obiekt <c>GameSaveData</c> zawiera pole sklepu z poprawną inicjalizacją.
    /// </summary>
    /// <remarks>
    /// Sprawdza, czy nowo utworzony obiekt <c>GameSaveData</c> posiada niezerowe pole <c>shop</c>
    /// z pustą listą poziomów ulepszeń (<c>upgradeLevels</c>).
    /// </remarks>
    [Test]
    public void GameSaveDataIncludesShopField()
    {
        object saveData = Create("GameSaveData");
        object shop = GetField(saveData, "shop");

        Assert.IsNotNull(shop);

        IList levels = (IList)GetField(shop, "upgradeLevels");
        Assert.IsNotNull(levels);
        Assert.AreEqual(0, levels.Count);
    }

    /// <summary>
    /// Weryfikuje, że koszt ulepszenia skaluje się poprawnie według wzoru wykładniczego.
    /// </summary>
    /// <remarks>
    /// Tworzy definicję ulepszenia z kosztem bazowym 50 i współczynnikiem skalowania 2.
    /// Sprawdza, czy koszt dla poziomu 0 wynosi 50, dla poziomu 1 wynosi 100,
    /// a dla poziomu 2 wynosi 200 (każdy kolejny poziom podwaja koszt).
    /// </remarks>
    [Test]
    public void UpgradeDefinitionCostScalesCorrectly()
    {
        object definition = Create("UpgradeDefinition");
        SetField(definition, "baseCost", 50f);
        SetField(definition, "costScaling", 2f);

        MethodInfo method = GetTypeByName("UpgradeDefinition")
            .GetMethod("GetCostForLevel", BindingFlags.Public | BindingFlags.Instance);

        float costLevel0 = (float)method.Invoke(definition, new object[] { 0 });
        float costLevel1 = (float)method.Invoke(definition, new object[] { 1 });
        float costLevel2 = (float)method.Invoke(definition, new object[] { 2 });

        Assert.AreEqual(50f, costLevel0);
        Assert.AreEqual(100f, costLevel1);
        Assert.AreEqual(200f, costLevel2);
    }

    /// <summary>
    /// Weryfikuje, że typ wyliczeniowy <c>UpgradeType</c> posiada dokładnie pięć wartości.
    /// </summary>
    /// <remarks>
    /// Sprawdza, czy enum <c>UpgradeType</c> zawiera wartości: GrillSpeed, CuttingSpeed,
    /// RewardBonus, OrderTime oraz MeatBatchSize.
    /// </remarks>
    [Test]
    public void UpgradeTypeEnumHasFiveValues()
    {
        Type upgradeType = GetTypeByName("UpgradeType");
        Assert.IsTrue(upgradeType.IsEnum);

        string[] names = Enum.GetNames(upgradeType);
        Assert.AreEqual(5, names.Length);
        Assert.Contains("GrillSpeed", names);
        Assert.Contains("CuttingSpeed", names);
        Assert.Contains("RewardBonus", names);
        Assert.Contains("OrderTime", names);
        Assert.Contains("MeatBatchSize", names);
    }

    // =========================================================================
    //  NOWE TESTY — Kamień milowy 2
    // =========================================================================

    /// <summary>
    /// Weryfikuje, że koszt ulepszenia w sklepie skaluje się wykładniczo z niestandardowym współczynnikiem.
    /// </summary>
    /// <remarks>
    /// Tworzy definicję ulepszenia z kosztem bazowym 40 i współczynnikiem skalowania 1,8.
    /// Sprawdza, że koszty rosną w sposób wykładniczy — stosunek kosztów kolejnych poziomów
    /// powinien być stały i równy współczynnikowi skalowania.
    /// </remarks>
    [Test]
    public void ShopUpgradeCostScalesExponentially()
    {
        object def = Create("UpgradeDefinition");
        SetField(def, "baseCost", 40f);
        SetField(def, "costScaling", 1.8f);

        MethodInfo method = GetTypeByName("UpgradeDefinition")
            .GetMethod("GetCostForLevel", BindingFlags.Public | BindingFlags.Instance);

        float cost0 = (float)method.Invoke(def, new object[] { 0 });
        float cost1 = (float)method.Invoke(def, new object[] { 1 });
        float cost2 = (float)method.Invoke(def, new object[] { 2 });

        Assert.AreEqual(40f, cost0);
        Assert.IsTrue(cost1 > cost0, "Koszt poziomu 1 powinien byc wyzszy niz poziomu 0.");
        Assert.IsTrue(cost2 > cost1, "Koszt poziomu 2 powinien byc wyzszy niz poziomu 1.");
        // Verify exponential: cost2/cost1 ≈ cost1/cost0 ≈ 1.8
        float ratio1 = cost1 / cost0;
        float ratio2 = cost2 / cost1;
        Assert.AreEqual(ratio1, ratio2, 0.01f, "Skalowanie powinno byc wykladnicze.");
    }

    /// <summary>
    /// Weryfikuje, że opis efektu ulepszenia w sklepie zwraca poprawny tekst.
    /// </summary>
    /// <remarks>
    /// Dla ulepszenia typu RewardBonus z tablicą wartości efektów sprawdza, czy opis
    /// na poziomie 0 zawiera znak procentu, a na maksymalnym poziomie zwraca tekst "MAX".
    /// </remarks>
    [Test]
    public void ShopEffectDescriptionReturnsCorrectText()
    {
        object def = Create("UpgradeDefinition");
        SetField(def, "type", ParseEnum("UpgradeType", "RewardBonus"));
        SetField(def, "maxLevel", 3);
        SetField(def, "effectValues", new float[] { 0f, 0.10f, 0.25f, 0.50f });

        MethodInfo method = GetTypeByName("UpgradeDefinition")
            .GetMethod("GetEffectDescription", BindingFlags.Public | BindingFlags.Instance);

        string desc0 = (string)method.Invoke(def, new object[] { 0 });
        string descMax = (string)method.Invoke(def, new object[] { 3 });

        StringAssert.Contains("%", desc0, "Opis powinien zawierac procent.");
        Assert.AreEqual("MAX", descMax, "Na max poziomie opis powinien byc MAX.");
    }

    /// <summary>
    /// Weryfikuje, że osiągnięcie maksymalnego poziomu ulepszenia blokuje dalsze ulepszanie.
    /// </summary>
    /// <remarks>
    /// Tworzy definicję ulepszenia typu GrillSpeed z maksymalnym poziomem 2.
    /// Sprawdza, czy metoda <c>GetEffectDescription</c> dla poziomu maksymalnego
    /// zwraca tekst "MAX", sygnalizując brak dalszych ulepszeń.
    /// </remarks>
    [Test]
    public void ShopMaxLevelBlocksFurtherUpgradeCheck()
    {
        // UpgradeDefinition with maxLevel = 2
        object def = Create("UpgradeDefinition");
        SetField(def, "type", ParseEnum("UpgradeType", "GrillSpeed"));
        SetField(def, "maxLevel", 2);
        SetField(def, "effectValues", new float[] { 1f, 0.8f, 0.6f });

        MethodInfo getDesc = GetTypeByName("UpgradeDefinition")
            .GetMethod("GetEffectDescription", BindingFlags.Public | BindingFlags.Instance);

        string atMax = (string)getDesc.Invoke(def, new object[] { 2 });
        Assert.AreEqual("MAX", atMax);
    }

    /// <summary>
    /// Weryfikuje poprawność cyklu serializacji i deserializacji danych ekonomicznych.
    /// </summary>
    /// <remarks>
    /// Tworzy obiekt <c>EconomySaveData</c> z ustawionym saldem (999.5), łącznymi zarobkami (1500)
    /// oraz łącznymi wydatkami (500.5). Po serializacji JSON i deserializacji sprawdza,
    /// czy wszystkie wartości zmiennoprzecinkowe zostały zachowane z dokładnością do 0.01.
    /// </remarks>
    [Test]
    public void EconomySaveDataRoundTrip()
    {
        object econ = Create("EconomySaveData");
        SetField(econ, "currentBalance", 999.5f);
        SetField(econ, "totalEarned", 1500f);
        SetField(econ, "totalSpent", 500.5f);

        string json = JsonUtility.ToJson(econ);
        object restored = JsonUtility.FromJson(json, GetTypeByName("EconomySaveData"));

        Assert.AreEqual(999.5f, (float)GetField(restored, "currentBalance"), 0.01f);
        Assert.AreEqual(1500f, (float)GetField(restored, "totalEarned"), 0.01f);
        Assert.AreEqual(500.5f, (float)GetField(restored, "totalSpent"), 0.01f);
    }

    /// <summary>
    /// Weryfikuje, że pusty stan elementu sieciowego poprawnie przechodzi cykl serializacji.
    /// </summary>
    /// <remarks>
    /// Tworzy pusty obiekt <c>NetworkItemState</c> za pomocą metody fabrycznej <c>Empty()</c>.
    /// Sprawdza, że pole <c>exists</c> jest ustawione na <c>false</c> oraz że metoda
    /// <c>ToKitchenItem()</c> zwraca <c>null</c> dla pustego stanu.
    /// </remarks>
    [Test]
    public void NetworkItemStateEmptyRoundTrip()
    {
        // Test that empty state serializes and deserializes correctly
        Type nisType = GetTypeByName("NetworkItemState");
        MethodInfo emptyMethod = nisType.GetMethod("Empty", BindingFlags.Public | BindingFlags.Static);
        object emptyState = emptyMethod.Invoke(null, null);

        bool exists = (bool)nisType.GetField("exists").GetValue(emptyState);
        Assert.IsFalse(exists, "Pusty NetworkItemState powinien miec exists = false.");

        MethodInfo toItem = nisType.GetMethod("ToKitchenItem", BindingFlags.Public | BindingFlags.Instance);
        object item = toItem.Invoke(emptyState, null);
        Assert.IsNull(item, "Pusty stan powinien zwrocic null KitchenItem.");
    }

    /// <summary>
    /// Weryfikuje poprawność konwersji dania do stanu sieciowego i z powrotem.
    /// </summary>
    /// <remarks>
    /// Tworzy danie z czterema składnikami (Lavash, Meat, Tomato, GarlicSauce) i konwertuje je
    /// do obiektu <c>NetworkItemState</c> za pomocą <c>FromKitchenItem</c>. Sprawdza, że pole
    /// <c>exists</c> jest <c>true</c>, <c>contentCount</c> wynosi 4, a konwersja z powrotem
    /// przez <c>ToKitchenItem</c> daje danie z 4 składnikami i flagą <c>isDish</c> ustawioną na <c>true</c>.
    /// </remarks>
    [Test]
    public void NetworkItemStateDishRoundTrip()
    {
        object dish = CreateDish(
            CreatePreparedIngredient("Lavash", "Raw"),
            CreatePreparedIngredient("Meat", "Cooked"),
            CreatePreparedIngredient("Tomato", "Chopped"),
            CreatePreparedIngredient("GarlicSauce", "Raw"));

        Type nisType = GetTypeByName("NetworkItemState");
        MethodInfo fromItem = nisType.GetMethod("FromKitchenItem", BindingFlags.Public | BindingFlags.Static);
        object netState = fromItem.Invoke(null, new object[] { dish });

        bool exists = (bool)nisType.GetField("exists").GetValue(netState);
        Assert.IsTrue(exists);

        int contentCount = (int)nisType.GetField("contentCount").GetValue(netState);
        Assert.AreEqual(4, contentCount, "Danie z 4 skladnikami powinno miec contentCount = 4.");

        MethodInfo toItem = nisType.GetMethod("ToKitchenItem", BindingFlags.Public | BindingFlags.Instance);
        object restored = toItem.Invoke(netState, null);
        Assert.IsNotNull(restored);
        Assert.IsTrue((bool)GetField(restored, "isDish"));
        Assert.AreEqual(4, ((IList)GetField(restored, "contents")).Count);
    }

    /// <summary>
    /// Weryfikuje, że walidator odrzuca danie zawierające nadmiarowe składniki.
    /// </summary>
    /// <remarks>
    /// Zamówienie wymaga tylko mięsa, ale danie zawiera mięso i dodatkowo pomidora.
    /// Oczekuje się, że walidator zwróci <c>false</c>, a komunikat błędu będzie zawierał
    /// słowo "nadmiarowe".
    /// </remarks>
    [Test]
    public void ValidatorRejectsExtraIngredients()
    {
        object order = Create("Order");
        IList requirements = (IList)GetField(order, "wymaganeSkladniki");
        requirements.Add(CreateIngredientRequirement("Meat", "Cooked"));

        // Dish has meat + extra tomato
        object dish = CreateDish(
            CreatePreparedIngredient("Meat", "Cooked"),
            CreatePreparedIngredient("Tomato", "Chopped"));

        object[] args = { order, dish, null };
        bool result = (bool)GetTypeByName("KitchenOrderValidator")
            .GetMethod("MatchesOrder", BindingFlags.Public | BindingFlags.Static)
            .Invoke(null, args);

        Assert.IsFalse(result, "Walidator powinien odrzucic kebab z nadmiarowymi skladnikami.");
        StringAssert.Contains("nadmiarowe", args[2] as string);
    }

    /// <summary>
    /// Weryfikuje, że walidator odrzuca element kuchenny, który nie jest daniem (nie jest złożony).
    /// </summary>
    /// <remarks>
    /// Tworzy pojedynczy surowy składnik (mięso) zamiast złożonego dania.
    /// Oczekuje się, że walidator zwróci <c>false</c>, ponieważ zamówienie
    /// wymaga złożonego dania, a nie pojedynczego składnika.
    /// </remarks>
    [Test]
    public void ValidatorRejectsNonDishItem()
    {
        object order = Create("Order");
        IList requirements = (IList)GetField(order, "wymaganeSkladniki");
        requirements.Add(CreateIngredientRequirement("Meat", "Cooked"));

        // Not a dish - just a raw ingredient
        object item = Create("KitchenItem");
        SetField(item, "isDish", false);
        SetField(item, "ingredientKind", ParseEnum("IngredientKind", "Meat"));

        object[] args = { order, item, null };
        bool result = (bool)GetTypeByName("KitchenOrderValidator")
            .GetMethod("MatchesOrder", BindingFlags.Public | BindingFlags.Static)
            .Invoke(null, args);

        Assert.IsFalse(result, "Walidator powinien odrzucic pojedynczy skladnik.");
    }

    /// <summary>
    /// Weryfikuje, że klonowanie zamówienia zachowuje wszystkie pola i tworzy głęboką kopię.
    /// </summary>
    /// <remarks>
    /// Tworzy zamówienie z ustawionymi polami (id, klient, nazwa, czas, nagroda) oraz dwiema
    /// pozycjami wymaganych składników. Po klonowaniu sprawdza, czy wszystkie wartości
    /// zostały skopiowane poprawnie. Dodatkowo weryfikuje, że modyfikacja oryginalnych
    /// wymagań nie wpływa na klon (głęboka kopia).
    /// </remarks>
    [Test]
    public void OrderClonePreservesAllFields()
    {
        object original = Create("Order");
        SetField(original, "orderId", "test-clone");
        SetField(original, "nazwaKlienta", "TestKlient");
        SetField(original, "nazwaZamowienia", "TestKebab");
        SetField(original, "czasNaRealizacje", 77f);
        SetField(original, "nagrodaPieniezna", 42f);

        IList reqs = (IList)GetField(original, "wymaganeSkladniki");
        reqs.Add(CreateIngredientRequirement("Meat", "Cooked", 2));
        reqs.Add(CreateIngredientRequirement("Tomato", "Chopped"));

        MethodInfo cloneMethod = GetTypeByName("Order")
            .GetMethod("Clone", BindingFlags.Public | BindingFlags.Instance);
        object clone = cloneMethod.Invoke(original, null);

        Assert.AreEqual("test-clone", GetField(clone, "orderId"));
        Assert.AreEqual("TestKlient", GetField(clone, "nazwaKlienta"));
        Assert.AreEqual(77f, (float)GetField(clone, "czasNaRealizacje"));
        Assert.AreEqual(42f, (float)GetField(clone, "nagrodaPieniezna"));

        IList clonedReqs = (IList)GetField(clone, "wymaganeSkladniki");
        Assert.AreEqual(2, clonedReqs.Count);

        // Verify deep copy - modifying original shouldn't affect clone
        reqs.Add(CreateIngredientRequirement("Onion", "Chopped"));
        Assert.AreEqual(2, clonedReqs.Count, "Klon powinien byc niezalezny od oryginalu.");
    }

    // =========================================================================
    //  NOWE TESTY — Mechanika gotowania i progressive difficulty
    // =========================================================================

    /// <summary>
    /// Weryfikuje, że metoda <c>KitchenItem.FromIngredient</c> poprawnie obsługuje wartość null.
    /// </summary>
    /// <remarks>
    /// Ponieważ <c>IngredientData</c> jest obiektem ScriptableObject i nie można go
    /// bezpośrednio utworzyć w testach EditMode, test sprawdza ścieżkę null —
    /// oczekuje się, że metoda zwróci nowy obiekt <c>KitchenItem</c> z domyślną nazwą
    /// "Skladnik" i flagą <c>isDish</c> ustawioną na <c>false</c>.
    /// </remarks>
    [Test]
    public void KitchenItemFromIngredientCreatesCorrectItem()
    {
        // Test that KitchenItem.FromIngredient handles null gracefully
        Type kitchenItemType = GetTypeByName("KitchenItem");

        // Since IngredientData is a ScriptableObject we can't instantiate it directly
        // in edit-mode tests, but we can test the null path
        MethodInfo fromIngredient = kitchenItemType.GetMethod("FromIngredient",
            BindingFlags.Public | BindingFlags.Static);

        object item = fromIngredient.Invoke(null, new object[] { null });
        Assert.IsNotNull(item);
        Assert.AreEqual("Skladnik", GetField(item, "itemName"));
        Assert.IsFalse((bool)GetField(item, "isDish"));
    }

    /// <summary>
    /// Weryfikuje, że klonowanie obiektu <c>KitchenItem</c> zachowuje zawartość dania i tworzy głęboką kopię.
    /// </summary>
    /// <remarks>
    /// Tworzy danie z trzema składnikami i klonuje je. Sprawdza, czy klon jest daniem
    /// z trzema składnikami. Następnie dodaje nowy składnik do oryginału i weryfikuje,
    /// że klon nie został zmodyfikowany (głęboka kopia).
    /// </remarks>
    [Test]
    public void KitchenItemClonePreservesContents()
    {
        object original = CreateDish(
            CreatePreparedIngredient("Lavash", "Raw"),
            CreatePreparedIngredient("Meat", "Cooked"),
            CreatePreparedIngredient("Tomato", "Chopped"));

        MethodInfo cloneMethod = GetTypeByName("KitchenItem")
            .GetMethod("Clone", BindingFlags.Public | BindingFlags.Instance);
        object cloned = cloneMethod.Invoke(original, null);

        Assert.IsTrue((bool)GetField(cloned, "isDish"));
        IList clonedContents = (IList)GetField(cloned, "contents");
        Assert.AreEqual(3, clonedContents.Count);

        // Verify deep copy
        IList origContents = (IList)GetField(original, "contents");
        origContents.Add(CreatePreparedIngredient("Onion", "Chopped"));
        Assert.AreEqual(3, clonedContents.Count, "Klon nie powinien zmieniac sie po modyfikacji oryginalu.");
    }

    /// <summary>
    /// Weryfikuje, że podsumowanie dania zawiera listę wszystkich składników.
    /// </summary>
    /// <remarks>
    /// Tworzy danie z nazwą "Kebab testowy" zawierające Lavash i mięso.
    /// Sprawdza, czy metoda <c>BuildSummary</c> zwraca tekst zawierający
    /// nazwę dania oraz polskie nazwy składników ("Lawasz", "Mieso").
    /// </remarks>
    [Test]
    public void DishBuildSummaryListsAllIngredients()
    {
        object dish = CreateDish(
            CreatePreparedIngredient("Lavash", "Raw"),
            CreatePreparedIngredient("Meat", "Cooked"));

        SetField(dish, "itemName", "Kebab testowy");

        MethodInfo buildSummary = GetTypeByName("KitchenItem")
            .GetMethod("BuildSummary", BindingFlags.Public | BindingFlags.Instance);
        string summary = (string)buildSummary.Invoke(dish, null);

        StringAssert.Contains("Kebab testowy", summary);
        StringAssert.Contains("Lawasz", summary);
        StringAssert.Contains("Mieso", summary);
    }

    /// <summary>
    /// Weryfikuje, że podsumowanie surowego składnika wyświetla jego stan przetworzenia.
    /// </summary>
    /// <remarks>
    /// Tworzy surowy pomidor (nie danie) i wywołuje metodę <c>BuildSummary</c>.
    /// Sprawdza, czy zwrócony tekst zawiera polskie nazwy: "Pomidor" i "surowy".
    /// </remarks>
    [Test]
    public void RawIngredientBuildSummaryShowsState()
    {
        object item = Create("KitchenItem");
        SetField(item, "itemName", "Pomidor");
        SetField(item, "ingredientKind", ParseEnum("IngredientKind", "Tomato"));
        SetField(item, "state", ParseEnum("IngredientProcessState", "Raw"));
        SetField(item, "isDish", false);

        MethodInfo buildSummary = GetTypeByName("KitchenItem")
            .GetMethod("BuildSummary", BindingFlags.Public | BindingFlags.Instance);
        string summary = (string)buildSummary.Invoke(item, null);

        StringAssert.Contains("Pomidor", summary);
        StringAssert.Contains("surowy", summary);
    }

    /// <summary>
    /// Weryfikuje pełną integrację walidatora dla kompletnego klasycznego kebaba.
    /// </summary>
    /// <remarks>
    /// Tworzy zamówienie na klasyczny kebab z pięcioma wymaganiami (Lavash, Meat, Tomato,
    /// Onion, GarlicSauce) oraz danie z dokładnie tymi samymi składnikami.
    /// Sprawdza, czy walidator akceptuje takie danie jako prawidłowe.
    /// </remarks>
    [Test]
    public void ValidatorAcceptsCompleteClassicKebab()
    {
        // Full integration test: classic kebab order with exact match
        object order = Create("Order");
        SetField(order, "orderId", "classic-test");
        SetField(order, "nazwaKlienta", "TestClient");
        SetField(order, "nazwaZamowienia", "Klasyczny kebab test");

        IList requirements = (IList)GetField(order, "wymaganeSkladniki");
        requirements.Add(CreateIngredientRequirement("Lavash", "Raw"));
        requirements.Add(CreateIngredientRequirement("Meat", "Cooked"));
        requirements.Add(CreateIngredientRequirement("Tomato", "Chopped"));
        requirements.Add(CreateIngredientRequirement("Onion", "Chopped"));
        requirements.Add(CreateIngredientRequirement("GarlicSauce", "Raw"));

        object dish = CreateDish(
            CreatePreparedIngredient("Lavash", "Raw"),
            CreatePreparedIngredient("Meat", "Cooked"),
            CreatePreparedIngredient("Tomato", "Chopped"),
            CreatePreparedIngredient("Onion", "Chopped"),
            CreatePreparedIngredient("GarlicSauce", "Raw"));

        object[] args = { order, dish, null };
        bool result = (bool)GetTypeByName("KitchenOrderValidator")
            .GetMethod("MatchesOrder", BindingFlags.Public | BindingFlags.Static)
            .Invoke(null, args);

        Assert.IsTrue(result, "Pelny klasyczny kebab powinien byc zaakceptowany. Blad: " + (args[2] as string));
    }

    /// <summary>
    /// Weryfikuje, że walidator odrzuca danie z brakującym wymaganym składnikiem.
    /// </summary>
    /// <remarks>
    /// Zamówienie wymaga mięsa i pomidora, ale danie zawiera tylko mięso.
    /// Oczekuje się, że walidator zwróci <c>false</c>, a komunikat błędu
    /// będzie zawierał słowo "Brakuje".
    /// </remarks>
    [Test]
    public void ValidatorRejectsMissingIngredient()
    {
        // Order requires meat + tomato, dish only has meat
        object order = Create("Order");
        IList requirements = (IList)GetField(order, "wymaganeSkladniki");
        requirements.Add(CreateIngredientRequirement("Meat", "Cooked"));
        requirements.Add(CreateIngredientRequirement("Tomato", "Chopped"));

        object dish = CreateDish(
            CreatePreparedIngredient("Meat", "Cooked"));

        object[] args = { order, dish, null };
        bool result = (bool)GetTypeByName("KitchenOrderValidator")
            .GetMethod("MatchesOrder", BindingFlags.Public | BindingFlags.Static)
            .Invoke(null, args);

        Assert.IsFalse(result, "Kebab bez wymaganego pomidora powinien byc odrzucony.");
        StringAssert.Contains("Brakuje", args[2] as string);
    }

    /// <summary>
    /// Weryfikuje, że walidator odrzuca danie z podwójną ilością składnika, gdy wymagana jest pojedyncza.
    /// </summary>
    /// <remarks>
    /// Zamówienie wymaga 1 sztuki mięsa, ale danie zawiera 2 sztuki mięsa.
    /// Oczekuje się, że walidator zwróci <c>false</c>, a komunikat błędu
    /// będzie zawierał słowo "nadmiarowe".
    /// </remarks>
    [Test]
    public void ValidatorRejectsDoubleQuantityWhenSingleRequired()
    {
        // Order requires 1x meat, dish has 2x meat
        object order = Create("Order");
        IList requirements = (IList)GetField(order, "wymaganeSkladniki");
        requirements.Add(CreateIngredientRequirement("Meat", "Cooked", 1));

        object dish = CreateDish(
            CreatePreparedIngredient("Meat", "Cooked"),
            CreatePreparedIngredient("Meat", "Cooked"));

        object[] args = { order, dish, null };
        bool result = (bool)GetTypeByName("KitchenOrderValidator")
            .GetMethod("MatchesOrder", BindingFlags.Public | BindingFlags.Static)
            .Invoke(null, args);

        Assert.IsFalse(result, "Nadmiar miesa powinien byc odrzucony.");
        StringAssert.Contains("nadmiarowe", args[2] as string);
    }

    /// <summary>
    /// Weryfikuje, że klasa <c>KitchenNaming</c> zwraca poprawne polskie etykiety składników i stanów.
    /// </summary>
    /// <remarks>
    /// Sprawdza tłumaczenia wszystkich typów składników (Meat → "Mieso", Tomato → "Pomidor",
    /// Onion → "Cebula", Lettuce → "Salata", GarlicSauce → "Sos czosnkowy",
    /// Lavash → "Lawasz", Kebab → "Kebab") oraz stanów przetworzenia
    /// (Raw → "surowy", Chopped → "pokrojony", Cooked → "upieczony", Assembled → "zlozony").
    /// </remarks>
    [Test]
    public void KitchenNamingReturnsPolishLabels()
    {
        Type naming = GetTypeByName("KitchenNaming");
        MethodInfo getIngredient = naming.GetMethod("GetIngredientLabel", BindingFlags.Public | BindingFlags.Static);
        MethodInfo getProcess = naming.GetMethod("GetProcessLabel", BindingFlags.Public | BindingFlags.Static);

        Assert.AreEqual("Mieso", getIngredient.Invoke(null, new object[] { ParseEnum("IngredientKind", "Meat") }));
        Assert.AreEqual("Pomidor", getIngredient.Invoke(null, new object[] { ParseEnum("IngredientKind", "Tomato") }));
        Assert.AreEqual("Cebula", getIngredient.Invoke(null, new object[] { ParseEnum("IngredientKind", "Onion") }));
        Assert.AreEqual("Salata", getIngredient.Invoke(null, new object[] { ParseEnum("IngredientKind", "Lettuce") }));
        Assert.AreEqual("Sos czosnkowy", getIngredient.Invoke(null, new object[] { ParseEnum("IngredientKind", "GarlicSauce") }));
        Assert.AreEqual("Lawasz", getIngredient.Invoke(null, new object[] { ParseEnum("IngredientKind", "Lavash") }));
        Assert.AreEqual("Kebab", getIngredient.Invoke(null, new object[] { ParseEnum("IngredientKind", "Kebab") }));

        Assert.AreEqual("surowy", getProcess.Invoke(null, new object[] { ParseEnum("IngredientProcessState", "Raw") }));
        Assert.AreEqual("pokrojony", getProcess.Invoke(null, new object[] { ParseEnum("IngredientProcessState", "Chopped") }));
        Assert.AreEqual("upieczony", getProcess.Invoke(null, new object[] { ParseEnum("IngredientProcessState", "Cooked") }));
        Assert.AreEqual("zlozony", getProcess.Invoke(null, new object[] { ParseEnum("IngredientProcessState", "Assembled") }));
    }

    /// <summary>
    /// Weryfikuje, że ciąg wyświetlania wymagania składnika zawiera nazwę i stan przetworzenia.
    /// </summary>
    /// <remarks>
    /// Tworzy wymaganie dotyczące upieczonego mięsa i sprawdza, czy metoda <c>ToDisplayString</c>
    /// zwraca tekst zawierający "Mieso" i "upieczony".
    /// </remarks>
    [Test]
    public void IngredientRequirementDisplayStringContainsNameAndState()
    {
        object requirement = CreateIngredientRequirement("Meat", "Cooked");
        MethodInfo toDisplay = GetTypeByName("IngredientRequirement")
            .GetMethod("ToDisplayString", BindingFlags.Public | BindingFlags.Instance);

        string display = (string)toDisplay.Invoke(requirement, null);
        StringAssert.Contains("Mieso", display);
        StringAssert.Contains("upieczony", display);
    }

    /// <summary>
    /// Weryfikuje, że wymaganie składnika z ilością większą niż 1 wyświetla mnożnik.
    /// </summary>
    /// <remarks>
    /// Tworzy wymaganie na 3 sztuki upieczonego mięsa i sprawdza, czy metoda
    /// <c>ToDisplayString</c> zawiera tekst "3x" oraz "Mieso".
    /// </remarks>
    [Test]
    public void IngredientRequirementQuantityShowsMultiplier()
    {
        object requirement = CreateIngredientRequirement("Meat", "Cooked", 3);
        MethodInfo toDisplay = GetTypeByName("IngredientRequirement")
            .GetMethod("ToDisplayString", BindingFlags.Public | BindingFlags.Instance);

        string display = (string)toDisplay.Invoke(requirement, null);
        StringAssert.Contains("3x", display);
        StringAssert.Contains("Mieso", display);
    }

    /// <summary>
    /// Weryfikuje, że opis zamówienia zawiera imię klienta oraz nazwę dania.
    /// </summary>
    /// <remarks>
    /// Tworzy zamówienie dla klienta "Marek" na "Testowy kebab" z wymaganiami na mięso i pomidora.
    /// Sprawdza, czy metoda <c>BuildDescription</c> generuje tekst zawierający imię klienta,
    /// nazwę zamówienia oraz polskie nazwy składników.
    /// </remarks>
    [Test]
    public void OrderBuildDescriptionContainsClientAndDishName()
    {
        object order = Create("Order");
        SetField(order, "nazwaKlienta", "Marek");
        SetField(order, "nazwaZamowienia", "Testowy kebab");

        IList reqs = (IList)GetField(order, "wymaganeSkladniki");
        reqs.Add(CreateIngredientRequirement("Meat", "Cooked"));
        reqs.Add(CreateIngredientRequirement("Tomato", "Chopped"));

        MethodInfo buildDesc = GetTypeByName("Order")
            .GetMethod("BuildDescription", BindingFlags.Public | BindingFlags.Instance);
        string desc = (string)buildDesc.Invoke(order, null);

        StringAssert.Contains("Marek", desc);
        StringAssert.Contains("Testowy kebab", desc);
        StringAssert.Contains("Mieso", desc);
        StringAssert.Contains("Pomidor", desc);
    }

    /// <summary>
    /// Weryfikuje, że ciąg wyświetlania przygotowanego składnika jest poprawny.
    /// </summary>
    /// <remarks>
    /// Tworzy obiekt <c>PreparedIngredientData</c> reprezentujący pokrojony pomidor
    /// i sprawdza, czy metoda <c>ToDisplayString</c> zwraca tekst zawierający
    /// "Pomidor" i "pokrojony".
    /// </remarks>
    [Test]
    public void PreparedIngredientDisplayStringCorrect()
    {
        object ingredient = CreatePreparedIngredient("Tomato", "Chopped");
        MethodInfo toDisplay = GetTypeByName("PreparedIngredientData")
            .GetMethod("ToDisplayString", BindingFlags.Public | BindingFlags.Instance);

        string display = (string)toDisplay.Invoke(ingredient, null);
        StringAssert.Contains("Pomidor", display);
        StringAssert.Contains("pokrojony", display);
    }

    /// <summary>
    /// Weryfikuje, że typ wyliczeniowy <c>KitchenStationType</c> posiada dokładnie pięć wartości.
    /// </summary>
    /// <remarks>
    /// Sprawdza, czy enum <c>KitchenStationType</c> zawiera wartości: IngredientSource,
    /// CuttingBoard, Grill, Assembly oraz Delivery.
    /// </remarks>
    [Test]
    public void KitchenStationTypeEnumHasFiveValues()
    {
        Type stationType = GetTypeByName("KitchenStationType");
        Assert.IsTrue(stationType.IsEnum);

        string[] names = Enum.GetNames(stationType);
        Assert.AreEqual(5, names.Length);
        Assert.Contains("IngredientSource", names);
        Assert.Contains("CuttingBoard", names);
        Assert.Contains("Grill", names);
        Assert.Contains("Assembly", names);
        Assert.Contains("Delivery", names);
    }

    /// <summary>
    /// Weryfikuje, że typ wyliczeniowy <c>IngredientKind</c> posiada dokładnie siedem wartości.
    /// </summary>
    /// <remarks>
    /// Sprawdza, czy enum <c>IngredientKind</c> zawiera wartości: Meat, Tomato, Onion,
    /// Lettuce, GarlicSauce, Lavash oraz Kebab.
    /// </remarks>
    [Test]
    public void IngredientKindEnumHasSevenValues()
    {
        Type kindType = GetTypeByName("IngredientKind");
        Assert.IsTrue(kindType.IsEnum);

        string[] names = Enum.GetNames(kindType);
        Assert.AreEqual(7, names.Length);
        Assert.Contains("Meat", names);
        Assert.Contains("Tomato", names);
        Assert.Contains("Onion", names);
        Assert.Contains("Lettuce", names);
        Assert.Contains("GarlicSauce", names);
        Assert.Contains("Lavash", names);
        Assert.Contains("Kebab", names);
    }

    /// <summary>
    /// Weryfikuje, że typ wyliczeniowy <c>IngredientProcessState</c> posiada dokładnie cztery wartości.
    /// </summary>
    /// <remarks>
    /// Sprawdza, czy enum <c>IngredientProcessState</c> zawiera wartości: Raw, Chopped,
    /// Cooked oraz Assembled.
    /// </remarks>
    [Test]
    public void ProcessStateEnumHasFourValues()
    {
        Type stateType = GetTypeByName("IngredientProcessState");
        Assert.IsTrue(stateType.IsEnum);

        string[] names = Enum.GetNames(stateType);
        Assert.AreEqual(4, names.Length);
        Assert.Contains("Raw", names);
        Assert.Contains("Chopped", names);
        Assert.Contains("Cooked", names);
        Assert.Contains("Assembled", names);
    }

    /// <summary>
    /// Tworzy obiekt dania (KitchenItem) z podanymi przygotowanymi składnikami.
    /// </summary>
    /// <param name="ingredients">Tablica przygotowanych składników do umieszczenia w daniu.</param>
    /// <returns>Obiekt dania typu <c>KitchenItem</c> z flagą <c>isDish</c> ustawioną na <c>true</c>,
    /// typem składnika Kebab, stanem Assembled i podanymi składnikami w polu <c>contents</c>.</returns>
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

    /// <summary>
    /// Tworzy obiekt wymagania składnika z określonym typem, stanem i ilością.
    /// </summary>
    /// <param name="ingredientKind">Nazwa typu składnika (np. "Meat", "Tomato").</param>
    /// <param name="processState">Nazwa wymaganego stanu przetworzenia (np. "Cooked", "Chopped").</param>
    /// <param name="quantity">Wymagana ilość składnika. Domyślnie 1.</param>
    /// <returns>Obiekt <c>IngredientRequirement</c> z ustawionymi polami typu, stanu i ilości.</returns>
    private static object CreateIngredientRequirement(string ingredientKind, string processState, int quantity = 1)
    {
        object requirement = Create("IngredientRequirement");
        SetField(requirement, "ingredientKind", ParseEnum("IngredientKind", ingredientKind));
        SetField(requirement, "requiredState", ParseEnum("IngredientProcessState", processState));
        SetField(requirement, "quantity", quantity);
        return requirement;
    }

    /// <summary>
    /// Tworzy obiekt przygotowanego składnika z określonym typem i stanem przetworzenia.
    /// </summary>
    /// <param name="ingredientKind">Nazwa typu składnika (np. "Meat", "Lavash").</param>
    /// <param name="processState">Nazwa stanu przetworzenia (np. "Raw", "Cooked").</param>
    /// <returns>Obiekt <c>PreparedIngredientData</c> z ustawionymi polami typu i stanu.</returns>
    private static object CreatePreparedIngredient(string ingredientKind, string processState)
    {
        object ingredient = Create("PreparedIngredientData");
        SetField(ingredient, "ingredientKind", ParseEnum("IngredientKind", ingredientKind));
        SetField(ingredient, "state", ParseEnum("IngredientProcessState", processState));
        return ingredient;
    }

    /// <summary>
    /// Parsuje wartość tekstową na odpowiadający jej element typu wyliczeniowego.
    /// </summary>
    /// <param name="typeName">Nazwa typu wyliczeniowego do wyszukania w assembly.</param>
    /// <param name="value">Wartość tekstowa do sparsowania (np. "Meat", "Cooked").</param>
    /// <returns>Sparsowana wartość enumeracji jako <c>object</c>.</returns>
    private static object ParseEnum(string typeName, string value)
    {
        return Enum.Parse(GetTypeByName(typeName), value);
    }

    /// <summary>
    /// Tworzy nową instancję typu o podanej nazwie z assembly głównego projektu.
    /// </summary>
    /// <param name="typeName">Nazwa typu do utworzenia (np. "Order", "KitchenItem").</param>
    /// <returns>Nowo utworzona instancja podanego typu.</returns>
    private static object Create(string typeName)
    {
        return Activator.CreateInstance(GetTypeByName(typeName));
    }

    /// <summary>
    /// Pobiera wartość pola publicznego z podanej instancji obiektu za pomocą refleksji.
    /// </summary>
    /// <param name="instance">Instancja obiektu, z której pobieramy wartość pola.</param>
    /// <param name="fieldName">Nazwa pola do odczytania.</param>
    /// <returns>Wartość pola jako <c>object</c>.</returns>
    private static object GetField(object instance, string fieldName)
    {
        return instance.GetType().GetField(fieldName).GetValue(instance);
    }

    /// <summary>
    /// Ustawia wartość pola publicznego w podanej instancji obiektu za pomocą refleksji.
    /// </summary>
    /// <param name="instance">Instancja obiektu, w której ustawiamy wartość pola.</param>
    /// <param name="fieldName">Nazwa pola do zapisania.</param>
    /// <param name="value">Nowa wartość do przypisania do pola.</param>
    private static void SetField(object instance, string fieldName, object value)
    {
        instance.GetType().GetField(fieldName).SetValue(instance, value);
    }

    /// <summary>
    /// Wyszukuje i zwraca typ o podanej nazwie z assembly <c>Assembly-CSharp</c>.
    /// </summary>
    /// <param name="typeName">Nazwa typu do wyszukania.</param>
    /// <returns>Obiekt <see cref="Type"/> odpowiadający podanej nazwie.</returns>
    /// <remarks>
    /// Metoda iteruje po wszystkich załadowanych assembly w bieżącej domenie aplikacji,
    /// szukając assembly o nazwie "Assembly-CSharp". Jeśli assembly nie jest załadowane
    /// lub typ nie zostanie znaleziony, test kończy się niepowodzeniem z odpowiednim komunikatem.
    /// </remarks>
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
