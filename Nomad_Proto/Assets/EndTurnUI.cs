using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EndTurnUI : MonoBehaviour
{
	[SerializeField] private Text _pointsWarningText;
	[SerializeField] private Text _unitsWarningText;
	private string _unitsWarning = " of your units will be forgotten !";

	public void DisplayWarning(int points, int units)
	{
		this.gameObject.SetActive (true);

		if(points > 0)
		{
			_pointsWarningText.gameObject.SetActive (true);
			_pointsWarningText.text = "You have " + points + " points left...";
		}
		else
			_pointsWarningText.gameObject.SetActive (false);

		if(units > 0)
		{
			_unitsWarningText.gameObject.SetActive (true);
			string unitsLost = "";
			switch (units)
			{
			case 1:
				unitsLost = "One";
				break;
			case 2:
				unitsLost = "Two";
				break;
			case 3:
				unitsLost = "Three";
				break;
			case 4:
				unitsLost = "Four";
				break;
			case 5:
				unitsLost = "Five";
				break;
			}
			_unitsWarningText.text = unitsLost + _unitsWarning;
		}
		else
			_unitsWarningText.gameObject.SetActive (false);
	}
}
