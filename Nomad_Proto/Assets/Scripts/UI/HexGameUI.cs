using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class HexGameUI : MonoBehaviour {

	public HexGrid grid;
	[SerializeField] private UnitUIFiller _unitDisplay;
	[SerializeField] private TurnManager _turnMan;
	[SerializeField] private HexMapEditor _hexMap;
	[SerializeField] private HexMapCamera _camera;
	[SerializeField] private GameObject _unitCreationInfo;
	[SerializeField] private GameObject _pillarInfo;
	private bool[] _actionEnable;
	private bool _inAction = false;
	private UnitAction _currentAction;
	private ActionButton _currentButton;

	public UnitTypes? UnitToCreate { get; set;}
	public bool SpawningUnit {get; set;}

	HexCell currentCell;

	HexUnit selectedUnit;

	void Awake()
	{
		_actionEnable = new bool[System.Enum.GetValues(typeof(ActionTypes)).Length];
		_hexMap.ShowGrid (false);
	}

	public void SetEditMode (bool toggle) {
		enabled = !toggle;
		grid.ShowUI(!toggle);
		grid.ClearPath();
		if (toggle) {
			Shader.EnableKeyword("HEX_MAP_EDIT_MODE");
		}
		else {
			Shader.DisableKeyword("HEX_MAP_EDIT_MODE");
		}
	}

	void Update () {
		if (!EventSystem.current.IsPointerOverGameObject()) 
		{
			if (Input.GetMouseButtonDown(0) && !SpawningUnit)
			{
				DoSelection();
			}
			else if (selectedUnit && _inAction)
			{
				//_camera.SetFollowedUnit (selectedUnit);
				ActionTypes type = _currentAction.type;
				switch (type)
				{
				case ActionTypes.Move:
					if (Input.GetMouseButtonDown(1) && grid.HasPath)
					{
						if (selectedUnit.SpeedLeft == selectedUnit.Speed)
						{
							if (_turnMan.CanDoAction (_currentAction)) 
							{
								DoMove ();
								if (selectedUnit.Type == UnitTypes.Producer)
									_turnMan.UpdateProduction ();
							}
						}
						else
						{
							DoMove ();
							selectedUnit.DidAction ((int)type, true);
							if (selectedUnit.Type == UnitTypes.Producer)
								_turnMan.UpdateProduction ();
							_inAction = false;
						}
						_unitDisplay.DisplayUnit (selectedUnit);
						_currentButton.SetMoveCost (_turnMan.PointsLeft);
					}
					else
					{
						DoPathfinding();
					}
					break;

				case ActionTypes.MoveRelic:
					if (Input.GetMouseButtonDown(1) && grid.HasPath)
					{
						if (_turnMan.CanDoAction (_currentAction)) 
						{
							grid.DistanceToRelic (currentCell);
							_turnMan.WarnEndTurn ();

							_inAction = false;
						}
					}
					else
					{
						DoPathfinding();
					}
					break;

				case ActionTypes.Arrow:
					if (Input.GetMouseButton (1) && grid.HasPath) 
					{
						//shoot arrow
						_turnMan.CanDoAction (_currentAction);
						ShootArrow ();
						selectedUnit.DidAction ((int)type, true);
						_currentButton.UpdateButtonInteract (_turnMan.PointsLeft, selectedUnit);
						_inAction = false;
					} else 
					{
						DoArrowFinding ();
					}
					break;

				case ActionTypes.NewFighter:
					if(_turnMan.CanDoAction (_currentAction))
					{
						UnitToCreate = UnitTypes.Fighter;
						_unitCreationInfo.SetActive (true);
						selectedUnit.DidAction ((int)type, true);
						selectedUnit.DidAction ((int)ActionTypes.NewProducer, true);
						_unitDisplay.DisplayActions (selectedUnit, _turnMan.PointsLeft);
						_inAction = false;
					}
					break;

				case ActionTypes.NewProducer:
					if(_turnMan.CanDoAction (_currentAction))
					{
						UnitToCreate = UnitTypes.Producer;
						_unitCreationInfo.SetActive (true);
						selectedUnit.DidAction ((int)type, true);
						selectedUnit.DidAction ((int)ActionTypes.NewFighter, true);
						_unitDisplay.DisplayActions (selectedUnit, _turnMan.PointsLeft);
						_inAction = false;
					}
					break;
				}
			}
			else if(SpawningUnit)
			{
				if (Input.GetMouseButton (1) && grid.HasPath)
				{
					DoCreateUnit ();
				}
				else{
					DoSpawnFinding ();
				}
			}
		}
	}

	public void DoSelection () {
		grid.ClearPath();
		UpdateCurrentCell();
		if(selectedUnit)selectedUnit.Location.DisableHighlight ();
		if (currentCell) 
		{
			selectedUnit = currentCell.Unit;
			_hexMap.ShowGrid (selectedUnit != null);
			if (selectedUnit != null) 
			{
				currentCell.EnableHighlight (selectedUnit.SelectedCol);
				_camera.SetFollowedUnit (selectedUnit);
			}
			_unitDisplay.DisplayUnit (selectedUnit);
			_unitDisplay.DisplayActions (selectedUnit, _turnMan.PointsLeft);
			ListenToActions ();
		}
	}

	void DoPathfinding () {
		if (UpdateCurrentCell()) {
			if (currentCell && selectedUnit.IsValidDestination(currentCell)) {
				grid.FindPath(selectedUnit.Location, currentCell, selectedUnit);
			}
			else {
				grid.ClearPath();
			}
		}
	}

	void DoMove () {
		if (grid.HasPath)
		{
			selectedUnit.Location.DisableHighlight ();
			selectedUnit.Travel(grid.GetPath());
			grid.ClearPath();
			selectedUnit.Location.EnableHighlight (selectedUnit.SelectedCol);
			if (selectedUnit.SpeedLeft == 0) {
				selectedUnit.SpeedLeft = selectedUnit.Speed;
				_inAction = false;
			}
		}
	}

	public void DoMoveRelic()
	{
		if(currentCell.Pillar)
		{
			currentCell.Pillar.Reveal ();
			_pillarInfo.SetActive (true);
		}
		DoMove ();

		//spawn a unit?
		if(UnitToCreate != null)
		{
			SpawningUnit = true;
			_unitDisplay.DisplaySpawningUnit (UnitToCreate.Value);
		} 
		else {
			grid.ClearPath ();
			selectedUnit.Location.DisableHighlight ();
			selectedUnit = null;
			_hexMap.ShowGrid (false);
			_unitDisplay.DisplayUnit (null);
			_unitDisplay.DisplayActions (null, _turnMan.PointsLeft);
		}

	}

	void DoArrowFinding()
	{
		if (UpdateCurrentCell()) {
			if (currentCell && selectedUnit.IsValidArrow (currentCell)) {
				grid.FindArrow(selectedUnit.Location, currentCell, selectedUnit);
			}
			else {
				grid.ClearPath();
			}
		}
	}

	void ShootArrow()
	{
		grid.AddArrow (Instantiate(ScoutArrow.arrowPrefab), currentCell, selectedUnit.ArrowRange);
	}

	void DoSpawnFinding()
	{
		if (UpdateCurrentCell()) {
			if (currentCell && selectedUnit.IsValidDestination (currentCell)) {
				grid.FindSpawnPosition(selectedUnit.Location, currentCell);
			}
			else {
				grid.ClearPath();
			}
		}
	}

	public void DoCreateUnit()
	{
		grid.AddUnit (Instantiate(HexUnit.unitPrefab), currentCell, 0f, UnitToCreate.Value);
		UnitToCreate = null;
		SpawningUnit = false;
		DoSelection ();
	}

	bool UpdateCurrentCell () {
		HexCell cell =
			grid.GetCell(Camera.main.ScreenPointToRay(Input.mousePosition));
		if (cell != currentCell)
		{
			currentCell = cell;
			return true;
		}
		return false;
	}

	void ListenToActions()
	{
		DisableActions ();
		ActionButton.OnActionClicked -= EnableAction;
		ActionButton.OnActionClicked += EnableAction;
	}

	void DisableActions()
	{
		_inAction = false;
		for(int i = 0 ; i < _actionEnable.Length ; i ++)
		{
			_actionEnable [i] = false;
		}
	}

	void EnableAction(UnitAction action, ActionButton button)
	{
		DisableActions ();
		_actionEnable [(int)action.type] = true;
		_inAction = true;
		_currentAction = action;
		_currentButton = button;
	}

	void OnDisable()
	{
		ActionButton.OnActionClicked -= EnableAction;
	}
}