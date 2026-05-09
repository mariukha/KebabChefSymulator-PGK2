using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public enum UpgradeType
{
    GrillSpeed,
    CuttingSpeed,
    RewardBonus,
    OrderTime,
    MeatBatchSize
}

[Serializable]
public class UpgradeDefinition
{
    public UpgradeType type;
    public string displayName;
    public string description;
    public string icon;
    public int maxLevel;
    public float baseCost;
    public float costScaling;
    public Color accentColor;
    public float[] effectValues;

    public float GetCostForLevel(int nextLevel)
    {
        if (nextLevel <= 0)
        {
            return baseCost;
        }

        return Mathf.Round(baseCost * Mathf.Pow(costScaling, nextLevel));
    }

    public string GetEffectDescription(int currentLevel)
    {
        if (effectValues == null || effectValues.Length == 0)
        {
            return string.Empty;
        }

        if (currentLevel >= maxLevel)
        {
            return "MAX";
        }

        int nextLevel = Mathf.Clamp(currentLevel + 1, 0, effectValues.Length - 1);

        switch (type)
        {
            case UpgradeType.GrillSpeed:
            case UpgradeType.CuttingSpeed:
                int percentFaster = Mathf.RoundToInt((1f - effectValues[nextLevel]) * 100f);
                return "-" + percentFaster + "% czasu";
            case UpgradeType.RewardBonus:
                int percentBonus = Mathf.RoundToInt(effectValues[nextLevel] * 100f);
                return "+" + percentBonus + "% nagrody";
            case UpgradeType.OrderTime:
                return "+" + effectValues[nextLevel] + " s";
            case UpgradeType.MeatBatchSize:
                return effectValues[nextLevel] + " porcji";
            default:
                return effectValues[nextLevel].ToString("F1");
        }
    }
}

[Serializable]
public class ShopSaveData
{
    public List<UpgradeLevelEntry> upgradeLevels = new List<UpgradeLevelEntry>();
    public int totalUpgradesPurchased;
}

