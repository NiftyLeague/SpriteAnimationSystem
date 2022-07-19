//#using Photon.Pun;
using System;
using System.Collections.Generic;
using UnityEngine;

public enum CharacterState
{
	Normal,
	Attacking,
	Bouncing,
	Special,
	Burping,
	Posing,
}

public enum AttackState
{
	Idle,
	Charging,
	Attacking,
	Recovering
}

public enum TongueState
{
	Extending,
	Retracting,
	AttachedToTerrain,
	RetractingHitEnemy,
	RetractingHitEnemyTongue,
	RetractingHitPowerup,
	HitEnemyTongueStunned,
	HitPowerupBurping
}

public enum ThrowState
{
	Idle,
	Charging,
	Throwing,
	Thrown
}

public enum TeleportState
{
	Idle,
	Teleporting
}

public enum SpinDashState
{
	Idle,
	Starting,
	Charging,
	Spinning,
	Ending
}

public enum PounceState
{
	Idle,
	Charging,
	Active
}

public class Character : MonoBehaviour
{
	public CharacterEvents characterEvents;
	public BoxCollider2D boxCollider;

	[HideInInspector]
	public Player lastHitByPlayer;
	public CharacterType type;
	public CharacterState state;
	public AttackState attackState;
	public static bool isTeamMode;

	private Vector2 boxColliderSize;

	public bool IsActive { get; protected set; }
	public Player player;
	int facingDir = 1;

	public float timebumpMax;
	public float spawnInvincibilityTimeout;

	[Header("Standard Motion")]
	public float maxRunSpeed;
	public float runAccel, airAccel, skidAccel;
	public float maxFallSpeed;
	public float maxFallSpeedWallSlide;
	public float jumpVel;
	public float gravity;
	public float jumpGraceTime;
	public float gravityGraceTime;
	public float graceGravityM;
	public float gravityGraceThreshold;
	public float doubleJumpMultiplier;
	public float doubleJumpDelay;

	[Header("Bounce Motion")]
	public float bounceAccel;
	public float bounceGravityMin;
	public float bounceGravityMax;
	public float bounceGravityRestoreDelay;
	public float bounceGravityRestoreTime;
	public float bounceDodgePower;
	public Vector2 skidRecoverTimeRange;
	public float skidRecoverTimeHitMultiplier;

	[HideInInspector]
	public bool canBounceDodge;
	[HideInInspector]
	public bool hasBounceDodged;

	bool canBounceTongue;
	bool hasBounceTongued;

	bool hasReachedApex;
	[HideInInspector]
	public bool wasHitDownwards;

	[HideInInspector]
	public float bounceGravityRestoreCounter;


	[Header("Attack")]
	public float attackChargeTime;
	float attackChargeCounter;

	[HideInInspector]
	public float gravityGraceTimeLeft;
	public float attackTime;
	public float attackRecoverTime;

	internal void TimeBump(float durationM, float scale, bool force = false)
	{
		if (TimeBumpActive)
		{
			timeBumpTimeScale = Mathf.Min(timeBumpTimeScale, scale);
		}
		else
			timeBumpTimeScale = scale;

		timeBumpTimeLeft = force ? durationM * timebumpMultiplier : Mathf.Max(timeBumpTimeLeft, durationM * timebumpMultiplier);
		timeBumpTimeLeft = Mathf.Clamp(timeBumpTimeLeft, 0f, timebumpMax);
	}

	public float attackRange;

	float jumpGraceTimeLeft;
	float jumpTime;
	bool doubleJump;

	const float width = 2f, height = 2f;

	[HideInInspector]
	public Vector2 specialAttackDir;

	[Header("Pounce")]
	public Vector2 pounceChargeTime;
	public Vector2 pounceAttackTime;
	public float pounceCooldown;
	public float pounceBoost;
	public float pounceAttackBoost;
	public PounceState pounceState { protected set; get; }
	internal float pounceCharged;
	private float pounceAttackLeft;
	private float pounceCooldownLeft;


	[Header("Spin Dash")]
	public float spinDashSpeed;
	public float spinDashDelay;
	public float spinDashCooldown;
	public Vector2 spinDashDir;
	public float spinDashRadius;
	public float spinDashPower;
	public float spinDashHitDelay;
	public int spinDashDirChangesMax;
	public SpinDashState spinDashState { protected set; get; }
	private float spinDashDelayLeft;
	private float spinDashCooldownLeft;
	private float lastFrameSpinSpeed = -1f;
	private float spinDashStartTime = 0f;
	private int spinDashDirChanges = 0;
	private List<Player> spinDashHitPlayers = new List<Player>();


	[Header("Throw")]
	public float throwSpeed;

	public Vector2 bombSpeedRange;

	public float throwDelay;
	public float throwChargeTimeMax;
	public float throwTimeout;
	public ThrowableBehavior bananaPrefab;
	public ThrowableBehavior bombPrefab;
	private ThrowableBehavior throwable = null;

	public ThrowState throwState { protected set; get; }
	private float throwDelayLeft;
	private float throwChargeTime = 0f;
	private float throwTimeoutLeft = 0f;

	[Header("Teleport")]
	public Vector2 teleportRange;
	public float teleportDelay;
	public float teleportCooldown;
	public float teleportExplosionRadius;
	public float teleportExplosionPower;
	[HideInInspector]
	public Vector2 teleportDir;
	public TeleportState teleportState { protected set; get; }
	private float teleportDelayLeft;
	private float teleportCooldownLeft;
	private Vector3 teleportPosition;

	[Header("Tongue")]
	public float tongueRange;
	public float tongueSpeed;
	public float tongueRetractSpeedLatched;
	public float tongueRetractSpeedMissed;
	[SerializeField]
	private Vector2 tongueOrigin;
	public Vector2 tongueDirOffset;
	public float tongueDelay;
	public float tongueTimeout;
	public float minimumTongueDistance;
	public float frogJumpMultiplier;
	public float frogWallSlideMultiplier;

	float tongueDelayLeft;
	float tongueLastEndTime;

	public Vector2 tongueDir { protected set; get; }
	public float tongueDistance { protected set; get; }
	public TongueState tongueState { protected set; get; }
	public bool wasBouncingBeforeSpecial { protected set; get; }
	float timeBumpTimeLeft;
	public float timeBumpTimeScale { get; protected set; }

	public bool TimeBumpActive { get { return timeBumpTimeLeft > 0f; } }

	public bool JustSpawned { get { return Time.time - spawnTime < spawnInvincibilityTimeout; } }
	public bool CanGetSoftHit
	{
		get
		{
			return !JustSpawned && teleportState != TeleportState.Teleporting &&
				spinDashState != SpinDashState.Charging && spinDashState != SpinDashState.Spinning &&
				pounceState != PounceState.Charging;
		}
	}

	public float t { get; protected set; }

	float skidRecoverTimeLeft;
	[HideInInspector]
	public Vector2 velocity, velocityT;

	public Vector3 Center
	{
		get
		{
			return transform.position + height * 0.5f * Vector3.up;
		}
	}

	[HideInInspector]
	public int hitsTaken;
	[HideInInspector]
	public int powerupHits;
	public Vector2Int powerupHitMax;
	private bool shouldBurpAfterHit = false;

	public Powerup ingestingPowerup { get; protected set; }


	[HideInInspector]
	public float attackTimeLeft;
	[HideInInspector]
	public float attackRecoverTimeLeft;


	public bool attackFullyCharged
	{
		get
		{
			return (attackChargeCounter > attackChargeTime) && attackState == AttackState.Charging;
		}
	}

	public float attackChargeM
	{
		get
		{
			return Mathf.Clamp01(attackChargeCounter / attackChargeTime);
		}
	}


	public bool OnGround
	{
		get
		{
			return onGround;
		}
	}

	public Vector2 Velocity
	{
		get
		{
			return velocity;
		}
	}

	public float HitTime
	{
		get
		{
			return timeSinceHit;
		}
	}

	public int FacingDirection
	{
		get
		{
			return facingDir;
		}
	}

	public bool WallSliding
	{
		get; protected set;
	}

	public bool IngestedPowerup
	{
		get; protected set;
	}


	//#public EffectsController.Side WallSlideSide { get; protected set; }

	bool onGround;

	float timeSinceHit;

	int terrainLayer, characterLayer, groundLayer, tongueLayer, powerupLayer, throwableLayer;

	InputState input; //#

	private const float timebumpMultiplier = 0.125f;
	//#internal CharacterView view;

	private float spawnTime = 0f;
	private bool lastDeathSuicide = false;

	public bool RecoveringFromBounce
	{
		get
		{
			return hasReachedApex;
		}
	}
	/*##*/
	void Start()
	{
		print($"Character.Start: state: {state}"); //#
		CheckInput();
	}
	
	void Awake()
	{
		terrainLayer = 1 << LayerMask.NameToLayer("Ground");
		groundLayer = terrainLayer | (1 << LayerMask.NameToLayer("OneWayPlatform"));
		characterLayer = 1 << LayerMask.NameToLayer("Character");
		tongueLayer = 1 << LayerMask.NameToLayer("Tongue");
		powerupLayer = 1 << LayerMask.NameToLayer("Powerup");
		throwableLayer = 1 << LayerMask.NameToLayer("Throwable");
#if USE_TEST_CHARACTER
		SetActive(true);
#endif
		if (boxCollider)
		{
			boxColliderSize = boxCollider.size;
		}
	}

