using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour {
	public ProjectileWeapon.Trajectory trajectory;
	private float powerRatio = 1;
	private int sign = 1;
	private float xOffset = 0;
	[Space(30)]
	public Player localPlayer;
	public int parentShipNumber;
	private bool masterBullet = false;
	private bool live = true;
	private float range;
	private float percentLifetime;
	private int damage;

	private Vector3 startPos;
	private float distanceTraveled;



	void Start ()
	{
		startPos = transform.position;
		localPlayer = GameManager.instance.localPlayer;

		if (localPlayer.isServer) {
			masterBullet = true;
		}
	}

	public void SetupBullet (float range, int damage, ProjectileWeapon.Trajectory trajectory, float powerRatio, int sign = 1) {
		this.range = range;
		this.damage = damage;
		this.trajectory = trajectory;
		this.powerRatio = powerRatio;
		this.sign = sign;
		percentLifetime = 0f;
	}

	void Update ()
	{	
		distanceTraveled = (transform.position - startPos).magnitude;
		percentLifetime = distanceTraveled / range;

		if (trajectory == ProjectileWeapon.Trajectory.Wave) {
			CalculateWaveOffset();
			transform.GetChild(0).localPosition = new Vector3 (xOffset, 0, 0);
		} else if (trajectory == ProjectileWeapon.Trajectory.Curve) {
			CalculateCurveOffset();
			transform.GetChild(0).localPosition = new Vector3 (xOffset, 0, 0);
		} 

		if (distanceTraveled > range) {
			if (live) {
				live = false;
				if (masterBullet) {
					localPlayer.HandleBulletMiss (parentShipNumber);
				}

				Destroy (gameObject);
			}
		}
	}

	void CalculateWaveOffset () {
		xOffset = 3 * Mathf.Sin(4f * percentLifetime * (1/powerRatio));
	}

	void CalculateCurveOffset () {
		xOffset = ((100 * percentLifetime) * percentLifetime) / (4 * powerRatio) * sign;

	}

	void OnTriggerEnter2D (Collider2D col)
	{
		if (col.gameObject.tag == "Ship") {
			if (live) {
				live = false;
				if (masterBullet) {
					localPlayer.HandleBulletHit (col.gameObject, parentShipNumber, damage);
				}

				Destroy (gameObject);
			}

		} else if (col.gameObject.tag == "Astroid") {
			if (live) {
				live = false;
				if (masterBullet) {
					localPlayer.HandleBulletMiss (parentShipNumber);
				}
				print ("Hit Astroid");

				Destroy (gameObject);
			}
		}
	}
}
