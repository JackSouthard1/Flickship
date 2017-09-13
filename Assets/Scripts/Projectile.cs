using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Projectile : MonoBehaviour {
	public ProjectileWeapon.Trajectory trajectory;
	protected float powerRatio = 1;
	protected int sign = 1;
	protected float xOffset = 0;
	public Player localPlayer;
	public int parentShipNumber;
	public bool master = false;
	protected bool live = true;
	protected float range;

	protected float percentLifetime;
	protected Vector3 startPos;
	protected float distanceTraveled;

	void Start ()
	{
		localPlayer = GameManager.instance.localPlayer;

		if (localPlayer.isServer) {
			master = true;
		}

		this.StartChild ();
	}

	public abstract void StartChild ();

	public void SetupProjectile (float range, ProjectileWeapon.Trajectory trajectory, float powerRatio, int sign = 1) {
		this.range = range;
		this.trajectory = trajectory;
		this.powerRatio = powerRatio;
		this.sign = sign;
		percentLifetime = 0f;

		startPos = transform.GetChild(0).position;
	}

	void Update ()
	{	
		distanceTraveled = (transform.GetChild(0).position - startPos).magnitude;
		percentLifetime = distanceTraveled / range;
		if (trajectory == ProjectileWeapon.Trajectory.Wave) {
			CalculateWaveOffset();
			transform.GetChild(0).localPosition = new Vector3 (xOffset, 0, 0);
		} else if (trajectory == ProjectileWeapon.Trajectory.Curve) {
			CalculateCurveOffset();
			transform.GetChild(0).localPosition = new Vector3 (xOffset, 0, 0);
		} 

		if (distanceTraveled > range) {
			this.RangeReached ();
		}
	}

	public abstract void RangeReached ();

	void CalculateWaveOffset () {
		xOffset = 3 * Mathf.Sin(4f * percentLifetime * (1/powerRatio));
	}

	void CalculateCurveOffset () {
		xOffset = ((100 * percentLifetime) * percentLifetime) / (4 * powerRatio) * sign;
	}

	void OnTriggerEnter2D (Collider2D col)
	{
		if (col.gameObject.tag == "Ship") {
			this.HitShip (col);
		} else if (col.gameObject.tag == "Astroid") {
			this.HitAstroid ();
		} else {
//			print ("Name: " + col.gameObject.name + ", Layer: " + col.gameObject.layer);
			if (col.gameObject.layer == 11) {
				this.HitShield (col);
			}
		}
	}

	public abstract void HitShip (Collider2D col);

	public abstract void HitShield (Collider2D col);

	public abstract void HitAstroid ();
}
