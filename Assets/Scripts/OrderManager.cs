using System.Globalization;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class OrderManager : MonoBehaviour
{
    public static OrderManager Instance { get; private set; }

    public List<IngredientData> dostepneSkladniki = new List<IngredientData>();

    [SerializeField] private List<Order> orderTemplates = new List<Order>();
    [SerializeField] private Order activeOrder;

    public int ActiveTemplateIndex { get; private set; } = -1;
    private string activeOrderDescription = "";
    private float remainingTime;
    private int completedOrders;
    private int failedOrders;
    private string lastMessage = "Przygotuj pierwszy kebab.";

    private readonly Dictionary<IngredientKind, IngredientData> ingredientLookup =
        new Dictionary<IngredientKind, IngredientData>();

    public Order ActiveOrder => activeOrder;
    public float RemainingOrderTime => remainingTime;
    public int CompletedOrders => completedOrders;
    public int FailedOrders => failedOrders;
    public string LastOrderMessage => lastMessage;
    public string ActiveOrderDescription => activeOrderDescription;
    public bool HasActiveOrder => activeOrder != null;
    public float RemainingTime => remainingTime;

    public void SyncNetworkState(int templateIndex, string desc, float time, int comp, int fail)
    {
        InitializeCatalogIfNeeded();
        BuildDefaultTemplatesIfNeeded();

        if (templateIndex >= 0 && templateIndex < orderTemplates.Count && ActiveTemplateIndex != templateIndex)
        {
            ActiveTemplateIndex = templateIndex;
            activeOrder = orderTemplates[templateIndex].Clone();
            activeOrder.nagrodaPieniezna = CalculateReward(activeOrder.wymaganeSkladniki);
        }

        activeOrderDescription = desc;
        remainingTime = time;
        completedOrders = comp;
        failedOrders = fail;
        lastMessage = desc;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializeCatalogIfNeeded();
        BuildDefaultTemplatesIfNeeded();
    }

    private void Start()
    {
        bool isMultiplayerClient = Unity.Netcode.NetworkManager.Singleton != null && 
                                   Unity.Netcode.NetworkManager.Singleton.IsListening &&
                                   !Unity.Netcode.NetworkManager.Singleton.IsServer;

        if (activeOrder == null && !isMultiplayerClient)
        {
            NoweZamowienie();
        }
    }

    private void Update()
    {
        bool isServer = Unity.Netcode.NetworkManager.Singleton == null ||
                        !Unity.Netcode.NetworkManager.Singleton.IsListening ||
                        Unity.Netcode.NetworkManager.Singleton.IsServer;

        if (!isServer || activeOrder == null)
        {
            return;
        }

        remainingTime -= Time.deltaTime;
        if (remainingTime > 0f)
        {
            return;
        }

        failedOrders++;
        lastMessage = "Klient odszedl. Zamowienie nie zostalo przygotowane na czas.";
        PlayTimeoutFeedback();
        NoweZamowienie();
    }

    public void InitializeCatalogIfNeeded()
    {
        if (ingredientLookup.Count > 0)
        {
            return;
        }

        foreach (IngredientData ingredient in dostepneSkladniki)
        {
            if (ingredient == null)
            {
                continue;
            }

            NormalizeAssignedIngredient(ingredient);
            ingredientLookup[ingredient.typSkladnika] = ingredient;
        }

        EnsureRuntimeIngredient(IngredientKind.Meat, "Mieso", IngredientProcessState.Raw, new Color(0.65f, 0.25f, 0.18f), 7f, 14f);
        EnsureRuntimeIngredient(IngredientKind.Tomato, "Pomidor", IngredientProcessState.Raw, new Color(0.86f, 0.2f, 0.2f), 2f, 5f);
        EnsureRuntimeIngredient(IngredientKind.Onion, "Cebula", IngredientProcessState.Raw, new Color(0.93f, 0.9f, 0.75f), 1.5f, 4f);
        EnsureRuntimeIngredient(IngredientKind.Lettuce, "Salata", IngredientProcessState.Raw, new Color(0.35f, 0.7f, 0.25f), 1.2f, 3f);
        EnsureRuntimeIngredient(IngredientKind.GarlicSauce, "Sos czosnkowy", IngredientProcessState.Raw, new Color(0.95f, 0.95f, 0.85f), 1.5f, 4f);
        EnsureRuntimeIngredient(IngredientKind.Lavash, "Lawasz", IngredientProcessState.Raw, new Color(0.86f, 0.74f, 0.5f), 3f, 6f);
        EnsureRuntimeIngredient(IngredientKind.Kebab, "Kebab", IngredientProcessState.Assembled, new Color(0.76f, 0.57f, 0.35f), 0f, 20f);
    }

    public IngredientData GetIngredientDefinition(IngredientKind kind)
    {
        InitializeCatalogIfNeeded();
        IngredientData data;
        ingredientLookup.TryGetValue(kind, out data);
        return data;
    }

    public bool TryDeliverDish(KitchenItem item, out string message)
    {
        if (activeOrder == null)
        {
            message = "Brak aktywnego zamowienia.";
            return false;
        }

        string failureReason;
        if (!KitchenOrderValidator.MatchesOrder(activeOrder, item, out failureReason))
        {
            message = "Zly kebab: " + failureReason;
            lastMessage = message;
            return false;
        }

        completedOrders++;
        float reward = activeOrder.nagrodaPieniezna;

        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.AddMoney(reward);
        }

        message = "Zamowienie zrealizowane. Nagroda: " + reward + " zl.";
        lastMessage = message;
        NoweZamowienie();
        SaveManager.Instance?.SaveGame();
        return true;
    }

    public void NoweZamowienie()
    {
        InitializeCatalogIfNeeded();
        BuildDefaultTemplatesIfNeeded();

        int maxIndex = GetAvailableTemplateCount();
        int index = Random.Range(0, maxIndex);
        ActiveTemplateIndex = index;
        activeOrder = orderTemplates[index].Clone();
        activeOrder.nagrodaPieniezna = CalculateReward(activeOrder.wymaganeSkladniki);
        float timeBonus = ShopManager.Instance != null ? ShopManager.Instance.GetOrderTimeBonus() : 0f;
        float difficultyMultiplier = GetDifficultyTimeMultiplier();
        remainingTime = (activeOrder.czasNaRealizacje * difficultyMultiplier) + timeBonus;
        string desc = "Nowe zamowienie: " + activeOrder.BuildDescription();
        lastMessage = desc;
        activeOrderDescription = desc;
        Debug.Log(desc);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayNewOrderSound();
    }

    private void PlayTimeoutFeedback()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayFailSound();
        }

        if (VFXManager.Instance != null)
        {
            VFXManager.Instance.PlayTimeoutEffect();
        }

        if (CameraEffects.Instance != null)
        {
            CameraEffects.Instance.ShakeCamera(0.045f, 0.22f);
            CameraEffects.Instance.FlashScreen(new Color(0.55f, 0.08f, 0.06f, 0.12f), 0.22f);
        }
    }

    private float GetDifficultyTimeMultiplier()
    {

        return Mathf.Max(0.3f, 1f - (completedOrders * 0.02f));
    }

    private int GetAvailableTemplateCount()
    {

        if (orderTemplates.Count <= 3)
        {
            return orderTemplates.Count;
        }

        if (completedOrders < 3)
        {
            return 3;
        }

        if (completedOrders < 6)
        {
            return Mathf.Min(orderTemplates.Count, 6);
        }

        return orderTemplates.Count;
    }

    public void SetOrderStateFromNetwork(string description, float time, int completed, int failed, string message)
    {
        activeOrderDescription = description;
        remainingTime = time;
        completedOrders = completed;
        failedOrders = failed;
        lastMessage = message;
    }

    public OrderProgressSaveData CaptureProgress()
    {
        OrderProgressSaveData data = new OrderProgressSaveData
        {
            completedOrders = completedOrders,
            failedOrders = failedOrders,
            remainingOrderTime = remainingTime,
            lastOrderMessage = lastMessage
        };

        if (activeOrder == null)
        {
            return data;
        }

        data.activeOrder = new OrderSaveData
        {
            orderId = activeOrder.orderId,
            clientName = activeOrder.nazwaKlienta,
            orderName = activeOrder.nazwaZamowienia,
            timeLimit = activeOrder.czasNaRealizacje,
            reward = activeOrder.nagrodaPieniezna
        };

        foreach (IngredientRequirement requirement in activeOrder.wymaganeSkladniki)
        {
            data.activeOrder.requirements.Add(new IngredientRequirement(
                requirement.ingredientKind,
                requirement.requiredState,
                requirement.quantity));
        }

        return data;
    }

    public void RestoreProgress(OrderProgressSaveData data)
    {
        if (data == null)
        {
            return;
        }

        completedOrders = Mathf.Max(0, data.completedOrders);
        failedOrders = Mathf.Max(0, data.failedOrders);
        if (!string.IsNullOrWhiteSpace(data.lastOrderMessage))
        {
            lastMessage = data.lastOrderMessage;
        }

        if (data.activeOrder == null || data.activeOrder.requirements == null || data.activeOrder.requirements.Count == 0)
        {
            activeOrder = null;
            NoweZamowienie();
            return;
        }

        activeOrder = new Order
        {
            orderId = data.activeOrder.orderId,
            nazwaKlienta = data.activeOrder.clientName,
            nazwaZamowienia = data.activeOrder.orderName,
            czasNaRealizacje = data.activeOrder.timeLimit,
            nagrodaPieniezna = data.activeOrder.reward
        };

        foreach (IngredientRequirement requirement in data.activeOrder.requirements)
        {
            activeOrder.wymaganeSkladniki.Add(new IngredientRequirement(
                requirement.ingredientKind,
                requirement.requiredState,
                requirement.quantity));
        }

        remainingTime = Mathf.Max(5f, data.remainingOrderTime);
        activeOrderDescription = "Wczytane: " + activeOrder.BuildDescription();
    }

    private void EnsureRuntimeIngredient(
        IngredientKind kind,
        string ingredientName,
        IngredientProcessState defaultState,
        Color debugColor,
        float purchasePrice,
        float saleValue)
    {
        if (ingredientLookup.ContainsKey(kind))
        {
            return;
        }

        IngredientData ingredient = ScriptableObject.CreateInstance<IngredientData>();
        ingredient.nazwaSkladnika = ingredientName;
        ingredient.typSkladnika = kind;
        ingredient.stanPoczatkowy = defaultState;
        ingredient.cenaZakupu = purchasePrice;
        ingredient.wartoscSprzedazy = saleValue;
        ingredient.kolorDebug = debugColor;

        dostepneSkladniki.Add(ingredient);
        ingredientLookup[kind] = ingredient;
    }

    private void NormalizeAssignedIngredient(IngredientData ingredient)
    {
        string sourceName = RemoveDiacritics(
            ((ingredient.DisplayName ?? string.Empty) + " " + ingredient.name).ToLowerInvariant());

        if (sourceName.Contains("mies"))
        {
            ingredient.typSkladnika = IngredientKind.Meat;
            ingredient.kolorDebug = new Color(0.65f, 0.25f, 0.18f);
            ingredient.wartoscSprzedazy = ingredient.wartoscSprzedazy <= 0f ? 14f : ingredient.wartoscSprzedazy;
        }
        else if (sourceName.Contains("pomid"))
        {
            ingredient.typSkladnika = IngredientKind.Tomato;
            ingredient.kolorDebug = new Color(0.86f, 0.2f, 0.2f);
            ingredient.wartoscSprzedazy = ingredient.wartoscSprzedazy <= 0f ? 5f : ingredient.wartoscSprzedazy;
        }

        if (ingredient.typSkladnika != IngredientKind.Kebab)
        {
            ingredient.stanPoczatkowy = IngredientProcessState.Raw;
        }
    }

    private string RemoveDiacritics(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        string normalized = value.Normalize(NormalizationForm.FormD);
        StringBuilder builder = new StringBuilder(normalized.Length);

        foreach (char character in normalized)
        {
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private void BuildDefaultTemplatesIfNeeded()
    {
        if (orderTemplates.Count > 0)
        {
            return;
        }

        orderTemplates.Add(CreateTemplate(
            "classic",
            "Adam",
            "Klasyczny kebab",
            95f,
            new IngredientRequirement(IngredientKind.Lavash, IngredientProcessState.Raw),
            new IngredientRequirement(IngredientKind.Meat, IngredientProcessState.Cooked),
            new IngredientRequirement(IngredientKind.Tomato, IngredientProcessState.Chopped),
            new IngredientRequirement(IngredientKind.Onion, IngredientProcessState.Chopped),
            new IngredientRequirement(IngredientKind.GarlicSauce, IngredientProcessState.Raw)));

        orderTemplates.Add(CreateTemplate(
            "meaty",
            "Basia",
            "Kebab miesny",
            85f,
            new IngredientRequirement(IngredientKind.Lavash, IngredientProcessState.Raw),
            new IngredientRequirement(IngredientKind.Meat, IngredientProcessState.Cooked, 2),
            new IngredientRequirement(IngredientKind.Onion, IngredientProcessState.Chopped),
            new IngredientRequirement(IngredientKind.GarlicSauce, IngredientProcessState.Raw)));

        orderTemplates.Add(CreateTemplate(
            "fresh",
            "Celina",
            "Kebab fresh",
            90f,
            new IngredientRequirement(IngredientKind.Lavash, IngredientProcessState.Raw),
            new IngredientRequirement(IngredientKind.Meat, IngredientProcessState.Cooked),
            new IngredientRequirement(IngredientKind.Tomato, IngredientProcessState.Chopped),
            new IngredientRequirement(IngredientKind.Lettuce, IngredientProcessState.Chopped),
            new IngredientRequirement(IngredientKind.GarlicSauce, IngredientProcessState.Raw)));

        orderTemplates.Add(CreateTemplate(
            "veggie",
            "Dawid",
            "Kebab warzywny",
            80f,
            new IngredientRequirement(IngredientKind.Lavash, IngredientProcessState.Raw),
            new IngredientRequirement(IngredientKind.Meat, IngredientProcessState.Cooked),
            new IngredientRequirement(IngredientKind.Tomato, IngredientProcessState.Chopped),
            new IngredientRequirement(IngredientKind.Onion, IngredientProcessState.Chopped),
            new IngredientRequirement(IngredientKind.Lettuce, IngredientProcessState.Chopped),
            new IngredientRequirement(IngredientKind.GarlicSauce, IngredientProcessState.Raw)));

        orderTemplates.Add(CreateTemplate(
            "garlic-bomb",
            "Ewa",
            "Kebab czosnkowy",
            75f,
            new IngredientRequirement(IngredientKind.Lavash, IngredientProcessState.Raw),
            new IngredientRequirement(IngredientKind.Meat, IngredientProcessState.Cooked, 2),
            new IngredientRequirement(IngredientKind.Tomato, IngredientProcessState.Chopped),
            new IngredientRequirement(IngredientKind.GarlicSauce, IngredientProcessState.Raw)));

        orderTemplates.Add(CreateTemplate(
            "light-quick",
            "Filip",
            "Kebab ekspresowy",
            60f,
            new IngredientRequirement(IngredientKind.Lavash, IngredientProcessState.Raw),
            new IngredientRequirement(IngredientKind.Meat, IngredientProcessState.Cooked),
            new IngredientRequirement(IngredientKind.Lettuce, IngredientProcessState.Chopped)));

        orderTemplates.Add(CreateTemplate(
            "double-deluxe",
            "Grzegorz",
            "Kebab podwojny deluxe",
            100f,
            new IngredientRequirement(IngredientKind.Lavash, IngredientProcessState.Raw),
            new IngredientRequirement(IngredientKind.Meat, IngredientProcessState.Cooked, 2),
            new IngredientRequirement(IngredientKind.Tomato, IngredientProcessState.Chopped),
            new IngredientRequirement(IngredientKind.Onion, IngredientProcessState.Chopped),
            new IngredientRequirement(IngredientKind.Lettuce, IngredientProcessState.Chopped),
            new IngredientRequirement(IngredientKind.GarlicSauce, IngredientProcessState.Raw)));

        orderTemplates.Add(CreateTemplate(
            "royal-feast",
            "Hanna",
            "Kebab krolewski",
            110f,
            new IngredientRequirement(IngredientKind.Lavash, IngredientProcessState.Raw),
            new IngredientRequirement(IngredientKind.Meat, IngredientProcessState.Cooked, 2),
            new IngredientRequirement(IngredientKind.Tomato, IngredientProcessState.Chopped),
            new IngredientRequirement(IngredientKind.Onion, IngredientProcessState.Chopped),
            new IngredientRequirement(IngredientKind.Lettuce, IngredientProcessState.Chopped)));
    }

    private Order CreateTemplate(
        string id,
        string clientName,
        string orderName,
        float timeLimit,
        params IngredientRequirement[] requirements)
    {
        Order order = new Order
        {
            orderId = id,
            nazwaKlienta = clientName,
            nazwaZamowienia = orderName,
            czasNaRealizacje = timeLimit
        };

        foreach (IngredientRequirement requirement in requirements)
        {
            order.wymaganeSkladniki.Add(requirement);
        }

        order.nagrodaPieniezna = CalculateReward(order.wymaganeSkladniki);
        return order;
    }

    private float CalculateReward(List<IngredientRequirement> requirements)
    {
        float total = 10f;
        foreach (IngredientRequirement requirement in requirements)
        {
            IngredientData ingredient;
            if (ingredientLookup.TryGetValue(requirement.ingredientKind, out ingredient))
            {
                total += ingredient.wartoscSprzedazy * requirement.quantity;
            }
            else
            {
                total += 4f * requirement.quantity;
            }

            if (requirement.requiredState != IngredientProcessState.Raw)
            {
                total += 1.5f * requirement.quantity;
            }
        }

        float rewardMultiplier = ShopManager.Instance != null ? ShopManager.Instance.GetRewardMultiplier() : 1f;
        return Mathf.Round(total * rewardMultiplier);
    }
}