	public void FakeDeath()
	{
		/*#lastDeathSuicide = lastHitByPlayer == null;
		SetActive(false);
		gameObject.SetActive(false);
		EndBurp();#*/
	}

	public void FakeSpawn(Vector3 position)
	{
		/*#if (view.photonView.IsMine)
		{
			transform.position = position;
			transform.rotation = Quaternion.identity;
		}
		gameObject.SetActive(true);
		SetActive(true);
		EndBurp();
		spawnTime = lastDeathSuicide ? 0f : Time.time;#*/
	}

	public bool IsDead()
	{
		return !IsActive && !gameObject.activeSelf;
	}

	public void SetActive(bool active)
	{
		IsActive = active;
		velocity = Vector2.zero;
		state = CharacterState.Normal;
		attackState = AttackState.Idle;
		throwState = ThrowState.Idle;
		pounceState = PounceState.Idle;
		teleportState = TeleportState.Idle;
		tongueState = TongueState.Extending;
		spinDashState = SpinDashState.Idle;
		spinDashDirChanges = 0;
		wasBouncingBeforeSpecial = false;
		skidRecoverTimeLeft = 0f;
		attackRecoverTimeLeft = 0f;
		attackTimeLeft = 0f;
		timeBumpTimeLeft = 0f;
		timeSinceHit = 0f;
		hitsTaken = 0;
		powerupHits = 0;
		attackTime = 0f;
		gravityGraceTimeLeft = 0f;
		specialAttackDir = Vector2.zero;
		ingestingPowerup = null;
		IngestedPowerup = false;
		shouldBurpAfterHit = false;
		EndBurp();
		GetComponent<CharacterAnimator>().Reset();
		facingDir = transform.position.x > 0f ? -1 : 1;
		spawnTime = 0f;
		lastHitByPlayer = null;
		throwTimeoutLeft = 0f;
		spinDashDelayLeft = 0f;
		spinDashCooldownLeft = 0f;
		pounceCooldownLeft = 0f;
		teleportCooldownLeft = 0f;
		tongueLastEndTime = 0f;
		ResetColliderHeight();
	}

	public void DeactivateAndPose()
	{
		SetActive(false);
		state = CharacterState.Posing;
	}
	
	void CheckInput()
	{
		return;/*#
		if (player != null && player.Input != null)
		{
#if USE_TEST_CHARACTER
			player.ReadInput();
#endif
			if (GameController.HasInstance && GameController.State == GameState.RoundFinished && !IsWinningPlayer() || !IsActive)
			{
				player.ClearInput();
			}
		}#*/
	}

	public bool IsWinningPlayer()
	{
		/*#if (GameController.isTeamMode)
		{
			if (GameController.GetWinningPlayer() != null)
				return player.Team == GameController.GetWinningPlayer().Team;
		}
		else
		{
			return player == GameController.GetWinningPlayer();
		}#*/
		return false;
	}
	
	void Update()
	{
		//#print($"Character.Update: state: {state}"); //#
		/*#if (IsDead())
		{
			return;
		}#*/
		CheckInput();

		if (TimeBumpActive)
		{
			timeBumpTimeLeft -= Time.deltaTime;
			t = Time.deltaTime * timeBumpTimeScale;

			if (!TimeBumpActive)
			{
				TimebumpEnded();
			}
		}
		else
		{
			t = Time.deltaTime;
		}

		if (type == CharacterType.Cat && pounceState == PounceState.Active)
		{
			pounceAttackLeft -= Time.deltaTime;
			t *= pounceAttackLeft > 0f ? pounceBoost : 1f;
			if (pounceAttackLeft <= 0)
			{
				pounceState = PounceState.Idle;
				pounceCooldownLeft = pounceCooldown;
				ResetColliderHeight();
			}
		}
		/*#
		if (input != null)
		{
			if (state != CharacterState.Bouncing)
				AddInputMotionNormal();
			else
				AddInputMotionBouncing();
		}
		
		RunPhysics();
		ClampMotion();
		ApplyMotionVector();#*/
		
		if (state == CharacterState.Attacking)
		{
			RunAttack();
		}
		else if (state == CharacterState.Special)
		{
			RunSpecial();
		}
		/*#
		if (input != null && input.PressedY)
		{
			Vector2 dir = Vector2.zero;
			if (input.right)
				dir = Vector2.right;
			if (input.left)
				dir = -Vector2.right;
			if (input.up)
				dir += Vector2.up;
			if (input.down)
				dir -= Vector2.up;

			//GetHit(dir, UnityEngine.Random.value, this);

		}#*/
		CheckPowerup();
		CheckDeath();

		if (throwable == null)
		{
			throwTimeoutLeft -= Time.deltaTime;
		}

		if (ingestingPowerup != null && !ingestingPowerup.gameObject.activeSelf)
		{
			ingestingPowerup.transform.position = Center;
		}

		if (spinDashCooldownLeft > 0f)
		{
			spinDashCooldownLeft -= t;
		}
		if (teleportCooldownLeft > 0f)
		{
			teleportCooldownLeft -= t;
		}
		if (pounceCooldownLeft > 0f)
		{
			pounceCooldownLeft -= t;
		}
	}

	private void TimebumpEnded()
	{
		if (shouldBurpAfterHit)
		{
			SpitPowerup(true);
		}
	}

	void CheckDeath()
	{
#if DEBUG_TEST //# ----------
		return;
#endif
		/*#if (transform.position.x < Terrain.LeftKillPoint || transform.position.x > Terrain.RightKillPoint || transform.position.y > Terrain.TopKillPoint || transform.position.y < Terrain.BotKillPoint)
		{
			characterEvents.Die(lastHitByPlayer == null ? null : lastHitByPlayer.Character);
		}#*/
	}

	public void Die(Player lastHitPlayer, EffectsController.Side? side = null)
	{
		/*#EffectsController.Side killedOnSide;
		if (side != null)
		{
			killedOnSide = (EffectsController.Side)side;
		}
		else if (transform.position.x < Terrain.LeftKillPoint)
		{
			killedOnSide = EffectsController.Side.Left;
		}
		else if (transform.position.x > Terrain.RightKillPoint)
		{
			killedOnSide = EffectsController.Side.Right;
		}
		else if (transform.position.y > Terrain.TopKillPoint)
		{
			killedOnSide = EffectsController.Side.Top;
		}
		else
		{
			killedOnSide = transform.position.y > 5f ? EffectsController.Side.Top : EffectsController.Side.Bottom;
		}

		SendRegisterKill(lastHitByPlayer, player, hitsTaken);

		if (lastHitByPlayer == null)
			SoundController.PlaySoundEffect("KnockoutSuicide", 0.45f, transform.position);
		else if (hitsTaken <= 1)
			SoundController.PlaySoundEffect("Knockout1", 0.55f, transform.position);
		else if (hitsTaken <= 3)
			SoundController.PlaySoundEffect("Knockout2", 0.65f, transform.position);
		else
			SoundController.PlaySoundEffect("Knockout3", 0.75f, transform.position);

		if (lastHitByPlayer != null)
		{
			int maxHits;
			if (GameController.PlayerCount <= 2)
			{
				maxHits = Mathf.Min(hitsTaken, 3);
			}
			else
			{
				maxHits = Mathf.Min(hitsTaken, 5);
			}
			EffectsController.CreateSideScorePlum(transform.position, killedOnSide, Mathf.Clamp(hitsTaken, 1, maxHits), lastHitByPlayer.Color);
		}
		else
		{
			EffectsController.CreateSideScorePlum(transform.position, killedOnSide, -1, player.Color);
		}
		if (transform.position.y > Terrain.TopKillPoint)
		{
			EffectsController.CreateKnockedUpEffect(GetComponent<CharacterAnimator>()); // TODO: Consider changing the name of this effect here and elsewhere.
			if (player != null)
				player.SetSpawnDelay(3f);

		}
		if (IngestedPowerup && ingestingPowerup && ingestingPowerup.gameObject)
		{
			PhotonNetwork.Destroy(ingestingPowerup.gameObject);
		}
		//Destroy(gameObject);
		FakeDeath();#*/
	}

	public Vector2 attackDir;/*##*/
	private float jumpCooldownLeft;

	void SendRegisterKill(Player gotPoint, Player gotKilled, int hits)
	{
		/*#if (view.photonView.IsMine && gotPoint != null && gotPoint.Character && gotKilled != null && gotKilled.Character)
		{
			characterEvents.RegisterKill(gotPoint.Character, gotKilled.Character, hits);
		}#*/
	}

