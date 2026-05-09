using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stacja kuchenna - obsługuje interakcje gracza ze stanowiskiem.
/// Typy stacji: źródło składników, deska do krojenia, grill (doner),
/// stanowisko montażu kebaba, punkt wydania zamówienia.
/// Integruje się z VFXManager dla efektów cząsteczkowych.
/// </summary>
public class KitchenStation : Interactable
{
    [SerializeField] private string stationName = "Stacja";
    [SerializeField] private KitchenStationType stationType = KitchenStationType.IngredientSource;
    [SerializeField] private IngredientData sourceIngredient;
    [SerializeField] private float processingDuration = 2.5f;
    [SerializeField] private Renderer visualRenderer;
    [SerializeField] private Color idleColor = new Color(0.35f, 0.35f, 0.35f);
    [SerializeField] private Color busyColor = new Color(0.95f, 0.65f, 0.2f);
    [SerializeField] private Color readyColor = new Color(0.35f, 0.8f, 0.35f);

    [SerializeField] private KitchenItem stationItem;
    [SerializeField] private bool hasLavash;
    [SerializeField] private List<PreparedIngredientData> assemblyIngredients = new List<PreparedIngredientData>();
    [SerializeField] private int preparedMeatServings;
    [SerializeField] private int preparedMeatBatchSize = 3;

    private bool isProcessing;
    private float processEndTime;
    private KitchenStation linkedMeatTray;
    private Transform meatVisual;

    // Dynamiczne modele 3D
    private GameObject dynamicStationItemVisual;
    private GameObject dynamicLavashVisual;
    private List<GameObject> dynamicAssemblyVisuals = new List<GameObject>();

    // Efekt pulsowania gotowej stacji (scale bounce)
    private float pulsePhase;
    private Vector3 baseScale;

    public bool IsProcessing => isProcessing;
    public float ProcessEndTime => processEndTime;
    public int PreparedMeatServings => preparedMeatServings;
    public bool HasLavash => hasLavash;
    public int AssemblyCount => assemblyIngredients.Count;
    public KitchenItem StationItem => stationItem;

    public void SyncNetworkState(bool netIsProcessing, float netProcessEndTime, int netPreparedMeatServings, bool netHasLavash, NetworkItemState netStationItem)
    {
        isProcessing = netIsProcessing;
        processEndTime = netProcessEndTime;
        preparedMeatServings = netPreparedMeatServings;
        hasLavash = netHasLavash;
        stationItem = netStationItem.exists ? netStationItem.ToKitchenItem() : null;
        RefreshVisualState();
    }

    public void Configure(
        string stationName,
        KitchenStationType stationType,
        IngredientData sourceIngredient,
        float processingDuration,
        Renderer visualRenderer)
    {
        this.stationName = stationName;
        this.stationType = stationType;
        this.sourceIngredient = sourceIngredient;
        this.processingDuration = processingDuration;
        this.visualRenderer = visualRenderer;
        PromptMessage = stationName;
        RefreshVisualState();
    }

    public void SetLinkedMeatTray(KitchenStation trayStation)
    {
        linkedMeatTray = trayStation;
        RefreshVisualState();
    }

    public void RefreshVisualState()
    {
        ApplyCurrentColor();
        UpdateDynamicVisuals();
    }

    private void Update()
    {
        // In multiplayer, only the server should process timers (clients receive state via NetworkKitchenStation)
        if (Unity.Netcode.NetworkManager.Singleton != null &&
            Unity.Netcode.NetworkManager.Singleton.IsListening &&
            !Unity.Netcode.NetworkManager.Singleton.IsServer)
        {
            UpdatePulseEffect();
            return;
        }

        if (!isProcessing || Time.time < processEndTime)
        {
            UpdatePulseEffect();
            return;
        }

        FinishProcessing();
    }

    /// <summary>
    /// Subtelny efekt pulsowania skali na stacjach z gotowym przedmiotem.
    /// Przyciąga uwagę gracza do stacji wymagającej interakcji.
    /// </summary>
    private void UpdatePulseEffect()
    {
        bool shouldPulse = !isProcessing && (
            stationItem != null ||
            (IsMeatTrayStation() && preparedMeatServings > 0));

        if (!shouldPulse)
        {
            return;
        }

        if (baseScale == Vector3.zero)
        {
            baseScale = transform.localScale;
        }

        pulsePhase += Time.deltaTime * 2.5f;
        float pulse = 1f + Mathf.Sin(pulsePhase) * 0.018f;
        transform.localScale = baseScale * pulse;
    }

