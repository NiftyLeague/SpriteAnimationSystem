using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class AnimationManager : MonoBehaviour
{
	public static AnimationManager I;

	public AnimationLayer[] layers;
	public Dictionary<string, AnimationLayerVariation> variations;
	public Dictionary<int, AnimationLayerVariation> variationTraitMap;
	public HashSet<int> fastFrames = new HashSet<int>();


	private void Awake()
	{
		DontDestroyOnLoad(gameObject);
		I = this;
		variations = new Dictionary<string, AnimationLayerVariation>();
		variationTraitMap = new Dictionary<int, AnimationLayerVariation>();
		fastFrames.UnionWith(AnimationTags.Turnaround.frames);
		fastFrames.UnionWith(AnimationTags.Run.frames);
		fastFrames.UnionWith(AnimationTags.RunDirChange.frames);
		fastFrames.UnionWith(AnimationTags.JumpDown.frames);
		fastFrames.UnionWith(AnimationTags.JumpDownArmsOut.frames);
		fastFrames.UnionWith(AnimationTags.JumpUp.frames);
		fastFrames.UnionWith(AnimationTags.JumpUpRight.frames);
		fastFrames.UnionWith(AnimationTags.JumpUpToDownTransition.frames);
		fastFrames.UnionWith(AnimationTags.JumpLaunch.frames);
		fastFrames.UnionWith(AnimationTags.JumpSomersault.frames);
		fastFrames.UnionWith(AnimationTags.Wallslide.frames);
		fastFrames.UnionWith(AnimationTags.WallslideJump.frames);
		fastFrames.UnionWith(AnimationTags.HitRecovered.frames);
		fastFrames.UnionWith(AnimationTags.HitComet.frames);
		fastFrames.UnionWith(AnimationTags.HitFly.frames);
		fastFrames.UnionWith(AnimationTags.Skid.frames);
		fastFrames.UnionWith(AnimationTags.SkidRecover.frames);
		fastFrames.UnionWith(AnimationTags.HitSpin.frames);
		fastFrames.UnionWith(AnimationTags.HitRotate.frames);
		fastFrames.UnionWith(AnimationTags.BurpLoop.frames);
		fastFrames.UnionWith(AnimationTags.BurpStart.frames);
		fastFrames.UnionWith(AnimationTags.Throw.frames);
		fastFrames.UnionWith(AnimationTags.ThrowDown.frames);
		fastFrames.UnionWith(AnimationTags.ThrowDownAir.frames);
		fastFrames.UnionWith(AnimationTags.ThrowDownRight.frames);
		fastFrames.UnionWith(AnimationTags.ThrowUp.frames);
		fastFrames.UnionWith(AnimationTags.FrogTongue.frames);
		fastFrames.UnionWith(AnimationTags.FrogTongueJumpDown.frames);
		fastFrames.UnionWith(AnimationTags.FrogTongueJumpUp.frames);
		fastFrames.UnionWith(AnimationTags.DogeCoinPowerup.frames);
		fastFrames.UnionWith(AnimationTags.DogeCoinSpin.frames);
		fastFrames.UnionWith(AnimationTags.DogeCoinTransition.frames);

		foreach (AnimationLayer l in layers)
		{
			foreach (AnimationLayerVariation lv in l.variations)
			{
				variations.Add(lv.variationName, lv);
				if (lv.traitId > 0)
				{
					if (lv.colorVariations.Length <= 1)
					{
						if (!variationTraitMap.ContainsKey(lv.traitId))
						{
							variationTraitMap.Add(lv.traitId, lv);
						}
					}
					else
					{
						for (int i = 0; i < lv.colorVariations.Length; i++)
						{
							if (!variationTraitMap.ContainsKey(lv.traitId + i))
							{
								variationTraitMap.Add(lv.traitId + i, lv);
							}
						}
					}
				}
			}
		}
	}


	public static IEnumerator CleanupUnusedFrames()
	{
		foreach (AnimationLayer l in I.layers)
		{
			foreach (AnimationLayerVariation lv in l.variations)
			{
				var cls = FindObjectsOfType<CustomizationLayer>();
				HashSet<AnimationLayerVariation> inUse = new HashSet<AnimationLayerVariation>(cls.Select(cl => cl.layerVariation).ToArray());
				if (inUse.Contains(lv))
				{
					continue;
				}
				//lv.ClearFrames();
				yield return new WaitForEndOfFrame();
			}
		}
		Resources.UnloadUnusedAssets();
	}

	public static AnimationLayerType GetLayerType(string name)
	{
		switch (name.ToLower())
		{
		case "a":
			return AnimationLayerType.Accessory;
		case "bat":
			return AnimationLayerType.Bat;
		case "b":
			return AnimationLayerType.Body;
		case "fx":
			return AnimationLayerType.FX;
		case "~":
		case "z":
			return AnimationLayerType.Composite;
		}
		throw new Exception($"AnimationLayerType for '{name}' could not be found");
	}

	public static CharacterType GetCharacterType(string name)
	{
		switch (name.ToLower())
		{
		case "share":
			return CharacterType.Share;
		case "alien":
			return CharacterType.Alien;
		case "ape":
			return CharacterType.Ape;
		case "cat":
			return CharacterType.Cat;
		case "doge":
			return CharacterType.Doge;
		case "frog":
			return CharacterType.Frog;
		case "human":
			return CharacterType.Human;
		}
		throw new Exception($"CharacterType for '{name}' could not be found");
	}


	public static string GetCharacterString(CharacterType type)
	{
		switch (type)
		{
		case CharacterType.Share:
			return "Share";
		case CharacterType.Alien:
			return "Alien";
		case CharacterType.Ape:
			return "Ape";
		case CharacterType.Cat:
			return "Cat";
		case CharacterType.Doge:
			return "Doge";
		case CharacterType.Frog:
			return "Frog";
		case CharacterType.Human:
			return "Human";
		}
		throw new Exception($"AnimationCharacterType for '{type}' could not be found");
	}


	public static AnimationLayer GetLayer(AccessoryType category, CharacterType characterType)
	{
		return I.layers.FirstOrDefault(l => l.customizationCategory == category && (l.characterType == characterType || l.characterType == CharacterType.Share));
	}

	public static AnimationLayer GetLayer(int layerNum)
	{
		return I.layers.FirstOrDefault(l => l.layer == layerNum);
	}
}


public enum AnimationLayerType
{
	Accessory,
	Bat,
	Body,
	FX,
	Composite, //#
}


public enum CharacterType
{
	Share,
	Alien,
	Ape,
	Cat,
	Doge,
	Frog,
	Human,
}