	void RunAttack()
	{
		print($"Character.RunAttack: state: {state}, attackState: {attackState}"); //#

		/*#if (attackState == AttackState.Charging)
		{
			attackDir = facingDir * Vector2.right;
			attackChargeCounter += t;
			if (input.up && !input.down)
			{
				attackDir = Vector2.up +
				Convert.ToInt32(input.left) * Vector2.left + // && !input.right isn't needed here.
				Convert.ToInt32(input.right) * Vector2.right; // && !input.left isn't needed here.
			} else if (input.down && !OnGround && !input.up) {
				attackDir = Vector2.down +
				Convert.ToInt32(input.left) * Vector2.left + // && !input.right isn't needed here.
				Convert.ToInt32(input.right) * Vector2.right; // && !input.left isn't needed here.
			}
			//
		}#*/

		/*#if (!input.xButton && input.wasXButton && attackState == AttackState.Charging)
		{
			attackState = AttackState.Attacking;
			SoundController.PlaySoundEffect("BatSwing", 0.4f + attackChargeM * 0.4f, transform.position);
			if (attackChargeM > 0.25f || IngestedPowerup)
				SoundController.PlaySoundEffect("BatSwingVoice", 0.4f, transform.position);
			attackTimeLeft = attackTime;
			if (attackChargeM > 0.5f)
			{
				#*//*#if (attackDir == Vector2.left || attackDir == Vector2.right)
				{
					EffectsController.CreateShingEffect(Center + (Vector3)attackDir * 3f + Vector3.up * 0.2f, attackDir);
				}
				else if (attackDir == Vector2.up)
				{
					EffectsController.CreateShingEffect(Center + (Vector3)attackDir * 3.75f, attackDir);
				}
				else if (attackDir == Vector2.down)
				{
					EffectsController.CreateShingEffect(Center + (Vector3)attackDir * 2.75f, attackDir);
				}
				else if (attackDir.y > 0f)
				{
					EffectsController.CreateShingEffect(Center + (Vector3)attackDir * 2.75f, attackDir);
				}
				else
				{
					EffectsController.CreateShingEffect(Center + (Vector3)attackDir * 2.75f, attackDir);
				}#*//*#
			}

		}
		else#*/
		if (attackState == AttackState.Attacking)
		{
			attackTimeLeft -= t;
			if (attackTimeLeft <= 0f)
			{
				RaycastHit2D[] hits;
				Vector2 attackOffset = Vector2.up;
				float rangeBonus = 0f;
				if (attackChargeM > 0.5f)
					rangeBonus = attackChargeM;
				float radius = 1.25f;
				if (attackDir.y < 0f)
					radius = 1.75f;
				hits = Physics2D.CircleCastAll((Vector2)transform.position + attackOffset, radius, attackDir, attackRange + rangeBonus, characterLayer | throwableLayer);
				Debug.DrawLine(transform.position + (Vector3)attackOffset, transform.position + (Vector3)attackOffset + (Vector3)attackDir.normalized * (attackRange + rangeBonus + radius), Color.red, 1f);
				/*#
				if (hits.Length > 0)
				{
					for (int i = 0; i < hits.Length; i++)
					{
						var hitChar = hits[i].collider.gameObject.GetComponent<Character>();
						if (hitChar != null && hitChar != this && !hitChar.JustSpawned)
						{
							var wallHits = Physics2D.LinecastAll(transform.position + Vector3.up, hitChar.transform.position + Vector3.up, terrainLayer);
							//Debug.Break();
							if (wallHits.Length == 0)
							{
								Hit(hitChar, attackDir);
							}
						}
						else
						{
							var th = hits[i].collider.gameObject.GetComponent<ThrowableBehavior>();
							if (th != null)
							{
								Vector2 vel = attackDir.normalized;
								if (th is BombBehavior)
								{
									if (vel.y == 0f)
									{
										vel.y += 0.5f;
									}
									vel *= Mathf.Lerp(bombSpeedRange.x, bombSpeedRange.y * 1.6f, GetBatPower());
								}
								else
								{
									vel *= th.velocity.magnitude;
								}
								th.owner.characterEvents.ThrowableReflect(this, th.transform.position, vel);
							}
						}
					}
				}#*/
				attackState = AttackState.Recovering;
				attackRecoverTimeLeft = attackRecoverTime;
			}
		}
		else if (attackState == AttackState.Recovering)
		{
			attackRecoverTimeLeft -= t;
			if (attackRecoverTimeLeft < 0f)
			{
				attackState = AttackState.Idle;
				state = CharacterState.Normal;
			}
		}/*##*/
	}

	internal void SetPlayer(Player player)
	{
		this.player = player;
		input = player.Input;
	}

	void BounceFromWall(EffectsController.Side side)
	{
		if (timeSinceHit > 0.35f)
		{
			if (!hasBounceDodged)
				canBounceDodge = true;
			if (!hasBounceTongued)
				canBounceTongue = true;
		}
		if (((side == EffectsController.Side.Left || side == EffectsController.Side.Right) && Mathf.Abs(velocity.x) > 5f)
			|| ((side == EffectsController.Side.Bottom || side == EffectsController.Side.Top) && Mathf.Abs(velocity.y) > 5f))
		{
			EffectsController.CreateBouncePuff(transform.position + (Vector3)velocityT, side);
			SoundController.PlaySoundEffect("FrogBounce", 0.5f, transform.position);
			SoundController.PlaySoundEffect("FrogBounceVoice", 0.4f, transform.position);
		}
		if (state == CharacterState.Special)
		{
			state = CharacterState.Bouncing;
		}
		wasHitDownwards = false;
	}

	void ClampMotion()
	{
		bool wasOnGround = OnGround;
		bool wasWallSlide = WallSliding;
		onGround = false;
		WallSliding = false;
		RaycastHit2D hit;
		bool bouncedThisFrame = false;

		Vector2 leftFootPos = (Vector2)transform.position - Vector2.right * width * 0.05f;
		if (velocityT.y < 0f)
		{
			Debug.DrawLine(leftFootPos, leftFootPos + Vector2.down * velocityT.y);

			int layer = groundLayer;

			if ((input != null && input.down && input.aButton) || (state == CharacterState.Bouncing && wasHitDownwards))
				layer = terrainLayer;

			hit = Physics2D.Raycast(leftFootPos, Vector2.up, velocityT.y, layer);
			if (hit.collider != null)
			{
				onGround = true;

				velocityT.y = hit.point.y - leftFootPos.y;
				if ((state == CharacterState.Bouncing && !RecoveringFromBounce) || (state == CharacterState.Special && wasBouncingBeforeSpecial))
				{
					BounceFromWall(EffectsController.Side.Bottom);
					bouncedThisFrame = true;
					velocity.y *= -0.5f;
				}
				else
					velocity.y = 0f;
			}
		}

		Vector2 rightFootPos = (Vector2)transform.position + Vector2.right * width * 0.05f;
		if (velocityT.y < 0f)
		{
			int layer = groundLayer;

			if ((input != null && input.down && input.aButton) || (state == CharacterState.Bouncing && wasHitDownwards))
				layer = terrainLayer;

			hit = Physics2D.Raycast(rightFootPos, Vector2.up, velocityT.y, layer);
			if (hit.collider != null)
			{
				onGround = true;
				velocityT.y = hit.point.y - rightFootPos.y;
				if ((state == CharacterState.Bouncing && !RecoveringFromBounce) || (state == CharacterState.Special && wasBouncingBeforeSpecial))
				{
					if (!bouncedThisFrame)
					{
						BounceFromWall(EffectsController.Side.Bottom);
						velocity.y *= -0.5f;
					}
				}
				else
					velocity.y = 0f;
			}
		}

		Vector2 leftHeadPos = (Vector2)transform.position - Vector2.right * width * 0.49f + Vector2.up * height;
		Vector2 rightHeadPos = (Vector2)transform.position + Vector2.right * width * 0.49f + Vector2.up * height;
		bouncedThisFrame = false;
		if (velocityT.y > 0f)
		{
			hit = Physics2D.Raycast(rightHeadPos, Vector2.up, velocityT.y, terrainLayer);
			if (hit.collider != null)
			{
				velocityT.y = hit.point.y - rightHeadPos.y;
				jumpGraceTimeLeft = 0f;
				if (state == CharacterState.Bouncing)
				{
					BounceFromWall(EffectsController.Side.Top);
					bouncedThisFrame = true;
					velocity.y = -velocity.y;
				}
				else
					velocity.y = 0f;
			}
			hit = Physics2D.Raycast(leftHeadPos, Vector2.up, velocityT.y, terrainLayer);
			if (hit.collider != null)
			{
				velocityT.y = hit.point.y - leftHeadPos.y;
				jumpGraceTimeLeft = 0f;
				if (state == CharacterState.Bouncing)
				{
					if (!bouncedThisFrame)
					{
						velocity.y = -velocity.y;
						BounceFromWall(EffectsController.Side.Top);
					}
				}
				else
					velocity.y = 0f;
			}
		}


		leftFootPos = (Vector2)transform.position - Vector2.right * width * 0.5f + Vector2.up * 0.05f;
		rightFootPos = (Vector2)transform.position + Vector2.right * width * 0.5f + Vector2.up * 0.05f;
		leftHeadPos = (Vector2)transform.position - Vector2.right * width * 0.5f + Vector2.up * height * 0.98f;
		rightHeadPos = (Vector2)transform.position + Vector2.right * width * 0.5f + Vector2.up * height * 0.98f;

		bouncedThisFrame = false;
		/*#
		if (velocityT.x > 0)
		{
			hit = Physics2D.Raycast(rightFootPos, Vector2.right, velocityT.x, terrainLayer);
			if (hit.collider != null)
			{
				velocityT.x = hit.point.x - rightFootPos.x;
				if (state == CharacterState.Bouncing)
				{
					bouncedThisFrame = true;
					velocity.x = -velocity.x;
					BounceFromWall(EffectsController.Side.Right);
				}
				else
				{
					velocity.x = 0;
					if (input != null && input.right && !OnGround && velocity.y < 0f)
					{
						WallSliding = true;
						WallSlideSide = EffectsController.Side.Right;
					}
				}
			}

			hit = Physics2D.Raycast(rightHeadPos, Vector2.right, velocityT.x, terrainLayer);
			if (hit.collider != null)
			{
				velocityT.x = hit.point.x - rightFootPos.x;
				if (state == CharacterState.Bouncing)
				{
					if (!bouncedThisFrame)
					{
						velocity.x = -velocity.x;
						BounceFromWall(EffectsController.Side.Right);
					}
				}
				else
					velocity.x = 0;
			}
		}

		if (velocityT.x < 0)
		{
			hit = Physics2D.Raycast(leftFootPos, Vector2.right, velocityT.x, terrainLayer);
			if (hit.collider != null)
			{
				velocityT.x = hit.point.x - leftFootPos.x;
				if (state == CharacterState.Bouncing)
				{
					velocity.x = -velocity.x;
					BounceFromWall(EffectsController.Side.Left);
					bouncedThisFrame = true;
				}
				else
				{
					velocity.x = 0;
					if (input != null && input.left && !OnGround && velocity.y < 0f)
					{
						WallSliding = true;
						WallSlideSide = EffectsController.Side.Left;
					}
				}
			}
		}

		if (velocityT.x < 0)
		{
			hit = Physics2D.Raycast(leftHeadPos, Vector2.right, velocityT.x, terrainLayer);
			if (hit.collider != null)
			{
				velocityT.x = hit.point.x - leftFootPos.x;
				if (state == CharacterState.Bouncing)
				{
					if (!bouncedThisFrame)
					{
						velocity.x = -velocity.x;
						BounceFromWall(EffectsController.Side.Left);
					}
				}
				else
					velocity.x = 0;
			}
		}#*/

		if (OnGround && !wasOnGround)
		{
			SoundController.PlaySoundEffect("Land", 0.4f, transform.position);
			jumpCooldownLeft = 0.1f;

		}
		if (WallSliding && !wasWallSlide)
		{
			SoundController.PlaySoundEffect("Land", 0.4f, transform.position);
			jumpCooldownLeft = 0.1f;
		}

		if (onGround)
		{
			jumpGraceTimeLeft = jumpGraceTime;
		}
		else if (WallSliding)
		{
			jumpGraceTimeLeft = jumpGraceTime * 0.66f;
		}
		else
		{
			jumpGraceTimeLeft -= t;
			if (velocity.y <= gravityGraceThreshold)
			{
				gravityGraceTimeLeft -= t;
				if (gravityGraceTimeLeft < 0f)
					gravityGraceTimeLeft = 0f;
			}
		}
	}