[Serializable]
public class UpgradeLevelEntry
{
    public string upgradeType;
    public int level;
}

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    private readonly Dictionary<UpgradeType, UpgradeDefinition> definitions =
        new Dictionary<UpgradeType, UpgradeDefinition>();

    public NetworkVariable<int> netGrillSpeedLevel = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> netCuttingSpeedLevel = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> netRewardBonusLevel = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> netOrderTimeLevel = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> netMeatBatchSizeLevel = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [SerializeField] private int totalUpgradesPurchased;

    public int TotalUpgradesPurchased => totalUpgradesPurchased;

    public event Action<UpgradeType, int> OnUpgradePurchased;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeDefinitions();
            return;
        }

        Destroy(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsServer)
        {
            netGrillSpeedLevel.OnValueChanged += OnGrillSpeedLevelChanged;
            netCuttingSpeedLevel.OnValueChanged += OnCuttingSpeedLevelChanged;
            netRewardBonusLevel.OnValueChanged += OnRewardBonusLevelChanged;
            netOrderTimeLevel.OnValueChanged += OnOrderTimeLevelChanged;
            netMeatBatchSizeLevel.OnValueChanged += OnMeatBatchSizeLevelChanged;
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (!IsServer)
        {
            netGrillSpeedLevel.OnValueChanged -= OnGrillSpeedLevelChanged;
            netCuttingSpeedLevel.OnValueChanged -= OnCuttingSpeedLevelChanged;
            netRewardBonusLevel.OnValueChanged -= OnRewardBonusLevelChanged;
            netOrderTimeLevel.OnValueChanged -= OnOrderTimeLevelChanged;
            netMeatBatchSizeLevel.OnValueChanged -= OnMeatBatchSizeLevelChanged;
        }
    }

    // Cached delegates for proper unsubscription (lambdas create new instances on each call)
    private void OnGrillSpeedLevelChanged(int oldVal, int newVal) => HandleLevelChanged(UpgradeType.GrillSpeed, newVal);
    private void OnCuttingSpeedLevelChanged(int oldVal, int newVal) => HandleLevelChanged(UpgradeType.CuttingSpeed, newVal);
    private void OnRewardBonusLevelChanged(int oldVal, int newVal) => HandleLevelChanged(UpgradeType.RewardBonus, newVal);
    private void OnOrderTimeLevelChanged(int oldVal, int newVal) => HandleLevelChanged(UpgradeType.OrderTime, newVal);
    private void OnMeatBatchSizeLevelChanged(int oldVal, int newVal) => HandleLevelChanged(UpgradeType.MeatBatchSize, newVal);

    private void HandleLevelChanged(UpgradeType type, int newLevel)
    {
        OnUpgradePurchased?.Invoke(type, newLevel);
    }

    private void InitializeDefinitions()
    {
        RegisterDefinition(new UpgradeDefinition
        {
            type = UpgradeType.GrillSpeed,
            displayName = "Szybszy Doner",
            description = "Przyspiesza scinanie miesa z donera.",
            icon = "\u2694",
            maxLevel = 4,
            baseCost = 45f,
            costScaling = 1.9f,
            accentColor = new Color(0.92f, 0.55f, 0.18f),
            effectValues = new float[] { 1.0f, 0.82f, 0.66f, 0.52f, 0.40f }
        });

        RegisterDefinition(new UpgradeDefinition
        {
            type = UpgradeType.CuttingSpeed,
            displayName = "Szybsze Krojenie",
            description = "Przyspiesza krojenie warzyw na desce.",
            icon = "\u2702",
            maxLevel = 4,
            baseCost = 35f,
            costScaling = 1.85f,
            accentColor = new Color(0.25f, 0.78f, 0.55f),
            effectValues = new float[] { 1.0f, 0.80f, 0.64f, 0.50f, 0.38f }
        });

        RegisterDefinition(new UpgradeDefinition
        {
            type = UpgradeType.RewardBonus,
            displayName = "Lepsza Reputacja",
            description = "Zwieksza nagrode za kazde zamowienie.",
            icon = "\u2605",
            maxLevel = 5,
            baseCost = 60f,
            costScaling = 2.0f,
            accentColor = new Color(1f, 0.82f, 0.28f),
            effectValues = new float[] { 0f, 0.10f, 0.22f, 0.36f, 0.52f, 0.70f }
        });

        RegisterDefinition(new UpgradeDefinition
        {
            type = UpgradeType.OrderTime,
            displayName = "Wiecej Czasu",
            description = "Dodaje czas na realizacje zamowien.",
            icon = "\u23F0",
            maxLevel = 4,
            baseCost = 40f,
            costScaling = 1.8f,
            accentColor = new Color(0.40f, 0.68f, 0.95f),
            effectValues = new float[] { 0f, 12f, 25f, 40f, 60f }
        });

        RegisterDefinition(new UpgradeDefinition
        {
            type = UpgradeType.MeatBatchSize,
            displayName = "Wieksza Porcja",
            description = "Wiecej porcji miesa z jednego sciecia.",
            icon = "\u2725",
            maxLevel = 3,
            baseCost = 55f,
            costScaling = 2.1f,
            accentColor = new Color(0.82f, 0.32f, 0.22f),
            effectValues = new float[] { 3f, 4f, 6f, 8f }
        });
    }

    private void RegisterDefinition(UpgradeDefinition definition)
    {
        definitions[definition.type] = definition;
    }

    public List<UpgradeDefinition> GetAllDefinitions()
    {
        List<UpgradeDefinition> list = new List<UpgradeDefinition>();
        list.Add(GetDefinition(UpgradeType.GrillSpeed));
        list.Add(GetDefinition(UpgradeType.CuttingSpeed));
        list.Add(GetDefinition(UpgradeType.RewardBonus));
        list.Add(GetDefinition(UpgradeType.OrderTime));
        list.Add(GetDefinition(UpgradeType.MeatBatchSize));
        return list;
    }

    public UpgradeDefinition GetDefinition(UpgradeType type)
    {
        UpgradeDefinition definition;
        definitions.TryGetValue(type, out definition);
        return definition;
    }

    public int GetUpgradeLevel(UpgradeType type)
    {
        switch (type)
        {
            case UpgradeType.GrillSpeed: return netGrillSpeedLevel.Value;
            case UpgradeType.CuttingSpeed: return netCuttingSpeedLevel.Value;
            case UpgradeType.RewardBonus: return netRewardBonusLevel.Value;
            case UpgradeType.OrderTime: return netOrderTimeLevel.Value;
            case UpgradeType.MeatBatchSize: return netMeatBatchSizeLevel.Value;
            default: return 0;
        }
    }

    public bool IsMaxLevel(UpgradeType type)
    {
        UpgradeDefinition definition = GetDefinition(type);
        if (definition == null)
        {
            return true;
        }

        return GetUpgradeLevel(type) >= definition.maxLevel;
    }

    public float GetNextUpgradeCost(UpgradeType type)
    {
        UpgradeDefinition definition = GetDefinition(type);
        if (definition == null)
        {
            return float.MaxValue;
        }

        int currentLevel = GetUpgradeLevel(type);
        if (currentLevel >= definition.maxLevel)
        {
            return 0f;
        }

        return definition.GetCostForLevel(currentLevel);
    }

    public bool CanAffordUpgrade(UpgradeType type)
    {
        if (IsMaxLevel(type))
        {
            return false;
        }

        if (EconomyManager.Instance == null)
        {
            return false;
        }

        return EconomyManager.Instance.CurrentBalance >= GetNextUpgradeCost(type);
    }

    [ServerRpc(RequireOwnership = false)]
    public void PurchaseUpgradeServerRpc(UpgradeType type, ulong clientId)
    {
        if (IsMaxLevel(type))
        {
            SendPurchaseResultClientRpc(false, type, clientId, RpcTargetSingle(clientId));
            return;
        }

        if (EconomyManager.Instance == null)
        {
            SendPurchaseResultClientRpc(false, type, clientId, RpcTargetSingle(clientId));
            return;
        }

        float cost = GetNextUpgradeCost(type);
        if (!EconomyManager.Instance.SpendMoney(cost))
        {
            SendPurchaseResultClientRpc(false, type, clientId, RpcTargetSingle(clientId));
            return;
        }

        int newLevel = GetUpgradeLevel(type) + 1;

        switch (type)
        {
            case UpgradeType.GrillSpeed: netGrillSpeedLevel.Value = newLevel; break;
            case UpgradeType.CuttingSpeed: netCuttingSpeedLevel.Value = newLevel; break;
            case UpgradeType.RewardBonus: netRewardBonusLevel.Value = newLevel; break;
            case UpgradeType.OrderTime: netOrderTimeLevel.Value = newLevel; break;
            case UpgradeType.MeatBatchSize: netMeatBatchSizeLevel.Value = newLevel; break;
        }

        totalUpgradesPurchased++;

        Debug.Log("Zakupiono ulepszenie: " + type + " -> poziom " + newLevel + " za " + cost + " zl.");

        HandleLevelChanged(type, newLevel);
        SaveManager.Instance?.SaveGame();

        SendPurchaseResultClientRpc(true, type, clientId, RpcTargetSingle(clientId));
    }

    private ClientRpcParams RpcTargetSingle(ulong clientId)
    {
        return new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        };
    }

    [ClientRpc]
    private void SendPurchaseResultClientRpc(bool success, UpgradeType type, ulong clientId, ClientRpcParams clientRpcParams = default)
    {
        ShopUI ui = FindFirstObjectByType<ShopUI>();
        if (ui != null && NetworkManager.Singleton.LocalClientId == clientId)
        {
            ui.HandlePurchaseResult(success, type);
        }
    }

    public float GetProcessingSpeedMultiplier(KitchenStationType stationType)
    {
        UpgradeType upgradeType;
        if (stationType == KitchenStationType.Grill)
        {
            upgradeType = UpgradeType.GrillSpeed;
        }
        else if (stationType == KitchenStationType.CuttingBoard)
        {
            upgradeType = UpgradeType.CuttingSpeed;
        }
        else
        {
            return 1f;
        }

        return GetEffectValue(upgradeType, 1f);
    }

    public float GetRewardMultiplier()
    {
        return 1f + GetEffectValue(UpgradeType.RewardBonus, 0f);
    }

    public float GetOrderTimeBonus()
    {
        return GetEffectValue(UpgradeType.OrderTime, 0f);
    }

    public int GetMeatBatchSize()
    {
        return Mathf.RoundToInt(GetEffectValue(UpgradeType.MeatBatchSize, 3f));
    }

    private float GetEffectValue(UpgradeType type, float defaultValue)
    {
        UpgradeDefinition definition = GetDefinition(type);
        if (definition == null || definition.effectValues == null)
        {
            return defaultValue;
        }

        int level = GetUpgradeLevel(type);
        if (level < 0 || level >= definition.effectValues.Length)
        {
            return defaultValue;
        }

        return definition.effectValues[level];
    }

    public ShopSaveData CaptureState()
    {
        ShopSaveData data = new ShopSaveData();
        data.totalUpgradesPurchased = totalUpgradesPurchased;

        foreach (UpgradeType type in Enum.GetValues(typeof(UpgradeType)))
        {
            int level = GetUpgradeLevel(type);
            if (level <= 0)
            {
                continue;
            }

            data.upgradeLevels.Add(new UpgradeLevelEntry
            {
                upgradeType = type.ToString(),
                level = level
            });
        }

        return data;
    }

    public void RestoreState(ShopSaveData data)
    {
        if (data == null || !IsServer)
        {
            return;
        }

        totalUpgradesPurchased = Mathf.Max(0, data.totalUpgradesPurchased);

        foreach (UpgradeLevelEntry entry in data.upgradeLevels)
        {
            UpgradeType parsedType;
            try
            {
                parsedType = (UpgradeType)Enum.Parse(typeof(UpgradeType), entry.upgradeType);
            }
            catch
            {
                continue;
            }

            UpgradeDefinition definition = GetDefinition(parsedType);
            int maxAllowed = definition != null ? definition.maxLevel : 10;
            int restoredLevel = Mathf.Clamp(entry.level, 0, maxAllowed);
            
            switch (parsedType)
            {
                case UpgradeType.GrillSpeed: netGrillSpeedLevel.Value = restoredLevel; break;
                case UpgradeType.CuttingSpeed: netCuttingSpeedLevel.Value = restoredLevel; break;
                case UpgradeType.RewardBonus: netRewardBonusLevel.Value = restoredLevel; break;
                case UpgradeType.OrderTime: netOrderTimeLevel.Value = restoredLevel; break;
                case UpgradeType.MeatBatchSize: netMeatBatchSizeLevel.Value = restoredLevel; break;
            }
        }
    }
}
