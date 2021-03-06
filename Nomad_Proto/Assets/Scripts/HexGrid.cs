﻿using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class HexGrid : MonoBehaviour {

	public int cellCountX = 20, cellCountZ = 15;

	public bool wrapping;
	public bool advancedVision = false;

	public HexCell cellPrefab;
	public Text cellLabelPrefab;
	public HexGridChunk chunkPrefab;
	public Transform unitsPool;
	public Transform enemiesPool;
	public HexUnit unitPrefab;
	public EnemyUnit enemyPrefab;
	public GameObject resourcePrefab;
	public ScoutArrow arrowPrefab;
	public MemoryPillar pillarPrefab;
	public UnitData fighterData;
	public UnitData producerData;
	public UnitData relicData;
	public HexMapCamera cam;

	public Texture2D noiseSource;

	public int seed;

	public bool HasPath {
		get {
			return currentPathExists;
		}
	}

	Transform[] columns;
	HexGridChunk[] chunks;
	HexCell[] cells;

	int chunkCountX, chunkCountZ;

	HexCellPriorityQueue searchFrontier;

	int searchFrontierPhase;

	HexCell currentPathFrom, currentPathTo;
	bool currentPathExists;

	int currentCenterColumnIndex = -1;

	List<HexUnit> units = new List<HexUnit>();
	private int _unitsInMemory = 0;
	List<ScoutArrow> arrows = new List<ScoutArrow> ();
	List<MemoryPillar> pillars = new List<MemoryPillar> ();
	List<EnemyUnit> enemies = new List<EnemyUnit> ();

	HexCellShaderData cellShaderData;

	void Awake () {
		HexMetrics.noiseSource = noiseSource;
		HexMetrics.InitializeHashGrid(seed);
		HexUnit.unitPrefab = unitPrefab;
		HexCell.resourcePrefab = resourcePrefab;
		ScoutArrow.arrowPrefab = arrowPrefab;
		MemoryPillar.pillarPrefab = pillarPrefab;
		EnemyUnit.enemyPrefab = enemyPrefab;
		cellShaderData = gameObject.AddComponent<HexCellShaderData>();
		cellShaderData.Grid = this;
		CreateMap(cellCountX, cellCountZ, wrapping);
	}

	void OnEnable () {
		if (!HexMetrics.noiseSource) {
			HexMetrics.noiseSource = noiseSource;
			HexMetrics.InitializeHashGrid(seed);
			HexUnit.unitPrefab = unitPrefab;
			HexMetrics.wrapSize = wrapping ? cellCountX : 0;
			ResetVisibility();
		}
	}

	#region Units Management
	public void AddUnit (HexUnit unit, HexCell location, float orientation, UnitTypes type)
	{
		units.Add(unit);
		unit.Grid = this;
		unit.Type = type;
		unit.Location = location;
		unit.Orientation = orientation;
		unit.transform.SetParent (unitsPool);
		unit.Camera = cam;

		//sort units by type
		IEnumerable<HexUnit> query = units.OrderBy (u => u.Type);
		units = query.ToList ();
	}

	public void RemoveUnit (HexUnit unit) {
		units.Remove(unit);
		unit.Die();
	}

	public void RemoveOnlyUnit(HexUnit unit)
	{
		units.Remove (unit);
	}

	void ClearUnits () {
		for (int i = 0; i < units.Count; i++) {
			units[i].Die();
		}
		units.Clear();
	}

	public List<HexUnit> GetUnits ()
	{
		return units;
	}

	public void DistanceToRelic (HexCell toCell)
	{
		_unitsInMemory = 0;
		foreach (var unit in units)
			unit.InMemory = false;

		int range = units [0].MemoryRange;

		HexCell[] cellsAroundRelic = GetVisibleCells (toCell, range).ToArray ();
		foreach (var cell in cellsAroundRelic)
		{
			if(cell.Unit)
			{
				cell.Unit.InMemory = true;
				_unitsInMemory++;
			}
		}
	}

	public int UnitsOutOfMemory
	{
		get{
			return units.Count - _unitsInMemory;
		}
	}


	public void PrintLocations()
	{
		for (int i = 0 ; i < units.Count ; i++)
		{
			HexUnit unit = units [i];
			Debug.Log ("Unit " + i + " : " + unit.Type.ToString () + ", cell coordinates : X = " + unit.Location.coordinates.X + " ; Z = " + unit.Location.coordinates.Z);
		}
		for(int y = 0; y <enemies.Count ; y++)
		{
			EnemyUnit enemy = enemies [y];
			Debug.Log ("Enemy " + y + " : cell coordinates : X = " + enemy.Location.coordinates.X + " ; Z = " + enemy.Location.coordinates.Z);
		}
	}

	public void SetCamera()
	{
		if(units.Count > 0)
			FindObjectOfType<HexMapCamera> ().SetFollowedUnit (units[0]);
	}

	/*public void MakeChildOfColumn (Transform child, int columnIndex) {
		child.SetParent(columns[columnIndex], false);
	}*/
	#endregion

	#region Arrows Management
	public void AddArrow(ScoutArrow arrow, HexCell location, int range)
	{
		arrows.Add (arrow);
		arrow.Grid = this;
		arrow.Range = range;
		arrow.Location = location;
	}

	public void RemoveArrow(ScoutArrow arrow)
	{
		arrows.Remove (arrow);
		arrow.Die ();
	}

	public void ClearArrows()
	{
		for (int i = 0; i < arrows.Count; i++) {
			arrows[i].Die();
		}
		arrows.Clear();
	}
	#endregion

	#region Pillars Management
	public void AddPillar(MemoryPillar pillar, HexCell location)
	{
		pillars.Add (pillar);
		pillar.Grid = this;
		//pillar.VisionRange = range;
		pillar.Location = location;
	}

	public void RemovePillar(MemoryPillar pillar)
	{
		pillars.Remove (pillar);
		pillar.Die ();
	}

	public void ClearPillars()
	{
		for (int i = 0; i < pillars.Count; i++) {
			pillars[i].Die();
		}
		pillars.Clear();
	}
	#endregion

	#region Enemies Management
	public void AddEnemy(EnemyUnit enemy, HexCell location)
	{
		enemy.transform.SetParent (enemiesPool);
		enemies.Add (enemy);
		enemy.Grid = this;
		enemy.Location = location;
		if (units.Count > 0)
			enemy.Relic = units [0];
	}

	public void RemoveEnemy(EnemyUnit enemy)
	{
		enemies.Remove (enemy);
		enemy.Die ();
	}

	public EnemyUnit[] GetEnemies()
	{
		return enemies.ToArray ();
	}

	public void ClearEnemies()
	{
		for (int i = 0; i < enemies.Count; i++) {
			enemies[i].Die();
		}
		enemies.Clear();
	}
	#endregion

	#region Map Management
	public bool CreateMap (int x, int z, bool wrapping) {
		if (
			x <= 0 || x % HexMetrics.chunkSizeX != 0 ||
			z <= 0 || z % HexMetrics.chunkSizeZ != 0
		) {
			Debug.LogError("Unsupported map size.");
			return false;
		}

		ClearPath();
		ClearUnits();
		ClearArrows ();
		if (columns != null) {
			for (int i = 0; i < columns.Length; i++) {
				Destroy(columns[i].gameObject);
			}
		}

		cellCountX = x;
		cellCountZ = z;
		this.wrapping = wrapping;
		currentCenterColumnIndex = -1;
		HexMetrics.wrapSize = wrapping ? cellCountX : 0;
		chunkCountX = cellCountX / HexMetrics.chunkSizeX;
		chunkCountZ = cellCountZ / HexMetrics.chunkSizeZ;
		cellShaderData.Initialize(cellCountX, cellCountZ);
		CreateChunks();
		CreateCells();
		return true;
	}

	void CreateChunks () {
		columns = new Transform[chunkCountX];
		for (int x = 0; x < chunkCountX; x++) {
			columns[x] = new GameObject("Column").transform;
			columns[x].SetParent(transform, false);
		}

		chunks = new HexGridChunk[chunkCountX * chunkCountZ];
		for (int z = 0, i = 0; z < chunkCountZ; z++) {
			for (int x = 0; x < chunkCountX; x++) {
				HexGridChunk chunk = chunks[i++] = Instantiate(chunkPrefab);
				chunk.transform.SetParent(columns[x], false);
			}
		}
	}

	void CreateCells () {
		cells = new HexCell[cellCountZ * cellCountX];

		for (int z = 0, i = 0; z < cellCountZ; z++) {
			for (int x = 0; x < cellCountX; x++) {
				CreateCell(x, z, i++);
			}
		}
	}

	void CreateCell (int x, int z, int i) {
		Vector3 position;
		position.x = (x + z * 0.5f - z / 2) * HexMetrics.innerDiameter;
		position.y = 0f;
		position.z = z * (HexMetrics.outerRadius * 1.5f);

		HexCell cell = cells[i] = Instantiate<HexCell>(cellPrefab);
		cell.transform.localPosition = position;
		cell.coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
		cell.Index = i;
		cell.ColumnIndex = x / HexMetrics.chunkSizeX;
		cell.ShaderData = cellShaderData;

		if (wrapping) {
			cell.Explorable = z > 0 && z < cellCountZ - 1;
		}
		else {
			cell.Explorable =
				x > 0 && z > 0 && x < cellCountX - 1 && z < cellCountZ - 1;
		}

		if (x > 0) {
			cell.SetNeighbor(HexDirection.W, cells[i - 1]);
			if (wrapping && x == cellCountX - 1) {
				cell.SetNeighbor(HexDirection.E, cells[i - x]);
			}
		}
		if (z > 0) {
			if ((z & 1) == 0) {
				cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX]);
				if (x > 0) {
					cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX - 1]);
				}
				else if (wrapping) {
					cell.SetNeighbor(HexDirection.SW, cells[i - 1]);
				}
			}
			else {
				cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX]);
				if (x < cellCountX - 1) {
					cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX + 1]);
				}
				else if (wrapping) {
					cell.SetNeighbor(
						HexDirection.SE, cells[i - cellCountX * 2 + 1]
					);
				}
			}
		}

		Text label = Instantiate<Text>(cellLabelPrefab);
		label.rectTransform.anchoredPosition =
			new Vector2(position.x, position.z);
		cell.uiRect = label.rectTransform;

		cell.Elevation = 0;

		AddCellToChunk(x, z, cell);
	}

	void AddCellToChunk (int x, int z, HexCell cell) {
		int chunkX = x / HexMetrics.chunkSizeX;
		int chunkZ = z / HexMetrics.chunkSizeZ;
		HexGridChunk chunk = chunks[chunkX + chunkZ * chunkCountX];

		int localX = x - chunkX * HexMetrics.chunkSizeX;
		int localZ = z - chunkZ * HexMetrics.chunkSizeZ;
		chunk.AddCell(localX + localZ * HexMetrics.chunkSizeX, cell);
	}

	public HexCell GetCell (Ray ray) {
		RaycastHit hit;
		if (Physics.Raycast(ray, out hit)) {
			return GetCell(hit.point);
		}
		return null;
	}

	public HexCell GetCell (Vector3 position) {
		position = transform.InverseTransformPoint(position);
		HexCoordinates coordinates = HexCoordinates.FromPosition(position);
		return GetCell(coordinates);
	}

	public HexCell GetCell (HexCoordinates coordinates) {
		int z = coordinates.Z;
		if (z < 0 || z >= cellCountZ) {
			return null;
		}
		int x = coordinates.X + z / 2;
		if (x < 0 || x >= cellCountX) {
			return null;
		}
		return cells[x + z * cellCountX];
	}

	public HexCell GetCell (int xOffset, int zOffset) {
		return cells[xOffset + zOffset * cellCountX];
	}

	public HexCell GetCell (int cellIndex) {
		return cells[cellIndex];
	}

	public void IncreaseVisibility (HexCell fromCell, int range) {
		List<HexCell> cells = GetVisibleCells(fromCell, range);
		for (int i = 0; i < cells.Count; i++) {
			cells[i].IncreaseVisibility();
		}
		ListPool<HexCell>.Add(cells);
	}

	public void DecreaseVisibility (HexCell fromCell, int range) {
		List<HexCell> cells = GetVisibleCells(fromCell, range);
		for (int i = 0; i < cells.Count; i++) {
			cells[i].DecreaseVisibility();
		}
		ListPool<HexCell>.Add(cells);
	}

	public void ResetVisibility () {
		for (int i = 0; i < cells.Length; i++) {
			cells[i].ResetVisibility();
		}
		for (int i = 0; i < units.Count; i++) {
			HexUnit unit = units[i];
			IncreaseVisibility(unit.Location, unit.VisionRange);
		}
	}

	List<HexCell> GetVisibleCells (HexCell fromCell, int range) {
		List<HexCell> visibleCells = ListPool<HexCell>.Get();

		searchFrontierPhase += 2;
		if (searchFrontier == null) {
			searchFrontier = new HexCellPriorityQueue();
		}
		else {
			searchFrontier.Clear();
		}

		if(advancedVision)
			range += fromCell.ViewElevation;
		fromCell.SearchPhase = searchFrontierPhase;
		fromCell.Distance = 0;
		searchFrontier.Enqueue(fromCell);
		HexCoordinates fromCoordinates = fromCell.coordinates;
		while (searchFrontier.Count > 0) {
			HexCell current = searchFrontier.Dequeue();
			current.SearchPhase += 1;
			visibleCells.Add(current);

			for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
				HexCell neighbor = current.GetNeighbor(d);
				if (
					neighbor == null ||
					neighbor.SearchPhase > searchFrontierPhase ||
					!neighbor.Explorable
				) {
					continue;
				}

				int distance = current.Distance + 1;
				if(advancedVision)
				{
					if (distance + neighbor.ViewElevation > range || distance > fromCoordinates.DistanceTo (neighbor.coordinates)) 
					{
						continue;
					}
				}
				else 
				{
					if (distance > range || distance > fromCoordinates.DistanceTo (neighbor.coordinates)) 
					{
						continue;
					}
				}

				if (neighbor.SearchPhase < searchFrontierPhase) {
					neighbor.SearchPhase = searchFrontierPhase;
					neighbor.Distance = distance;
					neighbor.SearchHeuristic = 0;
					searchFrontier.Enqueue(neighbor);
				}
				else if (distance < neighbor.Distance) {
					int oldPriority = neighbor.SearchPriority;
					neighbor.Distance = distance;
					searchFrontier.Change(neighbor, oldPriority);
				}
			}
		}
		return visibleCells;
	}

	public void CenterMap (float xPosition) {
		int centerColumnIndex = (int)
			(xPosition / (HexMetrics.innerDiameter * HexMetrics.chunkSizeX));

		if (centerColumnIndex == currentCenterColumnIndex) {
			return;
		}
		currentCenterColumnIndex = centerColumnIndex;

		int minColumnIndex = centerColumnIndex - chunkCountX / 2;
		int maxColumnIndex = centerColumnIndex + chunkCountX / 2;

		Vector3 position;
		position.y = position.z = 0f;
		for (int i = 0; i < columns.Length; i++) {
			if (i < minColumnIndex) {
				position.x = chunkCountX *
					(HexMetrics.innerDiameter * HexMetrics.chunkSizeX);
			}
			else if (i > maxColumnIndex) {
				position.x = chunkCountX *
					-(HexMetrics.innerDiameter * HexMetrics.chunkSizeX);
			}
			else {
				position.x = 0f;
			}
			columns[i].localPosition = position;
		}
	}
	#endregion

	public void ShowUI (bool visible) {
		for (int i = 0; i < chunks.Length; i++) {
			chunks[i].ShowUI(visible);
		}
	}

	#region Load/Save
	public void Save (BinaryWriter writer) {
		writer.Write(cellCountX);
		writer.Write(cellCountZ);
		writer.Write(wrapping);

		for (int i = 0; i < cells.Length; i++) {
			cells[i].Save(writer);
		}

		writer.Write(units.Count);
		for (int i = 0; i < units.Count; i++) {
			units[i].Save(writer);
		}

		writer.Write (arrows.Count);
		for (int i = 0; i < arrows.Count; i++) {
			arrows[i].Save(writer);
		}

		writer.Write (pillars.Count);
		for (int i = 0; i < pillars.Count; i++) {
			pillars[i].Save(writer);
		}

		writer.Write (enemies.Count);
		for (int i = 0; i < enemies.Count; i++) {
			enemies[i].Save(writer);
		}
	}

	public void Load (BinaryReader reader, int header) {
		ClearPath();
		ClearUnits();
		ClearArrows ();
		ClearPillars ();
		ClearEnemies ();
		int x = 20, z = 15;
		if (header >= 1) {
			x = reader.ReadInt32();
			z = reader.ReadInt32();
		}
		bool wrapping = header >= 5 ? reader.ReadBoolean() : false;
		if (x != cellCountX || z != cellCountZ || this.wrapping != wrapping) {
			if (!CreateMap(x, z, wrapping)) {
				return;
			}
		}

		bool originalImmediateMode = cellShaderData.ImmediateMode;
		cellShaderData.ImmediateMode = true;

		for (int i = 0; i < cells.Length; i++) {
			cells[i].Load(reader, header);
		}
		for (int i = 0; i < chunks.Length; i++) {
			chunks[i].Refresh();
		}

		if (header >= 2) {
			int unitCount = reader.ReadInt32();
			for (int i = 0; i < unitCount; i++) {
				HexUnit.Load(reader, this);
			}
			SetCamera ();
		}

		if(header >= 8)
		{
			int arrowCount = reader.ReadInt32 ();
			for (int i = 0; i < arrowCount; i++) {
				ScoutArrow.Load(reader, this);
			}
		}

		if(header >= 10)
		{
			int pillarCount = reader.ReadInt32 ();
			for (int i = 0; i < pillarCount; i++) {
				MemoryPillar.Load(reader, this);
			}
		}

		if(header >= 11)
		{
			int enemyCount = reader.ReadInt32 ();
			for (int i = 0; i < enemyCount; i++) {
				EnemyUnit.Load(reader, this);
			}
		}

		cellShaderData.ImmediateMode = originalImmediateMode;
	}
	#endregion

	#region Pathfinding
	public List<HexCell> GetPath () {
		if (!currentPathExists) {
			//return null;
		}
		List<HexCell> path = ListPool<HexCell>.Get();
		for (HexCell c = currentPathTo; c != currentPathFrom; c = c.PathFrom) {
			path.Add(c);
		}
		path.Add(currentPathFrom);
		path.Reverse();
		return path;
	}

	public void ClearPath () {
		if (currentPathExists) {
			HexCell current = currentPathTo;
			while (current != currentPathFrom) {
				current.SetLabel(null);
				current.DisableHighlight();
				current = current.PathFrom;
			}
			//current.DisableHighlight();
			currentPathExists = false;
		}
		else if (currentPathFrom) {
			//currentPathFrom.DisableHighlight();
			currentPathTo.DisableHighlight();
		}
		currentPathFrom = currentPathTo = null;
	}

	void ShowPath (int speed) {
		if (currentPathExists) {
			HexCell current = currentPathTo;
			while (current != currentPathFrom) {
				//int turn = (current.Distance - 1) / speed;
				int turn = current.Distance;
				current.SetLabel(turn.ToString());
				current.EnableHighlight(Color.white);
				current = current.PathFrom;
			}
		}
		//currentPathFrom.EnableHighlight(Color.blue);
		currentPathTo.EnableHighlight(Color.red);
	}

	void ShowArrow(int range)
	{
		if (currentPathExists) {
			HexCell current = currentPathTo;
			while (current != currentPathFrom) {
				//current.EnableHighlight (Color.white);
				current = current.PathFrom;
			}
			currentPathTo.EnableHighlight (Color.yellow);
		} else
			currentPathTo.EnableHighlight (Color.black);
	}

	public void FindPath (HexCell fromCell, HexCell toCell, HexUnit unit) 
	{
		ClearPath();
		currentPathFrom = fromCell;
		currentPathTo = toCell;
		currentPathExists = Search(fromCell, toCell, unit);
		ShowPath(unit.SpeedLeft);
	}

	bool Search (HexCell fromCell, HexCell toCell, HexUnit unit) {
		int speed = unit.SpeedLeft;
		searchFrontierPhase += 2;
		if (searchFrontier == null) {
			searchFrontier = new HexCellPriorityQueue();
		}
		else {
			searchFrontier.Clear();
		}

		fromCell.SearchPhase = searchFrontierPhase;
		fromCell.Distance = 0;
		searchFrontier.Enqueue(fromCell);
		while (searchFrontier.Count > 0) {
			HexCell current = searchFrontier.Dequeue();
			current.SearchPhase += 1;

			if (current == toCell) {
				return true;
			}

			if (current.Distance >= speed )
				return false;

			int currentTurn = (current.Distance - 1) / speed;

			for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
				HexCell neighbor = current.GetNeighbor(d);
				if (
					neighbor == null ||
					neighbor.SearchPhase > searchFrontierPhase
				) {
					continue;
				}
				if (!unit.IsValidDestination(neighbor)) {
					continue;
				}
				int moveCost = unit.GetMoveCost(current, neighbor, d);
				if (moveCost < 0) {
					continue;
				}

				int distance = current.Distance + moveCost;
				int turn = (distance - 1) / speed;
				if (turn > currentTurn) {
					distance = turn * speed + moveCost;
				}

				if (neighbor.SearchPhase < searchFrontierPhase) {
					neighbor.SearchPhase = searchFrontierPhase;
					neighbor.Distance = distance;
					neighbor.PathFrom = current;
					neighbor.SearchHeuristic =
						neighbor.coordinates.DistanceTo(toCell.coordinates);
					searchFrontier.Enqueue(neighbor);
				}
				else if (distance < neighbor.Distance) {
					int oldPriority = neighbor.SearchPriority;
					neighbor.Distance = distance;
					neighbor.PathFrom = current;
					searchFrontier.Change(neighbor, oldPriority);
				}
			}
		}
		return false;
	}

	public void FindArrow(HexCell fromCell, HexCell toCell, HexUnit unit)
	{
		ClearPath ();
		currentPathFrom = fromCell;
		currentPathTo = toCell;
		currentPathExists = SearchArrow (fromCell, toCell, unit);
		ShowArrow (unit.ArrowRange);
	}

	bool SearchArrow (HexCell fromCell, HexCell toCell, HexUnit unit)
	{
		int range = unit.ArrowRange;
		searchFrontierPhase += 2;
		if (searchFrontier == null) {
			searchFrontier = new HexCellPriorityQueue();
		}
		else {
			searchFrontier.Clear();
		}

		fromCell.SearchPhase = searchFrontierPhase;
		fromCell.Distance = 0;
		searchFrontier.Enqueue(fromCell);
		while (searchFrontier.Count > 0) {
			HexCell current = searchFrontier.Dequeue();
			current.SearchPhase += 1;

			if (current == toCell) {
				return true;
			}

			if (current.Distance > range -1)
				return false;


			int currentTurn = (current.Distance - 1) / range;

			for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
				HexCell neighbor = current.GetNeighbor(d);
				if (
					neighbor == null ||
					neighbor.SearchPhase > searchFrontierPhase
				) {
					continue;
				}

				int moveCost = 1;
				int distance = current.Distance + moveCost;
				int turn = (distance - 1) / range;
				if (turn > currentTurn) {
					distance = turn * range + moveCost;
				}

				if (neighbor.SearchPhase < searchFrontierPhase) {
					neighbor.SearchPhase = searchFrontierPhase;
					neighbor.Distance = distance;
					neighbor.PathFrom = current;
					neighbor.SearchHeuristic =
						neighbor.coordinates.DistanceTo(toCell.coordinates);
					searchFrontier.Enqueue(neighbor);
				}
				else if (distance < neighbor.Distance) {
					int oldPriority = neighbor.SearchPriority;
					neighbor.Distance = distance;
					neighbor.PathFrom = current;
					searchFrontier.Change(neighbor, oldPriority);
				}
			}
		}
		return false;
	}

	public void FindSpawnPosition(HexCell fromCell, HexCell toCell)
	{
		ClearPath ();
		currentPathFrom = fromCell;
		currentPathTo = toCell;
		currentPathExists = SearchSpawn (fromCell, toCell);
		ShowSpawnCell (1);
	}

	void ShowSpawnCell(int range)
	{
		if (currentPathExists) {
			HexCell current = currentPathTo;
			while (current != currentPathFrom) {
				current = current.PathFrom;
			}
			currentPathTo.EnableHighlight (Color.white);
		} else
			currentPathTo.EnableHighlight (Color.black);
	}

	bool SearchSpawn (HexCell fromCell, HexCell toCell)
	{
		int range = 1;
		searchFrontierPhase += 2;
		if (searchFrontier == null) {
			searchFrontier = new HexCellPriorityQueue();
		}
		else {
			searchFrontier.Clear();
		}

		fromCell.SearchPhase = searchFrontierPhase;
		fromCell.Distance = 0;
		searchFrontier.Enqueue(fromCell);
		while (searchFrontier.Count > 0) {
			HexCell current = searchFrontier.Dequeue();
			current.SearchPhase += 1;

			if (current == toCell) {
				return true;
			}

			if (current.Distance > range -1)
				return false;


			int currentTurn = (current.Distance - 1) / range;

			for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
				HexCell neighbor = current.GetNeighbor(d);
				if (
					neighbor == null ||
					neighbor.SearchPhase > searchFrontierPhase
				) {
					continue;
				}

				int moveCost = 1;
				int distance = current.Distance + moveCost;
				int turn = (distance - 1) / range;
				if (turn > currentTurn) {
					distance = turn * range + moveCost;
				}

				if (neighbor.SearchPhase < searchFrontierPhase) {
					neighbor.SearchPhase = searchFrontierPhase;
					neighbor.Distance = distance;
					neighbor.PathFrom = current;
					neighbor.SearchHeuristic =
						neighbor.coordinates.DistanceTo(toCell.coordinates);
					searchFrontier.Enqueue(neighbor);
				}
				else if (distance < neighbor.Distance) {
					int oldPriority = neighbor.SearchPriority;
					neighbor.Distance = distance;
					neighbor.PathFrom = current;
					searchFrontier.Change(neighbor, oldPriority);
				}
			}
		}
		return false;
	}

	public bool FindUnit (HexCell fromCell, HexCell toCell, int searchRange)
	{
		//ClearPath ();
		currentPathFrom = fromCell;
		currentPathTo = toCell;
		currentPathExists = SearchUnit (fromCell, toCell, searchRange);
		return currentPathExists;
	}

	public bool SearchUnit (HexCell fromCell, HexCell toCell, int searchRange)
	{
		int speed = searchRange;
		searchFrontierPhase += 2;
		if (searchFrontier == null) {
			searchFrontier = new HexCellPriorityQueue();
		}
		else {
			searchFrontier.Clear();
		}

		fromCell.SearchPhase = searchFrontierPhase;
		fromCell.Distance = 0;
		searchFrontier.Enqueue(fromCell);
		while (searchFrontier.Count > 0) {
			HexCell current = searchFrontier.Dequeue();
			current.SearchPhase += 1;

			if (current == toCell) {
				return true;
			}

			if (current.Distance >= speed) {
				currentPathTo = current;
				return false;
			}

			if (current.Unit)
				return true;

			for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) 
			{
				HexCell neighbor = current.GetNeighbor(d);
				if (
					neighbor == null ||
					neighbor.SearchPhase > searchFrontierPhase
				) {
					continue;
				}

				int distance = current.Distance + 1;

				if (neighbor.SearchPhase < searchFrontierPhase) {
					neighbor.SearchPhase = searchFrontierPhase;
					neighbor.Distance = distance;
					neighbor.PathFrom = current;
					neighbor.SearchHeuristic =
						neighbor.coordinates.DistanceTo(toCell.coordinates);
					searchFrontier.Enqueue(neighbor);
				}
				else if (distance < neighbor.Distance) {
					int oldPriority = neighbor.SearchPriority;
					neighbor.Distance = distance;
					neighbor.PathFrom = current;
					searchFrontier.Change(neighbor, oldPriority);
				}
			}
		}
		return false;
	}


	#endregion
}