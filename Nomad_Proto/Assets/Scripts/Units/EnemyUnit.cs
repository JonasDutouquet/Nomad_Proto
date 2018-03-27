using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class EnemyUnit : MonoBehaviour
{
	[SerializeField] private int _speed = 2;
	[SerializeField] private int _shortVisionRange = 5; //how far will it look for units?

	public static EnemyUnit enemyPrefab;

	public HexGrid Grid{ get; set; }
	public HexUnit Relic { get; set;}
	
	private HexCell location, currentTravelLocation;
	public HexCell Location {
		get {
			return location;
		}
		set {
			if (location) {
				location.Enemy = null;
			}
			location = value;
			value.Enemy = this;
			transform.localPosition = value.Position;
		}
	}

	public void Die () {
		location.Enemy = null;
		Destroy(gameObject);
	}

	#region Pathfinding
	public void Move()
	{
		//search for unit in speed range, attack if unit
		HexUnit unitToAttack = UnitInAttackRange (_speed);
		if(unitToAttack)
		{
			//Debug.Log ("Moving to attack " + unitToAttack.Type);
			Grid.FindUnit (location, unitToAttack.Location, _speed);
			Travel (Grid.GetPath ());
			return;
		}
		unitToAttack = UnitInAttackRange (_shortVisionRange);
		if(unitToAttack)
		{
			//Debug.Log ("Moving closer to " + unitToAttack.Type);
			Grid.FindUnit (location, unitToAttack.Location, _speed);
			Travel (Grid.GetPath ());
			return;
		}
		else
		{
			//Debug.Log ("Moving in random direction");
			HexCell randomDirection = Grid.GetCell (Random.Range (0, Grid.cellCountX * Grid.cellCountZ));
			Grid.FindUnit (location, randomDirection, _speed);
			Travel (Grid.GetPath ());
		}
	}

	HexUnit UnitInAttackRange(int range)
	{
		HexUnit[] units = Grid.GetUnits ().ToArray ();
		for(int i = 0 ; i < units.Length ; i++)
		{
			if (Grid.FindUnit (location, units [i].Location, range))
				return units [i];
		}
		return null;
	}
	#endregion

	#region Movement
	const float rotationSpeed = 180f;
	const float travelSpeed = 4f;
	float orientation;
	public float Orientation {
		get {
			return orientation;
		}
		set {
			orientation = value;
			transform.localRotation = Quaternion.Euler(0f, value, 0f);
		}
	}

	List<HexCell> pathToTravel;

	void Travel (List<HexCell> path) {
		location.Enemy = null;
		location = path[path.Count - 1];
		Grid.RemoveOnlyUnit (location.Unit);
		location.Enemy = this;
		pathToTravel = path;
		StopAllCoroutines();
		StartCoroutine(TravelPath());
	}

	IEnumerator TravelPath () {
		Vector3 a, b, c = pathToTravel[0].Position;
		yield return LookAt(pathToTravel[1].Position);

		if (!currentTravelLocation) {
			currentTravelLocation = pathToTravel[0];
		}
		int currentColumn = currentTravelLocation.ColumnIndex;

		float t = Time.deltaTime * travelSpeed;
		for (int i = 1; i < pathToTravel.Count; i++) {
			currentTravelLocation = pathToTravel[i];
			a = c;
			b = pathToTravel[i - 1].Position;

			int nextColumn = currentTravelLocation.ColumnIndex;
			if (currentColumn != nextColumn) {
				if (nextColumn < currentColumn - 1) {
					a.x -= HexMetrics.innerDiameter * HexMetrics.wrapSize;
					b.x -= HexMetrics.innerDiameter * HexMetrics.wrapSize;
				}
				else if (nextColumn > currentColumn + 1) {
					a.x += HexMetrics.innerDiameter * HexMetrics.wrapSize;
					b.x += HexMetrics.innerDiameter * HexMetrics.wrapSize;
				}
				//Grid.MakeChildOfColumn(transform, nextColumn);
				currentColumn = nextColumn;
			}

			c = (b + currentTravelLocation.Position) * 0.5f;

			for (; t < 1f; t += Time.deltaTime * travelSpeed) {
				transform.localPosition = Bezier.GetPoint(a, b, c, t);
				Vector3 d = Bezier.GetDerivative(a, b, c, t);
				d.y = 0f;
				transform.localRotation = Quaternion.LookRotation(d);
				yield return null;
			}
			t -= 1f;
		}
		currentTravelLocation = null;

		a = c;
		b = location.Position;
		c = b;
		for (; t < 1f; t += Time.deltaTime * travelSpeed) {
			transform.localPosition = Bezier.GetPoint(a, b, c, t);
			Vector3 d = Bezier.GetDerivative(a, b, c, t);
			d.y = 0f;
			transform.localRotation = Quaternion.LookRotation(d);
			yield return null;
		}

		transform.localPosition = location.Position;
		orientation = transform.localRotation.eulerAngles.y;
		ListPool<HexCell>.Add(pathToTravel);
		pathToTravel = null;

		//KILL UNIT
		if (location.Unit)
			location.Unit.Die ();

		//reset orientation for 2D sprites
		Quaternion fromRotation = transform.localRotation;
		Quaternion toRotation = Quaternion.Euler (Vector3.up);
		float angle = Quaternion.Angle (fromRotation, toRotation);
		if (angle > 0f) 
		{
			float speed = rotationSpeed / angle;
			for (
				float _t = Time.deltaTime * speed;
				_t < 1f;
				_t += Time.deltaTime * speed
			) {
				transform.localRotation =
					Quaternion.Slerp(fromRotation, toRotation, _t);
				yield return null;
			}
		}
		orientation = transform.localRotation.eulerAngles.y;
	}

	IEnumerator LookAt (Vector3 point) {
		if (HexMetrics.Wrapping) {
			float xDistance = point.x - transform.localPosition.x;
			if (xDistance < -HexMetrics.innerRadius * HexMetrics.wrapSize) {
				point.x += HexMetrics.innerDiameter * HexMetrics.wrapSize;
			}
			else if (xDistance > HexMetrics.innerRadius * HexMetrics.wrapSize) {
				point.x -= HexMetrics.innerDiameter * HexMetrics.wrapSize;
			}
		}

		point.y = transform.localPosition.y;
		Quaternion fromRotation = transform.localRotation;
		Quaternion toRotation =
			Quaternion.LookRotation(point - transform.localPosition);
		float angle = Quaternion.Angle(fromRotation, toRotation);

		if (angle > 0f) {
			float speed = rotationSpeed / angle;
			for (
				float t = Time.deltaTime * speed;
				t < 1f;
				t += Time.deltaTime * speed
			) {
				transform.localRotation =
					Quaternion.Slerp(fromRotation, toRotation, t);
				yield return null;
			}
		}

		transform.LookAt(point);
		orientation = transform.localRotation.eulerAngles.y;
	}
	#endregion

	#region Load/Save
	public void Save (BinaryWriter writer) {
		location.coordinates.Save(writer);
	}

	public static void Load (BinaryReader reader, HexGrid grid) {
		HexCoordinates coordinates = HexCoordinates.Load(reader);
		grid.AddEnemy (Instantiate(enemyPrefab), grid.GetCell (coordinates));
	}
	#endregion
}