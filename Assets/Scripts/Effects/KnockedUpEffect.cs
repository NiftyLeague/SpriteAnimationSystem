using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KnockedUpEffect : MonoBehaviour
{
	public SimpleAnim anim;
	public float rotSpeed, fallSpeed;

	public SpriteRenderer spriteRenderer;

	public float fallDelay;
	bool haveStartedSound;


	void Awake()
	{
		spriteRenderer = GetComponent<SpriteRenderer>();
		rotSpeed *= Random.Range(-2f, 2f);
	}

	void Update()
	{
		fallDelay -= Time.deltaTime;
		if (fallDelay > 0)
			return;
		if (!haveStartedSound)
		{
			haveStartedSound = true;
			GetComponent<AudioSource>().Play();
		}
		Vector3 pos = transform.position;
		pos.y -= fallSpeed * Time.deltaTime;
		transform.position = pos;

		transform.Rotate(Vector3.forward, rotSpeed * Time.deltaTime);

		if (transform.position.y < 0f && !spriteRenderer.isVisible)
			Destroy(gameObject);

	}

	internal void Initialize(CharacterAnimator characterAnimator)
	{
		anim.frames = new Sprite[] {
			characterAnimator.GetFrame(AnimationTagType.Impact, 0),
			characterAnimator.GetFrame(AnimationTagType.Impact, 1)
		};
		anim.transform.localScale = Vector3.one * 2.5f;
	}
}
