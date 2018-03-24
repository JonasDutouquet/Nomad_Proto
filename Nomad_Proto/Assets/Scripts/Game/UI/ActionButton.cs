using System.Collections;
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
		_action.text = action.displayName;
		_cost.text = unit.SpeedLeft == unit.Speed ? "(" + action.cost.ToString () + ")" : "(0)";
		_relatedAction = action;
		_relatedUnit = unit;
		_button.onClick.AddListener (SendActionOrder);
		UpdateButtonInteract (pointsLeft, unit);
	}

	public void SetMoveCost(UnitAction action, HexUnit unit)
	{
		_cost.text = unit.SpeedLeft == unit.Speed ? "(" + action.cost.ToString () + ")" : "(0)";
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
