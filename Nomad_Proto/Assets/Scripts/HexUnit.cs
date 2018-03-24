using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class HexUnit : MonoBehaviour
{
	//Variables player can change
	[SerializeField] private int _speed;
	[SerializeField] private int _visionRange;

	//Common variables
	private int _hunger;
	public bool _inMemory;

	//Fighter variables
	public int ArrowRange{ get; set;}

	//Producer variable
	private int _baseProd;
	private int _bonusProd;
	public int Production
	{
		get{
			int doubleProd = _didActions[(int)ActionTypes.Double] ? 1 : 0;
			return (_baseProd + _bonusProd * location.HasResource) * (1 + doubleProd);
		}
	}

	//Relic variable
	public int MemoryRange
	{
		get {
			return _visionRange;
		}
		set{
			_visionRange = value;
		}
	}

	const float rotationSpeed = 180f;
	const float travelSpeed = 4f;

	private UnitTypes _type;
	public Color SelectedCol{get;private set;}
	private int _speedUsed = 0;
	private bool[] _didActions;

	public UnitData _data;
	public static HexUnit unitPrefab;

	public UnitTypes Type
	{
		get
		{
			return _type;
		}
		set
		{
			_type = value;
			switch(_type)
			{
			case UnitTypes.Fighter:
				_data = Grid.fighterData;
				ArrowRange = _data.arrowRange;
				break;
			case UnitTypes.Producer:
				_data = Grid.producerData;
				_baseProd = _data.productionBase;
				_bonusProd = _data.productionBonus;
				break;
			case UnitTypes.Relic:
				_data = Grid.relicData;
				FindObjectOfType<TurnManager> ().Relic = this;
				break;
			case UnitTypes.Enemy:
				_data = Grid.enemyData;
				break;
			}
			InitializeUnit ();
		}
	}

	private void InitializeUnit()
	{
		Instantiate (_data.display, transform);
		_speed = _data.speed;
		_visionRange = _data.visionRange;
		_hunger = _data.hunger;
		SelectedCol = _data.selectedColor;
		_didActions = new bool[System.Enum.GetValues(typeof(ActionTypes)).Length];
		ResetActionsDone ();
	}

	public void ConsumeResource()
	{
		if(location.Resource > 0 && Type == UnitTypes.Producer)
		{
			location.RemoveResource ();
		}
	}

	public void ResetActionsDone()
	{
		for(int i = 0 ; i < _didActions.Length ; i++)
		{
			_didActions[i] = false;
		}

		_speedUsed = 0;
	}

	public bool DidAction(int action, bool set)
	{
		if (set)
			_didActions [action] = true;
		else
			return _didActions [action];
		return true;
	}

	public HexGrid Grid { get; set; }

	public int Hunger
	{
		get{
			return _hunger;
		}
	}

	public HexCell Location {
		get {
			return location;
		}
		set {
			if (location) {
				Grid.DecreaseVisibility(location, VisionRange);
				location.Unit = null;
			}
			location = value;
			value.Unit = this;
			Grid.IncreaseVisibility(value, VisionRange);
			transform.localPosition = value.Position;
			//Grid.MakeChildOfColumn(transform, value.ColumnIndex);
		}
	}

	HexCell location, currentTravelLocation;

	public float Orientation {
		get {
			return orientation;
		}
		set {
			orientation = value;
			transform.localRotation = Quaternion.Euler(0f, value, 0f);
		}
	}

	public int Speed {
		get {
			return _speed;
		}
		set{
			_speed = value;
		}
	}

	public int SpeedLeft
	{
		get
		{
			return _speed - _speedUsed;
		}
		set{
			_speedUsed = 0;

		}
	}

	public int VisionRange 
	{
		get {
			return _visionRange;
		}
		set{
			_visionRange = value;
		}
	}

	float orientation;

	List<HexCell> pathToTravel;

	public void ValidateLocation () {
		transform.localPosition = location.Position;
	}

	public bool IsValidDestination (HexCell cell) {
		return cell.IsExplored && !cell.IsUnderwater && !cell.Unit;
	}

	public bool IsValidArrow (HexCell cell)
	{
		return !cell.Unit && !cell.IsUnderwater && !cell.Arrow;
	}

	public void Travel (List<HexCell> path) {
		location.Unit = null;
		location = path[path.Count - 1];
		location.Unit = this;
		pathToTravel = path;
		StopAllCoroutines();
		StartCoroutine(TravelPath());
		_speedUsed += path.Count -1;
	}

	IEnumerator TravelPath () {
		Vector3 a, b, c = pathToTravel[0].Position;
		yield return LookAt(pathToTravel[1].Position);

		if (!currentTravelLocation) {
			currentTravelLocation = pathToTravel[0];
		}
		Grid.DecreaseVisibility(currentTravelLocation, VisionRange);
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
				Grid.MakeChildOfColumn(transform, nextColumn);
				currentColumn = nextColumn;
			}

			c = (b + currentTravelLocation.Position) * 0.5f;
			Grid.IncreaseVisibility(pathToTravel[i], VisionRange);

			for (; t < 1f; t += Time.deltaTime * travelSpeed) {
				transform.localPosition = Bezier.GetPoint(a, b, c, t);
				Vector3 d = Bezier.GetDerivative(a, b, c, t);
				d.y = 0f;
				transform.localRotation = Quaternion.LookRotation(d);
				yield return null;
			}
			Grid.DecreaseVisibility(pathToTravel[i], VisionRange);
			t -= 1f;
		}
		currentTravelLocation = null;

		a = c;
		b = location.Position;
		c = b;
		Grid.IncreaseVisibility(location, VisionRange);
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

	public int GetMoveCost (HexCell fromCell, HexCell toCell, HexDirection direction)
	{
		if (!IsValidDestination(toCell)) {
			return -1;
		}
		HexEdgeType edgeType = fromCell.GetEdgeType(toCell);
		if (edgeType == HexEdgeType.Cliff) {
			return -1;
		}

		return 1;
		/*
		int moveCost;

		if (fromCell.HasRoadThroughEdge(direction)) {
			moveCost = 1;
		}
		else if (fromCell.Walled != toCell.Walled) {
			return -1;
		}
		else {
			moveCost = edgeType == HexEdgeType.Flat ? 5 : 10;
			moveCost +=
				toCell.UrbanLevel + toCell.FarmLevel + toCell.PlantLevel;
		}
		return moveCost;*/
	}
		
	public void Die () {
		if (location) {
			Grid.DecreaseVisibility(location, VisionRange);
		}
		location.Unit = null;
		location.DisableHighlight ();
		Destroy(gameObject);
	}

	public void Save (BinaryWriter writer) {
		location.coordinates.Save(writer);
		writer.Write(orientation);
		writer.Write ((int)_type);

		for(int i = 0 ; i < _didActions.Length ; i++)
		{
			writer.Write (_didActions[i]);
		}
		//writer.Write (_speed);
		//writer.Write (_visionRange);
	}

	public static void Load (BinaryReader reader, HexGrid grid) {
		HexCoordinates coordinates = HexCoordinates.Load(reader);
		float orientation = reader.ReadSingle();
		UnitTypes type = (UnitTypes)reader.ReadInt32 (); 
		HexUnit unit = Instantiate (unitPrefab);

		grid.AddUnit(unit, grid.GetCell(coordinates), orientation, type);

		for (int i = 0; i < System.Enum.GetValues (typeof(ActionTypes)).Length; i++)
			unit.DidAction (i, reader.ReadBoolean ());
	}

	void OnEnable () {
		if (location) {
			transform.localPosition = location.Position;
			if (currentTravelLocation) {
				Grid.IncreaseVisibility(location, VisionRange);
				Grid.DecreaseVisibility(currentTravelLocation, VisionRange);
				currentTravelLocation = null;
			}
		}
	}

//	void OnDrawGizmos () {
//		if (pathToTravel == null || pathToTravel.Count == 0) {
//			return;
//		}
//
//		Vector3 a, b, c = pathToTravel[0].Position;
//
//		for (int i = 1; i < pathToTravel.Count; i++) {
//			a = c;
//			b = pathToTravel[i - 1].Position;
//			c = (b + pathToTravel[i].Position) * 0.5f;
//			for (float t = 0f; t < 1f; t += 0.1f) {
//				Gizmos.DrawSphere(Bezier.GetPoint(a, b, c, t), 2f);
//			}
//		}
//
//		a = c;
//		b = pathToTravel[pathToTravel.Count - 1].Position;
//		c = b;
//		for (float t = 0f; t < 1f; t += 0.1f) {
//			Gizmos.DrawSphere(Bezier.GetPoint(a, b, c, t), 2f);
//		}
//	}
}