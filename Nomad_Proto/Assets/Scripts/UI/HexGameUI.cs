﻿using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class HexGameUI : MonoBehaviour {

	public HexGrid grid;
	[SerializeField] private UnitUIFiller _unitDisplay;
	[SerializeField] private TurnManager _turnMan;
	[SerializeField] private HexMapEditor _hexMap;
	[SerializeField] private HexMapCamera _camera;
	[SerializeField] private GameObject _arrowPrefab;
	[SerializeField] private GameObject _resourcePrefab;
	private bool[] _actionEnable;
	private bool _inAction = false;
	private UnitAction _currentAction;
	private ActionButton _currentButton;

	HexCell currentCell;

	HexUnit selectedUnit;

	void Awake()
	{
		_actionEnable = new bool[System.Enum.GetValues(typeof(ActionTypes)).Length];
		HexCell.resourcePrefab = _resourcePrefab;
		HexCell.arrowPrefab = _arrowPrefab;
	}

	public HexUnit SelectedUnit
	{
		get{
			return selectedUnit;
		}
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
			if (Input.GetMouseButtonDown(0))
			{
				DoSelection();
			}
			else if (selectedUnit && _inAction)
			{
				ActionTypes type = _currentAction.type;
				switch (type)
				{
				case ActionTypes.Move:
					if (Input.GetMouseButtonDown(1) && grid.HasPath)
					{
						if (_turnMan.CanDoAction (_currentAction)) 
						{
							DoMove ();
							selectedUnit.DidAction ((int)type, true);
							if (selectedUnit.Type == UnitTypes.Producer)
								_turnMan.UpdateProduction ();
							_currentButton.UpdateButtonInteract (_turnMan.PointsLeft, selectedUnit);
						}
						_inAction = false;
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
				}
			}
		}
	}

	void DoSelection () {
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

	void DoMove () {
		if (grid.HasPath) {
			SelectedUnit.Location.DisableHighlight ();
			selectedUnit.Travel(grid.GetPath());
			grid.ClearPath();
			selectedUnit.Location.EnableHighlight (selectedUnit.SelectedCol);
		}
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