	float GetStunRecoverTime(Vector2 hitVelocity)
	{
		var t = Mathf.InverseLerp(12f, 50f, hitVelocity.magnitude);
		return Mathf.Lerp(skidRecoverTimeRange.x, skidRecoverTimeRange.y, t);
	}

	void Hit(Character hitChar, Vector2 hitDir)
	{
		characterEvents.Hit(hitChar, hitDir, GetBatPower());
	}

	float GetBatPower()
	{
		float ingestPowerBoost = 0f;
		if (IngestedPowerup)
		{
			ingestPowerBoost = 1.5f;
		}

		float power = attackChargeM + ingestPowerBoost;
		if (type == CharacterType.Cat && pounceState == PounceState.Active)
		{
			power += pounceAttackBoost;
		}
		return power;
	}

	void SoftHit(Character victim, Vector2 hitDir)
	{
		SoftHit(victim, hitDir, 25f, 0.75f, 0.5f);
	}

	public void SoftHit(Character victim, Vector2 hitDir, float power, float victimTimeBump, float attackerTimeBump)
	{
		/*#if (view.photonView.IsMine)
		{
			characterEvents.SoftHit(victim, hitDir, power, victimTimeBump, attackerTimeBump);
		}#*/
	}

	public void GetSoftHit(Vector2 hitDir, Character attacker, float totalPower, float timeBump, float attackerTimeBump)
	{
		/*#TimeBump(timeBump, 0f, true);
		attacker.TimeBump(attackerTimeBump, 0f, true);
		EndBurp();#*/
	}

	public void GetSoftHitInit(Vector2 hitDir, Character attacker, float totalPower, float timeBump, float attackerTimeBump)
	{
		/*#if (GameController.isTeamMode)
		{
			if (player.Team == attacker.player.Team)
				return;
		}
		if (!IngestedPowerup && ingestingPowerup != null)
		{
			ingestingPowerup.BeingIngested = false;
			ingestingPowerup = null;
		}
		if (hitDir.y < -0.1f)
			wasHitDownwards = true;
		hasReachedApex = false;
		if (attacker.player != player)
		{
			lastHitByPlayer = attacker.player;
		}
		canBounceDodge = false;
		hasBounceDodged = false;
		canBounceTongue = false;
		hasBounceTongued = false;
		state = CharacterState.Bouncing;
		attackState = AttackState.Idle;
		throwState = ThrowState.Idle;
		pounceState = PounceState.Idle;
		spinDashState = SpinDashState.Idle;
		teleportState = TeleportState.Idle;
		tongueState = TongueState.Extending;
		teleportCooldownLeft = 0f;
		spinDashCooldownLeft = 0f;
		pounceCooldownLeft = 0f;
		tongueLastEndTime = 0f;
		ResetColliderHeight();
		if (hitDir.y == 0)
			hitDir.y = 0.1f;
		hitDir.Normalize();

		velocity = hitDir.normalized * totalPower;
		skidRecoverTimeLeft = GetStunRecoverTime(velocity);
		timeSinceHit = 0f;
		SoundController.PlaySoundEffect("TongueCollide", 0.5f, transform.position);
		TimeBump(timeBump + 50f, 0f);
		attacker.TimeBump(attackerTimeBump + (attackerTimeBump > 0f ? 50f : 0f), 0f);
		//TimeController.TimeBumpCharacters(transform.position, hitsTaken, 15f, true);
		//EffectsController.CreateHitEffect(transform.position + Vector3.up * height * 0.5f, timeBumpTimeLeft);
		EndBurp();#*/
	}

	public void GetHitByBouncingCharacter(Vector2 hitVelocity, Character bouncer, Player attackingPlayer)
	{
		/*#hitsTaken++;
		if (hitVelocity.y < -0.1f)
			wasHitDownwards = true;
		if (!IngestedPowerup && ingestingPowerup != null)
		{
			ingestingPowerup.BeingIngested = false;
			ingestingPowerup = null;
		}
		hasReachedApex = false;
		lastHitByPlayer = attackingPlayer;
		canBounceDodge = false;
		hasBounceDodged = false;
		canBounceTongue = false;
		hasBounceTongued = false;
		state = CharacterState.Bouncing;
		attackState = AttackState.Idle;
		throwState = ThrowState.Idle;
		pounceState = PounceState.Idle;
		spinDashState = SpinDashState.Idle;
		teleportState = TeleportState.Idle;
		tongueState = TongueState.Extending;
		ResetColliderHeight();
		spinDashDelayLeft = 0f;
		spinDashCooldownLeft = 0f;
		teleportCooldownLeft = 0f;
		pounceCooldownLeft = 0f;
		spinDashDirChanges = 0;
		tongueLastEndTime = 0f;
		if (hitVelocity.y == 0)
			hitVelocity.y = 0.33f;

		velocity = hitVelocity;
		skidRecoverTimeLeft = GetStunRecoverTime(velocity);
		timeSinceHit = 0f;

		TimeBump(bouncer.hitsTaken, 0f);
		bouncer.TimeBump(bouncer.hitsTaken, 0f);
		//EffectsController.CreateHitParticles(transform.position + Vector3.up * height * 0.5f, hitDir, totalPower,(int) (totalPower / 5f));
		SoundController.PlaySoundEffect("CharacterCollision", 0.5f, transform.position);
		TimeController.TimeBumpCharacters(transform.position, bouncer.hitsTaken, 15f, true);
		EffectsController.CreateLocalizedShake(transform.position + Vector3.up * height * 0.5f, velocity, velocity.magnitude, timeBumpTimeLeft);
		EffectsController.CreateHitEffect((Center + bouncer.Center) * 0.5f, timeBumpTimeLeft, false);
		EndBurp();#*/
	}

	public void GetHit(Vector2 hitDir, float timebump, Character attacker)
	{
		/*#TimeBump(timebump, 0f, true);
		attacker.TimeBump(timebump, 0f, true);
		EndBurp();

		GameController.RegisterHit(attacker.player, player);#*/
	}

	public void SpitPowerup(bool burp)
	{
		/*#if (IngestedPowerup)
		{
			IngestedPowerup = false;
			ingestingPowerup.transform.position = Center + (burp ? Vector3.up : Vector3.zero);
			ingestingPowerup.BeingIngested = false;
			ingestingPowerup.gameObject.SetActive(true);
		}
		if (ingestingPowerup != null)
		{
			ingestingPowerup.BeingIngested = false;
			ingestingPowerup = null;
		}
		powerupHits = 0;
		shouldBurpAfterHit = false;
		if (burp)
		{
			state = CharacterState.Burping;
			Invoke(nameof(EndBurp), 0.65f);
			attackDir = facingDir * Vector2.right;
		}#*/
	}

