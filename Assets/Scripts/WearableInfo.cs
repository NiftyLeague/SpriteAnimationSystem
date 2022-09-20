using System.Collections.Generic;
using System.Linq;

public static class Wearables
{
	public static int version = 92;

	public static WearableInfo ShareBat01Above = new WearableInfo("ShareBat01", "bat_share_01", "bat 01 above", WearableType.Bat, CharacterType.Share, 10);
	public static WearableInfo ShareBatBreadAbove = new WearableInfo("ShareBatBread", "bat_share_bread", "bat bread above", WearableType.Bat, CharacterType.Share, 10);
	public static WearableInfo ShareBatDiamondAbove = new WearableInfo("ShareBatDiamond", "bat_share_diamond-cap", "bat diamond above", WearableType.Bat, CharacterType.Share, 10);
	public static WearableInfo ShareBatPurpleAbove = new WearableInfo("ShareBatPurple", "bat_share_purple-hat", "bat purple above", WearableType.Bat, CharacterType.Share, 10);

	public static WearableInfo ShareBat01Below = new WearableInfo("ShareBat01", "bat_share_01", "bat 01 below", WearableType.Bat, CharacterType.Share, 51);
	public static WearableInfo ShareBatBreadBelow = new WearableInfo("ShareBatBread", "bat_share_bread", "bat bread below", WearableType.Bat, CharacterType.Share, 51);
	public static WearableInfo ShareBatDiamondBelow = new WearableInfo("ShareBatDiamond", "bat_share_diamond-cap", "bat diamond below", WearableType.Bat, CharacterType.Share, 51);
	public static WearableInfo ShareBatPurpleBelow = new WearableInfo("ShareBatPurple", "bat_share_purple-hat", "bat purple below", WearableType.Bat, CharacterType.Share, 51);

	public static Dictionary<string, List<WearableInfo>> wearables = new Dictionary<string, List<WearableInfo>>() {
		{ "bat_share_01", new List<WearableInfo>{ ShareBat01Above, ShareBat01Below } },
		{ "bat_share_bread", new List<WearableInfo>{ ShareBatBreadAbove, ShareBatBreadBelow } },
		{ "bat_share_diamond", new List<WearableInfo>{ ShareBatDiamondAbove, ShareBatDiamondBelow } },
		{ "bat_share_purple", new List<WearableInfo>{ ShareBatPurpleAbove, ShareBatPurpleBelow } },
	};

	public static WearableType GetWearableType(int layer)
	{
		//# Wearables contain one or more layers, but all layers are of the same wearableType.
		var wearable = wearables.Values.FirstOrDefault(v => v[0].layer == layer);
		return wearable != null ? wearable[0].wearableType : WearableType.None;
	}

	public static List<WearableInfo> GetWearableInfo(string name)
	{
		if (wearables.ContainsKey(name))
		{
			return wearables[name];
		}
		return null;
	}
	/*#
	public static WearableInfo[] GetWearableInfos(CharacterType characterType)
	{
		return wearables.Values.Where(v => v.characterType == characterType || v.characterType == CharacterType.Share).ToArray();
	}

	public static WearableInfo[] GetWearableInfos(CharacterType characterType, WearableType wearableType)
	{
		return wearables.Values.Where(v => v.wearableType == wearableType && (v.characterType == characterType || v.characterType == CharacterType.Share)).ToArray();
	}#*/
	public static List<WearableInfo>[] GetWearableInfos(CharacterType characterType)
	{
		return wearables.Values.Where(v => v[0].characterType == characterType || v[0].characterType == CharacterType.Share).ToArray();
	}

	public static List<WearableInfo>[] GetWearableInfos(CharacterType characterType, WearableType wearableType)
	{
		return wearables.Values.Where(v => v[0].wearableType == wearableType && (v[0].characterType == characterType || v[0].characterType == CharacterType.Share)).ToArray();
	}
}

[System.Serializable]
public class WearableInfo
{
	public string name;
	public string displayName;
	public string layerName;
	public WearableType wearableType;
	public CharacterType characterType;
	public int layer;

	public WearableInfo(string name, string layerName, string displayName, WearableType wearableType, CharacterType characterType, int layer)
	{
		this.name = name;
		this.layerName = layerName;
		this.displayName = displayName;
		this.wearableType = wearableType;
		this.characterType = characterType;
		this.layer = layer;
	}

	public override string ToString()
	{
		return displayName;
	}
}

public enum WearableType
{
	None,
	Bat,
	Cape,
	Companion,
	Halo,
}
