using System.Collections;
using System.IO;
using Unity.Netcode;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [SerializeField] private string saveFileName = "kebab-save.json";
    [SerializeField] private float autoSaveInterval = 15f;

    private float autoSaveTimer;

    public string SavePath => Path.Combine(Application.persistentDataPath, saveFileName);

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
                LoadGame();
            }
        }
    }

    private void OnServerStarted()
    {
        LoadGame();
    }

    private void Update()
    {
        autoSaveTimer += Time.deltaTime;
        if (autoSaveTimer >= autoSaveInterval)
        {
            SaveGame();
            autoSaveTimer = 0f;
        }
    }

    public void SaveGame()
    {
        // In multiplayer, only the host saves
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !NetworkManager.Singleton.IsServer)
        {
            return;
        }

        GameSaveData data = new GameSaveData();

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
        File.WriteAllText(SavePath, json);
        Debug.Log("Stan gry zapisany do pliku: " + SavePath);
    }

    public void LoadGame()
    {
        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer)
        {
            return;
        }

        if (!File.Exists(SavePath))
        {
            Debug.Log("Brak pliku zapisu. Start nowej sesji.");
            return;
        }

        string json = File.ReadAllText(SavePath);
        GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);
        if (data == null)
        {
            Debug.LogWarning("Nie udalo sie odczytac pliku zapisu.");
            return;
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

        Debug.Log("Stan gry wczytany z pliku.");
    }

    private void OnApplicationQuit()
    {
        SaveGame();
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
        }
    }
}

[System.Serializable]
public class GameSaveData
{
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
