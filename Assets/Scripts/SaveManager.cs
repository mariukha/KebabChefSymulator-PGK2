using System.IO;
using Unity.Netcode;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private enum SaveSlot
    {
        Solo,
        Online
    }

    [SerializeField] private float autoSaveInterval = 15f;

    private const string SoloSaveFileName = "kebab-save-solo.json";
    private const string OnlineSaveFileName = "kebab-save-online.json";

    private float autoSaveTimer;
    private bool isDirty = false;
    private bool hasActiveSession;
    private SaveSlot currentSaveSlot = SaveSlot.Online;

    private string CurrentSaveFileName => currentSaveSlot == SaveSlot.Solo ? SoloSaveFileName : OnlineSaveFileName;
    private string CurrentSaveSlotName => currentSaveSlot == SaveSlot.Solo ? "solo" : "online";

    public string SavePath => Path.Combine(Application.persistentDataPath, CurrentSaveFileName);

    public void UseSaveSlot(bool isSolo)
    {
        currentSaveSlot = isSolo ? SaveSlot.Solo : SaveSlot.Online;
    }

    public void MarkSessionEnded()
    {
        hasActiveSession = false;
        isDirty = false;
        autoSaveTimer = 0f;
    }

    public void MarkDirty()
    {
        isDirty = true;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            return;
        }

        Destroy(gameObject);
    }

    private void Start()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted += OnServerStarted;
            if (NetworkManager.Singleton.IsServer)
            {
                hasActiveSession = true;
                LoadGame();
            }
        }
    }

    private void OnServerStarted()
    {
        hasActiveSession = true;
        LoadGame();
    }

    private void Update()
    {
        if (!hasActiveSession)
        {
            return;
        }

        autoSaveTimer += Time.deltaTime;
        if (autoSaveTimer >= autoSaveInterval)
        {
            if (isDirty)
            {
                SaveGame();
                isDirty = false;
            }
            autoSaveTimer = 0f;
        }
    }

    public void SaveGame()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !NetworkManager.Singleton.IsServer)
        {
            return;
        }

        try
        {
            GameSaveData data = new GameSaveData();
            string path = SavePath;

            if (EconomyManager.Instance != null)
            {
                data.economy = EconomyManager.Instance.CaptureState();
            }

            if (OrderManager.Instance != null)
            {
                data.orderProgress = OrderManager.Instance.CaptureProgress();
            }

            if (ShopManager.Instance != null)
            {
                data.shop = ShopManager.Instance.CaptureState();
            }

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(path, json);
            Debug.Log("Stan gry zapisany (" + CurrentSaveSlotName + "): " + path);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[SaveManager] Blad zapisu: " + e.Message);
        }
    }

    public void LoadGame()
    {
        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer)
        {
            return;
        }

        string path = SavePath;
        if (!File.Exists(path))
        {
            Debug.Log("Brak pliku zapisu (" + CurrentSaveSlotName + "). Start nowej sesji.");
            return;
        }

        try
        {
            string json = File.ReadAllText(path);
            GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);
            if (data == null)
            {
                Debug.LogWarning("Nie udalo sie odczytac pliku zapisu.");
                return;
            }

            if (data.economy != null)
            {
                data.economy.currentBalance = Mathf.Max(0f, data.economy.currentBalance);
                data.economy.totalEarned = Mathf.Max(0f, data.economy.totalEarned);
                data.economy.totalSpent = Mathf.Max(0f, data.economy.totalSpent);
            }

            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.RestoreState(data.economy);
            }

            if (OrderManager.Instance != null)
            {
                OrderManager.Instance.RestoreProgress(data.orderProgress);
            }

            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.RestoreState(data.shop);
            }

            Debug.Log("Stan gry wczytany (" + CurrentSaveSlotName + "): " + path);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[SaveManager] Blad wczytywania: " + e.Message);
        }
    }

    private void OnApplicationQuit()
    {
        if (hasActiveSession)
        {
            SaveGame();
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }
}

[System.Serializable]
public class GameSaveData
{
    public int version = 1;
    public EconomySaveData economy = new EconomySaveData();
    public OrderProgressSaveData orderProgress = new OrderProgressSaveData();
    public ShopSaveData shop = new ShopSaveData();
}

[System.Serializable]
public class EconomySaveData
{
    public float currentBalance = 100f;
    public float totalEarned;
    public float totalSpent;
}

[System.Serializable]
public class OrderProgressSaveData
{
    public int completedOrders;
    public int failedOrders;
    public float remainingOrderTime;
    public string lastOrderMessage = string.Empty;
    public OrderSaveData activeOrder;
}

[System.Serializable]
public class OrderSaveData
{
    public string orderId;
    public string clientName;
    public string orderName;
    public float timeLimit;
    public float reward;
    public System.Collections.Generic.List<IngredientRequirement> requirements =
        new System.Collections.Generic.List<IngredientRequirement>();
}
