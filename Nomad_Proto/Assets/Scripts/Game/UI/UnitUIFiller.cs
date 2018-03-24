using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UnitUIFiller : MonoBehaviour 
{
	[SerializeField] private Text _type;
	[SerializeField] private Image _illustration;
	[SerializeField] private Text _movement;
	[SerializeField] private Sprite[] _unitsDisplay;
	[SerializeField] private ActionButton _actionPrefab;
	[SerializeField] private Transform _actions;
	private List<ActionButton> _actionsDisplayed = new List<ActionButton>();

	void Start()
	{
		DisplayUnit (null);
		//DisplayActions (null);
	}

	public void DisplayUnit(HexUnit unit)
	{
		bool isValid = unit != null ? true : false;
		_type.text = isValid ? unit.Type.ToString () : "";
		_illustration.sprite = isValid? _unitsDisplay [(int)unit.Type] : null;
		_illustration.enabled = isValid;
		_movement.text = isValid? "Movement " + unit.SpeedLeft + "/" + unit.Speed : "";
	}

	public void DisplaySpawningUnit(UnitTypes type)
	{
		DisplayActions (null, 0);
		_type.text = type.ToString ();
		_illustration.sprite = _unitsDisplay [(int)type];
		_illustration.enabled = true;
		_movement.text = "Choose unit position...";
	}

	public void DisplayActions(HexUnit unit, int pointsLeft)
	{
		//StartCoroutine (UpdateActions (unit, pointsLeft));
		UpdateActions (unit, pointsLeft);
	}

	void ClearActions()
	{
		for (int i = 0; i < _actionsDisplayed.Count ; i++)
		{
			var action = _actionsDisplayed [i];
			Destroy (action.gameObject);
		}
		_actionsDisplayed.Clear ();
	}

	void UpdateActions(HexUnit unit, int pointsLeft)
	{
		bool isValid = unit != null ? true : false;
		ClearActions ();
		if(isValid)
		{
			foreach(var action in unit._data.actions)
			{
				ActionButton UIaction = Instantiate (_actionPrefab, _actions);
				_actionsDisplayed.Add (UIaction);
				UIaction.Setup (action, pointsLeft, unit);
			}
		}
	}

	/*IEnumerator ClearActions()
	{
		for (int i = 0; i < _actionsDisplayed.Count ; i++)
		{
			var action = _actionsDisplayed [i];
			Destroy (action.gameObject);
		}
		_actionsDisplayed.Clear ();
		yield return null;
	}

	IEnumerator UpdateActions(HexUnit unit, int pointsLeft)
	{
		bool isValid = unit != null ? true : false;
		yield return ClearActions ();
		if(isValid)
		{
			foreach(var action in unit._data.actions)
			{
				ActionButton UIaction = Instantiate (_actionPrefab, _actions);
				_actionsDisplayed.Add (UIaction);
				UIaction.Setup (action, pointsLeft, unit);
			}
		}
	}*/
}
