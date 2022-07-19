using System;
using System.Collections.Generic;
using UnityEngine;

public class DegenAssets : MonoBehaviour
{
	private static DegenAssets I;
	private static Dictionary<int, Sprite> degenSpriteCache = new Dictionary<int, Sprite>();

	private void Awake()
	{
		DontDestroyOnLoad(gameObject);
		I = this;
	}


	public static Sprite GetDegenImage(int id, Action<int, Sprite> onComplete = null)
	{
		if (degenSpriteCache.ContainsKey(id))
		{
			if (onComplete != null)
			{
				onComplete(id, degenSpriteCache[id]);
			}
			return degenSpriteCache[id];
		}
		string url = $"https://d7ct17ettlkln.cloudfront.net/assets/raw/bg/pixel/{id}.png";
		byte[] data = WebRequestHelper.GetDownloadedFile(url);
		if (data != null)
		{
			Sprite s = CreateDegenSpriteFromBytes(data);
			degenSpriteCache.Add(id, s);
			if (onComplete != null)
			{
				onComplete(id, s);
			}
			return s;
		}
		I.StartCoroutine(WebRequestHelper.DownloadFile(url, data =>
		{
			Sprite s = CreateDegenSpriteFromBytes(data);
			degenSpriteCache.Add(id, s);
			if (onComplete != null)
			{
				onComplete(id, s);
			}
		}));
		return null;
	}

	private static Sprite CreateDegenSpriteFromBytes(byte[] data)
	{
		if (data != null)
		{
			Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
			tex.LoadImage(data);
			tex.filterMode = FilterMode.Point;
			tex.Apply();
			return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100);
		}
		return null;
	}
}
