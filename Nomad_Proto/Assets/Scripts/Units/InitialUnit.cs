using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class InitialUnit : ScriptableObject 
{
	public UnitTypes type;
	public int X;
	public int Z;

	public HexCoordinates GetCoordinates()
	{
		HexCoordinates c = new HexCoordinates (X, Z);
		return c;
	}
}
