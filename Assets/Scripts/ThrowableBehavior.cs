using System;
using UnityEngine;

public class ThrowableBehavior : MonoBehaviour
{
	public SpriteRenderer sr;
	//#internal Character owner;
	internal Vector2 velocity;
	internal int terrainLayer, characterLayer, throwableLayer;
	internal bool canHitOwner;
	internal bool reflected;
	internal bool collided;
	/*#
	private void Awake()
	{
		terrainLayer = 1 << LayerMask.NameToLayer("Ground");
		characterLayer = 1 << LayerMask.NameToLayer("Character");
		throwableLayer = 1 << LayerMask.NameToLayer("Throwable");
	}

	public virtual void Initialize(Character owner, Vector2 velocity, Vector3 position)
	{
		this.owner = owner;
		this.velocity = velocity;
		transform.position = position;
		reflected = false;
		collided = false;
		transform.SetParent(owner.transform.parent);
	}

	private void FixedUpdate()
	{
		RunMotion();

		if (transform.position.x < Terrain.LeftKillPoint || transform.position.x > Terrain.RightKillPoint || transform.position.y > Terrain.TopKillPoint || transform.position.y < Terrain.BotKillPoint)
		{
			Destroy(gameObject);
		}
	}

	public virtual void RunMotion()
	{
	}

	public virtual void PerformAction()
	{
	}

	public virtual void DoReflect(Vector2 pos, Vector2 vel, bool byOwner)
	{
		canHitOwner = !byOwner;
		velocity = vel;
		reflected = true;
	}

	public virtual void DoCollide(Vector2 pos)
	{
		collided = true;
	}

	void OnDestroy()
	{
		owner.OnThrowableDestroy();
	}#*/
}
