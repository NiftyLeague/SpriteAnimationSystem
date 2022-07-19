using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CustomizationLauncher : MonoBehaviour
{
	private static CustomizationLauncher I;
	public static int[] removedTraits;

	public CustomizationManager customizationManager;
	public RectTransform options;
	public Text statusText;
	public Text versionText;
	public RectTransform block;

	private string userAddress;


	private void Awake()
	{
		I = this;
#if UNITY_STANDALONE && !UNITY_EDITOR
		StartCustomization();
	}
#else //#if UNITY_STANDALONE
		SetStatusText("Initializing");
		versionText.text = $"v{Application.version}{Accessories.version}";
		block.gameObject.SetActive(true);
	}

	private void Start()
	{
		JSWrapper.StartAuthentication(gameObject.name, nameof(OnAuthencationResponse));
	}

	[Beebyte.Obfuscator.SkipRename]
	private void OnAuthencationResponse(string result)
	{
		try
		{
			bool success = false;
			string address;
			List<string> characterTraits = new List<string>();

			string[] tokens = result.Split(',');
			success = bool.Parse(tokens[0]);
			if (!success)
			{
				throw new Exception($"Authentication unsuccessful: {result}");
			}
			address = tokens[1];
			if (address.Length < 20 || !Utils.IsHexString(address))
			{
				throw new Exception("Account address is not a valid hex string");
			}
			userAddress = address.ToLower();
		}
		catch (Exception e)
		{
			Debug.LogError("Authentication failed");
			Debug.LogError(e);
			AuthenticationFailed();
			return;
		}
		if (userAddress == null)
		{
			AuthenticationFailed();
			return;
		}
		SetStatusText("Fetching Available Traits");

		// ContractHelper.GetRemovedTraits(userAddress, OnRemovedTraitsReady);
		JSWrapper.GetRemovedTraits(gameObject.name, nameof(OnRemovedTraitsStringReady));

	}

	private void OnRemovedTraitsReady(int[] traits)
	{
		foreach (int t in traits)
		{
			print($"Trait ID {t} : \"{TraitInfo.traits[t].name}\" Removed");
		}
		removedTraits = traits;
		Invoke(nameof(StartCustomization), 2.01f);
	}

	[Beebyte.Obfuscator.SkipRename]
	private void OnRemovedTraitsStringReady(string traitsStr)
	{
		traitsStr = traitsStr.Replace("[", "").Replace("]", "").Replace(" ", "");
		if (traitsStr.Length > 0)
		{
			string[] tokens = traitsStr.Split(',');
			OnRemovedTraitsReady(tokens.Select(s => int.Parse(s)).ToArray());
		}
		else
		{
			OnRemovedTraitsReady(new int[0]);
		}
	}

	private void AuthenticationFailed()
	{
		SetStatusText("Initialization Failed!\n\nMake sure you wallet is connected and try again");
	}

	public static void BlockInput()
	{
		I.block.gameObject.SetActive(true);
	}

	public static void SubmitTraits(string traits)
	{
		I.block.gameObject.SetActive(true);
		CustomizationManager.SetActionState(-1, true);
		JSWrapper.SubmitTraits(traits, I.gameObject.name, nameof(OnSubmitTraitsResponse));
	}

	[Beebyte.Obfuscator.SkipRename]
	private void OnSubmitTraitsResponse(string result)
	{
		I.block.gameObject.SetActive(false);
		Debug.Log($"Submit trait response ready: {result}");
		bool success = bool.Parse(result);
		if (success)
		{
			DestroyImmediate(AnimationManager.I.gameObject);
			DestroyImmediate(CustomizationManager.I.gameObject);
			CustomizationManager.Reset();
			SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex, LoadSceneMode.Single);
		}
		else
		{
			//#CharacterCreatorLevel.EndMintEffects();
			CustomizationManager.SetActionState(0, true);
		}
	}

	private void SetStatusText(string text)
	{
		statusText.text = text.Trim().ToUpper();
	}

#endif //#else //#if UNITY_STANDALONE

	[Beebyte.Obfuscator.SkipRename]
	private void StartCustomization()
	{
		statusText.gameObject.SetActive(false);
		customizationManager.gameObject.SetActive(true);
		options.gameObject.SetActive(true);
		block.gameObject.SetActive(false);
	}
}
