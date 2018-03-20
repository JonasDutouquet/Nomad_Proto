using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class TurnManager : MonoBehaviour
{
	[SerializeField] private int _pointsPerTurn = 6;
	[SerializeField] private HexGrid _grid;
	[SerializeField] private HexGameUI _hexUI;
	[SerializeField] private bool _forget;

	[Header("UI")]
	[SerializeField] private ActionPointsUI _actionPointsUI;
	[SerializeField] private Material _terrainMaterial;	//for forget mechanic
	private int _pointsUsed = 0;
	private ResourceManager _resMan;

	public HexUnit Relic {get;set;}

	void Awake()
	{
		SetActionPoints ();
		_resMan = GetComponent<ResourceManager> ();
		if(_forget) _terrainMaterial.EnableKeyword ("END_TURN");
	}

	void SetActionPoints()
	{
		_pointsUsed = 0;
		_actionPointsUI.SetTotalPoints (_pointsPerTurn);
		_actionPointsUI.SetPointsLeft (_pointsPerTurn);
	}

	public int PointsLeft
	{
		get
		{
			return _pointsPerTurn - _pointsUsed;
		}
	}

	public bool CanDoAction (UnitAction action)
	{
		bool canDoAction = PointsLeft >= action.cost ? true : false;
		if (canDoAction)
		{
			_pointsUsed += action.cost;
			_actionPointsUI.SetPointsLeft (PointsLeft);
		}

		return canDoAction;
	}

	public void UpdateProduction()
	{
		_resMan.SetResource ();
	}

	void Update()
	{
		if(Input.GetKeyDown (KeyCode.Return))
		{
			EndTurn ();
		}
	}

	public void EndTurn()
	{
		//reset actions done for each unit
		_grid.DistanceToRelic (Relic.Location, Relic.MemoryRange);
		List <HexUnit> units = _grid.GetUnits ();
		for (int i = 0 ; i< units.Count ;i++)
		{
			HexUnit unit = units [i];
			unit.ResetActionsDone ();
			unit.ConsumeResource ();
			if (!unit._inMemory)
			{
				_grid.RemoveUnit (unit);
			}
		}

		SetActionPoints ();
		_resMan.EndTurn ();
		//_hexUI.ClearArrows ();
		_grid.ClearArrows ();
	}

	public void Save (BinaryWriter writer)
	{
		writer.Write ((byte)_pointsPerTurn);
		writer.Write ((byte)_pointsUsed);
	}

	public void Load (BinaryReader reader, int header)
	{
		if(header >= 6)
		{
			_pointsPerTurn = reader.ReadByte ();
			_pointsUsed = reader.ReadByte ();
			_actionPointsUI.SetPointsLeft (PointsLeft);
		}
	}
}
