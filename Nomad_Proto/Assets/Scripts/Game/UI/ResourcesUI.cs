using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ResourcesUI : MonoBehaviour
{
	[SerializeField] private Text _food;
	[SerializeField] private Text _research;
	[SerializeField] private Text _techPoints;
	public int TechPointCost{ get; set;}

	public void UpdateFood(int produced, int needed)
	{
		_food.text = "+" + produced + "/" + needed;
	}

	public int UpdateResearch(int produced, int current)
	{
		if (produced < 0)
			produced = 0;

		if (current + produced > TechPointCost)
		{
			current = current + produced - TechPointCost;
		}

		_research.text = current + "/" + TechPointCost + " (+" + produced +")";

		return current;
	}
}
