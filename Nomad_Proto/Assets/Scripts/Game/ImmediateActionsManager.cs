using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ImmediateActionsManager : MonoBehaviour
{
	private ResourceManager _resource;
	private TurnManager _turn;

	void Start()
	{
		_resource = GetComponent<ResourceManager> ();
		_turn = GetComponent<TurnManager> ();
	}

	void OnEnable()
	{
		ActionButton.OnImmediateAction += ExecuteAction;
	}

	void ExecuteAction(UnitAction action, HexUnit unit)
	{
		ActionTypes type = action.type;
		switch(type)
		{
		case ActionTypes.Double:
			if (_turn.CanDoAction (action))
			{
				_resource.DoubleFood (unit.Production);
				unit.DidAction ((int)type, true);
			}
			break;
		}
	}

	void OnDisable()
	{
		ActionButton.OnImmediateAction -= ExecuteAction;
	}
}
