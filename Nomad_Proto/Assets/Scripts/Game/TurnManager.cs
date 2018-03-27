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
	[Range(0f, 3f)][SerializeField] private float _oblivionDelay = 1f;

	[Header("UI")]
	[SerializeField] private ActionPointsUI _actionPointsUI;
	[SerializeField] private Material _terrainMaterial;	//for forget mechanic
	[SerializeField] private EndTurnUI _endTurnWarning;
	[SerializeField] private InterTurnUI _interTurnInfo;
	private int _pointsUsed = 0;
	private ResourceManager _resMan;

	public HexUnit Relic {get;set;}

	void Awake()
	{
		//_actionPointsUI.SetTotalPoints (_pointsPerTurn);
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

	public bool CanDoAction (int cost)
	{
		bool canDoAction = PointsLeft >= cost ? true : false;
		if (canDoAction)
		{
			_pointsUsed += cost;
			_actionPointsUI.SetPointsLeft (PointsLeft);
		}

		return canDoAction;
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
		//DEBUG
		if(Input.GetKeyDown (KeyCode.Return))
		{
			StartCoroutine (DoEndTurn (0f));
		}
	}

	public void WarnEndTurn()
	{
		int unitsOutOfMemory = _grid.UnitsOutOfMemory;

		if (PointsLeft > 0 || unitsOutOfMemory > 0) 
		{
			//warn
			_endTurnWarning.DisplayWarning (PointsLeft, unitsOutOfMemory);
		} else
		{
			StartCoroutine (DoEndTurn (_oblivionDelay));
			_hexUI.DoMoveRelic ();
		}
	}

	public void EndTurn()
	{
		StartCoroutine (DoEndTurn (_oblivionDelay));
	}

	IEnumerator DoEndTurn(float delay)
	{
		_interTurnInfo.gameObject.SetActive (true);

		yield return new WaitForSeconds (delay);

		//_interTurnInfo.DisplayForgetting ();

		//reset actions done for each unit
		_grid.DistanceToRelic (Relic.Location);
		List <HexUnit> units = _grid.GetUnits ();
		for (int i = 0 ; i< units.Count ;i++)
		{
			HexUnit unit = units [i];
			unit.ResetActionsDone ();
			unit.ConsumeResource ();
			if (!unit.InMemory)
			{
				_grid.RemoveUnit (unit);
			}
		}

		SetActionPoints ();
		_resMan.EndTurn ();
		_grid.ClearArrows ();

		yield return null;

		_interTurnInfo.DisplayEnemiesTurn ();

		//enemies turn
		EnemyUnit[] enemies = _grid.GetEnemies ();
		for(int y = 0 ; y < enemies.Length ; y++)
		{
			enemies[y].Move ();
		}

		yield return new WaitForSeconds (1f);

		_interTurnInfo.gameObject.SetActive (false);
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
