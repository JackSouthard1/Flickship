using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ProjectileWeapon : Weapon {
	public enum Trajectory
	{
		Straight,
		Wave,
		Curve,
	};
	public Trajectory trajectory;

	public float projectileSpeed;
	public float projectileRange;
	public int projectileDamage;

	public GameObject projectilePrefab;

	public override void Fire (Vector2 direction, int sign) {
		direction = RotateVector2 (direction, baseRotZ);
		var angle = Mathf.Atan2 (direction.y, direction.x) * Mathf.Rad2Deg - 90f;
		Quaternion newRot = Quaternion.AngleAxis (angle, Vector3.forward);

		GameObject newBullet = GameObject.Instantiate (projectilePrefab);
		Bullet bulletScript = newBullet.GetComponent<Bullet> ();
		bulletScript.localPlayer = parentShip.localPlayer;
		bulletScript.parentShipNumber = parentShip.shipNumber;
		bulletScript.SetupBullet(range: projectileRange, damage: projectileDamage, trajectory: trajectory, powerRatio: direction.magnitude, sign: sign);

		newBullet.transform.rotation = newRot;
		newBullet.transform.position = transform.position;
		newBullet.GetComponent<Rigidbody2D> ().velocity = direction.normalized * projectileSpeed;
		parentShip.liveBullets++;

	}

	public override void UpdateShootPath (Vector2 dragVector, float sway, float shootRadius, float moveRadius)
	{
		float basicRatio = (dragVector.magnitude - shootRadius) / ((moveRadius) - (shootRadius));

		float modifierRatio = 1;
		int nodeCount = 2;

		transform.localRotation = Quaternion.Euler(new Vector3(0,0, baseRotZ + sway));

		Vector3[] pathVerts;
		if (trajectory == Trajectory.Wave) {
			modifierRatio = (-4f * basicRatio) * (basicRatio - 1f);
			modifierRatio = Mathf.Clamp (modifierRatio, minPowerRatio, 1f);

			nodeCount = Mathf.RoundToInt (resolution * projectileRange);
			shootPath.positionCount = nodeCount;

			float nodeIncriment = 1f / nodeCount;
			float[] nodePercentageY = new float[nodeCount];
			for (int i = 0; i < nodePercentageY.Length; i++) {
				nodePercentageY [i] = nodeIncriment * i;
			}

			pathVerts = new Vector3[nodeCount];
			for (int i = 0; i < pathVerts.Length; i++) {
				float x = 3 * Mathf.Sin (4f * nodePercentageY [i] / modifierRatio);
				Vector3 localPos = new Vector3 (x, (projectileRange * nodePercentageY [i] + transform.localPosition.y), 0);
				pathVerts [i] = transform.TransformPoint (localPos);
			}
		} else if (trajectory == Trajectory.Straight) {
			shootPath.positionCount = 2;
			pathVerts = new Vector3[2];
			pathVerts [0] = transform.TransformPoint(transform.localPosition);
			pathVerts [1] = transform.TransformPoint ((Vector3.up * projectileRange * modifierRatio) + transform.localPosition);
		} else {
			modifierRatio = (-4f * basicRatio) * (basicRatio - 1f);
			modifierRatio = Mathf.Clamp (modifierRatio, minPowerRatio, 1f);

			int sign;
			if (basicRatio < 0.5f) {
				sign = 1;
			} else {
				sign = -1;
			}

			modifierRatio = modifierRatio * sign;

			nodeCount = Mathf.RoundToInt (resolution * projectileRange);
			shootPath.positionCount = nodeCount;

			float nodeIncriment = 1f / nodeCount;
			float[] nodePercentageY = new float[nodeCount];
			for (int i = 0; i < nodePercentageY.Length; i++) {
				nodePercentageY [i] = nodeIncriment * i;
			}

			pathVerts = new Vector3[nodeCount];
			for (int i = 0; i < pathVerts.Length; i++) {
				float x = (100 * nodePercentageY[i] * nodePercentageY[i]) / (4 * modifierRatio);
				Vector3 localPos = new Vector3 (x, (projectileRange * nodePercentageY [i] + shootPath.transform.localPosition.y), 0);
				pathVerts [i] = shootPath.transform.TransformPoint (localPos);
			}
		}
		shootPath.SetPositions (pathVerts);
		shootPath.enabled = true;
	}

	private Vector2 RotateVector2 (Vector2 aPoint, float aDegree)
	{
		return Quaternion.Euler(0,0, aDegree) * aPoint;
	}
}
