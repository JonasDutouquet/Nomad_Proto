using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ActionPointsUI : MonoBehaviour 
{
	//[SerializeField] private Text _pointsLeft;
	//[SerializeField] private Text _totalPoints;
	[SerializeField] private GameObject _pointPrefab;
	[SerializeField] private Transform _pointsPool;
	[SerializeField] private Color _availableColor;
	[SerializeField] private Color _usedColor;
	private List<Image> _actionPoints = new List<Image> ();

	public void SetTotalPoints(int amount)
	{
		//_totalPoints.text ="/ " + amount.ToString ();
		ClearPoints ();
		for(int i = 0 ; i < amount ; i++)
		{
			Image point = Instantiate (_pointPrefab, _pointsPool).GetComponent<Image> ();
			point.color = _availableColor;
			_actionPoints.Add (point);
		}
	}

	void ClearPoints()
	{
		for (int i = 0 ; i < _actionPoints.Count ; i++)
		{
			Destroy (_actionPoints [i].gameObject);
		}
		_actionPoints.Clear ();
	}

	public void SetPointsLeft(int amount)
	{
		//_pointsLeft.text = amount.ToString ();
		for(int i = amount ; i < _actionPoints.Count ; i++)
		{
			_actionPoints [i].color = _usedColor;
		}
	}

}
