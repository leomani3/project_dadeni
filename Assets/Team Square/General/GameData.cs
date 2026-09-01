using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Utils;

public struct CurrencyAmount
{
	public CurrencyAsset currencyAsset;
	public double amount;
}

[CreateAssetMenu(menuName = "Config/GameData")]
public class GameData : ScriptableObject
{
	[ES3NonSerializable] public Action<CurrencyAsset, double> onCurrencyChanged;
	[ES3NonSerializable] public Action<CurrencyAsset, double> onCurrencyAdded;
	[ES3NonSerializable] public Action<CurrencyAsset> onNotEnoughCurrency;
	[ES3NonSerializable] public Action<int> onNodeLevelUp;
	[ES3NonSerializable] public Action OnResetData;

	private static GameData _instance;
	public static GameData Instance => _instance ?? Load();

	public void Init()
	{
#if UNITY_EDITOR
		if (GameConfig.Instance.cheatSettings.startResetData)
		{
			ResetGameData();
		}
#endif
	}

	[Button]
	public void Save()
	{
		if (GameConfig.Instance.cheatSettings.preventSave)
			return;
			
		ES3.Save("GameData", this);
		isDirty = false;
	}

	private static GameData Load()
	{
		_instance = Resources.Load<GameData>("GameData");
		if (ES3.KeyExists("GameData"))
			ES3.LoadInto("GameData", _instance);
		else
			ResetGameData();

		return _instance;
	}


	// ---------------------------------------------------------
	[ES3NonSerializable] public bool isDirty = false;

	#region General

	public bool firstGameLaunch = true;
	public bool runExistInSave;
	public bool runStarted;
	
	public static void ResetGameData()
	{
		Instance.ResetCurrencies();
		Instance.teckTreeNodesLevels = new SerializableDictionary<string, int>();
		Instance.firstGameLaunch = true;
		Instance.runStarted = false;
		Instance.completedFtueSteps.Clear();

		Instance.runExistInSave = false;

		Instance.currentQuestIndex = 0;
		Instance.completedQuestIds = new List<string>();
		Instance.questObjectiveSnapshots = new SerializableDictionary<string, double>();

		Instance.trackedValues = new SerializableDictionary<TrackedValueType, TrackedValue>();
		foreach (TrackedValueType trackedValueType in Enum.GetValues(typeof(TrackedValueType)))
		{
			if (trackedValueType != TrackedValueType.NONE)
				Instance.trackedValues.Add(trackedValueType, new TrackedValue(trackedValueType, 0));
		}

		Instance.Save();
		Instance.OnResetData?.Invoke();
	}

	public void ResetRun()
	{
		ResetRunCurrencies();
		
		runStarted = false;

		Save();
	}

	#endregion
	
	#region Currencies
	[Header("Currencies")]
	public SerializableDictionary<Currency, double> currencies = new SerializableDictionary<Currency, double>();

	[Button]
	public void AddCurrency(CurrencyAsset _currencyAsset, double amount, bool forceSave = false)
	{
		Currency currency = _currencyAsset.Currency;
		if (_currencyAsset == null) return;

		if (currencies == null)
			currencies = new SerializableDictionary<Currency, double>();
		
		if (currencies.ContainsKey(currency))
		{
			currencies[currency] += amount;
		}
		else
		{
			currencies[currency] = amount;
		}

		foreach (TrackedValueType trackedValueType in _currencyAsset.trackedValuesWithCurrencyGained)
			IncrementTrackedValue(trackedValueType, amount);
		
		onCurrencyChanged?.Invoke(_currencyAsset, currencies[currency]);
		onCurrencyAdded?.Invoke(_currencyAsset, amount);

		isDirty = true;
	}

	public bool SpendCurrency(CurrencyAsset _currencyAsset, double amount)
	{
		Currency currency = _currencyAsset.Currency;
		if (_currencyAsset == null || currencies == null) return false;

		if (currencies.ContainsKey(currency) || GameConfig.Instance.cheatSettings.noCurrencyRequired)
		{
			double current = currencies[currency];
			double newValue = current > amount ? current - amount : 0UL;
			currencies[currency] = newValue;

			onCurrencyChanged?.Invoke(_currencyAsset, currencies[currency]);
			Save();
			return true;
		}
		return false;
	}

	public bool HasEnoughCurrency(CurrencyAsset _currencyAsset, double _amount, bool _feedBack = false)
	{
		if (GameConfig.Instance.cheatSettings.noCurrencyRequired)
			return true;

		Currency currency = _currencyAsset.Currency;

		if (_amount == 0)
			return true;

		if (_currencyAsset == null || currencies == null || !currencies.ContainsKey(currency) || currencies[currency] < _amount)
		{
			if (_feedBack)
				onNotEnoughCurrency?.Invoke(_currencyAsset);
			return false;
		}

		return true;
	}

	public double GetInventoryAmount(CurrencyAsset _currencyAsset)
	{
		Currency currency = _currencyAsset.Currency;
		if (_currencyAsset == null || currencies == null) return 0;
		return currencies.ContainsKey(currency) ? currencies[currency] : 0;
	}

