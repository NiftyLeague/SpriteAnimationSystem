using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using System.Linq;

public class SpriteRasterizer : MonoBehaviour
{
	private static SpriteRasterizer I;

	public CharacterCustomizer cc;
	public Camera renderCamera;
	public bool Busy { get { return jobs.Count != 0; } }

	private static Dictionary<string, RasterizationResult> spriteCache = new Dictionary<string, RasterizationResult>();
	private static string cachePath;
	private static Queue<RasterizationJob> jobs = new Queue<RasterizationJob>();
	private static int width, height;
	private static int batchSize = 10;
	private static int maxCacheSize = 16;

#if DEBUG_TEST //# ----------
	private int testMaxCount;
	private int testCounter;
	private float testStartTime;
#endif

	[Serializable]
	public struct RasterizationResult
	{
		public string hash;
		public bool isImportant;
		public List<Sprite> Sprites
		{
			get
			{
				lastTimeUsed = Time.realtimeSinceStartup;
				return sprites;
			}
		}
		public float lastTimeUsed;

		private List<Sprite> sprites;

		public RasterizationResult(string hash, List<Sprite> sprites, bool isImportant)
		{
			this.hash = hash;
			this.sprites = sprites;
			this.isImportant = isImportant;
			lastTimeUsed = Time.realtimeSinceStartup;
		}

		public void ClearSprites()
		{
			if (sprites != null)
			{
				foreach (var s in sprites)
				{
					Destroy(s.texture);
					Destroy(s);
				}
				sprites = null;
			}
			Resources.UnloadUnusedAssets();
			GC.Collect();
		}
	}

	private void Awake()
	{
		I = this;
		width = renderCamera.targetTexture.width;
		height = renderCamera.targetTexture.height;
		cachePath = Path.Combine(Application.persistentDataPath, "cache");
	}

	private void Update()
	{
		if (jobs.Count > 0 && jobs.Peek().state == RasterizationJob.State.Created)
		{
			jobs.Peek().Start();
		}

		// TODO: Revisit renderCamera

		if (renderCamera.enabled && jobs.Count == 0)
		{
			renderCamera.enabled = false;
		}
	}

	public Texture2D CaptureTexture2D(bool cleanPalettePixels)
	{
		/*
		Rect rect = new Rect(0, 0, width, height);
		var currentRT = RenderTexture.active;
		RenderTexture.active = renderCamera.targetTexture;
		renderCamera.Render();
		Texture2D image = new Texture2D(width, height, TextureFormat.RGBA32, false);
		image.filterMode = FilterMode.Point;
		image.ReadPixels(rect, 0, 0);
		image.Apply();
		RenderTexture.active = currentRT;
		return cleanPalettePixels ? CleanupPalette(image) : image;
		*/
		Rect rect = new Rect(0, 0, width, height);
		var currentRT = RenderTexture.active;
		RenderTexture.active = renderCamera.targetTexture;
		renderCamera.Render();
		Texture2D image = new Texture2D(width, height, TextureFormat.RGBA32, false);
		image.filterMode = FilterMode.Point;
		image.ReadPixels(rect, 0, 0);
		image.Apply();
		RenderTexture.active = currentRT;
		return cleanPalettePixels ? CleanupPalette(image) : image;
	}

	public Sprite CreateSprite(Texture2D texture, string name)
	{
		Rect rect = new Rect(0, 0, texture.width, texture.height);
		Sprite s = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), 16);
		s.name = name;
		return s;
	}

	public Texture2D RasterizeFrame(int frame)
	{
		cc.SetFrameAbsolute(frame);
		return CaptureTexture2D(false);
	}

	[HideInInspector] public bool devUseCache = false;
	[HideInInspector] public bool devSavePng = true;
	[HideInInspector] public bool devSaveSharedItemsPng = true;

	public void RasterizeAllFrames(Dictionary<string, Trait> traits, string traitHash, Action<List<Sprite>, string> onRasterizationComplete, bool isImportant)
	{
		if (spriteCache.ContainsKey(traitHash))
		{
			//print("using memory cache for " + traitHash);
			onRasterizationComplete(spriteCache[traitHash].Sprites, traitHash);
			return;
		}
		renderCamera.enabled = true;
#if DEBUG_TEST //# ----------
		bool useCache = devUseCache; // false;
		bool savePng = devSavePng; // true;
		//bool saveSharedItemsPng = devSaveSharedItemsPng;
#elif UNITY_WEBGL && !UNITY_EDITOR
		bool useCache = true;
		bool savePng = false;
#else
		bool useCache = true;
		bool savePng = false;
#endif
		jobs.Enqueue(new RasterizationJob(traits, traitHash, OnRasterizationJobComplete, onRasterizationComplete, useCache, savePng, isImportant));
	}

