using UnityEngine;

public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance { get; private set; }

    public event System.Action<float> OnBalanceChanged;

    [SerializeField] private float startingMoney = 100f;

    private float balance;
    private float totalEarned;
    [SerializeField] private float totalSpent;

    public float CurrentBalance => balance;
    public float TotalEarned => totalEarned;
    public float TotalSpent => totalSpent;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            balance = startingMoney;
            return;
        }

        Destroy(gameObject);
    }

    public void AddMoney(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        balance += amount;
        totalEarned += amount;
        OnBalanceChanged?.Invoke(balance);
        if (SaveManager.Instance != null) SaveManager.Instance.MarkDirty();
    }

    public bool SpendMoney(float amount)
    {
        if (amount <= 0f)
        {
            return false;
        }

        if (balance < amount)
        {
            Debug.Log("Brak wystarczajacej ilosci pieniedzy.");
            return false;
        }

        balance -= amount;
        totalSpent += amount;
        OnBalanceChanged?.Invoke(balance);
        if (SaveManager.Instance != null) SaveManager.Instance.MarkDirty();
        return true;
    }

    public void SetBalanceFromNetwork(float newBalance, float newTotalEarned)
    {
        balance = newBalance;
        totalEarned = newTotalEarned;
        OnBalanceChanged?.Invoke(balance);
    }

    public EconomySaveData CaptureState()
    {
        return new EconomySaveData
        {
            currentBalance = balance,
            totalEarned = totalEarned,
            totalSpent = totalSpent
        };
    }

    public void RestoreState(EconomySaveData saveData)
    {
        if (saveData == null)
        {
            return;
        }

        balance = Mathf.Max(0f, saveData.currentBalance);
        totalEarned = Mathf.Max(0f, saveData.totalEarned);
        totalSpent = Mathf.Max(0f, saveData.totalSpent);
        OnBalanceChanged?.Invoke(balance);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