	public void DepleteCurrency(CurrencyAsset _currencyAsset)
	{
		Currency currency = _currencyAsset.Currency;
		if (currencies.ContainsKey(currency))
		{
			currencies[currency] = 0;
			onCurrencyChanged?.Invoke(_currencyAsset, 0);
			Save();
		}
	}
	
	public void ResetCurrencies()
	{
		Instance.currencies = new SerializableDictionary<Currency, double>();
		foreach (CurrencyAsset currencyAsset in GameAssets.Instance.currencyAssets)
		{
			if (!Instance.currencies.ContainsKey(currencyAsset.Currency))
				Instance.currencies.Add(currencyAsset.Currency, 0);
		}
	}

	public void ResetRunCurrencies()
	{
		foreach (CurrencyAsset currencyAsset in GameConfig.Instance.gameSettings.resetedCurrency)
		{
			if (Instance.currencies.ContainsKey(currencyAsset.Currency))
				Instance.currencies[currencyAsset.Currency] = 0;
		}
	}
	
	#endregion

	#region FTUE

	[Header("FTUE")]
	public List<string> completedFtueSteps = new List<string>();

	#endregion

	#region Quest Objective Snapshots
	[Header("Quest")]
	public int currentQuestIndex = 0;
	public List<string> completedQuestIds = new List<string>();
	public SerializableDictionary<string, double> questObjectiveSnapshots = new SerializableDictionary<string, double>();
	
	public void SetQuestObjectiveSnapshot(string snapshotKey, double value)
	{
		if (questObjectiveSnapshots.ContainsKey(snapshotKey))
			questObjectiveSnapshots[snapshotKey] = value;
		else
			questObjectiveSnapshots.Add(snapshotKey, value);
	}
	
	public double GetQuestObjectiveSnapshot(string snapshotKey)
	{
		if (questObjectiveSnapshots.ContainsKey(snapshotKey))
			return questObjectiveSnapshots[snapshotKey];
		
		return 0;
	}
	
	public void ClearQuestObjectiveSnapshots()
	{
		questObjectiveSnapshots.Clear();
	}
	
	#endregion

	#region Tracked Values

	[Header("Value Tracker")]
	public SerializableDictionary<TrackedValueType, TrackedValue> trackedValues = new SerializableDictionary<TrackedValueType, TrackedValue>();
	[ES3NonSerializable] public Action<TrackedValueType, double> OnTrackedValueChanged;
	
	public void IncrementTrackedValue(TrackedValueType type, double amount = 1f)
	{
		if (trackedValues.ContainsKey(type))
			trackedValues[type].Increment(amount);
		else
			trackedValues.Add(type, new TrackedValue(type, amount));
		
		OnTrackedValueChanged?.Invoke(type, trackedValues[type].value);
	}

	public void DecrementTrackedValue(TrackedValueType type, double amount = 1f)
	{
		if (trackedValues.ContainsKey(type))
		{
			trackedValues[type].Increment(-amount);
			if (trackedValues[type].value < 0)
				trackedValues[type].SetValue(0);
		}
		
		OnTrackedValueChanged?.Invoke(type, trackedValues[type].value);
	}
    
	public void SetTrackedValue(TrackedValueType type, double value)
	{
		if (trackedValues.ContainsKey(type))
			trackedValues[type].SetValue(value);
		else
			trackedValues.Add(type, new TrackedValue(type, value));
		
		OnTrackedValueChanged?.Invoke(type, trackedValues[type].value);
	}
    
	public double GetTrackedValue(TrackedValueType type)
	{
		if (trackedValues.ContainsKey(type))
			return trackedValues[type].value;
		
		return 0f;
	}
    
	public void ResetTrackedValue(TrackedValueType type)
	{
		if (trackedValues.ContainsKey(type))
			trackedValues[type].Reset();
		
		OnTrackedValueChanged?.Invoke(type, trackedValues[type].value);
	}
    
	public void ResetAllTrackedValues()
	{
		foreach (var value in trackedValues.Values)
			value.Reset();
	}

	public Dictionary<TrackedValueType, double> GetAllValues()
	{
		Dictionary<TrackedValueType, double> allValues = new Dictionary<TrackedValueType, double>();
		foreach (var kvp in trackedValues)
			allValues[kvp.Key] = kvp.Value.value;
		return allValues;
	}

	#endregion
	
	#region Teck Tree Nodes
	[Header("Teck Tree")]
	public SerializableDictionary<string, int> teckTreeNodesLevels = new SerializableDictionary<string, int>();

	public int GetNodeLevel(string _nodeID)
	{
		return teckTreeNodesLevels.ContainsKey(_nodeID) ? teckTreeNodesLevels[_nodeID] : 0;
	}

	public int LevelUpNode(string _nodeID)
	{
		if (teckTreeNodesLevels.ContainsKey(_nodeID))
		{
			teckTreeNodesLevels[_nodeID]++;
		}
		else
		{
			teckTreeNodesLevels[_nodeID] = 1;
		}
		Save();

		onNodeLevelUp?.Invoke(teckTreeNodesLevels[_nodeID]);
		return teckTreeNodesLevels[_nodeID];
	}
	#endregion
}