	public void GetHitInit(Vector2 hitDir, int hitsTaken, float power, Character attacker)
	{
		/*#this.hitsTaken = hitsTaken + 1;
		SpitPowerup(false);

		if (attacker.IngestedPowerup)
		{
			attacker.powerupHits++;
			if (attacker.powerupHits >= (GameController.PlayerCount > 2 ? powerupHitMax.y : powerupHitMax.x))
			{
				attacker.shouldBurpAfterHit = true;
			}
		}

		if (hitDir.y < -0.1f)
			wasHitDownwards = true;
		hasReachedApex = false;
		lastHitByPlayer = attacker.player;
		canBounceDodge = false;
		hasBounceDodged = false;
		canBounceTongue = false;
		hasBounceTongued = false;
		state = CharacterState.Bouncing;
		attackState = AttackState.Idle;
		throwState = ThrowState.Idle;
		pounceState = PounceState.Idle;
		spinDashState = SpinDashState.Idle;
		teleportState = TeleportState.Idle;
		tongueState = TongueState.Extending;
		ResetColliderHeight();
		if (hitDir.y == 0)
			hitDir.y = 0.33f;
		hitDir.Normalize();
		float totalPower = 10f + hitsTaken * 10f + power * 30f;

		velocity = hitDir.normalized * totalPower;
		skidRecoverTimeLeft = GetStunRecoverTime(velocity);
		timeSinceHit = 0f;
		float timeBump = hitsTaken + power;
		TimeBump(timeBump + 50f, 0f);
		attacker.TimeBump(timeBump + 50f, 0f);
		//EffectsController.CreateHitParticles(transform.position + Vector3.up * height * 0.5f, hitDir, totalPower,(int) (totalPower / 5f));
		SoundController.PlaySoundEffect("BatHit" + Mathf.Clamp(hitsTaken, 1, 5).ToString(), 0.5f, transform.position);
		SoundController.PlaySoundEffect("BatHitVoice" + Mathf.Clamp(hitsTaken, 1, 5).ToString(), 0.5f, transform.position);
		//TimeController.TimeBumpCharacters(transform.position, timeBump + 50f, 15f, true);
		float timebumpLife = Mathf.Clamp(timeBump * timebumpMultiplier, 0f, timebumpMax);
		EffectsController.CreateLocalizedShake(transform.position + Vector3.up * height * 0.5f, velocity, velocity.magnitude, timebumpLife);
		EffectsController.CreateHitEffect(transform.position + Vector3.up * height * 0.5f, timebumpLife, power >= 1f);
		if (power >= 1f)
		{
			EffectsController.ShakeCamera(hitDir, hitsTaken * 0.75f);
		}
		EndBurp();#*/
	}

	void ApplyMotionVector()
	{
		var pos = transform.position;
		pos += (Vector3)velocityT;
		pos.z = (state == CharacterState.Attacking || state == CharacterState.Special ? -0.2f : 0f);
		pos.z += player != null ? Mathf.Clamp(player.OrderPriority * -0.02f, -0.19f, 0f) : 0f;
		transform.position = pos;
	}

	void RunPhysicsBouncing()
	{
		/*#timeSinceHit += t;
		float gravityThisFrame = bounceGravityMin;
		if (timeSinceHit > bounceGravityRestoreDelay)
		{
			bounceGravityRestoreCounter += t;
			gravityThisFrame = Mathf.Lerp(bounceGravityMin, bounceGravityMax, Mathf.Clamp01(bounceGravityRestoreCounter / bounceGravityRestoreTime));
		}

		if (velocity.y >= 0f && velocity.y - gravityThisFrame * t < 0f)
		{
			hasReachedApex = true;
			if (hasReachedApex && !hasBounceDodged)
				canBounceDodge = true;
			if (!hasBounceTongued)
				canBounceTongue = true;
		}

		if (timeSinceHit > 1f && !hasBounceDodged)
		{
			canBounceDodge = true;

		}

		if (timeSinceHit > 1f && !hasBounceTongued)
		{
			canBounceTongue = true;
		}

		if (velocity.y > maxFallSpeed)
			velocity.y -= gravityThisFrame * t;

		//if (velocity.y < maxFallSpeed)
		//velocity.y = maxFallSpeed;

		if (GameController.charactersBounceEachOther && !GameController.isTeamMode && !hasBounceDodged && hitsTaken >= 1 && !OnGround)
		{
			var cols = Physics2D.OverlapCircleAll((Vector2)transform.position + Vector2.up * height * 0.5f, 0.5f, characterLayer);
			foreach (var col in cols)
			{
				var chr = col.GetComponent<Character>();
				if (chr != null && chr != this && chr.state != CharacterState.Bouncing && chr.player != lastHitByPlayer && !(chr.state == CharacterState.Attacking && chr.attackState == AttackState.Attacking))
				{
					if (!RecoveringFromBounce || !GameController.onlyBounceBeforeRecover)
					{
						if (!(TimeBumpActive && timeBumpTimeScale == 0))
						{
							chr.GetHitByBouncingCharacter(velocity * 0.75f, this, lastHitByPlayer);

							if (GameController.weirdBounceTrajectories)
							{
								Vector3 relativePos = transform.position - chr.transform.position;
								velocity = velocity.magnitude * relativePos.normalized * 0.75f;
							}
						}
					}
				}
			}
		}

		if (onGround && RecoveringFromBounce)
		{
			if (state == CharacterState.Special)
				state = CharacterState.Bouncing;
			if (velocity.x > 0)
			{
				velocity.x -= t * skidAccel;
				if (velocity.x < 0)
				{
					velocity.x = 0;
				}
			}
			else if (velocity.x < 0)
			{
				velocity.x += t * skidAccel;
				if (velocity.x > 0)
				{
					velocity.x = 0;
				}
			}

			if (Mathf.Abs(velocity.x) < maxRunSpeed)
			{
				skidRecoverTimeLeft -= t;
				if (skidRecoverTimeLeft <= 0f)
					StopBouncing();
			}
		}#*/
	}

	void RunPhysicsNormal()
	{
		if (input != null && input.aButton && velocity.y <= gravityGraceThreshold && gravityGraceTimeLeft > 0f)
		{
			float gravM = 1f - (gravityGraceTimeLeft) / gravityGraceTime;
			velocity.y -= gravity * gravM * t;
		}
		else
			velocity.y -= gravity * t;
		if (WallSliding)
		{
			float wallSlideSpeed = maxFallSpeedWallSlide * (type == CharacterType.Frog ? frogWallSlideMultiplier : 1f);
			if (velocity.y < wallSlideSpeed)
				velocity.y = wallSlideSpeed;
		}
		else
		{
			if (velocity.y < maxFallSpeed)
				velocity.y = maxFallSpeed;
		}
	}

	void RunPhysics()
	{
		if (state == CharacterState.Bouncing)
		{
			RunPhysicsBouncing();
		}
		else if (state == CharacterState.Special && type == CharacterType.Frog && tongueState == TongueState.AttachedToTerrain)
		{
			velocity = tongueDir * tongueRetractSpeedLatched;
		}
		else if (state == CharacterState.Special && type == CharacterType.Frog)
		{
			if (wasBouncingBeforeSpecial)
				RunPhysicsBouncing();
			else
				RunPhysicsNormal();
		}
		else if (state == CharacterState.Special && type == CharacterType.Alien && teleportState == TeleportState.Teleporting)
		{
			return;
		}
		else
		{
			RunPhysicsNormal();
		}
		velocityT = velocity * t;
		if (state != CharacterState.Attacking && state != CharacterState.Special)
		{
			if (velocity.x > 0f)
				facingDir = 1;
			if (velocity.x < 0f)
				facingDir = -1;
		}

		jumpCooldownLeft -= t;

		if (state == CharacterState.Special)
		{
			if (type == CharacterType.Frog)
			{
				if (tongueDir.x > 0)
					facingDir = 1;
				else if (tongueDir.x < 0)
					facingDir = -1;
			}
			else if (type == CharacterType.Human || type == CharacterType.Ape || type == CharacterType.Cat || type == CharacterType.Doge)
			{
				if (specialAttackDir.x > 0)
					facingDir = 1;
				else if (specialAttackDir.x < 0)
					facingDir = -1;
			}
		}
	}

	void AddInputMotionBouncing()
	{
		if (input.right)
		{
			if (velocity.x < maxRunSpeed * 0.5f)
				velocity.x += bounceAccel * t;
		}
		else if (input.left)
		{
			if (velocity.x > -maxRunSpeed * 0.5f)
				velocity.x -= bounceAccel * t;
		}

		if (canBounceDodge && input.aButton && !OnGround)
		{

			canBounceDodge = false;
			hasBounceDodged = true;

			Vector2 dir = Vector2.up;
			if (input.up)
				dir += Vector2.up;
			if (input.down)
				dir += Vector2.down;
			if (input.left)
				dir += Vector2.left;
			if (input.right)
				dir += Vector2.right;

			if (dir == Vector2.zero)
				dir = Vector2.up;
			dir.Normalize();

			velocity = dir * bounceDodgePower;
			bounceGravityRestoreCounter = 0f;

		}

		if (input.PressedB)
		{
			StartSpecialAttack();
		}
	}

	void StopBouncing()
	{
		state = CharacterState.Normal;
		wasHitDownwards = false;
		hitsTaken = 0;
		lastHitByPlayer = null;
		spinDashDelayLeft = 0f;
		spinDashCooldownLeft = 0f;
		teleportCooldownLeft = 0f;
		tongueLastEndTime = 0f;
		pounceCooldownLeft = 0f;
		spinDashDirChanges = 0;
		spinDashState = SpinDashState.Idle;
		pounceState = PounceState.Idle;
		throwState = ThrowState.Idle;
		teleportState = TeleportState.Idle;
		tongueState = TongueState.Extending;
		ResetColliderHeight();
	}


