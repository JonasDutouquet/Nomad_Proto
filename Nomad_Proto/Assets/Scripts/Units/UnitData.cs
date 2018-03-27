using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitData : ScriptableObject
{
	public GameObject display;
	public UnitAction[] actions;
	[Header("Shared variables")]
	public int hunger;
	public int speed;
	public int visionRange;
	[Header("Fighter")]
	public int arrowRange;
	[Header("Producer")]
	public int productionBase;
	public int productionBonus;
	[Header("Highlight Color")]
	public Color selectedColor;
}

public enum UnitTypes
{
	Relic, Fighter, Producer
}