    public override string GetPrompt(PlayerInteraction player)
    {
        switch (stationType)
        {
            case KitchenStationType.IngredientSource:
                if (IsMeatTrayStation())
                {
                    return preparedMeatServings > 0
                        ? "E: Wez mieso"
                        : "Potnij mieso z donera";
                }

                return "E: Wez " + (sourceIngredient != null ? sourceIngredient.DisplayName : "skladnik");
            case KitchenStationType.CuttingBoard:
                if (isProcessing)
                {
                    return "Deska: krojenie " + Mathf.CeilToInt(processEndTime - Time.time) + " s";
                }

                if (stationItem != null)
                {
                    return "E: Odbierz z deski";
                }

                return "E: Poloz warzywo do krojenia";
            case KitchenStationType.Grill:
                if (IsDonerStation())
                {
                    if (isProcessing)
                    {
                        return "Doner: krojenie " + Mathf.CeilToInt(processEndTime - Time.time) + " s";
                    }

                    if (linkedMeatTray != null && linkedMeatTray.HasPreparedMeat())
                    {
                        return "Doner: taca z miesem jest pelna";
                    }

                    return "E: Potnij mieso z donera";
                }

                if (isProcessing)
                {
                    return "Grill: pieczenie " + Mathf.CeilToInt(processEndTime - Time.time) + " s";
                }

                if (stationItem != null)
                {
                    return "E: Odbierz mieso z grilla";
                }

                return "E: Poloz mieso na grillu";
            case KitchenStationType.Assembly:
                if (player != null && player.HasItemInHand)
                {
                    return "E: Dodaj skladnik do kebaba";
                }

                return CanCreateDish()
                    ? "E: Zawin gotowego kebaba"
                    : "E: Dodaj lawasz i przygotowane skladniki";
            case KitchenStationType.Delivery:
                return "E: Wydaj gotowego kebaba klientowi";
            default:
                return base.GetPrompt(player);
        }
    }

    public override void Interact(PlayerInteraction player)
    {
        if (player == null)
        {
            base.Interact(player);
            return;
        }

        switch (stationType)
        {
            case KitchenStationType.IngredientSource:
                HandleIngredientSource(player);
                break;
            case KitchenStationType.CuttingBoard:
                HandleProcessingStation(player, IngredientProcessState.Chopped);
                break;
            case KitchenStationType.Grill:
                HandleProcessingStation(player, IngredientProcessState.Cooked);
                break;
            case KitchenStationType.Assembly:
                HandleAssembly(player);
                break;
            case KitchenStationType.Delivery:
                HandleDelivery(player);
                break;
        }
    }

    private void HandleIngredientSource(PlayerInteraction player)
    {
        if (IsMeatTrayStation())
        {
            HandleMeatTraySource(player);
            return;
        }

        if (player.HasItemInHand)
        {
            player.SetFeedback("Najpierw odloz to, co trzymasz.");
            return;
        }

        KitchenItem item = KitchenItem.FromIngredient(sourceIngredient);
        if (player.TryReceiveItem(item))
        {
            player.SetFeedback("Pobrano: " + item.BuildSummary());
        }
    }

    private void HandleProcessingStation(PlayerInteraction player, IngredientProcessState outputState)
    {
        if (stationType == KitchenStationType.Grill && IsDonerStation())
        {
            HandleDonerStation(player);
            return;
        }

        if (isProcessing)
        {
            player.SetFeedback(stationName + " jest zajeta.");
            return;
        }

        if (stationItem != null)
        {
            if (!player.TryReceiveItem(stationItem))
            {
                player.SetFeedback("Masz juz cos w rece.");
                return;
            }

            player.SetFeedback("Odebrano: " + stationItem.BuildSummary());
            stationItem = null;
            ApplyCurrentColor();
            return;
        }

        if (!player.HasItemInHand)
        {
            player.SetFeedback("Nie trzymasz zadnego skladnika.");
            return;
        }

        KitchenItem item = player.HeldItem;
        if (!CanBeProcessedHere(item, outputState))
        {
            player.SetFeedback("Ten skladnik nie pasuje do stacji " + stationName + ".");
            return;
        }

        stationItem = player.RemoveHeldItem();
        isProcessing = true;
        float adjustedDuration = processingDuration * GetShopSpeedMultiplier();
        processEndTime = Time.time + adjustedDuration;
        player.SetFeedback("Rozpoczeto przygotowanie: " + KitchenNaming.GetIngredientLabel(stationItem.ingredientKind));
        ApplyCurrentColor();

        // Efekt wizualny: cząsteczki krojenia z kolorem składnika
        if (stationType == KitchenStationType.CuttingBoard && VFXManager.Instance != null)
        {
            Color chopColor = sourceIngredient != null ? sourceIngredient.kolorDebug : new Color(0.6f, 0.8f, 0.3f);
            VFXManager.Instance.PlayChopEffect(transform.position, chopColor);
        }
    }

