using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class ResourceManager : MonoBehaviour
{
	[SerializeField] private HexGrid _grid;
	[SerializeField] private float _techPointCost = 10;
	[SerializeField] private int _riskPerTurn = 5;

	[Header("UI")]
	[SerializeField] private Text _food;
	[SerializeField] private Text _research;
	[SerializeField] private Text _techPoints;
	[SerializeField] private Image _currentTechStatus;
	[SerializeField] private Image _nextTechStatus;
	[SerializeField] private Text _risk;
	[SerializeField] private Text _penalty;

	private int _foodProduced, _foodNeeded;
	private float _researchStatus = 0;
	private int _riskStatus = 0;
	private int _techPointsCount, _extraRisk;

	private int ResearchProduced
	{
		get{
			_extraRisk = 0;
			int extra = _foodProduced - _foodNeeded;
			if(extra > 0)
			{
				return extra;
			}
			else 
			{
				_extraRisk -= extra;
				return 0;
			}
		}
	}

	private int RiskProduced
	{
		get{
			return _riskPerTurn + _extraRisk;
		}
	}

	void FoodCount()
	{
		_foodNeeded = _foodProduced = 0;		//reset food
		List<HexUnit> units = _grid.GetUnits ();				//get units in game
		for (int i = 0 ; i < units.Count ; i++ )				//calculate food produced and needed
		{
			HexUnit unit = units [i];
			_foodNeeded += unit.Hunger;
			_foodProduced += unit.Production;
		}
	}

	void UpdateUI()
	{
		//Update food
		_food.text = "+" + _foodProduced + "/" + _foodNeeded;

		//Update research
		_research.text = _researchStatus + "/" + _techPointCost + " (+" + ResearchProduced +")";

		//update tech points
		_currentTechStatus.fillAmount = (_researchStatus / _techPointCost);
		_nextTechStatus.fillAmount = (_researchStatus + ResearchProduced) / _techPointCost;
		_techPoints.text = _techPointsCount.ToString ();

		//update risk
		_risk.text = _riskStatus + _extraRisk + "% (+" + RiskProduced + "%)";
		_penalty.text = "(" + _extraRisk + "% penalty)";
	}

	public void DoubleFood (int extraFood)
	{
		_foodProduced += extraFood;
		UpdateUI ();
	}

	public void EndTurn()
	{
		//update tech points
		_researchStatus += ResearchProduced;
		if (_researchStatus >= _techPointCost)
		{
			_researchStatus = _researchStatus - _techPointCost;
			_techPointsCount += 1;
		}

		//update risk
		_riskStatus += RiskProduced;
			
		SetResource ();

		//CALL RISK MANAGER PASSING RISK STATUS
	}

	public void SetResource()
	{
		FoodCount ();

		UpdateUI ();
	}

	public void Save (BinaryWriter writer)
	{
		writer.Write ((byte)_researchStatus);
		writer.Write ((byte)_techPointsCount);
		writer.Write ((byte)_riskStatus);
	}

	public void Load (BinaryReader reader, int header)
	{
		if(header >= 6)
		{
			_researchStatus = reader.ReadByte ();
			_techPointsCount = reader.ReadByte ();
		}
		if(header >= 9)
		{
			_riskStatus = reader.ReadByte ();
		}

		SetResource ();
	}
}
