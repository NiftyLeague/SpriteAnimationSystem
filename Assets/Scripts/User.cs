using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public static class NiftyUsers
{
	public static NiftyUser me { get; private set; }

	private static Dictionary<string, NiftyDegen> degenCache = new Dictionary<string, NiftyDegen>();


	public static NiftyUser Login(string verification)
	{
		me = new NiftyUser(verification);
		return me;
	}

	public static NiftyDegen GetNiftyDegen(int[] traitsInt, bool isRental)
	{
		return GetNiftyDegen(string.Join(".", traitsInt), isRental);
	}

	public static NiftyDegen GetNiftyDegen(string traitStr, bool isRental)
	{
		if (degenCache.ContainsKey(traitStr))
		{
			return degenCache[traitStr];
		}
		var degen = new NiftyDegen(traitStr, isRental);
		return degen;
	}

	public static string GetMyAuthorization()
	{
		string token = me.authorization;
#if USE_TEST_TOKEN || DEBUG_TEST //#
		string[] toks = {
			"gAAAAABhtRWrCxVg0pI0el5V-_wDPVHzZ2G-z2ecuKH7HL9Delxi0E1DTs-2ZYdMnvJJCmSRKZ9h1Ww1KwcNFRHELJccZU19rBS-BvTXI9XTg5E7FD6qKwWWAb_9YyGtYmWJKR0fZf99d7UciWtgNaCKgUJJoV8-Ctn8n6QaYnBATb5uusGCGMuGBuXEEbMbsFb5Q4EEykhjXkEmmGkoPUQ8aqO6WlSzoFpzH7OAynJbakZMyu1gbqbFAhzKRP06vssAPUQeMmD2ap2jWyj6fpdaLafgGEwivQ==",
			"gAAAAABhswWkWxRya2GpYsnclia737_G9UK2T3pC1X_ZwP-aMnlqfVyyvsq0DH7_8dvIkeeyG95vZZ43Wc4n6VgKZvPACniBLs3lmy_5XCB_s7G2k4slFynjTp4F7gQaquM1IlRl-SclTzGNE8VAHxnKwGLO77e_bwKb0M9xMuuligjYsdpyjo8ScjvfpqEndZNDiUxYT5GNnS_FUv6Wceadwj1zMVA9BDa5sfit8EM6hOZuK8Xqzzuks_jNz12VmqgdOzOsFj4gATnGSMJG8k4efwOXyFFn9g==",
		};
		token = toks[Application.dataPath.Contains("_clone") ? 1: 0];
#endif
		return token;
	}
}

[Serializable]
public class NiftyUser
{
	public string id { get; }
	public string Address { get { return $"0x{id}"; } }
	public List<NiftyDegen> Degens
	{
		get
		{
			if (degensMap == null)
			{
				return null;
			}
			var degens = degensMap.Values.ToList();
			degens.Sort((x, y) => x.id > y.id ? 1 : -1);
			return degens;
		}
	}
	internal string authorization;
	private Dictionary<int, NiftyDegen> degensMap;

	public NiftyUser(string verification)
	{
		string address = verification;
		if (verification.Contains(','))
		{
			string[] tokens = verification.Split(',');
			address = tokens[0];
#if DEBUG_TEST //# ----------
			address = "0xB970e591772F2CEb482bcD03a8d2f1924a4044Ce".ToLower(); //# //
#endif
			authorization = tokens[1];
		}
		id = address.Replace("0x", "").ToLower();
		degensMap = new Dictionary<int, NiftyDegen>();
	}

	public void ClearDegens()
	{
		degensMap = new Dictionary<int, NiftyDegen>();
	}

	public void SetDegens(List<int[]> characterTraits)
	{
		characterTraits.RemoveAll(traits => traits.Length != CustomizationManager.NumTraitsKeys);
		degensMap = new Dictionary<int, NiftyDegen>();
		foreach (var traits in characterTraits)
		{
			var degen = NiftyUsers.GetNiftyDegen(traits, false);
			degensMap[degen.id] = degen;
		}
	}

	public void AddDegen(int[] traits, int id, string name, int createdAt, bool isRental)
	{
		var degen = NiftyUsers.GetNiftyDegen(traits, isRental);
		degen.id = id;
		degen.name = name;
		degen.createdAt = createdAt;
		degensMap[degen.id] = degen;
	}

	public void RasterizeDegens()
	{
		foreach (var degen in degensMap.Values)
		{
			degen.Rasterize(true);
		}
	}

	public void SortDegens()
	{
		//degensMap.Sort((x, y) => x.id > y.id ? 1 : -1);
	}
}


[Serializable]
public class NiftyDegen
{
	public int id;
	public string name;
	public int createdAt;
	public bool isRental;
	public Dictionary<string, Trait> traits { get; }
	public Color32 primaryColor { get; }

	public List<Sprite> sprites;
	public string hash { get; private set; }
	public CharacterType type { get; }
	public string traitsStr { get; }


	public NiftyDegen(int[] traitInts, bool isRental)
	{
		traits = CustomizationManager.I.ValidateTraits(traitInts);
		type = CustomizationManager.I.GetCharacterTypeFromTraits(traitInts);
		string skinColor = traits[CustomizationManager.Names.SkinColor].name;
		primaryColor = PaletteInfo.nameMap[skinColor.Replace($" {traits[CustomizationManager.Names.Tribe].name}", "")].color;
		traitsStr = string.Join(".", traitInts);
		this.isRental = isRental;
	}

	public NiftyDegen(string traitsStr, bool isRental) : this(traitsStr.Split('.').Select(s => int.Parse(s)).ToArray(), isRental) { }

	public void Rasterize(bool isImportant, Action<NiftyDegen> onRasterizationComplete = null)
	{
		if (sprites == null || sprites.Count == 0 || sprites.Count != AnimationTags.totalFrameCount || sprites[0] == null)
		{
			CustomizationManager.GenerateSprites(traits, (sprites, hash) =>
			{
				this.sprites = sprites;
				this.hash = hash;
				if (onRasterizationComplete != null)
				{
					onRasterizationComplete(this);
				}
			}, isImportant);
		}
	}

	public string GetDisplayName()
	{
		if (!isRental)
		{
			return string.IsNullOrEmpty(name) ? $"#{id}" : name;
		}
		return string.IsNullOrEmpty(name) ? $"RENTAL #{id}" : name;
	}

	public Sprite GetImage()
	{
		return DegenAssets.GetDegenImage(id);
	}
}
