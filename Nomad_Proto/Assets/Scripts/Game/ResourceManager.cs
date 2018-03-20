using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class ResourceManager : MonoBehaviour
{
	[SerializeField] private HexGrid _grid;
	[SerializeField] private float _techPointCost = 10;

	[Header("UI")]
	[SerializeField] private Text _food;
	[SerializeField] private Text _research;
	[SerializeField] private Text _techPoints;
	[SerializeField] private Image _currentTechStatus;
	[SerializeField] private Image _nextTechStatus;

	private int _foodProduced, _foodNeeded;
	private float _researchStatus = 0;
	private int _techPointsCount;
	private int ResearchProduced
	{
		get{
			int extra = _foodProduced - _foodNeeded;
			return extra > 0 ? extra : 0;
		}
	}

	private float NextResearchCount
	{
		get{
			return _researchStatus + ResearchProduced;
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
		_nextTechStatus.fillAmount = (NextResearchCount / _techPointCost);

		_techPoints.text = _techPointsCount.ToString ();
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

		//reset food
		SetResource ();
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
	}

	public void Load (BinaryReader reader, int header)
	{
		if(header >= 6)
		{
			_researchStatus = reader.ReadByte ();
			_techPointsCount = reader.ReadByte ();

			SetResource ();
		}
	}
}
