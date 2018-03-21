using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class MemoryPillar : MonoBehaviour
{
	public static MemoryPillar pillarPrefab;

	private int _range;
	public int VisionRange
	{ get
		{ return _range;

		}
		set{ _range = value;
		}
	}

	public HexGrid Grid{ get; set; }

	private HexCell location;
	public HexCell Location {
		get {
			return location;
		}
		set {
			if (location) {
				Grid.DecreaseVisibility(location, VisionRange);
				location.Pillar = null;
			}
			location = value;
			value.Pillar = this;
//			Grid.IncreaseVisibility(value, VisionRange);
			transform.localPosition = value.Position;
			transform.SetParent (value.transform);
		}
	}

	private bool _isRevealed = false;

	public void Reveal()
	{
		Grid.IncreaseVisibility(location, VisionRange);
		_isRevealed = true;
	}

	public void Die () {
		if (location) {
			Grid.DecreaseVisibility(location, VisionRange);
		}
		location.Pillar = null;
		if(!location.Unit) location.DisableHighlight ();
		Destroy(gameObject);
	}

	public void Save (BinaryWriter writer) {
		location.coordinates.Save (writer);
		writer.Write ((byte)_range);
		writer.Write (_isRevealed);
	}

	public static void Load (BinaryReader reader, HexGrid grid)
	{
		HexCoordinates coordinates = HexCoordinates.Load(reader);
		MemoryPillar pillar = Instantiate (pillarPrefab);
		grid.AddPillar (Instantiate(pillar), grid.GetCell (coordinates), reader.ReadByte ());
		if (reader.ReadBoolean ())
			pillar.Reveal ();
	}
}
