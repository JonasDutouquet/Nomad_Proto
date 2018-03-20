using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class UnitAction : ScriptableObject
{
	public string displayName;
	public ActionTypes type;
	public int cost;
	public bool canRepeat = false;
	public bool immediate = false;
}

public enum ActionTypes
{
	Move, Arrow, Double, Heal, Freeze, Sacrifice, NewFighter, NewProducer, TechPoint
}
