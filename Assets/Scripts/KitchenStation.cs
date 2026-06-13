using System.Collections.Generic;
using UnityEngine;

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

    private GameObject dynamicStationItemVisual;
    private GameObject dynamicLavashVisual;
    private List<GameObject> dynamicAssemblyVisuals = new List<GameObject>();

    private float pulsePhase;
    private Vector3 baseScale;
    private int lastVisualHash;

    public bool IsProcessing => isProcessing;
    public float ProcessEndTime => processEndTime;
    public int PreparedMeatServings => preparedMeatServings;
    public bool HasLavash => hasLavash;
    public int AssemblyCount => assemblyIngredients.Count;
    public KitchenItem StationItem => stationItem;
    public KitchenStationType StationType => stationType;
    public List<PreparedIngredientData> AssemblyIngredients => assemblyIngredients;

    public void SyncNetworkState(StationStateSnapshot snapshot)
    {
        isProcessing = snapshot.isProcessing;
        processEndTime = snapshot.isProcessing
            ? Time.time + snapshot.remainingProcessTime
            : 0f;
        preparedMeatServings = snapshot.preparedMeatServings;
        hasLavash = snapshot.hasLavash;
        stationItem = snapshot.stationItem.exists ? snapshot.stationItem.ToKitchenItem() : null;

        assemblyIngredients.Clear();
        int asmCount = Mathf.Min(snapshot.assemblyCount, 8);
        for (int i = 0; i < asmCount; i++)
        {
            snapshot.GetAssemblySlot(i, out IngredientKind kind, out IngredientProcessState state);
            assemblyIngredients.Add(new PreparedIngredientData(kind, state));
        }

        RefreshVisualState();
    }

    public void WriteAssemblyToSnapshot(ref StationStateSnapshot snapshot)
    {
        int count = Mathf.Min(assemblyIngredients.Count, 8);
        snapshot.assemblyCount = count;
        for (int i = 0; i < count; i++)
        {
            snapshot.SetAssemblySlot(i, assemblyIngredients[i].ingredientKind, assemblyIngredients[i].state);
        }
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

    private void UpdatePulseEffect()
    {
        bool shouldPulse = !isProcessing && (
            stationItem != null ||
            (IsMeatTrayStation() && preparedMeatServings > 0));

        if (!shouldPulse)
        {
            if (baseScale != Vector3.zero)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, baseScale, Time.deltaTime * 8f);
            }
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
            if (AudioManager.Instance != null) AudioManager.Instance.PlayPickupSound();
            PlayPickupFeedback(item);
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
            KitchenItem completedItem = stationItem;
            if (!player.TryReceiveItem(completedItem))
            {
                player.SetFeedback("Masz juz cos w rece.");
                return;
            }

            player.SetFeedback("Odebrano: " + completedItem.BuildSummary());
            stationItem = null;
            ApplyCurrentColor();
            if (AudioManager.Instance != null) AudioManager.Instance.PlayPickupSound();
            PlayPickupFeedback(completedItem);
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

        if (stationType == KitchenStationType.CuttingBoard)
        {
            if (VFXManager.Instance != null)
            {
                Color chopColor = stationItem != null
                    ? KitchenItemVisualFactory.GetIngredientColor(stationItem.ingredientKind, stationItem.state)
                    : new Color(0.6f, 0.8f, 0.3f);
                VFXManager.Instance.PlayChopEffect(transform.position, chopColor);
            }
            if (AudioManager.Instance != null) AudioManager.Instance.PlayChopSound();
        }

        if (stationType == KitchenStationType.Grill)
        {
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.PlaySteamEffect(transform.position);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayDropSound();
                AudioManager.Instance.StartGrillAmbient();
            }
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
        if (AudioManager.Instance != null) AudioManager.Instance.PlayPickupSound();
        PlayPickupFeedback(item);
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

        if (VFXManager.Instance != null)
        {
            VFXManager.Instance.PlayDonerSmokeEffect(transform.position);
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayChopSound();
            AudioManager.Instance.StartGrillAmbient();
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
                RefreshVisualState();
                PlayPlaceFeedback(item);
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
            RefreshVisualState();
            PlayPlaceFeedback(item);
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
        RefreshVisualState();
        if (VFXManager.Instance != null)
        {
            VFXManager.Instance.PlayWrapEffect(transform.position);
            VFXManager.Instance.PlayReadyEffect(transform.position, KitchenItemVisualFactory.GetIngredientColor(IngredientKind.Kebab, IngredientProcessState.Assembled));
        }
        if (AudioManager.Instance != null) AudioManager.Instance.PlayWrapSound();
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

            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.PlayDeliverySuccessEffect(transform.position);
                VFXManager.Instance.PlayMoneyEffect(transform.position);
            }

            if (AudioManager.Instance != null) AudioManager.Instance.PlayMoneySound();

            if (PostProcessSetup.Instance != null)
            {
                PostProcessSetup.Instance.PulseBloom(0.6f, 0.5f);
            }

            if (CameraEffects.Instance != null)
            {
                CameraEffects.Instance.ShakeCamera(0.08f, 0.3f);
                CameraEffects.Instance.FlashScreen(new Color(0.15f, 0.6f, 0.3f, 0.15f), 0.3f);
            }
        }
        else
        {
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.PlayDeliveryFailEffect(transform.position);
            }

            if (AudioManager.Instance != null) AudioManager.Instance.PlayFailSound();

            if (CameraEffects.Instance != null)
            {
                CameraEffects.Instance.ShakeCamera(0.06f, 0.25f);
                CameraEffects.Instance.FlashScreen(new Color(0.6f, 0.12f, 0.1f, 0.15f), 0.25f);
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
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.StopDonerSmokeEffect(transform.position);
            }
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StopGrillAmbient();
            }

            if (linkedMeatTray != null)
            {
                int batchSize = ShopManager.Instance != null
                    ? ShopManager.Instance.GetMeatBatchSize()
                    : preparedMeatBatchSize;
                linkedMeatTray.ReceivePreparedMeat(batchSize);
                if (VFXManager.Instance != null)
                {
                    VFXManager.Instance.PlayReadyEffect(
                        linkedMeatTray.transform.position,
                        KitchenItemVisualFactory.GetIngredientColor(IngredientKind.Meat, IngredientProcessState.Cooked));
                }
                if (AudioManager.Instance != null) AudioManager.Instance.PlayReadySound();
            }

            RefreshVisualState();
            PlayStationPulse(1.035f);
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
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.StopSteamEffect(transform.position);
            }
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StopGrillAmbient();
            }
        }

        RefreshVisualState();
        PlayReadyFeedback(stationItem);
        PlayStationPulse(1.035f);
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

    private void PlayPickupFeedback(KitchenItem item)
    {
        if (VFXManager.Instance == null || item == null)
        {
            return;
        }

        VFXManager.Instance.PlayPickupEffect(transform.position, GetItemFeedbackColor(item));
        PlayMicroCameraFeedback(0.012f, 0.08f);
        PlayStationPulse(1.018f);
    }

    private void PlayPlaceFeedback(KitchenItem item)
    {
        if (VFXManager.Instance != null)
        {
            VFXManager.Instance.PlayDropEffect(transform.position);
            if (item != null)
            {
                VFXManager.Instance.PlayPickupEffect(transform.position, GetItemFeedbackColor(item));
            }
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayDropSound();
        }

        PlayMicroCameraFeedback(0.010f, 0.07f);
        PlayStationPulse(1.015f);
    }

    private void PlayReadyFeedback(KitchenItem item)
    {
        if (item == null)
        {
            return;
        }

        if (VFXManager.Instance != null)
        {
            VFXManager.Instance.PlayReadyEffect(transform.position, GetItemFeedbackColor(item));
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayReadySound();
        }

        PlayMicroCameraFeedback(0.018f, 0.10f);
    }

    private void PlayStationPulse(float intensity)
    {
        if (ItemAnimator.Instance != null)
        {
            ItemAnimator.Instance.AnimatePop(gameObject, intensity);
        }
    }

    private void PlayMicroCameraFeedback(float intensity, float duration)
    {
        if (CameraEffects.Instance != null)
        {
            CameraEffects.Instance.ShakeCamera(intensity, duration);
        }
    }

    private Color GetItemFeedbackColor(KitchenItem item)
    {
        if (item == null)
        {
            return Color.white;
        }

        return KitchenItemVisualFactory.GetIngredientColor(item.ingredientKind, item.state);
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

    private int ComputeVisualHash()
    {
        int hash = 17;
        hash = hash * 31 + (isProcessing ? 1 : 0);
        if (stationItem != null)
        {
            hash = hash * 31 + (int)stationItem.ingredientKind;
            hash = hash * 31 + (int)stationItem.state;
            hash = hash * 31 + (stationItem.isDish ? 1 : 0);
        }
        hash = hash * 31 + (hasLavash ? 1 : 0);
        hash = hash * 31 + assemblyIngredients.Count;
        for (int i = 0; i < assemblyIngredients.Count; i++)
        {
            hash = hash * 31 + (int)assemblyIngredients[i].ingredientKind;
            hash = hash * 31 + (int)assemblyIngredients[i].state;
        }
        return hash;
    }

    private void UpdateDynamicVisuals()
    {
        int currentHash = ComputeVisualHash();
        if (currentHash == lastVisualHash) return;
        lastVisualHash = currentHash;

        if (dynamicStationItemVisual != null) Destroy(dynamicStationItemVisual);
        if (dynamicLavashVisual != null) Destroy(dynamicLavashVisual);
        foreach (var vis in dynamicAssemblyVisuals) if (vis != null) Destroy(vis);
        dynamicAssemblyVisuals.Clear();

        if (stationItem != null)
        {

            Vector3 itemPos = new Vector3(0f, 0.42f, 0f);
            Vector3 itemRot = Vector3.zero;
            float itemSize = 0.30f;

            if (stationItem.isDish)
            {
                itemRot = new Vector3(0f, 25f, 90f);
                itemSize = 0.35f;
            }
            else if (stationType == KitchenStationType.CuttingBoard)
            {

                itemRot = new Vector3(-5f, 30f, 0f);
                itemSize = 0.28f;
            }
            else if (stationType == KitchenStationType.Grill)
            {

                itemRot = new Vector3(0f, 15f, 0f);
                itemSize = 0.30f;
            }

            dynamicStationItemVisual = KitchenItemVisualFactory.CreateItemVisual(
                stationItem.ingredientKind, stationItem.state, stationItem.isDish,
                transform, itemPos, itemRot, itemSize);
        }
        else if (stationType == KitchenStationType.Assembly)
        {

            float baseHeight = 0.425f;
            Vector3 centerOffset = new Vector3(0.14f, 0f, 0f);

            if (hasLavash)
            {
                dynamicLavashVisual = KitchenItemVisualFactory.CreateItemVisual(
                    IngredientKind.Lavash, IngredientProcessState.Raw, false,
                    transform, centerOffset + new Vector3(0f, baseHeight, 0f), new Vector3(0f, 12f, 0f), 0.65f);
            }

            float layerHeight = baseHeight + 0.012f;

            Vector3[] slotPositions = new Vector3[]
            {
                new Vector3( 0.00f, 0f,  0.00f),
                new Vector3(-0.06f, 0f,  0.05f),
                new Vector3( 0.06f, 0f,  0.04f),
                new Vector3( 0.00f, 0f, -0.06f),
                new Vector3(-0.05f, 0f, -0.04f),
                new Vector3( 0.05f, 0f, -0.05f),
                new Vector3(-0.03f, 0f,  0.00f),
                new Vector3( 0.03f, 0f,  0.00f),
            };

            float[] slotRotations = new float[] { 0f, 25f, -15f, 45f, -30f, 60f, 10f, -45f };

            for (int i = 0; i < assemblyIngredients.Count; i++)
            {
                var ingredient = assemblyIngredients[i];
                int slotIndex = i % slotPositions.Length;

                float stackOffset = i * 0.005f;
                Vector3 pos = centerOffset + new Vector3(
                    slotPositions[slotIndex].x,
                    layerHeight + stackOffset,
                    slotPositions[slotIndex].z);

                if (ingredient.ingredientKind == IngredientKind.Lettuce || ingredient.ingredientKind == IngredientKind.GarlicSauce)
                {
                    int pieces = ingredient.ingredientKind == IngredientKind.GarlicSauce ? 4 : 6;
                    float spread = 0.035f;
                    float pSize = ingredient.ingredientKind == IngredientKind.GarlicSauce ? 0.03f : 0.02f;

                    GameObject scatteredVis = KitchenItemVisualFactory.CreateScatteredVisual(
                        ingredient.ingredientKind, ingredient.state,
                        transform, pos, pieces, spread, pSize);

                    if (scatteredVis != null) dynamicAssemblyVisuals.Add(scatteredVis);
                }
                else
                {

                    float ingSize = 0.16f;
                    Vector3 ingRot = new Vector3(0f, slotRotations[slotIndex], 0f);

                    switch (ingredient.ingredientKind)
                    {
                        case IngredientKind.Meat:
                            ingSize = 0.18f;
                            ingRot.x = -10f;
                            break;
                        case IngredientKind.Tomato:
                            ingSize = 0.14f;
                            pos.y += 0.02f;
                            break;
                        case IngredientKind.Onion:
                            ingSize = 0.13f;
                            pos.y += 0.02f;
                            break;
                    }

                    GameObject ingVis = KitchenItemVisualFactory.CreateItemVisual(
                        ingredient.ingredientKind, ingredient.state, false,
                        transform, pos, ingRot, ingSize);

                    if (ingVis != null) dynamicAssemblyVisuals.Add(ingVis);
                }
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
