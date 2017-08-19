using System.Collections;
using UnityEngine;

[System.Serializable]
public class Weapon {
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
}
