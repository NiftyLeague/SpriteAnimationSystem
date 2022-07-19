//#using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Powerup : MonoBehaviour
{/*#
	public SpriteRenderer rend;
	public float maxSpeed;
	public float ingestionTimeout;
	public float flashDelay;

	private float timeUningested = 0;
	private bool beingIngested = false;
	private int flashNumber;
	private float flashDelayCounter;
	private PowerupView view;
	private bool localIngestRequestSent = false;

	internal bool BeingIngested
	{
		get { return beingIngested; }
		set
		{
			beingIngested = value;
			if (beingIngested == false)
			{
				timeUningested = Time.time;
				localIngestRequestSent = false;
			}
		}
	}

	internal bool CanBeIngested
	{
		get
		{
			return !beingIngested && Time.time - timeUningested > ingestionTimeout;
		}
	}

	float updateDirectionDelay;
	Vector2 velocity, targetVelocity;
	int terrainLayer;


	private void Awake()
	{
		view = GetComponent<PowerupView>();
		BeingIngested = false;
	}

	void Start()
	{
		terrainLayer = 1 << LayerMask.NameToLayer("Ground");
		velocity = Random.insideUnitCircle.normalized * 10f;
		beingIngested = false;
		timeUningested = -10f;
		localIngestRequestSent = false;
	}

	void FixedUpdate()
	{
		updateDirectionDelay -= Time.deltaTime;
		if (updateDirectionDelay < 0f)
		{
			updateDirectionDelay = Random.Range(3f, 10f);
			UpdateDirection();
		}

		if (view.photonView.IsMine && !BeingIngested)
		{
			RunMotion();
		}

		if (view.photonView.IsMine && transform.position.x < Terrain.LeftKillPoint || transform.position.x > Terrain.RightKillPoint || transform.position.y > Terrain.TopKillPoint || transform.position.y < Terrain.BotKillPoint)
			PhotonNetwork.Destroy(gameObject);

		if (Time.time - timeUningested < ingestionTimeout)
		{
			RunFlashingPoweredUp();
		}
		else if (flashNumber > 0)
		{
			flashNumber = 0;
			rend.color = Color.white;
		}
	}

	void RunMotion()
	{
		velocity = Vector2.MoveTowards(velocity, targetVelocity, 5f * Time.deltaTime);

		Vector2 velocityT = velocity * Time.deltaTime + Vector2.up * Mathf.Sin(Time.time * 8f) * 3f * Time.deltaTime;
		if (velocityT.x < 0)
		{
			if (Physics2D.Raycast(transform.position, Vector2.left, Mathf.Abs(velocityT.x) + 1f, terrainLayer))
			{
				velocityT.x = 0;
				velocity.x *= -0.5f;
				UpdateDirection();
			}
		}
		if (velocityT.x > 0)
		{
			if (Physics2D.Raycast(transform.position, Vector2.right, Mathf.Abs(velocityT.x) + 1f, terrainLayer))
			{
				velocityT.x = 0;
				velocity.x *= -0.5f;
				UpdateDirection();
			}
		}
		if (velocityT.y < 0)
		{
			if (Physics2D.Raycast(transform.position, Vector2.down, Mathf.Abs(velocityT.y) + 1f, terrainLayer))
			{
				velocityT.y = 0;
				velocity.y *= -0.5f;
				UpdateDirection();
			}
		}
		if (velocityT.y > 0)
		{
			if (Physics2D.Raycast(transform.position, Vector2.up, Mathf.Abs(velocityT.y) + 1f, terrainLayer))
			{
				velocityT.y = 0;
				velocity.y *= -0.5f;
				UpdateDirection();
			}
		}

		if (velocityT.x < 0)
			transform.localScale = new Vector3(1f, 1f, 1f);
		else
			transform.localScale = new Vector3(-1f, 1f, 1f);

		var pos = transform.position + (Vector3)velocityT;
		pos.z = -0.3f;
		transform.position = pos;
	}

	private void UpdateDirection()
	{
		if (Random.value < 0.1f)
		{
			targetVelocity = Vector2.zero;
			updateDirectionDelay = Random.Range(1f, 3f);
		}
		else
		{
			targetVelocity = Random.insideUnitCircle.normalized * maxSpeed;
		}
	}
	void RunFlashingPoweredUp()
	{
		flashDelayCounter -= Time.deltaTime;
		if (flashDelayCounter <= 0f)
		{
			flashNumber++;
			flashDelayCounter += flashDelay;
			flashNumber++;
			if (flashNumber % 3 == 1)
			{
				rend.color = Color.black;
			}
			else
			{
				rend.color = Color.white;
			}
		}
	}

	public bool TryIngest(Character ch)
	{
		if (localIngestRequestSent)
		{
			return false;
		}
		localIngestRequestSent = true;
		view.RequestIngest(ch);
		CancelInvoke(nameof(ResetIngestRequestFlag));
		Invoke(nameof(ResetIngestRequestFlag), 0.5f);
		return true;
	}

	[Beebyte.Obfuscator.SkipRename]
	private void ResetIngestRequestFlag()
	{
		localIngestRequestSent = false;
	}#*/
}
