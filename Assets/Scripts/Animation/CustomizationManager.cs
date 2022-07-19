using NaughtyAttributes;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class CustomizationManager : MonoBehaviour
{
	public static CustomizationManager I;

	public SpriteRasterizer spriteRasterizer;
	public Transform floatParent;
	public Character character;
	[SerializeField]
	private CharacterType characterType;
	//#public UIOptions categoryUIOption;
	//#public UIOptions[] subcategoryUIOptions;
	public float rotationRate;
	public float rotationStart;
	public float floatRate;
	public float floatDomain;
	public Material defaultMaterial;
	public int frame;
	public Texture2D swapTexture;
	public float randomizationProbabiity;
	public Color32[] colors = PaletteInfo.colors;
	public List<Sprite> sprites;
	public static Action onChange;
	public static Action onInitialized;
	public static Action onOptionBurst;
	public static Action<string> onSubmitTraits;

	public static int NumTraitsKeys { get { return traitKeys.Length; } }

	private CharacterCustomizer cs { get { return character.GetComponent<CharacterCustomizer>(); } }
	private static Dictionary<CharacterType, Dictionary<AnimationLayer, List<AnimationLayerVariation>>> charLayerMap;
	private static Dictionary<CharacterType, Dictionary<AnimationLayer, List<AnimationLayerVariation>>> compositeCharLayerMap; //#

	private static CharacterType[] allCharacterTypes = new CharacterType[6] {
		CharacterType.Alien, CharacterType.Ape, CharacterType.Cat, CharacterType.Doge, CharacterType.Frog, CharacterType.Human };
	private static string[] categoryOptionKeys = new string[] { Names.Character, Names.Head, Names.Clothing, Names.Accessories, Names.Items };
	private static Dictionary<string, string[]> subcategoriesOptionIds = new Dictionary<string, string[]> {
		{ Names.Character, new string[] { Names.Tribe, Names.SkinColor, Names.FurColor, Names.EyeColor, Names.PupilColor } },
		{ Names.Head, new string[] { Names.Hair, Names.Mouth, Names.Beard } },
		{ Names.Clothing, new string[] { Names.Top, Names.Outerwear, Names.Print, Names.Bottom, Names.Footwear, Names.Belt } },
		{ Names.Accessories, new string[] { Names.Hat, Names.Eyewear, Names.Piercing, Names.Wrist, Names.Hands, Names.Neckwear } },
		{ Names.Items, new string[] { Names.LeftItem, Names.RightItem } },
	};
	private static string[] traitKeys = new string[] {
		Names.Tribe, Names.SkinColor, Names.FurColor, Names.EyeColor, Names.PupilColor, Names.Hair, Names.Mouth, Names.Beard, Names.Top,
		Names.Outerwear, Names.Print, Names.Bottom, Names.Footwear, Names.Belt, Names.Hat, Names.Eyewear, Names.Piercing, Names.Wrist,
		Names.Hands, Names.Neckwear, Names.LeftItem, Names.RightItem,
	};

	private static Dictionary<string, string[]> subcategoryOptionKeys = new Dictionary<string, string[]> {
		{ Names.Tribe, new string[] { "Ape", "Human", "Doge", "Frog", "Cat", "Alien" } },
	};

	private static Dictionary<string, AccessoryType> accessoryTypeMap = new Dictionary<string, AccessoryType> {
		{ Names.Beard, AccessoryType.Beard },
		{ Names.Belt, AccessoryType.Belt },
		{ Names.Bottom, AccessoryType.Bottom },
		{ Names.Eyewear, AccessoryType.Eyewear },
		{ Names.Footwear, AccessoryType.Footwear },
		{ Names.Hair, AccessoryType.Hair },
		{ Names.Hands, AccessoryType.Hands },
		{ Names.Hat, AccessoryType.Hat },
		{ Names.LeftItem, AccessoryType.LeftItem },
		{ Names.Mouth, AccessoryType.Mouth },
		{ Names.Neckwear, AccessoryType.Neckwear },
		{ Names.Outerwear, AccessoryType.Outerwear },
		{ Names.Piercing, AccessoryType.Piercing },
		{ Names.Print, AccessoryType.Print },
		{ Names.RightItem, AccessoryType.RightItem },
		{ Names.Top, AccessoryType.Top },
		{ Names.Wrist, AccessoryType.Wrist }
	};

	private static Dictionary<string, Trait> traits = new Dictionary<string, Trait>
	{
		{ Names.Tribe, TraitInfo.EmptyTrait },
		{ Names.SkinColor, TraitInfo.EmptyTrait },
		{ Names.FurColor, TraitInfo.EmptyTrait },
		{ Names.EyeColor, TraitInfo.EmptyTrait },
		{ Names.PupilColor, TraitInfo.EmptyTrait },
		{ Names.Hair, TraitInfo.EmptyTrait },
		{ Names.Mouth, TraitInfo.EmptyTrait },
		{ Names.Beard, TraitInfo.EmptyTrait },
		{ Names.Top, TraitInfo.EmptyTrait },
		{ Names.Outerwear, TraitInfo.EmptyTrait },
		{ Names.Print, TraitInfo.EmptyTrait },
		{ Names.Bottom, TraitInfo.EmptyTrait },
		{ Names.Footwear, TraitInfo.EmptyTrait },
		{ Names.Belt, TraitInfo.EmptyTrait },
		{ Names.Hat, TraitInfo.EmptyTrait },
		{ Names.Eyewear, TraitInfo.EmptyTrait },
		{ Names.Piercing, TraitInfo.EmptyTrait },
		{ Names.Wrist, TraitInfo.EmptyTrait },
		{ Names.Hands, TraitInfo.EmptyTrait },
		{ Names.Neckwear, TraitInfo.EmptyTrait },
		{ Names.LeftItem, TraitInfo.EmptyTrait },
		{ Names.RightItem, TraitInfo.EmptyTrait },
	};

	private static Dictionary<AnimationLayer, string> animationLayerSubcategoryMap = new Dictionary<AnimationLayer, string>();

	private static AnimationLayerVariation.ColorVariationInfo[] eyeColors;
	private static HashSet<Trait> removedTraits = new HashSet<Trait>();
	private static HashSet<string> existingCharacters = new HashSet<string>();
	private static bool isInitialized = false;
	private bool isCustomizationScene = false;
	private float rotationTime = 0f;
	private int rotationFrame = 1;

	private float burstClickClearTime = 0f;
	private int numBurstClicks = 0;
	private float lastChangeTime = 0f;
	private float lastRemovedTraitsFetchTime = 0f;

	private void Awake()
	{
		SceneManager.sceneLoaded += OnSceneLoaded;
		DontDestroyOnLoad(gameObject);
		I = this;
		isInitialized = false;
		isCustomizationScene = SceneManager.GetActiveScene().name == "NiftyCreator";
		if (isCustomizationScene)
		{
			InvokeRepeating(nameof(CleaupUnusedFrames), 10f, 10f);
			StartCoroutine(UpdateRemovedTraits());
		}
		lastRemovedTraitsFetchTime = Time.time;
	}

	[Beebyte.Obfuscator.SkipRename]
	private void CleaupUnusedFrames()
	{
		StartCoroutine(AnimationManager.CleanupUnusedFrames());
	}

	private IEnumerator UpdateRemovedTraits()
	{
		yield return new WaitForSeconds(3f);
	}

	private void OnSceneLoaded(Scene scene, LoadSceneMode loadMode)
	{
		isCustomizationScene = scene.name == "Customization";
	}

	private void Start()
	{
		Initalize();
	}

	private void LateUpdate()
	{
		if (!isCustomizationScene)
		{
			return;
		}

		if (frame == -1)
		{
			rotationTime += Time.deltaTime;
			rotationFrame = Mathf.RoundToInt(rotationTime * rotationRate) % AnimationTags.Turnaround.frameCount;
			cs.SetFrame(AnimationTagType.Turnaround, rotationFrame, true);
		}
		else if (frame < -1)
		{
			cs.SetFrameAbsolute(Mathf.RoundToInt(Time.time * 10f) % AnimationTags.totalFrameCount, true);
		}
		else
		{
			if (rotationFrame != 1)
			{
				rotationTime += Time.deltaTime;
				rotationFrame = Mathf.RoundToInt(rotationTime * rotationRate) % AnimationTags.Turnaround.frameCount;
				cs.SetFrame(AnimationTagType.Turnaround, rotationFrame, true);
			}
			else
			{
				cs.SetFrameAbsolute(frame, true);
			}
		}
		floatParent.localPosition = new Vector3(0f, (Mathf.Sin(Time.time * floatRate) * floatDomain) + (Mathf.Sin(Time.time * floatRate * 0.6f) * 0.8f * floatDomain), 0f);
	}

	private void Initalize()
	{
		character.enabled = false;
		character.GetComponent<CharacterAnimator>().enabled = false;
		/*#if (isCustomizationScene && CustomizationLauncher.removedTraits != null)
		{
			SetRemovedTraits(CustomizationLauncher.removedTraits);
		}#*/
		InitializeCharacterLayerMap();
		InitializeEyeColors();
		/*#if (isCustomizationScene)
		{
			categoryUIOption.Initialize(new SimpleOptionEnumrator("category", categoryOptionKeys, categoryOptionKeys, OnCategoryOptionsChanged, null, false));
			//actionUIOption.Initialize(new SimpleOptionEnumrator("action", new string[] { "POSE", "ROTATE", "-DEBUG-" }, new object[] { 0, -1, -2 }, OnActionChange, null, false));
		}#*/
		isInitialized = true;
		if (onInitialized != null)
		{
			onInitialized();
		}
	}
	
	private void SetExistingCharacters(List<int[]> characters)
	{
#if DEVELOPMENT_BUILD && false
		existingCharacters.Clear();
		existingCharacters.Add("3,29,82,101,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0");
#endif
		foreach (int[] character in characters)
		{
			existingCharacters.Add(string.Join(",", character));
		}
	}

	private void SetRemovedTraits(int[] removed)
	{
	}
	/*#
	public void ReselectOptions()
	{
		categoryUIOption.Reselect();
		for (int i = 0; i < subcategoryUIOptions.Length; i++)
		{
			if (i < subcategoriesOptionIds[categoryUIOption.GetCurrent().ToString()].Length)
			{
				try
				{
					subcategoryUIOptions[i].Reselect();
				}
				catch { }
			}
		}
	}
	#*/
	public static bool IsInitialized()
	{
		return isInitialized;
	}

	private void OnChange()
	{
		if (onChange != null)
		{
			onChange();
		}
		lastChangeTime = Time.time;
	}

	public static void GenerateSprites(Dictionary<string, Trait> traits, Action<List<Sprite>, string> onRasterizationComplete, bool isImportant)
	{
		string traitStr = GetTraitString(traits);
#if DEBUG_TEST //# ----------
		print($"CustomizationManager.GenerateSprites: {traitStr}");
#endif
		I.spriteRasterizer.RasterizeAllFrames(traits, Utils.GetSHA1Hash(traitStr), onRasterizationComplete, isImportant);
	}

	private void OnRasterizationComplete(List<Sprite> sprites, string traitHash)
	{
		this.sprites = sprites;
#if DEBUG_TEST //# ----------
		int f = 20 * 5;
		sprite0 = sprites[f];
		sprite1 = sprites[f + 1];
		sprite2 = sprites[f + 2];
		sprite3 = sprites[f + 3];
		sprite4 = sprites[f + 4];
		f = 54 * 5;
		sprite5 = sprites[f];
		sprite6 = sprites[f + 1];
		sprite7 = sprites[f + 2];
		sprite8 = sprites[f + 3];
		sprite9 = sprites[f + 4];
		showActions = true;
		print($"CustomizationManager.OnRasterizationComplete: {(Time.time - dirtyStartTime):F4} seconds elapsed.");
		//#I.spriteRasterizer.cc.ClearLayers();
		//# Note: Reference ResetCharacter(){I.InitializeCharacter(true); I.CharacterTypeChanged(true);}
		//# Note: Reference SetCompositeLayers(AnimationLayerVariation[] animationLayerVariations)
		//#I.spriteRasterizer.cc.SetCompositeLayers();
		InitializeCompositeCharacter(true); //#(AnimationLayerVariation headLayer, _) =
#endif
	}

	public Texture2D GenerateTexture2D(int frame)
	{
		var texs = spriteRasterizer.RasterizeFrame(frame);
		return texs;
	}

	private void InitializeEyeColors()
	{
		(_, var eyes) = GetHeadAndEyesVariations(CharacterType.Ape);
		eyeColors = eyes.colorVariations;
	}

	private object GetCurrentSelection(string optionId)
	{
		switch (optionId)
		{
		case Names.Tribe:
			return AnimationManager.GetCharacterString(character.type);
		case Names.SkinColor:
			return cs.GetCurrentBodyColorInfo(0);
		case Names.FurColor:
			return cs.GetCurrentBodyColorInfo(characterType == CharacterType.Cat ? 2 : 1);
		case Names.EyeColor:
		case Names.PupilColor:
			int colorIndex = optionId == Names.EyeColor ? 0 : 1;
			var colors = cs.GetCurrentEyeColorInfo(colorIndex);
			if (colors != null)
			{
				foreach (var cv in eyeColors)
				{
					if (cv.to[colorIndex].SameColorAs(colors.to[colorIndex]))
					{
						return cv;
					}
				}
			}
			return null;
		}
		return null;
	}

	private AccessoryOptionEnumrator.AccessoryOption GetCurrentAccessorySelection(string optionId, AnimationLayerVariation[] variations)
	{
		return cs.GetCurrentAccessoryOption(variations);
	}

	public static void SetActionState(int state, bool setOption)
	{
		if (state != I.frame)
		{
			I.OnChange();
			if (state == -1 && I.rotationFrame == 1)
			{
				I.rotationTime = I.rotationStart;
			}
			else if (state == -2)
			{
				I.rotationFrame = 1;
				I.rotationTime = I.rotationStart;
			}
			I.frame = state;
		}
/*#
#if !UNITY_STANDALONE || UNITY_EDITOR
		if (setOption)
		{
			CharacterCreatorLevel.SetPoseRotate(state == 0);
		}
#endif
#*/
	}
	/*#
	private void OnCategoryOptionsChanged(OptionEnumarator options)
	{
		OnChange();
		string category = options.Current.Key;
		for (int i = 0; i < subcategoryUIOptions.Length; i++)
		{
			bool subcategoryActive = false;
			bool soldOut = false;
			if (i < subcategoriesOptionIds[category].Length)
			{
				string subcategoryId = subcategoriesOptionIds[category][i];
				if (accessoryTypeMap.ContainsKey(subcategoryId))
				{
					var layer = AnimationManager.GetLayer(accessoryTypeMap[subcategoryId], characterType);
					if (layer)
					{
						animationLayerSubcategoryMap[layer] = subcategoryId;
						var variations = layer.GetCharacterVariations(characterType);
						subcategoryUIOptions[i].Initialize(new AccessoryOptionEnumrator(layer, variations.Select(v => v.accessoryInfo.displayName).ToArray(), variations, OnAccessoryOptionsChanged, GetCurrentAccessorySelection(subcategoryId, variations), true, $"--{subcategoryId}--"));
						subcategoryActive = true;
					}
					else
					{
						subcategoryUIOptions[i].Initialize(new SimpleOptionEnumrator(subcategoryId, new string[] { subcategoryId }, new string[] { subcategoryId }, null, null, false));
						subcategoryActive = false;
					}
				}
				else if (subcategoryOptionKeys.ContainsKey(subcategoryId))
				{
					subcategoryUIOptions[i].Initialize(new SimpleOptionEnumrator(subcategoryId, subcategoryOptionKeys[subcategoryId], subcategoryOptionKeys[subcategoryId], OnSubcategoryOptionsChanged, GetCurrentSelection(subcategoryId), false));
					subcategoryActive = true;
				}
				else if (category != Names.Character)
				{
					subcategoryUIOptions[i].Initialize(new SimpleOptionEnumrator(subcategoryId, new string[] { subcategoryId }, new string[] { subcategoryId }, OnSubcategoryOptionsChanged, GetCurrentSelection(subcategoryId), false));
				}
				else if (subcategoryId == Names.SkinColor || subcategoryId == Names.FurColor || subcategoryId == Names.EyeColor || subcategoryId == Names.PupilColor)
				{
					subcategoryActive = subcategoryUIOptions[i].GetId().ToString().ToLower().Contains("color");
				}
				soldOut = subcategoryActive && subcategoryUIOptions[i].IsSoldOut;
				subcategoryActive = subcategoryActive && subcategoryUIOptions[i].GetCount() > 0 && subcategoryUIOptions[i].GetCurrent() != null;
			}
			else
			{
				subcategoryActive = false;
			}
			if (soldOut)
			{
				subcategoryUIOptions[i].gameObject.SetActive(true);
			}
			else
			{
				subcategoryUIOptions[i].gameObject.SetActive(subcategoryActive);
			}
		}
	}
#*/
		private void OnSubcategoryOptionsChanged(OptionEnumarator options)
	{
		OnChange();
		object key = options.Id;
		if (key is string && (string)key == Names.Tribe)
		{
			characterType = AnimationManager.GetCharacterType((string)options.Current.Value);
			CharacterTypeChanged(characterType != character.type);
		}
		TraitChanged(options);
	}

	private void OnAccessoryOptionsChanged(OptionEnumarator options)
	{
		OnChange();
		object key = options.Id;
		if (options is AccessoryOptionEnumrator)
		{
			SetLayerVariation(key as AnimationLayer, options.Current.Value as AccessoryOptionEnumrator.AccessoryOption);
		}
		TraitChanged(options);
		if (isCustomizationScene)
		{
			if (Time.time - burstClickClearTime > 1f)
			{
				numBurstClicks = 0;
				burstClickClearTime = Time.time;
			}
			else if (options.SelectionIndex > 2)
			{
				numBurstClicks++;
				if (numBurstClicks > 3)
				{
					if (onOptionBurst != null)
					{
						onOptionBurst.Invoke();
					}
				}
			}
		}
	}
	
	private void SetLayerVariation(AnimationLayer layer, AccessoryOptionEnumrator.AccessoryOption option)
	{
		if (option.colorVariationIndex < 0)
		{
			cs.SetLayer(layer, option.animationLayerVariation);
		}
		else
		{
			cs.SetLayer(layer, option.animationLayerVariation, option.animationLayerVariation.colorVariations[option.colorVariationIndex]);
		}
	}

	private void SetLayerVariation(AnimationLayer layer, AnimationLayerVariation[] accessoryVariations, Trait trait)
	{
		if (trait.id == TraitInfo.EmptyTrait.id)
		{
			cs.SetLayer(layer, null);
			return;
		}

		AnimationLayerVariation alv = AnimationManager.I.variationTraitMap[trait.id];
		AnimationLayerVariation characterAlv = accessoryVariations.First(v => v.traitId == alv.traitId);
		if (alv.colorVariations.Length <= 1)
		{
			cs.SetLayer(layer, characterAlv);
		}
		else
		{
			cs.SetLayer(layer, characterAlv, characterAlv.colorVariations[trait.id - alv.traitId]);
		}
	}

	public static Trait GetTraitFromOption(OptionEnumarator options, KeyValuePair<string, object> option)
	{
		string id;
		if (options is AccessoryOptionEnumrator)
		{
			id = animationLayerSubcategoryMap[options.Id as AnimationLayer];
		}
		else
		{
			id = (string)options.Id;
		}
		CharacterType characterType = I.characterType;

		string characterString = AnimationManager.GetCharacterString(characterType);
		Trait trait = TraitInfo.EmptyTrait;
		switch (id)
		{
		case Names.Tribe:
			trait = TraitInfo.GetTrait(characterType, characterString);
			break;
		case Names.SkinColor:
			trait = TraitInfo.GetTrait(characterType, $"{option.Key} {characterString}");
			break;
		case Names.FurColor:
			trait = TraitInfo.GetTrait(characterType, $"{option.Key.Replace(" Fur", "")} {characterString} Fur");
			break;
		case Names.EyeColor:
		case Names.PupilColor:
			trait = TraitInfo.GetTrait(characterType, option.Key);
			break;
		default:
			if (string.IsNullOrEmpty(option.Key) || !(option.Value is AccessoryOptionEnumrator.AccessoryOption))
			{
				break;
			}
			AccessoryOptionEnumrator.AccessoryOption opt = option.Value as AccessoryOptionEnumrator.AccessoryOption;
			string accessoryName = Utils.ToFirstLetterUppercase(opt.ToString());
			trait = TraitInfo.GetTrait(opt.animationLayerVariation.accessoryInfo.accessoryType, accessoryName);
			break;
		}

		if (!string.IsNullOrEmpty(option.Key) && option.Key != Names.Character && trait.id == 0)
		{
			Debug.LogWarning($"Trait '{id}: {option.Key}' for character '{characterType}' not found. Defaulting to EmptyTrait");
		}
		return trait;
	}

	private void TraitChanged(OptionEnumarator options)
	{
		string id = null;
		if (options is AccessoryOptionEnumrator)
		{
			id = animationLayerSubcategoryMap[options.Id as AnimationLayer];
		}
		else
		{
			id = (string)options.Id;
		}
		if (!string.IsNullOrEmpty(id))
		{
			traits[id] = GetTraitFromOption(options, options.Current);
		}
	}

	private void InitializeCharacterLayerMap()
	{
		charLayerMap = new Dictionary<CharacterType, Dictionary<AnimationLayer, List<AnimationLayerVariation>>>();
		foreach (var ct in allCharacterTypes)
		{
			charLayerMap.Add(ct, new Dictionary<AnimationLayer, List<AnimationLayerVariation>>());
			foreach (var al in AnimationManager.I.layers)
			{
				charLayerMap[ct].Add(al, new List<AnimationLayerVariation>());
				foreach (var av in al.variations)
				{
					if (av.characterType == ct || av.characterType == CharacterType.Share)
					{
						charLayerMap[ct][al].Add(av);
					}
				}
			}
		}
		/*#
		compositeCharLayerMap = new Dictionary<CharacterType, Dictionary<AnimationLayer, List<AnimationLayerVariation>>>();
		foreach (var ct in allCharacterTypes)
		{
			compositeCharLayerMap.Add(ct, new Dictionary<AnimationLayer, List<AnimationLayerVariation>>());
			foreach (var al in AnimationManager.I.layers)
			{
				compositeCharLayerMap[ct].Add(al, new List<AnimationLayerVariation>());
				foreach (var av in al.variations)
				{
					if (av.characterType == ct || av.characterType == CharacterType.Share)
					{
						compositeCharLayerMap[ct][al].Add(av);
					}
				}
			}
		}
		
		print($"InitializeCharacterMap: {charLayerMap.ToArray()}");
		print($"InitializeCharacterMap: {compositeCharLayerMap.ToArray()}");#*/
	}

	public static void Reset()
	{
		isInitialized = false;
		I = null;
		onChange = null;
		onInitialized = null;
		onSubmitTraits = null;
	}

	public static void ResetCharacter()
	{
		//I.characterType = I.character.type;
		I.InitializeCharacter(true);
		I.CharacterTypeChanged(true);
	}

	private void CharacterTypeChanged(bool resetTraits = false)
	{
		animationLayerSubcategoryMap.Clear();
		if (resetTraits)
		{
			foreach (var k in traits.Keys.ToArray())
			{
				if (k != Names.Tribe)
				{
					traits[k] = TraitInfo.EmptyTrait;
				}
			}
		}

		(AnimationLayerVariation headLayer, AnimationLayerVariation eyesLayer) = InitializeCharacter();
		/*#
		if (!isCustomizationScene || (categoryUIOption.GetCurrent() != null && categoryUIOption.GetCurrent().ToString() != Names.Character))
		{
			return;
		}

		int i = 1;
		string id = subcategoriesOptionIds[Names.Character][1];
		subcategoryUIOptions[i].Initialize(new SimpleOptionEnumrator(id, headLayer.colorVariations.Select((v, i) => $"{PaletteInfo.colorMap[v.to[0]].name}").ToArray(), headLayer.colorVariations, OnSkinColorChanged, GetCurrentSelection(id), false));
		subcategoryUIOptions[i].gameObject.SetActive(true);
		i++;

		if (characterType == CharacterType.Ape || characterType == CharacterType.Cat || characterType == CharacterType.Doge)
		{
			id = subcategoriesOptionIds[Names.Character][2];
			subcategoryUIOptions[i].Initialize(new SimpleOptionEnumrator(id, headLayer.colorVariations.Select((v, i) => $"{PaletteInfo.colorMap[v.to[v.to.Length - 1]].name} Fur").ToArray(), headLayer.colorVariations, OnSecondarySkinColorChanged, GetCurrentSelection(id), false));
			subcategoryUIOptions[i].gameObject.SetActive(true);
			i++;
		}
		id = subcategoriesOptionIds[Names.Character][3];
		subcategoryUIOptions[i].Initialize(new SimpleOptionEnumrator(id, eyeColors.Select((v, i) => $"{PaletteInfo.colorMap[v.to[0]].name} Eyes").ToArray(), eyeColors, OnEyeColorChanged, GetCurrentSelection(id), false));
		subcategoryUIOptions[i].gameObject.SetActive(true);
		i++;

		if (characterType != CharacterType.Doge && characterType != CharacterType.Alien)
		{
			id = subcategoriesOptionIds[Names.Character][4];
			subcategoryUIOptions[i].Initialize(new SimpleOptionEnumrator(id, eyeColors.Select((v, i) => $"{PaletteInfo.colorMap[v.to[v.to.Length - 1]].name} Pupils").ToArray(), eyeColors, OnSecondaryEyeColorChanged, GetCurrentSelection(id), false));
			subcategoryUIOptions[i].gameObject.SetActive(true);
			i++;
		}

		for (; i < subcategoryUIOptions.Length; i++)
		{
			subcategoryUIOptions[i].gameObject.SetActive(false);
		}#*/
	}

	private (AnimationLayerVariation, AnimationLayerVariation) InitializeCharacter(bool force = false)
	{
		AnimationLayerVariation headLayer = null;
		AnimationLayerVariation eyesLayer = null;
		List<AnimationLayerVariation> layers = new List<AnimationLayerVariation>();
		if (characterType != character.type || force)
		{
			character.type = characterType;
			foreach (var layerAndVariations in charLayerMap[characterType])
			{
				AnimationLayer l = layerAndVariations.Key;
				List<AnimationLayerVariation> vars = layerAndVariations.Value;
				if (l.Required)
				{
					if (vars.Count > 0)
					{
						int randomSelection = UnityEngine.Random.Range(0, vars.Count);
						AnimationLayerVariation variation = vars[randomSelection];
						layers.Add(variation);
					}
				}
			}

			cs.SetLayers(layers.ToArray());
			cs.CharacterTypeChanged();
			cs.ReplaceBodyAndEyeColors();
		}

		foreach (var layerAndVariations in charLayerMap[characterType])
		{
			AnimationLayer l = layerAndVariations.Key;
			List<AnimationLayerVariation> vars = layerAndVariations.Value;
			if (l.Required && vars.Count > 0)
			{
				if (vars[0].variationName.EndsWith("_head"))
				{
					headLayer = vars[0];
				}
				else if (vars[0].variationName.EndsWith("_eyes"))
				{
					eyesLayer = vars[0];
				}
			}
		}
		return (headLayer, eyesLayer);
	}

	private void InitializeCompositeCharacter(bool force = false) //#(AnimationLayerVariation, AnimationLayerVariation)
	{
		//#AnimationLayerVariation headLayer = null;
		//#AnimationLayerVariation eyesLayer = null;
		List<AnimationLayerVariation> layers = new List<AnimationLayerVariation>();
		if (characterType != character.type || force)
		{
			character.type = characterType;
			foreach (var layerAndVariations in charLayerMap[characterType])
			{
				AnimationLayer l = layerAndVariations.Key;
				List<AnimationLayerVariation> vars = layerAndVariations.Value;
				if (l.Required)
				{
					if (vars.Count > 0)
					{
						int randomSelection = UnityEngine.Random.Range(0, vars.Count);
						AnimationLayerVariation variation = vars[randomSelection];
						layers.Add(variation);
					}
				}
			}

			cs.SetCompositeLayers(layers.ToArray());
			//#cs.CharacterTypeChanged();
			//#cs.ReplaceBodyAndEyeColors();
		}
		/*#
		foreach (var layerAndVariations in charLayerMap[characterType])
		{
			AnimationLayer l = layerAndVariations.Key;
			List<AnimationLayerVariation> vars = layerAndVariations.Value;
			if (l.Required && vars.Count > 0)
			{
				if (vars[0].variationName.EndsWith("_head"))
				{
					headLayer = vars[0];
				} else if (vars[0].variationName.EndsWith("_eyes"))
				{
					eyesLayer = vars[0];
				}
			}
		}
		return (headLayer, eyesLayer);#*/
	}

	private void OnSkinColorChanged(OptionEnumarator options)
	{
		OnChange();
		SetSkinColor(options.Current.Value as AnimationLayerVariation.ColorVariationInfo);
		TraitChanged(options);
	}

	private void SetSkinColor(AnimationLayerVariation.ColorVariationInfo cvi)
	{
		switch (characterType)
		{
		case CharacterType.Alien:
		case CharacterType.Frog:
		case CharacterType.Human:
		case CharacterType.Ape:
		case CharacterType.Doge:
			cs.SetBodyColor(0, cvi.to[0]);
			break;
		case CharacterType.Cat:
			cs.SetBodyColor(0, cvi.to[0]);
			cs.SetBodyColor(1, cvi.to[1]);
			break;
		}
	}

	private void OnSecondarySkinColorChanged(OptionEnumarator options)
	{
		OnChange();
		SetSecondarySkinColor(options.Current.Value as AnimationLayerVariation.ColorVariationInfo);
		TraitChanged(options);
	}

	private void SetSecondarySkinColor(AnimationLayerVariation.ColorVariationInfo cvi)
	{
		switch (characterType)
		{
		case CharacterType.Ape:
		case CharacterType.Doge:
			cs.SetBodyColor(1, cvi.to[1]);
			break;
		case CharacterType.Cat:
			cs.SetBodyColor(2, cvi.to[2]);
			break;
		}
	}

	private void OnEyeColorChanged(OptionEnumarator options)
	{
		OnChange();
		SetEyeColor(options.Current.Value as AnimationLayerVariation.ColorVariationInfo);
		TraitChanged(options);
	}

	private void SetEyeColor(AnimationLayerVariation.ColorVariationInfo cvi)
	{
		cs.SetEyeColor(0, cvi.to[0]);
	}

	private void OnSecondaryEyeColorChanged(OptionEnumarator options)
	{
		OnChange();
		SetSecondaryEyeColor(options.Current.Value as AnimationLayerVariation.ColorVariationInfo);
		TraitChanged(options);
	}

	private static (AnimationLayerVariation, AnimationLayerVariation) GetHeadAndEyesVariations(CharacterType characterType)
	{
		string headName = $"b_{characterType.ToString().ToLower()}_head";
		string eyesName = $"b_{characterType.ToString().ToLower()}_eyes";
		return (AnimationManager.I.variations[headName], AnimationManager.I.variations[eyesName]);
	}

	private void SetSecondaryEyeColor(AnimationLayerVariation.ColorVariationInfo cvi)
	{
		cs.SetEyeColor(1, cvi.to[1]);
	}

	public Dictionary<string, Trait> ValidateTraits(int[] traits)
	{
		Dictionary<string, Trait> traitDict = new Dictionary<string, Trait>();
		for (int i = 0; i < traitKeys.Length; i++)
		{
			traitDict.Add(traitKeys[i], traits[i] != TraitInfo.EmptyTrait.id ? TraitInfo.traits[traits[i]] : TraitInfo.EmptyTrait);
		}
		return traitDict;
	}

	public CharacterType GetCharacterTypeFromTraits(int[] traits)
	{
		return AnimationManager.GetCharacterType(subcategoryOptionKeys[Names.Tribe][traits[0] - 1]);
	}

	public void PreloadAllTraitLayerData()
	{
		cs.PreoadAllLayerData();
	}

	public bool AllTraitLayerDataPreloaded() //# was AllTraitLayerDatasPreloaded
	{
		return cs.AllLayerDataPreloaded(); //# was AllLayerDataPerloaded
	}

	public void ClearAllTraitLayerData()
	{
		cs.ClearAllLayerData();
	}

	public void SetFromTraitsForRasterization(Dictionary<string, Trait> traits)
	{
		SetFromTraits(traits.ToDictionary(e => e.Key, e => e.Value.id), true);
	}

	private void SetFromTraits(Dictionary<string, int> traits, bool forRasterization = false)
	{
		if (!forRasterization && spriteRasterizer.Busy)
		{
			Debug.LogError("Tried setting traits while rasterization jobs in progress!");
			return;
		}
		const int EMPTY_TRAIT = 0;

		foreach (var kv in traits)
		{
			CustomizationManager.traits[kv.Key] = kv.Value != EMPTY_TRAIT ? TraitInfo.traits[kv.Value] : TraitInfo.EmptyTrait;
		}

		characterType = AnimationManager.GetCharacterType(subcategoryOptionKeys[Names.Tribe][traits[Names.Tribe] - 1]);
		(AnimationLayerVariation headLayer, _) = InitializeCharacter();
		CharacterTypeChanged(false);
		(_, AnimationLayerVariation eyeLayer) = GetHeadAndEyesVariations(CharacterType.Ape);

		string skinColorName = TraitInfo.traits[traits[Names.SkinColor]].name.Replace($" {AnimationManager.GetCharacterString(characterType)}", "");
		SetSkinColor(GetColorVariationFromName(headLayer, 0, skinColorName));
		if (characterType != CharacterType.Human && characterType != CharacterType.Frog && characterType != CharacterType.Alien && traits[Names.FurColor] != EMPTY_TRAIT)
		{
			string furColorName = TraitInfo.traits[traits[Names.FurColor]].name.Replace($" {AnimationManager.GetCharacterString(characterType)} Fur", "");
			SetSecondarySkinColor(GetColorVariationFromName(headLayer, characterType == CharacterType.Cat ? 2 : 1, furColorName));
		}

		string eyeColorName = TraitInfo.traits[traits[Names.EyeColor]].name.Replace(" Eyes", "");
		SetEyeColor(GetColorVariationFromName(eyeLayer, 0, eyeColorName));
		if (characterType != CharacterType.Alien && characterType != CharacterType.Doge && traits[Names.PupilColor] != EMPTY_TRAIT)
		{
			string pupilColorName = TraitInfo.traits[traits[Names.PupilColor]].name.Replace(" Pupils", "");
			SetSecondaryEyeColor(GetColorVariationFromName(eyeLayer, 1, pupilColorName));
		}

		foreach (var kv in traits)
		{
			if (accessoryTypeMap.ContainsKey(kv.Key))
			{
				var layer = AnimationManager.GetLayer(accessoryTypeMap[kv.Key], characterType);
				if (layer)
				{
					animationLayerSubcategoryMap[layer] = kv.Key;
					var variations = layer.GetCharacterVariations(characterType);
					SetLayerVariation(layer, variations, TraitInfo.traits[kv.Value]);
				}
			}
			CustomizationManager.traits[kv.Key] = kv.Value > 0 ? TraitInfo.traits[kv.Value] : TraitInfo.EmptyTrait;
		}
	}

	private AnimationLayerVariation.ColorVariationInfo GetColorVariationFromName(AnimationLayerVariation alv, int index, string colorName)
	{
		Color32 c = PaletteInfo.nameMap[colorName].color;
		return alv.colorVariations.FirstOrDefault(cv => c.SameColorAs(cv.to[index]));
	}

	public void SetFromTraitsString(string traitsStr)
	{
		Dictionary<string, int> traits = new Dictionary<string, int>();
		traitsStr = Regex.Replace(traitsStr, @"[^a-zA-z0-9,\]\[ ]", "").Replace("[", "").Replace("],", ":");
		foreach (string trait in traitsStr.Split(':'))
		{
			var tokens = trait.Replace("]", "").Split(',');
			int traitId = int.Parse(tokens[1].Trim());
			traits.Add(tokens[0].Trim(), traitId);
		}
		SetFromTraits(traits);
	}

	public static bool IsRemovedTrait(Trait trait)
	{
		return removedTraits != null && removedTraits.Contains(trait);
	}

	public static bool IsRemovedTrait(int traitId)
	{
		return IsRemovedTrait(TraitInfo.traits[traitId]);
	}

	public static bool TraitCategorySelected(string category)
	{
		return traits[category].id > 0;
	}

	public void UI_SubmitTraitMap()
	{
		string traitStr = GetTraitsString(traits);
		SetFromTraitsString(traitStr);
		if (onSubmitTraits != null)
		{
			onSubmitTraits(traitStr);
		}
	}

	//#
	public static string GetTraitString(Dictionary<string, Trait> traits)
	{
		string traitStr = "";
		foreach (var t in traitKeys)
		{
			traitStr += $"{t}:{traits[t].id}";
		}
		return traitStr;
	}

	private string GetTraitsString(Dictionary<string, Trait> traits)
	{
		string res = "[";
		foreach (string key in traitKeys)
		{
			res += $"\n  [\"{key}\", {traits[key].id}],";
			//res += $"\n  [\"{key}\", {traits[key].id}, \"{traits[key].name}\"],"; //#
		}
		res = res.Substring(0, res.Length - 1);
		res += "\n]";
		return res;
	}

	//#[Button]
	internal Dictionary<string, Trait> GetRandomTraits()
	{
		Dictionary<string, Trait> randomTraits = new Dictionary<string, Trait>();
		string characterString = XRandom.NextMember(subcategoryOptionKeys[Names.Tribe]);
		CharacterType characterType = AnimationManager.GetCharacterType(characterString);
		randomTraits[Names.Tribe] = TraitInfo.GetTrait(characterType, characterString);
		foreach (string key in traitKeys)
		{
			switch (key)
			{
			case Names.Tribe:
				continue;
			case Names.SkinColor:
			case Names.FurColor:
			case Names.EyeColor:
			case Names.PupilColor:
				randomTraits.Add(key, TraitInfo.GetRandomTrait(characterType, key, removedTraits));
				break;
			default:
#if DEBUG_TEST //# ----------
				Trait randomTrait = TraitInfo.GetRandomTrait(characterType, accessoryTypeMap[key], removedTraits);
				//#print($"\tkey: {key}, randomTrait.id: {randomTrait.id}, .name: {randomTrait.name}");
				randomTraits.Add(key, randomTrait);
#else
				if (XRandom.NextFloat() > randomizationProbabiity)
				{
					randomTraits.Add(key, TraitInfo.GetRandomTrait(characterType, accessoryTypeMap[key], removedTraits));
				}
				else
				{
					randomTraits.Add(key, TraitInfo.EmptyTrait);
				}
#endif
				break;
			}
		}
#if DEBUG_TEST //# ----------
		//#rndTraits = GetTraitsString(randomTraits);
		print(GetTraitsString(randomTraits));
#endif
		return randomTraits;
	}

	public static void RandomizeTraits()
	{
		var traits = I.GetRandomTraits().ToDictionary(e => e.Key, e => e.Value.id);
		I.SetFromTraits(traits);
		I.CharacterTypeChanged(false);
		//#I.ReselectOptions();
		I.CleaupUnusedFrames();
	}

	public void GetRandomSprites(Action<List<Sprite>> onSpritesReady)
	{
		spriteRasterizer.RasterizeAllFrames(GetRandomTraits(), Utils.GetSHA1Hash((XUtils.Timestamp() / 1).ToString()), (sprites, hash) => { onSpritesReady(sprites); }, false);
	}
	/*#
	[Button]
	private void GenerateSprites0()
	{
#if DEBUG_TEST //# ----------
		//Trait emptyTrait = TraitInfo.EmptyTrait;
		const int EMPTY_TRAIT = 0;

		foreach (var kv in traits)
		{
			if (kv.Value.id == EMPTY_TRAIT)
			{
				Debug.LogWarning("CustomizationManager.GenerateSprites0: empty trait found.");
				return;
			}
		}

		dirtyStartTime = Time.time;
		print(GetTraitsString(traits));
#endif
		spriteRasterizer.RasterizeAllFrames(traits, Utils.GetSHA1Hash(GetTraitString(traits)), OnRasterizationComplete, false); //#Utils.GetSHA1Hash((XUtils.Timestamp() / 100).ToString())
	}
	#*/
#if DEBUG_TEST //# ----------
	//#[Multiline]
	//#public string rndTraits = "void";
	public bool devUseCache = true; //false; //
	public bool devSavePng = false; //true; //
	public bool devSaveSharedItemsPng = false; //true;
	private float dirtyStartTime;
	private bool showActions;

	[ShowAssetPreview, ShowIf("showActions"), Tooltip("chrLayer0")] public Sprite sprite0;
	[ShowAssetPreview, ShowIf("showActions"), Tooltip("batLayer0 (bat above)")] public Sprite sprite1;
	[ShowAssetPreview, ShowIf("showActions"), Tooltip("chrLayer1")] public Sprite sprite2;
	[ShowAssetPreview, ShowIf("showActions"), Tooltip("batLayer1 (bat below)")] public Sprite sprite3;
	[ShowAssetPreview, ShowIf("showActions"), Tooltip("chrLayer2")] public Sprite sprite4;

	[ShowAssetPreview, ShowIf("showActions"), Tooltip("chrLayer0")] public Sprite sprite5;
	[ShowAssetPreview, ShowIf("showActions"), Tooltip("batLayer0 (bat above)")] public Sprite sprite6;
	[ShowAssetPreview, ShowIf("showActions"), Tooltip("chrLayer1")] public Sprite sprite7;
	[ShowAssetPreview, ShowIf("showActions"), Tooltip("batLayer1 (bat below)")] public Sprite sprite8;
	[ShowAssetPreview, ShowIf("showActions"), Tooltip("chrLayer2")] public Sprite sprite9;

	[Button]
	public void GenerateSprites()
	{
		showActions = false;
		character.EnableCharacterScripts(false);
		//existingCharacters.Clear();
		//existingCharacters.Add("3,29,82,101,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0");
		dirtyStartTime = Time.time;
		spriteRasterizer.devUseCache = devUseCache;
		spriteRasterizer.devSavePng = devSavePng;
		spriteRasterizer.devSaveSharedItemsPng = devSaveSharedItemsPng;
		spriteRasterizer.ClearSpriteCache();
		Dictionary<string, Trait> randomTraits = GetRandomTraits();
		string traitStr = GetTraitString(randomTraits);
		//#spriteRasterizer.RasterizeAllFrames(randomTraits, Utils.GetSHA1Hash((XUtils.Timestamp()).ToString()), (sprites, hash) => { }, false);
		spriteRasterizer.RasterizeAllFrames(randomTraits, Utils.GetSHA1Hash(traitStr), OnRasterizationComplete, false);
	}
	/*#
	[Button]
	public void GenerateSprites2()
	{
		showActions = false;
		character.EnableCharacterScripts(false);
		dirtyStartTime = Time.time;
		int max = 4;
		spriteRasterizer.StartTest(max);
		spriteRasterizer.devUseCache = devUseCache;
		spriteRasterizer.devSavePng = devSavePng;
		spriteRasterizer.devSaveSharedItemsPng = devSaveSharedItemsPng;
		for (var i = 0; i < max; i++)
		{
			spriteRasterizer.ClearSpriteCache();
			Dictionary<string, Trait> randomTraits = GetRandomTraits();
			string traitStr = GetTraitString(randomTraits);
			//#spriteRasterizer.RasterizeAllFrames(randomTraits, Utils.GetSHA1Hash((XUtils.Timestamp()).ToString()), (sprites, hash) => { }, false);
			spriteRasterizer.RasterizeAllFrames(randomTraits, Utils.GetSHA1Hash(traitStr), OnRasterizationComplete, false);
			//#print($"CustomizationManager.GetRandomTraits3: {i + 1} of {max} complete.");
		}
	}
	#*/
	//#[Button, ShowIf("showActions")] public void Normal() { character.Normal(); }
	//#[Button, ShowIf("showActions")] public void Attack() { ClearAllTraitLayerData(); character.Attack(); }
	//#[Button, ShowIf("showActions")] public void Bounce() { character.Bounce(); }
	//#[Button, ShowIf("showActions")] public void Special() { character.Special(); }
	//#[Button, ShowIf("showActions")] public void Burp() { character.Burp(); }
	//#[Button, ShowIf("showActions")] public void Pose() { character.Pose(); }
	/*#
	[Dropdown("DevTribes")]
	public string DevTribe;
	private List<string> DevTribes
	{
		get
		{
			return new List<string>() { "Random", "Ape", "Human", "Doge", "Frog", "Cat", "Alien" };
		}
	}
	
	[Dropdown("DevDegens")]
	public string DevDegen;
	private List<string> DevDegens
	{
		get {
			return new List<string>() {
				"Randomize", "Snarfy", "Andy", "Spike", "Morgan", "Bolo", "Will" //# Add more test degens as desired.
			};
		}
	}

	[Dropdown("DevDegens")]
	public string DevDegen;
	private DropdownList<string> DevDegens()
	{
		return new DropdownList<string>()
		{
			{ "Randomize", null },
			{ "Snarfy", "DataTest_Snarfy" },
			{ "Andy", "DataTest_Andy" },
			{ "Spike", "DataTest_Spike" },
			{ "Morgan", "DataTest_Morgan" },
			{ "Bolo", "DataTest_Bolo" },
			{ "Will", "DataTest_Will" }
		};
	}
	/*#
	[Dropdown("DevDegens")]
	public string DevDegen;
	private DropdownList<string> DevDegens()
	{
		return new DropdownList<string>()
		{
			{ "Randomize", null },
			{ "Snarfy", "3,29,82,101,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0" },
			{ "Snarfy2", "1,14,77,101,112,679,695,732,789,817,822,866,887,979,0,0,0,0,0,0,0,0" },
			{ "Andy", "1,17,73,104,110,263,684,714,817,822,860,894,991,0,0,0,0,0,0,0,0,0" },
			{ "Spike", "5,48,90,107,110,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0" },
			{ "Spike2", "4,38,104,110,384,622,792,834,0,0,0,0,0,0,0,0,0,0,0,0,0,0" },
			{ "Morgan", "6,60,101,112,263,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0" },
			{ "Bolo", "1,10,70,101,110,439,619,684,695,727,942,0,0,0,0,0,0,0,0,0,0,0" },
			{ "Will", "1,10,70,108,111,428,599,729,756,817,827,866,909,1011,0,0,0,0,0,0,0,0" }
		};

		//return new DropdownList<string>()
		//{
		//	{ "Randomize", null },
		//	{ "Snarfy", "101,82,29,3,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0" },
		//	{ "Snarfy2", "695,101,789,679,77,822,732,887,866,112,979,14,1,817,0,0,0,0,0,0,0,0" },
		//	{ "Andy", "104,684,73,822,714,894,263,860,110,991,17,1,817,0,0,0,0,0,0,0,0,0" },
		//	{ "Spike", "107,90,110,48,5,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0" },
		//	{ "Spike2", "622,104,792,834,110,38,384,4,0,0,0,0,0,0,0,0,0,0,0,0,0,0" },
		//	{ "Morgan", "101,263,112,60,6,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0" },
		//	{ "Bolo", "695,619,101,684,70,727,942,439,110,10,1,0,0,0,0,0,0,0,0,0,0,0" },
		//	{ "Will", "599,108,756,70,827,729,909,866,111,1011,10,428,1,817,0,0,0,0,0,0,0,0" }
		//};
	}
	#*//*#
	[ReorderableList]
	[Tooltip("This is the list of layers above the 'bat above'.")]
	public string[] LayersAbove_BatAbove = new string[] {
		"1", "2", "3", "4", "5"
	};

	[ReorderableList]
	[Tooltip("This is the list of layers below the 'bat above'.")]
	public string[] LayersBelow_BatAbove = new string[] {
		"6", "7", "8", "9", "10"
	};

	[ReorderableList]
	[Tooltip("This is the list of layers below the 'bat below'.")]
	public string[] LayersBelow_BatBelow = new string[] {
		"11", "12", "13", "14", "15"
	};
	#*/
	public void StartTest()
	{
		character.enabled = true;
		character.GetComponent<CharacterAnimator>().enabled = true;
		//#character.SetActive(true);
	}
	/*#
	//# -----

	private NiftyUser user = null;
	internal static bool canPlay { get { return I.user != null && I.user.Degens != null && I.user.Degens.Count > 0; } }

	try
		{user = NiftyUsers.Login(cachedVerification);
			user = NiftyUsers.Login(verification);
		}
		catch { }

if (user == null)
{
	VerificationFailed();
	yield break;
}

[Beebyte.Obfuscator.SkipRename]
private void InitializeProfile()
{
	state = State.Initialization;
	ui.SetStatus("Initializing Profile");

	StartCoroutine(_InitializeProfile(user.Address));

	DiscordController.UpdatePresence("");
}

private void OnGetCharactersResponse(List<int[]> characterTraits)
	{
#if DEVELOPMENT_BUILD
		if (characterTraits.Count == 0)
		{
			characterTraits = new List<int[]> {
					CustomizationManager.I.GetRandomTraits().Select(e=>e.Value.id).ToArray(),
					CustomizationManager.I.GetRandomTraits().Select(e=>e.Value.id).ToArray(),
					CustomizationManager.I.GetRandomTraits().Select(e=>e.Value.id).ToArray(),
					CustomizationManager.I.GetRandomTraits().Select(e=>e.Value.id).ToArray(),
					CustomizationManager.I.GetRandomTraits().Select(e=>e.Value.id).ToArray(),
					CustomizationManager.I.GetRandomTraits().Select(e=>e.Value.id).ToArray(),
				};
		}
#endif
		user.SetDegens(characterTraits);
	}

	private void ProfileDegensReady()
	{
		if (user.Degens.Count == 0)
			return;
		//rasterizationStartTime = Time.realtimeSinceStartup;
		user.SortDegens();
		user.RasterizeDegens();
		//state = State.Rasterization;
		//ui.SetStatus("Loading Your Degens");
	}#*/

#endif

#if DEVELOPMENT_BUILD //# || DEBUG_TEST
	[Button]
	private void RunTraitRemovalSimulation()
	{
		StartCoroutine(DoRunSim());
	}

	private IEnumerator DoRunSim()
	{
		var poolSizes = new Dictionary<string, int>();
		poolSizes.Add(Names.SkinColor, 59);
		poolSizes.Add(Names.FurColor, 30);
		poolSizes.Add(Names.EyeColor, 9);
		poolSizes.Add(Names.PupilColor, 9);
		foreach (var trait in traitKeys)
		{
			if (accessoryTypeMap.ContainsKey(trait))
			{
				AccessoryType t = accessoryTypeMap[trait];
				poolSizes.Add(trait, TraitInfo.accessoryTraits[t].Count);
			}
		}

		removedTraits = new HashSet<Trait>();
		var removedPerCategory = new Dictionary<string, int>();
		string output = "Minted,Total";
		foreach (var trait in traitKeys)
		{
			if (trait != Names.Tribe)
			{
				removedPerCategory.Add(trait, 0);
				output += $",{trait}";
			}
		}
		output += "\n";
		string traitsOutput = "";
		int count = 0;
		while (count++ < 10000)
		{
			var traits = GetRandomTraits();
			traitsOutput += $"{string.Join(",", traits.Select(e => e.Value.id))},\n";
			if (count % 100 == 0)
			{
				output += $"{count},{removedTraits.Count}";
				foreach (var kv in removedPerCategory)
				{
					output += $",{poolSizes[kv.Key] - kv.Value}";
				}
				output += "\n";
			}
			RemoveTrait(count, traits, removedPerCategory, poolSizes);
			if (count % 10 == 0)
			{
				print(count);
				yield return new WaitForEndOfFrame();
			}
		}
		using (StreamWriter writer = new StreamWriter(Path.Combine(Application.dataPath, $"T4/Data/{XUtils.Timestamp()}.csv"), false))
		{
			writer.Write(output);
		}
		using (StreamWriter writer = new StreamWriter(Path.Combine(Application.dataPath, $"T4/Data/minted-{XUtils.Timestamp()}.json"), false))
		{
			writer.Write(traitsOutput);
		}
		output = "";
		foreach (var trait in removedTraits)
		{
			output += $"{trait.id},";
		}
		print(output);

		void RemoveTrait(int newCharId, Dictionary<string, Trait> traits, Dictionary<string, int> categories, Dictionary<string, int> originalPoolSizes)
		{
			if ((removedTraits.Count < 200 && newCharId % 4 == 0) ||
			(removedTraits.Count < 400 && newCharId % 6 == 0) ||
			(removedTraits.Count < 600 && newCharId % 8 == 0) ||
			(removedTraits.Count < 800 && newCharId % 10 == 0))
			{
				var randomTraitToRemove = traits.Keys.ToArray()[XRandom.NextInt(1, 22)];
				var trait = traits[randomTraitToRemove];

				int poolSize = originalPoolSizes[randomTraitToRemove];
				bool skip = XRandom.NextInt(-12, 13) < (100 / poolSize);

				if (!skip &&
					trait.id != 0 &&
					trait.id != 10 &&
					trait.id != 22 &&
					trait.id != 29 &&
					trait.id != 37 &&
					trait.id != 48 &&
					trait.id != 59 &&
					trait.id != 70 &&
					trait.id != 82 &&
					trait.id != 90 &&
					trait.id != 101 &&
					trait.id != 110
				)
				{
					removedTraits.Add(trait);
					categories[randomTraitToRemove]++;
				}
			}
		}
	}
#endif

	public static class Names
	{
		public const string Character = "Character";
		public const string Head = "Head";
		public const string Clothing = "Clothing";
		public const string Accessories = "Accessories";
		public const string Items = "Items";
		public const string Tribe = "Tribe";
		public const string SkinColor = "Skin Color";
		public const string FurColor = "Fur Color";
		public const string EyeColor = "Eye Color";
		public const string PupilColor = "Pupil Color";
		public const string Hair = "Hair";
		public const string Mouth = "Mouth";
		public const string Beard = "Beard";
		public const string Top = "Top";
		public const string Outerwear = "Outerwear";
		public const string Print = "Print";
		public const string Bottom = "Bottom";
		public const string Footwear = "Footwear";
		public const string Belt = "Belt";
		public const string Hat = "Hat";
		public const string Eyewear = "Eyewear";
		public const string Piercing = "Piercing";
		public const string Wrist = "Wrist";
		public const string Hands = "Hands";
		public const string Neckwear = "Neckwear";
		public const string LeftItem = "Left Item";
		public const string RightItem = "Right Item";
	}
}
