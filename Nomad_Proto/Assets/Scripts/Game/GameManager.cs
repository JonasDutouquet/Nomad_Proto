using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour 
{
	[SerializeField] private bool _playing;
	[SerializeField] private string _saveToLoad;
	[SerializeField] private HexGrid _grid;
	[SerializeField] private SaveLoadMenu _loader;
	[SerializeField] private HexGameUI _hexUI;
	[SerializeField] private HexMapEditor _mapEditor;
	[SerializeField] private GameObject _editModeUI;
	[SerializeField] private GameObject _launchMenuUI;
	[SerializeField] private GameObject _gameUI;
	private TurnManager _turn;

	void Awake()
	{
		if(_playing)
		{
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            _loader.Load ("/Users/jonas/Library/Application Support/DefaultCompany/Nomad_Proto/"+ _saveToLoad +".map");
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            _loader.Load("C:\\Users\\jonas.dutouquet\\AppData\\LocalLow\\DefaultCompany\\Nomad_Proto\\" +_saveToLoad + ".map");
#endif
			_hexUI.SetEditMode (false);
			_mapEditor.SetEditMode (false);
			_editModeUI.SetActive (false);
			_launchMenuUI.SetActive (false);
		}
		_gameUI.SetActive (_playing);
	}

	public bool IsPlaying
	{
		get{
			return _playing;
		}
		set{
			_playing = value;
			if(value)
			{
				_grid.SetCamera ();
				GetComponent<ResourceManager> ().SetResource ();
				_hexUI.SetEditMode (false);
				_gameUI.SetActive (true);
			}
		}
	}

	void Update()
	{
		if(Input.GetKeyDown (KeyCode.Escape))
		{
			if (Input.GetKey (KeyCode.Space))
				_editModeUI.SetActive (true);
			else {
				_launchMenuUI.SetActive (true);
			}
		}
	}
}