    private void HandleMeatTraySource(PlayerInteraction player)
    {
        if (preparedMeatServings <= 0)
        {
            player.SetFeedback("Najpierw potnij mieso z donera.");
            return;
        }

        if (player.HasItemInHand)
        {
            player.SetFeedback("Najpierw odloz to, co trzymasz.");
            return;
        }

        KitchenItem item = KitchenItem.FromIngredient(sourceIngredient);
        item.state = IngredientProcessState.Cooked;

        if (!player.TryReceiveItem(item))
        {
            player.SetFeedback("Masz juz cos w rece.");
            return;
        }

        preparedMeatServings = Mathf.Max(0, preparedMeatServings - 1);
        player.SetFeedback("Pobrano: " + item.BuildSummary());
        ApplyCurrentColor();
        linkedMeatTray?.ApplyCurrentColor();
    }

    private void HandleDonerStation(PlayerInteraction player)
    {
        if (isProcessing)
        {
            player.SetFeedback(stationName + " jest zajeta.");
            return;
        }

        if (player.HasItemInHand)
        {
            player.SetFeedback("Najpierw odloz to, co trzymasz.");
            return;
        }

        if (linkedMeatTray == null)
        {
            player.SetFeedback("Brak tacy na gotowe mieso.");
            return;
        }

        if (linkedMeatTray.HasPreparedMeat())
        {
            player.SetFeedback("Taca na mieso jest juz pelna.");
            return;
        }

        isProcessing = true;
        float adjustedDonerDuration = processingDuration * GetShopSpeedMultiplier();
        processEndTime = Time.time + adjustedDonerDuration;
        player.SetFeedback("Rozpoczeto scinanie miesa z donera.");
        ApplyCurrentColor();

        // Efekt wizualny: para nad donererm podczas pieczenia/krojenia
        if (VFXManager.Instance != null)
        {
            VFXManager.Instance.PlaySteamEffect(transform.position);
        }
    }

    private void HandleAssembly(PlayerInteraction player)
    {
        if (player.HasItemInHand)
        {
            KitchenItem item = player.HeldItem;

            if (item.isDish)
            {
                player.SetFeedback("Ten kebab jest juz zlozony.");
                return;
            }

            if (item.ingredientKind == IngredientKind.Lavash)
            {
                if (hasLavash)
                {
                    player.SetFeedback("Lawasz jest juz przygotowany na stanowisku.");
                    return;
                }

                hasLavash = true;
                player.RemoveHeldItem();
                player.SetFeedback("Polozono lawasz na stanowisku.");
                ApplyCurrentColor();
                return;
            }

            if (!CanBeAddedToAssembly(item))
            {
                player.SetFeedback("Ten skladnik nie jest gotowy do zlozenia kebaba.");
                return;
            }

            assemblyIngredients.Add(new PreparedIngredientData(item.ingredientKind, item.state));
            player.RemoveHeldItem();
            player.SetFeedback("Dodano do kebaba: " + item.BuildSummary());
            ApplyCurrentColor();
            return;
        }

        if (!CanCreateDish())
        {
            player.SetFeedback("Potrzebujesz lawasza, upieczonego miesa i przygotowanych dodatkow.");
            return;
        }

        KitchenItem dish = BuildDish();
        if (!player.TryReceiveItem(dish))
        {
            player.SetFeedback("Masz juz cos w rece.");
            return;
        }

        ResetAssembly();
        player.SetFeedback("Kebab zostal zawiniety.");
        ApplyCurrentColor();
    }

