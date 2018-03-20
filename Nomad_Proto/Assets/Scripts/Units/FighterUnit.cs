using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FighterUnit : MonoBehaviour
{
	[SerializeField] private int _arrowRange;

	public int ArrowRange 
	{ 
		get
		{
			return _arrowRange;
		}
		set
		{
			_arrowRange = value;
		}
	}
}
