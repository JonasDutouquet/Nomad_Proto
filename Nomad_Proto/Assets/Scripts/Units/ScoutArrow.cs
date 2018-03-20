using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class ScoutArrow : MonoBehaviour 
{
	public static ScoutArrow arrowPrefab;
	public int Range
	{ get
		{ return _range;
			
		}
		set{ _range = value;
		}
	}
	public HexGrid Grid{ get; set; }

	private int _range;
	private HexCell location;
	public HexCell Location {
		get {
			return location;
		}
		set {
			if (location) {
				Grid.DecreaseVisibility(location, Range);
				location.Arrow = null;
			}
			location = value;
			value.Arrow = this;
			Grid.IncreaseVisibility(value, Range);
			transform.localPosition = value.Position;
			transform.SetParent (value.transform);
		}
	}

	public void Die () {
		if (location) {
			Grid.DecreaseVisibility(location, Range);
		}
		location.Arrow = null;
		if(!location.Unit) location.DisableHighlight ();
		Destroy(gameObject);
	}
		
	public void Save (BinaryWriter writer) {
		location.coordinates.Save (writer);
		writer.Write ((byte)_range);
	}

	public static void Load (BinaryReader reader, HexGrid grid)
	{
		HexCoordinates coordinates = HexCoordinates.Load(reader);
		grid.AddArrow (Instantiate(arrowPrefab), grid.GetCell (coordinates), reader.ReadByte ());
	}
}
