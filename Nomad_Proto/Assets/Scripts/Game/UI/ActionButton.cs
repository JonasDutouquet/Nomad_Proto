﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ActionButton : MonoBehaviour 
{
	[SerializeField] private Text _action;
	[SerializeField] private Text _cost;
	private Button _button;
	private UnitAction _relatedAction;
	private HexUnit _relatedUnit;

	public delegate void ActionClicked (UnitAction action, ActionButton button);
	public static event ActionClicked OnActionClicked;

	public delegate void ImmediateAction (UnitAction action, HexUnit unit, Button button);
	public static event ImmediateAction OnImmediateAction;

	void OnEnable()
	{
		_button = GetComponent<Button> ();
	}

	public void Setup(UnitAction action, int pointsLeft, HexUnit unit)
	{
		_relatedAction = action;
		_relatedUnit = unit;
		_action.text = action.displayName;
		if (action.type == ActionTypes.Move)
			SetMoveCost (pointsLeft);
		else 
		{
			_cost.text = "(" + action.cost.ToString () + ")";
			UpdateButtonInteract (pointsLeft, unit);
		}
		_button.onClick.AddListener (SendActionOrder);
	}

	public void SetMoveCost(int pointsLeft)
	{
		if(_relatedUnit.SpeedLeft == _relatedUnit.Speed)
		{
			_cost.text = "(" + _relatedAction.cost.ToString () + ")";
			UpdateButtonInteract (pointsLeft, _relatedUnit);
		}
		else 
		{
			_cost.text = "(0)";
		}
	}

	void SendActionOrder()
	{
		if(!_relatedAction.immediate) 
		{
			if (OnActionClicked != null)
				OnActionClicked (_relatedAction, this);
		}
		else
		{
			if (OnImmediateAction != null)
				OnImmediateAction (_relatedAction, _relatedUnit, _button);
		}
	}

	public void UpdateButtonInteract(int pointsLeft, HexUnit unit)
	{
		if(_relatedAction.cost > pointsLeft || (!_relatedAction.canRepeat && unit.DidAction ((int)_relatedAction.type, false)))
			_button.interactable = false;
	}
}
