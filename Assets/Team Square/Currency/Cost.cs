using UnityEngine;

[System.Serializable]
public struct Cost
{
    public CurrencyAsset currencyAsset;
    [SerializeField] private ulong[] m_amount;
    
    public ulong GetAmount(int index)
    {
        if (index < 0) //Used to return 0 (free) in the case of free buildings
            return 0;
        
        if (m_amount == null || m_amount.Length == 0)
        {
            Debug.LogWarning($"Cost.GetAmount: m_amount is null or empty for currency {currencyAsset}");
            return 0;
        }
        if (index >= m_amount.Length)
            return m_amount[m_amount.Length - 1];
        return m_amount[index];
    }

    // Allow setting the amounts from editor scripts
    public void SetAmounts(ulong[] amounts)
    {
        m_amount = amounts;
    }

    public void SetAmountAt(int index, ulong value)
    {
        if (m_amount == null || index < 0) return;
        if (index >= m_amount.Length)
        {
            // expand array to fit
            ulong[] newArr = new ulong[index + 1];
            for (int i = 0; i < m_amount.Length; i++) newArr[i] = m_amount[i];
            m_amount = newArr;
        }
        m_amount[index] = value;
    }
}