    private void HandleDelivery(PlayerInteraction player)
    {
        if (!player.HasItemInHand)
        {
            player.SetFeedback("Podejdz z gotowym kebabem.");
            return;
        }

        if (!player.HeldItem.isDish)
        {
            player.SetFeedback("Klient czeka na gotowego kebaba, a nie luzny skladnik.");
            return;
        }

        if (OrderManager.Instance == null)
        {
            player.SetFeedback("Brak systemu zamowien.");
            return;
        }

        string message;
        if (OrderManager.Instance.TryDeliverDish(player.HeldItem, out message))
        {
            player.ClearHeldItem();
            DeliveryTrayDisplay.ShowServedKebab();

            // Efekty wizualne udanej dostawy
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.PlayDeliverySuccessEffect(transform.position);
                VFXManager.Instance.PlayMoneyEffect(transform.position);
            }
        }
        else
        {
            // Efekt wizualny nieudanej dostawy
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.PlayDeliveryFailEffect(transform.position);
            }
        }

        player.SetFeedback(message, 3.5f);
    }

    private bool CanBeProcessedHere(KitchenItem item, IngredientProcessState outputState)
    {
        if (item == null || item.isDish || item.state != IngredientProcessState.Raw)
        {
            return false;
        }

        if (outputState == IngredientProcessState.Cooked)
        {
            return item.ingredientKind == IngredientKind.Meat;
        }

        if (outputState == IngredientProcessState.Chopped)
        {
            return item.ingredientKind == IngredientKind.Tomato ||
                item.ingredientKind == IngredientKind.Onion ||
                item.ingredientKind == IngredientKind.Lettuce;
        }

        return false;
    }

    private bool CanBeAddedToAssembly(KitchenItem item)
    {
        if (item == null || item.isDish)
        {
            return false;
        }

        if (item.ingredientKind == IngredientKind.Meat)
        {
            return item.state == IngredientProcessState.Cooked;
        }

        if (item.ingredientKind == IngredientKind.Tomato ||
            item.ingredientKind == IngredientKind.Onion ||
            item.ingredientKind == IngredientKind.Lettuce)
        {
            return item.state == IngredientProcessState.Chopped;
        }

        if (item.ingredientKind == IngredientKind.GarlicSauce)
        {
            return item.state == IngredientProcessState.Raw;
        }

        return false;
    }

    private bool CanCreateDish()
    {
        if (!hasLavash || assemblyIngredients.Count == 0)
        {
            return false;
        }

        bool hasCookedMeat = false;
        foreach (PreparedIngredientData ingredient in assemblyIngredients)
        {
            if (ingredient.ingredientKind == IngredientKind.Meat &&
                ingredient.state == IngredientProcessState.Cooked)
            {
                hasCookedMeat = true;
                break;
            }
        }

        return hasCookedMeat;
    }

    private KitchenItem BuildDish()
    {
        KitchenItem dish = new KitchenItem
        {
            itemName = "Zawiniety kebab",
            ingredientKind = IngredientKind.Kebab,
            state = IngredientProcessState.Assembled,
            isDish = true
        };

        dish.contents.Add(new PreparedIngredientData(IngredientKind.Lavash, IngredientProcessState.Raw));
        foreach (PreparedIngredientData ingredient in assemblyIngredients)
        {
            dish.contents.Add(new PreparedIngredientData(ingredient.ingredientKind, ingredient.state));
        }

        return dish;
    }

    private void FinishProcessing()
    {
        isProcessing = false;
        if (stationType == KitchenStationType.Grill && IsDonerStation())
        {
            // Zatrzymaj parę z grilla po zakończeniu
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.StopSteamEffect(transform.position);
            }

            if (linkedMeatTray != null)
            {
                int batchSize = ShopManager.Instance != null
                    ? ShopManager.Instance.GetMeatBatchSize()
                    : preparedMeatBatchSize;
                linkedMeatTray.ReceivePreparedMeat(batchSize);
            }

            RefreshVisualState();
            return;
        }

        if (stationItem == null)
        {
            RefreshVisualState();
            return;
        }

        if (stationType == KitchenStationType.CuttingBoard)
        {
            stationItem.state = IngredientProcessState.Chopped;
        }
        else if (stationType == KitchenStationType.Grill)
        {
            stationItem.state = IngredientProcessState.Cooked;
        }

        RefreshVisualState();
    }

    private void ResetAssembly()
    {
        hasLavash = false;
        assemblyIngredients.Clear();
    }

    private void ReceivePreparedMeat(int servings)
    {
        preparedMeatServings = Mathf.Max(0, servings);
        RefreshVisualState();
    }

    private bool HasPreparedMeat()
    {
        return preparedMeatServings > 0;
    }

    private bool IsMeatTrayStation()
    {
        return stationType == KitchenStationType.IngredientSource &&
            sourceIngredient != null &&
            sourceIngredient.typSkladnika == IngredientKind.Meat;
    }

    private bool IsDonerStation()
    {
        return stationType == KitchenStationType.Grill && linkedMeatTray != null;
    }

    private void ApplyCurrentColor()
    {
        RefreshSpecialVisuals();

        if (visualRenderer == null)
        {
            return;
        }

        Color color = idleColor;
        if (isProcessing)
        {
            color = busyColor;
        }
        else if (stationType == KitchenStationType.Grill && linkedMeatTray != null && linkedMeatTray.HasPreparedMeat())
        {
            color = readyColor;
        }
        else if (IsMeatTrayStation() && preparedMeatServings > 0)
        {
            color = readyColor;
        }
        else if (stationItem != null || hasLavash || assemblyIngredients.Count > 0)
        {
            color = readyColor;
        }
        else if (sourceIngredient != null)
        {
            color = sourceIngredient.kolorDebug;
        }

        visualRenderer.material.color = color;
    }

    private void UpdateDynamicVisuals()
    {
        // 1. Czyszczenie starych modeli
        if (dynamicStationItemVisual != null) Destroy(dynamicStationItemVisual);
        if (dynamicLavashVisual != null) Destroy(dynamicLavashVisual);
        foreach (var vis in dynamicAssemblyVisuals) if (vis != null) Destroy(vis);
        dynamicAssemblyVisuals.Clear();

        // Jeśli stacja przygotowuje przedmiot, może być on w trakcie procesu, ale nadal go wyświetlamy
        if (stationItem != null)
        {
            Vector3 itemPos = new Vector3(0f, 0.38f, 0f);
            Vector3 itemRot = stationItem.isDish ? new Vector3(0f, 0f, 90f) : Vector3.zero;
            float itemSize = stationItem.isDish ? 0.3f : 0.25f;

            dynamicStationItemVisual = KitchenItemVisualFactory.CreateItemVisual(
                stationItem.ingredientKind, stationItem.state, stationItem.isDish,
                transform, itemPos, itemRot, itemSize);
        }
        else if (stationType == KitchenStationType.Assembly)
        {
            // Na stacji montażu wyświetlamy lawasz i składniki, dopóki nie zostanie zawinięty w stationItem
            if (hasLavash)
            {
                dynamicLavashVisual = KitchenItemVisualFactory.CreateItemVisual(
                    IngredientKind.Lavash, IngredientProcessState.Raw, false,
                    transform, new Vector3(0f, 0.36f, 0f), new Vector3(0f, 12f, 0f), 0.72f);
            }

            for (int i = 0; i < assemblyIngredients.Count; i++)
            {
                var ingredient = assemblyIngredients[i];
                // Rozmieść składniki lekko wokół środka na lawaszu
                float angle = i * (360f / Mathf.Max(1, assemblyIngredients.Count)) * Mathf.Deg2Rad;
                float radius = 0.08f;
                Vector3 offset = new Vector3(Mathf.Cos(angle) * radius, 0.02f + (i * 0.01f), Mathf.Sin(angle) * radius);

                GameObject ingVis = KitchenItemVisualFactory.CreateItemVisual(
                    ingredient.ingredientKind, ingredient.state, false,
                    transform, new Vector3(0f, 0.36f, 0f) + offset, Vector3.zero, 0.18f);

                if (ingVis != null) dynamicAssemblyVisuals.Add(ingVis);
            }
        }
    }

    private void RefreshSpecialVisuals()
    {
        if (!IsMeatTrayStation())
        {
            return;
        }

        if (meatVisual == null)
        {
            meatVisual = transform.Find("MeatVisual");
        }

        if (meatVisual != null)
        {
            meatVisual.gameObject.SetActive(preparedMeatServings > 0);
        }
    }

    private float GetShopSpeedMultiplier()
    {
        if (ShopManager.Instance == null)
        {
            return 1f;
        }

        return ShopManager.Instance.GetProcessingSpeedMultiplier(stationType);
    }
}
