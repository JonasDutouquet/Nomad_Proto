using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ImmediateActionsManager : MonoBehaviour
{
	[SerializeField] private HexGrid _grid;
	[Header("UI")]
	[SerializeField] private GameObject _sacrificeWarningUI;
	private ResourceManager _resource;
	private TurnManager _turn;
	private HexUnit _unitToSacrifice;
	private int _sacrificeCost;

	void Start()
	{
		_resource = GetComponent<ResourceManager> ();
		_turn = GetComponent<TurnManager> ();
	}

	void OnEnable()
	{
		ActionButton.OnImmediateAction += ExecuteAction;
	}

	void ExecuteAction(UnitAction action, HexUnit unit, Button button)
	{
		ActionTypes type = action.type;
		switch(type)
		{
		case ActionTypes.Double:
			if (_turn.CanDoAction (action))
			{
				_resource.DoubleFood (unit.Production);
				unit.DidAction ((int)type, true);
				button.interactable = false;
			}
			break;

		case ActionTypes.Sacrifice:
			_sacrificeWarningUI.SetActive (true);
			_unitToSacrifice = unit;
			_sacrificeCost = action.cost;
			break;
		}
	}

	public void SacrificeUnit()
	{
		if(_turn.CanDoAction (_sacrificeCost)) 
		{
			_grid.RemoveUnit (_unitToSacrifice);
			_unitToSacrifice = null;
		}
	}

	void OnDisable()
	{
		ActionButton.OnImmediateAction -= ExecuteAction;
	}
}
