using Unity.Netcode;
using UnityEngine;

public class EconomyManager : NetworkBehaviour
{
    public static EconomyManager Instance { get; private set; }

    [SerializeField] private float startingMoney = 100f;

    public NetworkVariable<float> netBalance = new NetworkVariable<float>(
        100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<float> netTotalEarned = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [SerializeField] private float totalSpent;

    public float CurrentBalance => netBalance.Value;
    public float TotalEarned => netTotalEarned.Value;
    public float TotalSpent => totalSpent;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            return;
        }

        Destroy(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            netBalance.Value = Mathf.Max(netBalance.Value, startingMoney);
        }
    }

    public void AddMoney(float amount)
    {
        if (!IsServer)
        {
            return;
        }

        if (amount <= 0f)
        {
            return;
        }

        netBalance.Value += amount;
        netTotalEarned.Value += amount;
        Debug.Log("Dodano pieniadze: " + amount + ". Aktualny stan konta: " + netBalance.Value);
    }

    public bool SpendMoney(float amount)
    {
        if (!IsServer)
        {
            return false;
        }

        if (amount <= 0f)
        {
            return false;
        }

        if (netBalance.Value < amount)
        {
            Debug.Log("Brak wystarczajacej ilosci pieniedzy.");
            return false;
        }

        netBalance.Value -= amount;
        totalSpent += amount;
        Debug.Log("Wydano: " + amount + ". Pozostalo: " + netBalance.Value);
        return true;
    }

    public EconomySaveData CaptureState()
    {
        return new EconomySaveData
        {
            currentBalance = netBalance.Value,
            totalEarned = netTotalEarned.Value,
            totalSpent = totalSpent
        };
    }

    public void RestoreState(EconomySaveData saveData)
    {
        if (saveData == null)
        {
            return;
        }

        if (IsServer)
        {
            netBalance.Value = Mathf.Max(0f, saveData.currentBalance);
            netTotalEarned.Value = Mathf.Max(0f, saveData.totalEarned);
        }
        totalSpent = Mathf.Max(0f, saveData.totalSpent);
    }
}
