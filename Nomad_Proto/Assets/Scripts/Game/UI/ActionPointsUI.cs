using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ActionPointsUI : MonoBehaviour 
{
	[SerializeField] private Text _pointsLeft;
	[SerializeField] private Text _totalPoints;

	public void SetPointsLeft(int amount)
	{
		_pointsLeft.text = amount.ToString ();
	}

	public void SetTotalPoints(int amount)
	{
		_totalPoints.text ="/ " + amount.ToString ();
	}
}
