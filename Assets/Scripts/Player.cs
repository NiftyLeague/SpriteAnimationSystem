//#using Photon.Realtime;
using UnityEngine;

public class Player
{
	//#public NetworkPlayer NetworkPlayer { get { return NetworkManager.GetPlayerByNetworkId(networkPlayerId); } }
	public NiftyDegen degen = null;
	public InputState Input => input;
	public InputReader.Device InputDevice => inputDevice;
	public Team Team => team;
	public Color Color { get { return degen != null ? (Color)degen.primaryColor : Color.white; } }
	public int Score => score;
	public int RoundWins => roundWins;
	public SpriteRenderer OffscreenDot => offscreenDot;
	public int OrderPriority => orderPriority;
	public Character Character => character;
	public float SpawnDelay => spawnDelay;
	public int RoundSortPriority => score * 100000 + roundHits * 10 + orderPriority;
	public int WinSortPriority => RoundWins * 10000 + totalScore * 10 + orderPriority;
	public Sprite originalSprite;

	private InputState input = new InputState();
	private Team team;
	private int score;
	private int totalScore;
	private int totalHits;
	private int roundHits;
	private int roundWins;
	private SpriteRenderer offscreenDot;
	private int orderPriority;
	private Character character;
	private float spawnDelay = 0f;
	private InputReader.Device inputDevice;
	private int networkPlayerId = -1;
	/*#
	internal InputView inputView;
	
	public Player(NetworkPlayer networkPlayer, int orderPriority, InputReader.Device inputDevice = InputReader.Device.AnyFree)
	{
		this.networkPlayerId = networkPlayer.NetworkId;
		this.inputDevice = inputDevice;
		this.orderPriority = orderPriority;
	}

	public InputState ReadInput()
	{
		InputReader.GetInput(inputDevice, input);
		return input;
	}

	public void SetDegen(NiftyDegen degen)
	{
		if (character)
		{
			this.degen = degen;
			character.type = degen.type;
			degen.Rasterize(false, res => { *//*Debug.Log($"Rasterization complete for {networkPlayer.NetworkId}");*//* });
		}
		else
		{
			Debug.LogError("Set degen called without a character");
		}
	}

	public void ClearInput()
	{
		InputReader.ClearInputState(input);
	}

	public void SetInputDevice(InputReader.Device? device)
	{
		inputDevice = device.HasValue ? device.Value : InputReader.Device.AnyFree;
	}

	public void SetTeam(Team team)
	{
		this.team = team;
	}

	public void AddScore(int toAdd)
	{
		score += toAdd;
	}

	public void SetScore(int score)
	{
		this.score = score;
	}

	public void SetSpawnDelay(float spawnDelay)
	{
		this.spawnDelay = spawnDelay;
	}

	public void SetOffscreenDot(SpriteRenderer offscreenDot)
	{
		this.offscreenDot = offscreenDot;
	}

	public void SetCharacter(Character character)
	{
		this.character = character;
		originalSprite = Character.GetComponent<CharacterAnimator>().GetFrame(AnimationTagType.Accessories, 0);
		inputView = character.GetComponent<InputView>();
		if (inputView)
		{
			inputView.SetPlayer(this);
		}
	}

	public void SetRoundWins(int roundWins)
	{
		this.roundWins = roundWins;
		totalScore += score;
		totalHits += roundHits;
		score = 0;
		roundHits = 0;
	}

	public void RegisterHit()
	{
		roundHits++;
	}

	public void InitializeForNewRound()
	{
		roundHits = 0;
		score = 0;
	}#*/
}


public enum Team
{
	Blue,
	Red
}
