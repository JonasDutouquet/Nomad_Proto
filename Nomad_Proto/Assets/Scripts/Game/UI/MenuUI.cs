using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuUI : MonoBehaviour 
{
	[SerializeField] private GameManager _gameManager;
	[SerializeField] private GameObject[] _menuButtons;

	void OnEnable()
	{
		bool playing = _gameManager.IsPlaying;
		foreach (GameObject button in _menuButtons)
			button.SetActive (playing);
	}
}
