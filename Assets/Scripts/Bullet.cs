using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : Projectile {
	public int damage;

	public override void StartChild () {

	}

	public override void RangeReached () {
		if (live) {
			live = false;
			if (master) {
				localPlayer.HandleProjectileActionComplete (parentShipNumber);
			}

			Destroy (gameObject);
		}
	}

	public override void HitShip (Collider2D col) {
		if (live) {
			live = false;
			if (master) {
				localPlayer.HandleBulletHitShip (col.gameObject, parentShipNumber, damage);
			}

			Destroy (gameObject);
		}
	}

	public override void HitShield (Collider2D col) {
		if (live) {
			live = false;
			col.transform.parent.GetComponent<Shield> ().TakeDamage (damage);
			if (master) {
				localPlayer.HandleProjectileActionComplete (parentShipNumber);
			}
			Destroy (gameObject);
		}
	}

	public override void HitAstroid () {
		if (live) {
			live = false;
			if (master) {
				localPlayer.HandleProjectileActionComplete (parentShipNumber);
			}

			Destroy (gameObject);
		}
	}
}