	void AddInputMotionNormal()
	{
		if (input.right && state == CharacterState.Normal)
		{
			facingDir = 1;
			if (onGround)
			{
				if (velocity.x < 0f)
				{
					if (velocity.x < maxRunSpeed * 0.9f)
					{
						EffectsController.CreateTurnAroundPuff(transform.position, 1f);
					}
					velocity.x = 0f;

				}
				velocity.x += t * runAccel;
			}
			else
			{
				velocity.x += t * airAccel;
			}
			if (velocity.x > maxRunSpeed)
				velocity.x = maxRunSpeed;
		}
		else if (input.left && state == CharacterState.Normal)
		{
			facingDir = -1;
			if (onGround)
			{
				if (velocity.x > 0f)
				{
					if (velocity.x > -maxRunSpeed * 0.9f)
					{
						EffectsController.CreateTurnAroundPuff(transform.position, -1f);
					}
					velocity.x = 0f;
				}
				velocity.x -= t * runAccel;
			}
			else
			{
				velocity.x -= t * airAccel;
			}
			if (velocity.x < -maxRunSpeed)
				velocity.x = -maxRunSpeed;
		}
		else if (type == CharacterType.Doge && state == CharacterState.Special && spinDashState == SpinDashState.Spinning)
		{
			if (velocity.x == 0f && lastFrameSpinSpeed == 0f)
			{
				EndSpinDash(false);
			}
			else if (spinDashDirChanges > spinDashDirChangesMax)
			{
				EndSpinDash(true);
			}
			lastFrameSpinSpeed = velocity.x;
			if (onGround)
			{
				velocity.x += t * runAccel * facingDir;
			}
			else
			{
				velocity.x += t * airAccel * facingDir;
			}
			if (velocity.x > spinDashSpeed)
				velocity.x = spinDashSpeed;
			if (velocity.x < -spinDashSpeed)
				velocity.x = -spinDashSpeed;
			if ((input.PressedLeft && specialAttackDir.x > 0.1f) || (input.PressedRight && specialAttackDir.x < -0.1f))
			{
				specialAttackDir.x = -specialAttackDir.x;
				spinDashDirChanges++;
			}
		}
		else
		{
			if (onGround)
			{
				if (velocity.x > 0)
				{
					velocity.x -= t * runAccel;
					if (velocity.x < 0)
						velocity.x = 0;
				}
				else if (velocity.x < 0)
				{
					velocity.x += t * runAccel;
					if (velocity.x > 0)
						velocity.x = 0;
				}
			}
		}
		return; //#
		if (input.xButton)
		{
			if (state == CharacterState.Normal)
			{
				state = CharacterState.Attacking;
				if (attackState == AttackState.Idle)
				{
					attackState = AttackState.Charging;
					SoundController.PlaySoundEffect("BatChargeUp", 0.5f, transform.position);
					attackChargeCounter = 0f;
				}
			}

			if (attackState == AttackState.Charging)
			{
				if (input.right)
				{
					facingDir = 1;
				}
				else if (input.left)
				{
					facingDir = -1;
				}
			}
		}

		if (input.bButton)
		{
			if (state == CharacterState.Normal && !input.wasBButton)
			{
				StartSpecialAttack();
			}
		}

		if (input.aButton &&
			!input.down &&
			(jumpCooldownLeft <= 0f || (!input.wasAButton && state != CharacterState.Attacking)) &&
			!(state == CharacterState.Special && (type == CharacterType.Cat || type == CharacterType.Human || type == CharacterType.Ape)))
		{
			if (state == CharacterState.Special && type == CharacterType.Doge)
			{
				EndSpinDash(false);
			}
			if (onGround || WallSliding)
			{
				Jump();
				gravityGraceTimeLeft = gravityGraceTime;

				/*#if (WallSliding)
				{
					if (WallSlideSide == EffectsController.Side.Left)
						velocity.x = maxRunSpeed;
					else if (WallSlideSide == EffectsController.Side.Right)
						velocity.x = -maxRunSpeed;
				}

				//Debug.Break();
				SoundController.PlaySoundEffect("Jump", 0.4f, transform.position);
				if (WallSliding)
					EffectsController.CreateJumpPuffStraight(transform.position, WallSlideSide);
				else
					EffectsController.CreateJumpPuffStraight(transform.position, EffectsController.Side.Bottom);
			#*/
			} else if (jumpGraceTimeLeft > 0f) // && (velocity.y > 0f || !input.wasAButton))
			{
				Jump();
			}
		}

		if (input.PressedA && !onGround && !WallSliding && !doubleJump && (type == CharacterType.Cat || type == CharacterType.Frog) && Time.time - jumpTime > doubleJumpDelay)
		{
			velocity.y = (input.down ? -jumpVel : jumpVel) * doubleJumpMultiplier;
			if (input.right || input.left)
			{
				velocity.x = input.right ? maxRunSpeed : -maxRunSpeed;
			}
			doubleJump = true;
			SoundController.PlaySoundEffect("Jump", 0.4f, transform.position);
			EffectsController.CreateSmokeRingWhite(transform.position, Quaternion.identity, Color.white);
		}
	}

	private void Jump()
	{
		velocity.y = jumpVel * (type == CharacterType.Frog ? frogJumpMultiplier : 1f);
		jumpTime = Time.time;
		doubleJump = false;
	}


	private void RunSpecial()
	{
		//#print($"Character.RunSpecial: state: {state}, type: {type}"); //#
		switch (type)
		{
		case CharacterType.Alien:
			print($"Character.RunSpecial: state: {state}, type: {type} - RunTeleport"); //#
			RunTeleport();
			break;
		case CharacterType.Ape:
			print($"Character.RunSpecial: state: {state}, type: {type} - RunThrow"); //#
			RunThrow();
			break;
		case CharacterType.Cat:
			print($"Character.RunSpecial: state: {state}, type: {type} - RunPounce"); //#
			RunPounce();
			break;
		case CharacterType.Doge:
			print($"Character.RunSpecial: state: {state}, type: {type} - RunSpinDash"); //#
			RunSpinDash();
			break;
		case CharacterType.Frog:
			print($"Character.RunSpecial: state: {state}, type: {type} - RunTongue"); //#
			RunTongue();
			break;
		case CharacterType.Human:
			print($"Character.RunSpecial: state: {state}, type: {type} - RunThrow"); //#
			RunThrow();
			break;
		}
	}

	private void RunPounce()
	{
		switch (pounceState)
		{
		case PounceState.Charging:
			if (!input.bButton && input.wasBButton)
			{
				if (pounceCharged >= pounceChargeTime.x)
				{
					pounceAttackLeft = Mathf.Lerp(pounceAttackTime.x, pounceAttackTime.y, ((pounceCharged - pounceChargeTime.x) / pounceChargeTime.y));
				}
				else
				{
					pounceAttackLeft = 0f;
				}
				state = wasBouncingBeforeSpecial ? CharacterState.Bouncing : CharacterState.Normal;
				pounceState = PounceState.Active;
				pounceCharged = 0f;
				pounceCooldownLeft = pounceCooldown;
			}
			specialAttackDir = GetSpecialDirection();
			pounceCharged += t;
			break;
		}
	}

	private void RunSpinDash()
	{
		if (spinDashDelayLeft > 0f && input.bButton)
		{
			spinDashDelayLeft -= t;
			return;
		}

		switch (spinDashState)
		{
		case SpinDashState.Starting:
			if (spinDashDelayLeft <= 0f && input.bButton)
			{
				spinDashState = SpinDashState.Charging;
				SoundController.PlaySoundEffect("SpinDashStart", 0.8f, transform.position);
				SetColliderHeight(boxColliderSize.y * 0.667f);
			}
			else
			{
				EndSpinDash(true);
			}
			break;
		case SpinDashState.Charging:
			if (input.ReleasedB)
			{
				StartSpinDash();
			}
			specialAttackDir = GetSpecialDirection();
			break;
		case SpinDashState.Spinning:
			if (input.PressedB)
			{
				EndSpinDash(true);
			}
			else if (Time.time - spinDashStartTime > spinDashHitDelay)
			{
				var cols = Physics2D.OverlapCircleAll(transform.position, spinDashRadius, characterLayer);
				foreach (var col in cols)
				{
					var chr = col.GetComponent<Character>();
					if (chr != null && chr != this && chr.CanGetSoftHit && !spinDashHitPlayers.Contains(chr.player))
					{
						spinDashHitPlayers.Add(chr.player);
						//chr.GetSoftHit(Vector2.up + Vector2.right * facingDir, this, spinDashPower, 0f, 0f);
						SoftHit(chr, Vector2.up + Vector2.right * facingDir, spinDashPower, 0f, 0f);
					}
				}
			}
			break;
		case SpinDashState.Ending:
			spinDashHitPlayers.Clear();
			state = wasBouncingBeforeSpecial ? CharacterState.Bouncing : CharacterState.Normal;
			spinDashState = SpinDashState.Idle;
			spinDashDirChanges = 0;
			ResetColliderHeight();
			break;
		}
	}

	private void StartSpinDash()
	{
		/*#if (view.photonView.IsMine)
		{
			characterEvents.SpinDashStart(new Vector2(facingDir, input.up ? 1f : 0f));
		}#*/
	}

	private void EndSpinDash(bool delay)
	{
		/*#if (view.photonView.IsMine)
		{
			characterEvents.SpinDashEnd(delay);
		}
		spinDashCooldownLeft = spinDashCooldown;#*/
	}

