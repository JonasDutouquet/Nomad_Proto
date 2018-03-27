using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InterTurnUI : MonoBehaviour 
{
	[SerializeField] private Text _info;

	void OnEnable()
	{
		//_info.text = "Moving Relic...";
		DisplayForgetting ();
	}

	public void DisplayForgetting()
	{
		_info.text = "Forgetting...";
	}

	public void DisplayEnemiesTurn()
	{
		_info.text = "Barbarians moving...";
	}
}
