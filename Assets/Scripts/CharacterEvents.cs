
using System;
using UnityEngine;

public class CharacterEvents : MonoBehaviour
{
	public EventAction onEventDispatched;
	public Character character;

	private void Awake()
	{
		character = GetComponent<Character>();
	}

	public void Hit(Character victim, Vector2 hitDir, float power)
	{
		OnEventDispatched(CharacterEventType.HitInit, victim, hitDir, power, victim.hitsTaken);
	}

	public void SoftHit(Character victim, Vector2 hitDir, float power, float victimTimeBump, float attackerTimeBump)
	{
		OnEventDispatched(CharacterEventType.SoftHitInit, victim, hitDir, power, victimTimeBump, attackerTimeBump);
	}

	public void Die(Character lastHitCharacter)
	{
		OnEventDispatched(CharacterEventType.Death, lastHitCharacter);
	}

	public void ThrowThrowable(Vector2 velocity, Vector3 pos)
	{
		OnEventDispatched(CharacterEventType.ThrowableThrow, velocity, pos);
	}

	public void ThrowableAction(Vector2 pos)
	{
		OnEventDispatched(CharacterEventType.ThrowableAction, pos);
	}

	public void ThrowableReflect(Character chr, Vector2 pos, Vector2 vel)
	{
		OnEventDispatched(CharacterEventType.ThrowableReflect, chr, pos, vel);
	}

	public void ThrowableCollide(Vector2 pos)
	{
		OnEventDispatched(CharacterEventType.ThrowableCollide, pos);
	}

	public void SpinDashStart(Vector2 dir)
	{
		OnEventDispatched(CharacterEventType.SpinDashStart, dir);
	}

	public void SpinDashEnd(bool delay)
	{
		OnEventDispatched(CharacterEventType.SpinDashEnd, delay);
	}

	public void RegisterKill(Character gotPoint, Character gotKilled, int hits)
	{
		OnEventDispatched(CharacterEventType.RegisterKill, gotPoint, gotKilled, hits);
	}

	private void OnEventDispatched(CharacterEventType type, params object[] args)
	{
		if (onEventDispatched != null)
		{
			onEventDispatched(type, args);
		}
	}
}

public delegate void EventAction(CharacterEventType type, params object[] args);


public enum CharacterEventType
{
	None,
	HitInit,
	SoftHitInit,
	Death,
	ThrowableThrow,
	ThrowableAction,
	ThrowableReflect,
	ThrowableCollide,
	RegisterKill,
	SpinDashStart,
	SpinDashEnd,
}
