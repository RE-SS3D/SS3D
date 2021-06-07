﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace SS3D.Engine.Inventory.UI
{
	/// <summary>
	/// Allows the Internal Clothing user interface to be collapsible by clicking a button.
	/// </summary>
	public class ClickExpandInternalClothing : MonoBehaviour
	{
		private Button ExpandButton;
		private CanvasGroup InternalClothingCanvas;
		private bool isShowing = true;

		void Start()
		{
			InternalClothingCanvas = GameObject.Find("InternalClothing").GetComponent<CanvasGroup>();
			ExpandButton = GetComponent<Button>();
			ExpandButton.onClick.AddListener(TaskOnClick);
		}
		void TaskOnClick()
		{
			if (isShowing)
			{
				InternalClothingCanvas.alpha = 0f; //this makes everything transparent
				InternalClothingCanvas.blocksRaycasts = false; //this prevents the UI element to receive input events
			}
			else
			{
				InternalClothingCanvas.alpha = 1f; //this makes it visible again
				InternalClothingCanvas.blocksRaycasts = true; //this allows the UI to receive inputs again.
			}
			isShowing = !isShowing;
		}
	}
}