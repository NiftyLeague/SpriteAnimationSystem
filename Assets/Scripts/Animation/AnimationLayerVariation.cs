using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Assertions;
using UnityEngine.ResourceManagement.AsyncOperations;
//using UnityEngine.Serialization;

public class AnimationLayerVariation : MonoBehaviour
{
	public int traitId;
	public string variationName;
	public CharacterType characterType;
	public AccessoryInfo accessoryInfo;
	public AnimationLayer animationLayer;
	public ColorVariationInfo[] colorVariations;
	[NaughtyAttributes.ReadOnly] //, FormerlySerializedAs("palleteColors")] // FormerlySerializedAs is not supported at runtime.
	public PaletteColor[] palleteColors; //paletteColors;
	[NaughtyAttributes.ReadOnly]
	public Color32[] colors;
	public PaletteColor[] originalColors;
	public string layerDataAddress;

	internal bool layerDataLoaded = false;

	[SerializeField]
	private LayerData layerData;
	private AsyncOperationHandle<LayerData> layerDataLoadOp;


	private static Sprite[] emptyFrames = new Sprite[AnimationTags.totalFrameCount];
	public Sprite[] Frames
	{
		get
		{
			if (layerData == null || layerData.frames == null || layerData.frames.Length == 0)
			{
				if (!layerDataLoadOp.IsValid())
				{
					LoadLayerData();
				}
			}
			return layerData ? layerData.frames : emptyFrames;
		}
	}

	public void LoadLayerData()
	{
		layerDataLoaded = false;
		//print("Load data for [" + variationName + "]");
		//print("> layerDataAddress: [" + layerDataAddress + "]");
		layerDataLoadOp = Addressables.LoadAssetAsync<LayerData>(layerDataAddress);
		layerDataLoadOp.Completed += LayerDataLoadOp_Completed;

		// Synchronous loading is not supported on WebGL
		//layerDataLoadOp.WaitForCompletion();
		//layerData = layerDataLoadOp.Result;
		//ClearColorPalettePixels();
	}

	private void LayerDataLoadOp_Completed(AsyncOperationHandle<LayerData> op)
	{
		if (op.Result)
		{
			layerData = op.Result;
			ClearColorPalettePixels();
			layerDataLoaded = true;
		}
		else
		{
			Debug.LogError($"({variationName}) layerData failed to load ({op.Status}) ({op.OperationException})");
		}
	}

	public int GetTraitId(int colorIndex)
	{
		if (colorIndex <= 0)
		{
			return traitId;
		}
		Assert.IsTrue(colorIndex < colorVariations.Length);
		return traitId + colorIndex;
	}

	public void ClearLayerData()
	{
		Addressables.Release(layerData);
		layerData = null;
		layerDataLoaded = false;
		if (layerDataLoadOp.IsValid())
		{
			Addressables.Release(layerDataLoadOp);
		}
	}

	public void Initialize(LayerData layerData)
	{
#if UNITY_EDITOR
		layerDataAddress = layerData.GetAddressableAssetEntry().address;
#endif
		this.layerData = layerData;
		InitializeFrameColors();
		InitializeVariationColors();
		accessoryInfo = Accessories.GetAccessoryInfo(variationName);
		this.layerData = null;
	}

	private void InitializeVariationColors()
	{
		Sprite f = Frames[0];
		if (f == null)
		{
			return;
		}
		int w = f.texture.width;
		int h = f.texture.height;
		Color32[] pix = f.texture.GetPixels32();
		List<PaletteColor> fromColors = new List<PaletteColor>();

		int i = h - 1;
		int j = 0;

		while (pix[i * w + j].a > 0f)
		{
			fromColors.Add(PaletteInfo.colorMap[pix[i * w + j]]);
			j++;
		}
		originalColors = fromColors.ToArray();

		j = 0;
		i--;

		List<Color32> toColors;
		List<ColorVariationInfo> variations = new List<ColorVariationInfo>();
		while (pix[i * w + j].a > 0f)
		{
			toColors = new List<Color32>();
			while (pix[i * w + j].a > 0f)
			{
				toColors.Add(pix[i * w + j]);
				j++;
			}
			if (toColors.Count > 0)
			{
				if (fromColors.Count == toColors.Count)
				{
					variations.Add(new ColorVariationInfo(originalColors, toColors.ToArray()));
				}
				else
				{
					throw new Exception($"{name} color variations are not configured correctly");
				}
			}
			i--;
			j = 0;
		}
		colorVariations = variations.ToArray();
	}

	public void ClearColorPalettePixels()
	{
		Sprite f = Frames[0];
		if (f == null)
		{
			return;
		}
		int w = f.texture.width;
		int h = f.texture.height;
		Color32[] pix = f.texture.GetPixels32();

		int i = h - 1;
		int j = 0;

		bool textureDirty = false;
		while (pix[i * w + j].a > 0f)
		{
			while (pix[i * w + j].a > 0f)
			{
				f.texture.SetPixel(j, i, Color.clear);
				textureDirty = true;
				j++;
			}
			i--;
			j = 0;
		}
		if (textureDirty)
		{
			f.texture.Apply();
		}
	}

	private void InitializeFrameColors()
	{
		HashSet<byte> greens = new HashSet<byte>();
		for (int i = 1; i < Frames.Length; i++)
		{
			Sprite f = Frames[i];
			if (f != null)
			{
				Color32[] pix = f.texture.GetPixels32();
				foreach (Color32 c in pix)
				{
					if (c.a > 0f)
					{
						greens.Add(c.g);
					}
				}
			}
		}

		var palettes = new List<PaletteColor>();
		List<Color32> allColors = new List<Color32>();

		foreach (var v in greens.OrderBy(p => p))
		{
			if (!PaletteInfo.indexMap.ContainsKey(v))
			{
				Debug.LogError(v + " : " + PaletteInfo.indexMap.ContainsKey(v));
			}
			palettes.Add(PaletteInfo.indexMap[v]);
			allColors.Add(PaletteInfo.indexMap[v].color);
		}
		palleteColors = palettes.ToArray(); //paletteColors;
		colors = allColors.ToArray();
	}

	public Sprite GetFrame(AnimationTagType tag, int frameNum)
	{
		return Frames[AnimationTags.tags[tag].frames[frameNum]];
	}


	[Serializable]
	public class ColorVariationInfo
	{
		public PaletteColor[] from;
		public Color32[] to;

		public ColorVariationInfo(PaletteColor[] from, Color32[] to)
		{
			this.from = from;
			this.to = to;
		}
	}

	public override string ToString()
	{
		return accessoryInfo != null && !string.IsNullOrEmpty(accessoryInfo.displayName) ? accessoryInfo.displayName : variationName;
	}
}
