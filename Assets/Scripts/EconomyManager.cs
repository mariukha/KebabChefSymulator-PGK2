using UnityEngine;

public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance { get; private set; }

    [SerializeField] private float startingMoney = 100f;
    [SerializeField] public float playerMoney = 100f;
    [SerializeField] private float totalEarned;
    [SerializeField] private float totalSpent;

    public float CurrentBalance => playerMoney;
    public float TotalEarned => totalEarned;
    public float TotalSpent => totalSpent;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            playerMoney = Mathf.Max(playerMoney, startingMoney);
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

        playerMoney += amount;
        totalEarned += amount;
        Debug.Log("Dodano pieniadze: " + amount + ". Aktualny stan konta: " + playerMoney);
    }

    public bool SpendMoney(float amount)
    {
        if (amount <= 0f)
        {
            return false;
        }

        if (playerMoney < amount)
        {
            Debug.Log("Brak wystarczajacej ilosci pieniedzy.");
            return false;
        }

        playerMoney -= amount;
        totalSpent += amount;
        Debug.Log("Wydano: " + amount + ". Pozostalo: " + playerMoney);
        return true;
    }

    public EconomySaveData CaptureState()
    {
        return new EconomySaveData
        {
            currentBalance = playerMoney,
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

        playerMoney = Mathf.Max(0f, saveData.currentBalance);
        totalEarned = Mathf.Max(0f, saveData.totalEarned);
        totalSpent = Mathf.Max(0f, saveData.totalSpent);
    }
}
