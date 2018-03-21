using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour 
{
	public bool _playing;
	[SerializeField] private string _saveToLoad;
	[SerializeField] private SaveLoadMenu _loader;
	[SerializeField] private HexGameUI _hexUI;
	[SerializeField] private HexMapEditor _mapEditor;
	[SerializeField] private GameObject _editModeUI;
	private TurnManager _turn;

	void Awake()
	{
		if(_playing) {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            _loader.Load ("/Users/jonas/Library/Application Support/DefaultCompany/Nomad_Proto/"+ _saveToLoad +".map");
#endif
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            _loader.Load("C:\\Users\\jonas.dutouquet\\AppData\\LocalLow\\DefaultCompany\\Nomad_Proto\\" +_saveToLoad + ".map");
#endif
			_loader.Load ("/Users/jonas/Library/Application Support/DefaultCompany/Nomad_Proto/"+ _saveToLoad +".map");
			_hexUI.SetEditMode (false);
			_mapEditor.SetEditMode (false);
			_editModeUI.SetActive (false);
			//_turn = GetComponent<TurnManager> ();
			//_turn.EndTurn ();
		}

	}

	void Update()
	{
		if(Input.GetKeyDown (KeyCode.Escape))
		{
			//_hexUI.SetEditMode (true);
			//_mapEditor.SetEditMode (true);
			_editModeUI.SetActive (true);
		}
	}
}