	public void DoSpinDashStart(Vector2 dir)
	{
		state = CharacterState.Special;
		spinDashState = SpinDashState.Spinning;
		if (dir.y > 0f)
		{
			velocity.y = jumpVel * doubleJumpMultiplier;
		}
		lastFrameSpinSpeed = -1f;
		spinDashStartTime = Time.time;
		specialAttackDir = dir.x > 0f ? Vector2.right : Vector2.left;
	}

	public void DoSpinDashEnd(bool delay)
	{
		if (delay)
		{
			spinDashDelayLeft = spinDashDelay;
		}
		spinDashCooldownLeft = spinDashCooldown;
		spinDashState = SpinDashState.Ending;
	}

	private void RunTeleport()
	{
		if (teleportDelayLeft > 0f)
		{
			teleportDelayLeft -= t;
			return;
		}
		switch (teleportState)
		{
		case TeleportState.Teleporting:
			transform.position = teleportPosition;
			EffectsController.CreateTeleportMewtwo(teleportPosition + new Vector3(0f, 1.5f, -2f));
			teleportState = TeleportState.Idle;
			state = wasBouncingBeforeSpecial ? CharacterState.Bouncing : CharacterState.Normal;
			EffectsController.CreateLocalizedShake(transform.position + Vector3.up * height * 0.5f, velocity, velocity.magnitude, 0.5f);
			teleportCooldownLeft = teleportCooldown;

			var cols = Physics2D.OverlapCircleAll(transform.position, teleportExplosionRadius, characterLayer);
			foreach (var col in cols)
			{
				var chr = col.GetComponent<Character>();
				if (chr != null && chr != this && (chr.CanGetSoftHit || chr.type == CharacterType.Cat || chr.type == CharacterType.Doge))
				{
					var wallHits = Physics2D.LinecastAll(transform.position + Vector3.up, chr.transform.position + Vector3.up, terrainLayer);
					if (wallHits.Length == 0)
					{
						Vector2 dir = (chr.transform.position - (transform.position + Vector3.down)).normalized;
						dir = dir + Vector2.up * 0.5f;
						SoftHit(chr, dir, teleportExplosionPower, 0f, 0f);
						SoundController.PlaySoundEffect("CharacterCollision", 0.5f, chr.transform.position);
					}
				}
			}

			break;
		}
	}

	private Vector3 GetTeleportPosition()
	{
		var hit = Physics2D.Raycast((Vector2)transform.position + Vector2.up * Mathf.Sign(teleportDir.y), teleportDir, teleportRange.y, groundLayer);
		if (hit)
		{
			if (hit.distance <= teleportRange.x)
			{
				return transform.position;
			}
			return hit.point - teleportDir - Vector2.up * Mathf.Sign(teleportDir.y);
		}
		return transform.position + (Vector3)teleportDir * teleportRange.y;
	}

	private void RunThrow()
	{
		if (throwDelayLeft <= 0f && throwState == ThrowState.Charging)
		{
			if (input.bButton)
			{
				throwChargeTime += t;
				specialAttackDir = GetSpecialDirection(false);
				return;
			}
			else if (input.wasBButton)
			{
				throwDelayLeft = throwDelay;
				float speed = type == CharacterType.Ape ? throwSpeed : Mathf.Lerp(bombSpeedRange.x, bombSpeedRange.y, throwChargeTime / throwChargeTimeMax);
				characterEvents.ThrowThrowable(speed * specialAttackDir, transform.position + (Vector3.up * height / 1.5f));
			}
		}

		if (throwDelayLeft > 0f)
		{
			throwDelayLeft -= t;
			return;
		}
		throwState = ThrowState.Thrown;
		state = wasBouncingBeforeSpecial ? CharacterState.Bouncing : CharacterState.Normal;
	}

	public void ThrowThroable(Vector2 velocity, Vector3 pos)
	{
		/*#throwDelayLeft = throwDelay;
		if (throwable != null)
		{
			Destroy(throwable);
		}
		throwable = Instantiate(type == CharacterType.Ape ? bananaPrefab : bombPrefab);
		throwable.Initialize(this, velocity, pos);
		throwState = ThrowState.Throwing;
		SoundController.PlaySoundEffect("Throw", 1f, pos);#*/
	}

	public void OnThrowableDestroy()
	{
		/*#if (!(throwable is BombBehavior))
		{
			throwTimeoutLeft = throwTimeout;
		}#*/
	}
	/*##*/
	public Vector3 GetTongueOrigin()
	{
		Vector3 offset = new Vector3(0f, 0f, 1.1f);
		if (tongueDir.y > 0.5f)
		{
			offset.y = -tongueDirOffset.y;
		}
		else if (tongueDir.y < -0.5)
		{
			offset.y = tongueDirOffset.y;
		}
		else if (tongueDir.x > 0.5f)
		{
			offset.x = -tongueDirOffset.x;
		}
		else if (tongueDir.x < -0.5f)
		{
			offset.x = tongueDirOffset.x;
		}
		return (Vector3)tongueOrigin + offset;
	}
	/*##*/
	void RunTongue()
	{
		/*#if (tongueDelayLeft > 0f)
		{
			tongueDelayLeft -= t;
			return;
		}

		if (tongueState == TongueState.Extending)
		{
			tongueDistance += tongueSpeed * t;

			if (tongueDir.x != 0f)
			{
				if (Mathf.Sign(tongueDir.x) != Mathf.Sign(velocity.x))
				{
					tongueDistance += Mathf.Abs(velocity.x) * t;
				}
			}
			if (tongueDir.y != 0f)
			{
				if (Mathf.Sign(tongueDir.y) != Mathf.Sign(velocity.y))
				{
					tongueDistance += Mathf.Abs(velocity.y) * t;
				}
			}
			int layer = terrainLayer;
			if (tongueDir.y < 0f)
				layer = groundLayer;
			var powerup = Physics2D.OverlapCircle((Vector2)(transform.position + GetTongueOrigin()) + tongueDir * tongueDistance, 0.5f, powerupLayer);
			if (powerup != null)
			{
				var powerupBehavior = powerup.GetComponent<Powerup>();
				if (powerupBehavior.CanBeIngested)
				{
					tongueState = TongueState.RetractingHitPowerup;
					ingestingPowerup = powerupBehavior;
					*//*ingestingPowerup.BeingIngested = true;*//*
					SoundController.PlaySoundEffect("TongueCollideSurface", 0.5f, TongueTipPos);
				}
			}
			else
				if (Physics2D.OverlapCircle((Vector2)(transform.position + GetTongueOrigin()) + tongueDir * tongueDistance, 0.5f, layer) != null)
			{
				if (tongueDistance > minimumTongueDistance)
				{
					tongueState = TongueState.AttachedToTerrain;
					SoundController.PlaySoundEffect("TongueCollideSurface", 0.5f, TongueTipPos);
				}
			}
			else
			{
				if (tongueDistance > minimumTongueDistance)
				{
					bool hitTongue = false;
					var cols = Physics2D.OverlapCircleAll((Vector2)(transform.position + GetTongueOrigin()) + tongueDir * tongueDistance, 1f, tongueLayer);
					foreach (var col in cols)
					{
						var chr = col.GetComponentInParent<Character>();
						if (chr != null && chr != this)
						{
							tongueState = TongueState.RetractingHitEnemyTongue;
							EffectsController.CreateTongueHitEffect(TongueTipPos, 0.2f);
							hitTongue = true;
						}
					}
					if (!hitTongue)
					{
						cols = Physics2D.OverlapCircleAll((Vector2)(transform.position + GetTongueOrigin()) + tongueDir * tongueDistance, 1f, characterLayer);
						foreach (var col in cols)
						{
							var chr = col.GetComponent<Character>();
							if (chr != null && chr != this && chr.CanGetSoftHit)
							{
								if (!GameController.isTeamMode || (chr.player.Team != player.Team))
								{
									//chr.GetSoftHit(-tongueDir, this);
									SoftHit(chr, -tongueDir);
									tongueState = TongueState.RetractingHitEnemy;
									EffectsController.CreateTongueHitEffect(TongueTipPos, 0.2f);
								}
							}
						}
					}
				}
			}


			if (tongueState == TongueState.Extending)
			{
				if (tongueDistance > tongueRange)
					tongueState = TongueState.Retracting;

				//if (!input.bButton && tongueDistance > minimumTongueDistance)
				//    tongueState = TongueState.Retracting;
			}

		}
		else if (tongueState == TongueState.AttachedToTerrain)
		{
			tongueDistance -= tongueRetractSpeedLatched * t;
			if (tongueDistance <= 0f)
			{
				if (wasBouncingBeforeSpecial)
				{
					//if (tongueDir.y > 0f && tongueDir.x != 0f)
					bounceGravityRestoreCounter = 0f;

					state = CharacterState.Bouncing;
				}
				else
				{
					tongueLastEndTime = Time.time;
					state = CharacterState.Normal;
					jumpGraceTimeLeft = jumpGraceTime;
				}
			}
		}
		else if (tongueState == TongueState.Retracting)
		{
			tongueDistance -= tongueRetractSpeedMissed * t;
			if (tongueDistance <= 0f)
			{
				tongueLastEndTime = Time.time;
				state = wasBouncingBeforeSpecial ? CharacterState.Bouncing : CharacterState.Normal;
			}
		}
		else if (tongueState == TongueState.RetractingHitEnemy)
		{
			tongueDistance -= tongueSpeed * t;
			if (tongueDistance <= 0f)
			{
				tongueLastEndTime = Time.time;
				state = wasBouncingBeforeSpecial ? CharacterState.Bouncing : CharacterState.Normal;
			}
		}
		else if (tongueState == TongueState.RetractingHitEnemyTongue)
		{
			tongueDistance -= tongueRetractSpeedMissed * t;
			if (tongueDistance <= 0f)
			{
				if (wasBouncingBeforeSpecial)
					state = CharacterState.Bouncing;
				else
				{
					tongueState = TongueState.HitEnemyTongueStunned;
					tongueDelayLeft = 0.65f;
				}
			}
		}
		else if (tongueState == TongueState.RetractingHitPowerup)
		{
			tongueDistance -= tongueRetractSpeedMissed * t;
			ingestingPowerup.transform.position = TongueTipPos;
			if (tongueDistance <= 0f)
			{
				//ConsumePowerup(powerup);
				ingestingPowerup.GetComponent<Powerup>().TryIngest(this);
				tongueState = TongueState.HitPowerupBurping;
				tongueDelayLeft = 0.65f;
			}
		}
		else if (tongueState == TongueState.HitEnemyTongueStunned || tongueState == TongueState.HitPowerupBurping)
		{
			state = CharacterState.Normal;
			tongueLastEndTime = Time.time;
		}#*/
	}

