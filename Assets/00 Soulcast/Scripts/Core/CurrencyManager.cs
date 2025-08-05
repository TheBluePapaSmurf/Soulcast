// Core/CurrencyManager.cs
using UnityEngine;
using System;

public class CurrencyManager : MonoBehaviour
{
    [Header("Currency Amounts")]
    [SerializeField] private int soulCoins = 6000;
    [SerializeField] private int crystals = 100; // Premium currency

    [Header("Settings")]
    public int maxSoulCoins = 999999;
    public int maxCrystals = 9999;

    // Events voor UI updates
    public static event Action<int> OnSoulCoinsChanged;
    public static event Action<int> OnCrystalsChanged;

    public static CurrencyManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ========== SOUL COINS ==========

    public void AddSoulCoins(int amount)
    {
        if (amount <= 0) return;

        soulCoins = Mathf.Min(soulCoins + amount, maxSoulCoins);
        OnSoulCoinsChanged?.Invoke(soulCoins);

        Debug.Log($"💰 Added {amount} Soul Coins. Total: {soulCoins}");
        SaveManager.Instance?.AutoSave();
    }

    public bool SpendSoulCoins(int amount)
    {
        if (amount <= 0) return false;

        if (soulCoins >= amount)
        {
            soulCoins -= amount;
            OnSoulCoinsChanged?.Invoke(soulCoins);

            Debug.Log($"💸 Spent {amount} Soul Coins. Remaining: {soulCoins}");
            SaveManager.Instance?.AutoSave();
            return true;
        }

        Debug.LogWarning($"❌ Not enough Soul Coins! Have {soulCoins}, need {amount}");
        return false;
    }

    public int GetSoulCoins() => soulCoins;
    public bool CanAffordSoulCoins(int amount) => soulCoins >= amount;

    // ========== CRYSTALS (Premium Currency) ==========

    public void AddCrystals(int amount)
    {
        if (amount <= 0) return;

        crystals = Mathf.Min(crystals + amount, maxCrystals);
        OnCrystalsChanged?.Invoke(crystals);

        Debug.Log($"💎 Added {amount} Crystals. Total: {crystals}");
        SaveManager.Instance?.AutoSave();
    }

    public bool SpendCrystals(int amount)
    {
        if (amount <= 0) return false;

        if (crystals >= amount)
        {
            crystals -= amount;
            OnCrystalsChanged?.Invoke(crystals);

            Debug.Log($"💎 Spent {amount} Crystals. Remaining: {crystals}");
            SaveManager.Instance?.AutoSave();
            return true;
        }

        Debug.LogWarning($"❌ Not enough Crystals! Have {crystals}, need {amount}");
        return false;
    }

    public int GetCrystals() => crystals;
    public bool CanAffordCrystals(int amount) => crystals >= amount;

    // ========== SAVE/LOAD ==========

    public void SaveCurrency()
    {
        ES3.Save("SoulCoins", soulCoins, SaveManager.SAVE_FILE);
        ES3.Save("Crystals", crystals, SaveManager.SAVE_FILE);
        Debug.Log($"💾 Saved currency: {soulCoins} Soul Coins, {crystals} Crystals");
    }

    public void LoadCurrency()
    {
        soulCoins = ES3.Load("SoulCoins", SaveManager.SAVE_FILE, 6000);
        crystals = ES3.Load("Crystals", SaveManager.SAVE_FILE, 100);

        // Trigger events to update UI
        OnSoulCoinsChanged?.Invoke(soulCoins);
        OnCrystalsChanged?.Invoke(crystals);

        Debug.Log($"💰 Loaded currency: {soulCoins} Soul Coins, {crystals} Crystals");
    }

    // ========== UTILITY ==========

    public string GetFormattedSoulCoins()
    {
        if (soulCoins >= 1000000)
            return $"{soulCoins / 1000000f:F1}M";
        else if (soulCoins >= 1000)
            return $"{soulCoins / 1000f:F1}K";
        else
            return soulCoins.ToString();
    }

    public string GetFormattedCrystals()
    {
        if (crystals >= 1000)
            return $"{crystals / 1000f:F1}K";
        else
            return crystals.ToString();
    }
}