#if DEBUG_TEST //# ----------

	public void ClearSpriteCache()
	{
		foreach (var keyVal in spriteCache) {
			keyVal.Value.ClearSprites();
		}
		spriteCache.Clear();
	}

	public void StartTest(int max)
	{
		testMaxCount = max;
		testCounter = 0;
		testStartTime = Time.time;
	}

	public void EndTest()
	{
		testMaxCount = 0;
		CustomizationManager.I.StartTest();
	}

#endif

	private void OnRasterizationJobComplete(RasterizationJob job)
	{
		jobs.Dequeue();
		if (!spriteCache.ContainsKey(job.hash))
		{
			RasterizationResult res = new RasterizationResult(job.hash, job.result, job.isImportant);
			if (spriteCache.Count >= maxCacheSize)
			{
				RasterizationResult leastUsed = new RasterizationResult();
				float leastUsedTime = float.MaxValue;
				foreach (var kv in spriteCache)
				{
					if (kv.Value.lastTimeUsed <= leastUsedTime /*&& !kv.Value.isImportant*/)
					{
						leastUsedTime = kv.Value.lastTimeUsed;
						leastUsed = kv.Value;
					}
				}
				if (!string.IsNullOrEmpty(leastUsed.hash))
				{
					leastUsed.ClearSprites();
					Debug.Log("Max rasterization cache size reached, removing the least used " + leastUsed.hash);
					spriteCache.Remove(leastUsed.hash);
				}
			}
			spriteCache.Add(res.hash, res);
		}
		job.callback(job.result, job.hash);

#if DEBUG_TEST //# ----------
		if (testMaxCount > 0)
		{
			testCounter++;
			if (testCounter % batchSize == 0 || testCounter == testMaxCount) {
				print($"({testCounter} of {testMaxCount}) {(Time.time - testStartTime):F4} total seconds elapsed.");
			}
			if (testCounter == testMaxCount) {
				EndTest();
			}
		}
#endif
	}

	private T[] SubArray<T>(T[] data, int index, int length)
	{
		T[] result = new T[length];
		Array.Copy(data, index, result, 0, length);
		return result;
	}

	private Texture2D CleanupPalette(Texture2D texture)
	{
		int w = texture.width;
		int h = texture.height;
		Color32[] pix = texture.GetPixels32();

		int i = h - 1;
		int j = 0;
		bool dirty = false;

		while (pix[i * w + j].a > 0f)
		{
			while (pix[i * w + j].a > 0f)
			{
				texture.SetPixel(j, i, Color.clear);
				j++;
				dirty = true;
			}
			i--;
			j = 0;
		}
		if (dirty)
		{
			texture.Apply();
		}
		return texture;
	}


	private class RasterizationJob
	{
		Dictionary<string, Trait> traits;
		public string hash;
		public string fileName;
		private string fileNameForSharedItems = "shared_items";
		public string error;
		public State state;
		public bool isImportant;
		private bool useCache;
		private bool savePng;
		public List<Sprite> result;
		public Action<List<Sprite>, string> callback;

		private Action<RasterizationJob> onComplete;
		private MemoryStream ms = new MemoryStream();

		public RasterizationJob(Dictionary<string, Trait> traits, string hash, Action<RasterizationJob> onComplete, Action<List<Sprite>, string> callback, bool useCache, bool savePng, bool isImportant)
		{
			this.traits = traits;
			this.hash = hash;
			this.fileName = $"{Accessories.version}{hash}";
			this.fileNameForSharedItems = $"{Accessories.version}{Utils.GetSHA1Hash(fileNameForSharedItems)}";
			this.onComplete = onComplete;
			this.callback = callback;
			this.useCache = useCache;
			this.savePng = savePng;
			this.isImportant = isImportant;
			state = State.Created;
			error = null;
			result = new List<Sprite>();
		}

		public void Start()
		{
			if (spriteCache.ContainsKey(hash))
			{
				//print("using memory cache for " + hash);
				result = spriteCache[hash].Sprites;
				Finish();
			}
			else
			{
				I.StartCoroutine(Rasterize());
			}
		}

		public void Finish()
		{
			bool success = string.IsNullOrEmpty(error) && result.Count == AnimationTags.totalFrameCount;
			state = success ? State.Succeeded : State.Failed;
			onComplete(this);
			CustomizationManager.I.ClearAllTraitLayerData();
		}

		public bool AreFramesValid()
		{
			if (result.Count < AnimationTags.totalFrameCount)
			{
				return false;
			}
			int[] framesToValidate = new int[] {
				AnimationTags.Accessories.frameFrom,
				AnimationTags.StandRight.frameFrom,
				AnimationTags.Run.frameFrom,
				AnimationTags.JumpUp.frameFrom,
				AnimationTags.Win.frameFrom,
			};
			foreach (int i in framesToValidate)
			{
				Sprite s = result[AnimationTags.StandRight.frameFrom];
				Color c1 = s.texture.GetPixel(width / 2, height / 2);
				Color c2 = s.texture.GetPixel(69, 128 - 93);
				Color c3 = s.texture.GetPixel(60, 128 - 65);
				if (c1.a < 0.5 || c2.a < 0.5 || c3.a < 0.5)
				{
					return false;
				}
			}

			return true;
		}

		private IEnumerator Rasterize()
		{
			if (state != State.Created)
			{
				error = $"Rasterize called on Job in '{state}' state";
				state = State.Failed;
				Finish();
				yield break;
			}
			state = State.InProgress;
			if (useCache)
			{
				yield return I.StartCoroutine(LoadFromCache());
				if (result.Count == AnimationTags.totalFrameCount && AreFramesValid())
				{
					//print("Used file cache for " + hash);
					Finish();
					yield break;
				}
				yield return I.StartCoroutine(LoadFromWebCache());
				if (result.Count == AnimationTags.totalFrameCount && AreFramesValid())
				{
					//print("Used web cache for " + hash);
					Finish();
					yield break;
				}
				ms.Write(BitConverter.GetBytes(AnimationTags.totalFrameCount), 0, 4);
			}

			//# -----

			result = new List<Sprite>();
			CustomizationManager.I.SetFromTraitsForRasterization(traits);
			CustomizationManager.I.PreloadAllTraitLayerData();
			Debug.Log("Waiting for layerdata to load");

			float startTme = Time.time;
			yield return new WaitUntil(() => CustomizationManager.I.AllTraitLayerDataPreloaded()); //# was AllTraitLayerDatasPreloaded
			Debug.Log($"All LayerData preloaded in {(Time.time - startTme):F2} seconds");

			//# -----

			for (int i = 0; i < AnimationTags.totalFrameCount; i++)
			{
				RasterizeFrame(i);
				if (i % batchSize == 0)
				{
					yield return new WaitForEndOfFrame();
				}
			}
			I.cc.SetFrameAbsolute(0); //# TEMP
			Finish();

			//# -----

			if (useCache)
			{
				try
				{
					string path = Path.Combine(cachePath, fileName);
					Task.Run(() =>
					{
						var watch = new System.Diagnostics.Stopwatch();
						watch.Start();
						XUtils.SaveBytes(ms.ToArray(), path, true);
						watch.Stop();
						print($"Wrote ({fileName}) in {(watch.Elapsed.TotalSeconds):F2}");
						JSWrapper.SyncFs();
					});
					ms.Close();
				}
				catch (Exception e)
				{
					Debug.LogWarning("Exception while writing cache " + e.Message);
				}
			}
		}

		private void RasterizeFrame(int frame)
		{
			/*#
			I.cc.SetFrameAbsolute(frame);
			Texture2D tex = I.CaptureTexture2D(true);
			result.Add(I.CreateSprite(tex, $"{frame:000}_{hash}"));
			if (useCache)
			{
				byte[] raw = tex.GetRawTextureData();
				ms.Write(BitConverter.GetBytes(raw.Length), 0, 4);
				ms.Write(raw, 0, raw.Length);
			}
			else if (savePng)
			{
				SavePng(tex, $"{frame:0000}.png", false, frame == 0); //# % 10
			}
			#*/
			I.cc.SetFrameAbsolute(frame);

			// Set the number of blended layers.

			// Repeat for the number of blended layers desired.

			// Turn on only those layers applicable for a given blended layer.

			//List<string> blendedLayers = new List<string> { };

			int i;
			int numLayers = I.cc.layers.Length;
			bool[] activeStates = new bool[numLayers];
			for (i = 0; i < numLayers; i++)
			{
				//activeStates[i] = I.cc.layers[i].transform.gameObject.activeSelf;
				activeStates[i] = I.cc.layers[i].layerVariation.gameObject.activeSelf;
			}

			// ----------
			// Layers above bat_above. These are the top-most layers.
			// Turn on the layers that are above bat_above and rasterize as a blended layer.

			string str = "";
			bool conditionMet = false;
			int iAtLayer = 0;
			for (i = 0; i < numLayers; i++)
			{
				if (!conditionMet && I.cc.layers[i].layerVariation.animationLayer.layerType == AnimationLayerType.Bat)
				{
					conditionMet = true;
					iAtLayer = i;
				}
				// If the condition is not yet met, then show the layer.
				I.cc.layers[i].transform.gameObject.SetActive(!conditionMet);
				str += !conditionMet ? "1" : "0";
			}
			str += $" chrLayer0 (iAtLayer: {iAtLayer})\n";
			RasterizeBlendedFrame(frame, "chrLayer0");

			// ----------
			// Bat_above.
			// Turn on the bat_above layer and rasterize.

			//I.cc.layers[iAtLayer].transform.gameObject.SetActive(true);
			for (i = 0; i < numLayers; i++)
			{
				conditionMet = i == iAtLayer;
				I.cc.layers[i].transform.gameObject.SetActive(conditionMet);
				str += conditionMet ? "1" : "0";
			}
			str += " batLayer0\n";
			RasterizeBlendedFrame(frame, "batLayer0", true); // This is a shared item.

			// ----------
			// Layers above bat_below. These are the layers between bat_above and bat_below.
			// Turn on the layers that are above bat_below and rasterize as a blended layer.

			//I.cc.layers[iAtLayer].transform.gameObject.SetActive(false);
			iAtLayer++;
			for (i = 0; i < iAtLayer; i++)
			{
				I.cc.layers[i].transform.gameObject.SetActive(false);
				str += "0";
			}

			conditionMet = false;
			for (i = iAtLayer; i < numLayers; i++)
			{
				if (I.cc.layers[i].layerVariation.animationLayer.layerType == AnimationLayerType.Bat)
				{
					conditionMet = true;
					iAtLayer = i;
				}
				// If the condition is not yet met, then show the layer.
				I.cc.layers[i].transform.gameObject.SetActive(!conditionMet);
				str += !conditionMet ? "1" : "0";
			}
			str += $" chrLayer1 (iAtLayer: {iAtLayer})\n";
			RasterizeBlendedFrame(frame, "chrLayer1");

			// ----------
			// Bat_below.
			// Turn on the bat_below layer and rasterize.

			//I.cc.layers[iAtLayer].transform.gameObject.SetActive(true);
			for (i = 0; i < numLayers; i++)
			{
				conditionMet = i == iAtLayer;
				I.cc.layers[i].transform.gameObject.SetActive(conditionMet);
				str += conditionMet ? "1" : "0";
			}
			str += " batLayer1\n";
			RasterizeBlendedFrame(frame, "batLayer1", true); // This is a shared item.

			// ----------
			// Layers below bat_below. These are the bottom-most layers.
			// Turn on the layers that are above bat_below and rasterize as blended layer.

			//I.cc.layers[iAtLayer].transform.gameObject.SetActive(false);
			iAtLayer++;
			for (i = 0; i < numLayers; i++)
			{
				conditionMet = i >= iAtLayer;
				I.cc.layers[i].transform.gameObject.SetActive(conditionMet);
				str += conditionMet ? "1" : "0";
			}
			str += " chrLayer2";
			RasterizeBlendedFrame(frame, "chrLayer2");

			if (frame == 1) // || frame == 27
				print(frame + "\n" + str);

			// ----------
			// Reset the active states for all layers.

			for (i = 0; i < numLayers; i++)
			{
				//I.cc.layers[i].transform.gameObject.SetActive(activeStates[i]);
				//I.cc.layers[i].layerVariation.gameObject.SetActive(activeStates[i]);
				I.cc.layers[i].layerVariation.gameObject.SetActive(true);
			}

			//# NOTE: FORMAT MAY CHANGE -----

		}

		private void RasterizeBlendedFrame(int frame, string label, bool isSharedItem = false)
		{
			Texture2D tex = I.CaptureTexture2D(true);
			result.Add(I.CreateSprite(tex, $"{frame:000}_{hash}"));
			if (useCache)
			{
				byte[] raw = tex.GetRawTextureData();
				ms.Write(BitConverter.GetBytes(raw.Length), 0, 4);
				ms.Write(raw, 0, raw.Length);
			} else if (savePng) //((savePng && !isSharedItem) || (devSaveSharedItemsPng && isSharedItem))
			{
				//# Accommodate devSaveSharedItemsPng
				//SavePng(tex, $"{frame:0000}.png", isSharedItem, frame == 0); //# % 10
				SavePng(tex, $"{frame:0000}_{label}.png", isSharedItem, frame == 0); //# % 10
			}

			//# NOTE: FORMAT MAY CHANGE -----

		}


		private IEnumerator LoadFromCache()
		{
			byte[] bytes = null;
			try
			{
				string path = Path.Combine(cachePath, "924b13710494f70eb2f65432ee81554880023bdd33"); // fileName);
				bytes = XUtils.LoadBytes(path, true);
			}
			catch
			{
				Directory.CreateDirectory(cachePath);
			}

			if (bytes == null || bytes.Length == 0)
			{
				yield break;
			}

			yield return I.StartCoroutine(TexturesFromCachedBytes(bytes));
		}


		private IEnumerator LoadFromWebCache()
		{
			byte[] bytes = null;
			string url = $"https://d7ct17ettlkln.cloudfront.net/assets/degens/{Launcher.apiNetwork}/{Accessories.version}/{fileName}";
			UnityWebRequest request = UnityWebRequest.Get(url);
			yield return request.SendWebRequest();
			try
			{
				bytes = XUtils.DecryptToBytes(request.downloadHandler.data, true);
			}
			catch { }

			if (bytes == null || bytes.Length == 0)
			{
				yield break;
			}

			yield return I.StartCoroutine(TexturesFromCachedBytes(bytes));

			if (result.Count == AnimationTags.totalFrameCount)
			{
				try
				{
					string path = Path.Combine(cachePath, fileName);
					Debug.Log($"Caching web file ({fileName})");
					File.WriteAllBytes(path, request.downloadHandler.data);
					JSWrapper.SyncFs();
				}
				catch { }
			}
		}


		private IEnumerator TexturesFromCachedBytes(byte[] bytes)
		{
			int index = 0;
			int numFrames;
			try
			{
				numFrames = BitConverter.ToInt32(bytes, index);
			}
			catch { yield break; }

			if (numFrames != AnimationTags.totalFrameCount) { yield break; }

			index += 4;
			for (int i = 0; i < numFrames; i++)
			{
				try
				{
					int frameSize = BitConverter.ToInt32(bytes, index);
					index += 4;
					Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
					tex.filterMode = FilterMode.Point;
					tex.LoadRawTextureData(I.SubArray(bytes, index, frameSize));
					tex.Apply();
					/*if (AnimationManager.I.fastFrames.Contains(i))
					{
						tex.Compress(true);
						tex.Apply();
					}*/
					index += frameSize;
					result.Add(I.CreateSprite(tex, $"{i:000}_{hash}"));
				}
				catch { yield break; }
				if (i % batchSize == 0)
				{
					yield return new WaitForEndOfFrame();
				}
			}
		}


		private void SavePng(Texture2D texture, string pngName, bool isSharedItem = false, bool debugLog = false)
		{
			string path = Path.Combine(Application.persistentDataPath, isSharedItem ? fileNameForSharedItems : fileName);
			Directory.CreateDirectory(path);
			File.WriteAllBytes(Path.Combine(path, pngName), texture.EncodeToPNG());

#if DEBUG_TEST //# ----------
			if (debugLog)
				print($"SpriteRasterizer.SavePng:\n\tpngName: {pngName},\n\tpath: {path}");
#endif
		}


		public enum State
		{
			Created,
			InProgress,
			Succeeded,
			Failed
		}
	}
}