	private bool CheckPowerup()
	{
		/*#var powerup = Physics2D.OverlapCircle(transform.position + GetTongueOrigin(), 0.6f, powerupLayer);
		if (powerup != null)
		{
			var powerupBehavior = powerup.GetComponent<Powerup>();
			bool ingestRequestSent = powerupBehavior.TryIngest(this);
			if (ingestRequestSent && powerupBehavior.CanBeIngested)
			{
				tongueState = TongueState.RetractingHitPowerup;
				//ingestingPowerup = powerup.GetComponent<Powerup>();
				//ingestingPowerup.BeingIngested = true;
				tongueDelayLeft = 0.65f;
				SoundController.PlaySoundEffect("TongueCollideSurface", 0.5f, TongueTipPos);
				return true;
			}
		}#*/
		return false;
	}

	public void ConsumePowerup(Powerup powerup)
	{
		if (wasBouncingBeforeSpecial)
		{
			StopBouncing();
		}
		SoundController.PlaySoundEffect("Burp", 0.5f, TongueTipPos);
		ingestingPowerup = powerup;
		IngestedPowerup = true;
		hitsTaken = 0;
		powerupHits = 0;
		ingestingPowerup.gameObject.SetActive(false);
		state = CharacterState.Burping;
		Invoke(nameof(EndBurp), 0.65f);
	}

	[Beebyte.Obfuscator.SkipRename]
	private void EndBurp()
	{
		shouldBurpAfterHit = false;
		if (state == CharacterState.Burping)
		{
			state = CharacterState.Normal;
			pounceState = PounceState.Idle;
			spinDashState = SpinDashState.Idle;
			throwState = ThrowState.Idle;
			teleportState = TeleportState.Idle;
			attackState = AttackState.Idle;
			ResetColliderHeight();
		}
		CancelInvoke(nameof(EndBurp));
	}

	Vector2 TongueTipPos
	{
		get
		{
			return (Vector2)(transform.position + GetTongueOrigin()) + tongueDir * tongueDistance;
		}
	}

	void SetColliderHeight(float height)
	{
		if (boxCollider)
		{
			boxCollider.size = new Vector2(boxColliderSize.x, height);
			boxCollider.offset = new Vector2(boxCollider.offset.x, height / 2f);
		}
	}

	void ResetColliderHeight()
	{
		if (boxCollider)
		{
			boxCollider.size = boxColliderSize;
			boxCollider.offset = new Vector2(boxCollider.offset.x, boxColliderSize.y / 2f);
		}
	}


	void StartSpecialAttack()
	{
		if (state == CharacterState.Bouncing)
		{
			if (!canBounceTongue)
				return;
			wasBouncingBeforeSpecial = true;
			canBounceTongue = false;
			hasBounceDodged = true;
		}
		else
		{
			wasBouncingBeforeSpecial = false;
		}

		bool specialStarted = false;
		switch (type)
		{
		case CharacterType.Alien:
			specialStarted = StartTeleportAttack();
			break;
		case CharacterType.Ape:
			specialStarted = StartThrowAttack();
			break;
		case CharacterType.Cat:
			specialStarted = StartPounceAttack();
			break;
		case CharacterType.Doge:
			specialStarted = StartSpinDashAttack();
			break;
		case CharacterType.Frog:
			specialStarted = StartTongueAttack();
			break;
		case CharacterType.Human:
			specialStarted = StartThrowAttack();
			break;
		}
		if (specialStarted)
		{
			state = CharacterState.Special;
		}
	}

	private bool StartPounceAttack()
	{
		if (pounceState != PounceState.Idle && pounceState != PounceState.Active || pounceCooldownLeft > 0f)
		{
			return false;
		}
		specialAttackDir = GetSpecialDirection();
		pounceState = PounceState.Charging;
		pounceCharged = 0f;
		SetColliderHeight(0.05f);
		return true;
	}

	private bool StartSpinDashAttack()
	{
		if (spinDashState != SpinDashState.Idle || spinDashCooldownLeft > 0f)
		{
			return false;
		}
		spinDashHitPlayers.Clear();
		specialAttackDir = GetSpecialDirection();
		spinDashState = SpinDashState.Starting;
		spinDashDelayLeft = spinDashDelay;
		return true;
	}

	private bool StartTeleportAttack()
	{
		if (teleportState != TeleportState.Idle || teleportCooldownLeft > 0f)
		{
			return false;
		}

		teleportDir = GetSpecialDirection(false);
		teleportState = TeleportState.Teleporting;
		teleportDelayLeft = teleportDelay;
		teleportPosition = GetTeleportPosition();
		EffectsController.CreateTeleportSolo(transform.position + new Vector3(0f, 1.5f, 0.5f));
		SoundController.PlaySoundEffect("Teleport", 0.65f, transform.position);
		EffectsController.CreateLocalizedShake(transform.position + Vector3.up * height * 0.5f, velocity, velocity.magnitude + 0.5f, 0.6f);
		return true;
	}

	private bool StartThrowAttack()
	{
		return false;/*#if (throwable != null)
		{
			throwable.PerformAction();
			if (throwable is BombBehavior)
			{
				throwTimeoutLeft = throwTimeout;
			}
			return false;
		}
		if (throwTimeoutLeft > 0f)
		{
			return false;
		}

		throwChargeTime = 0f;
		specialAttackDir = GetSpecialDirection(false);
		throwState = ThrowState.Charging;
		return true;#*/
	}

	private bool StartTongueAttack()
	{
		if (Time.time - tongueLastEndTime < tongueTimeout)
		{
			return false;
		}
		tongueDistance = 0f;
		tongueState = TongueState.Extending;
		tongueDelayLeft = tongueDelay;
		SoundController.PlaySoundEffect("TongueLaunch", 0.5f, transform.position);
		tongueDir = GetSpecialDirection();
		return true;
	}

	public void ThrowableAction(Vector2 pos)
	{
		/*#if (throwable)
		{
			if (throwable is BombBehavior)
			{
				(throwable as BombBehavior).DoExplode(pos);
			}
			else if (throwable is BananaBehavior)
			{
				(throwable as BananaBehavior).DoReturn(pos);
			}
		}#*/
	}

	public void ThrowableReflect(Character by, Vector2 pos, Vector2 vel)
	{
		/*#if (throwable)
		{
			throwable.DoReflect(pos, vel, by == this);
		}#*/
	}

	public void ThrowableCollide(Vector2 pos)
	{
		/*#if (throwable)
		{
			throwable.DoCollide(pos);
		}#*/
	}

	private Vector2 GetSpecialDirection(bool blockGround = true)
	{
		Vector2 dir = facingDir * Vector2.right;
		if (input.right)
			dir = Vector2.right;
		else if (input.left)
			dir = Vector2.left;
		if (input.up)
		{
			if (!input.left && !input.right)
			{
				dir = Vector2.up;
			}
			else if (input.left)
			{
				dir = Vector2.up + Vector2.left;
			}
			else if (input.right)
			{
				dir = Vector2.up + Vector2.right;
			}
		}
		else if (input.down && !(OnGround && blockGround))
		{
			if (!input.left && !input.right)
			{
				dir = Vector2.down;

			}
			else if (input.left)
			{
				dir = Vector2.down + Vector2.left;

			}
			else if (input.right)
			{
				dir = Vector2.down + Vector2.right;

			}
		}
		return dir.normalized;
	}/*##*/

	public void EnableCharacterScripts(bool enabled)
	{
		GetComponent<Character>().enabled = enabled;
		//GetComponent<CharacterCustomizer>().enabled = enabled;
		GetComponent<CharacterAnimator>().enabled = enabled;
		print($"state: {state}");
	}

	public void Normal() { state = CharacterState.Normal; EnableCharacterScripts(true); }
	public void Attack() { state = CharacterState.Attacking; attackState = AttackState.Attacking; EnableCharacterScripts(true); }
	public void Bounce() { state = CharacterState.Bouncing; EnableCharacterScripts(true); }
	public void Special() { state = CharacterState.Special; EnableCharacterScripts(true); }
	public void Burp() { state = CharacterState.Burping; EnableCharacterScripts(true); }
	public void Pose() { state = CharacterState.Posing; EnableCharacterScripts(true); }
}
