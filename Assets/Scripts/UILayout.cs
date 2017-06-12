using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tetris
{
	public class UILayout : MonoBehaviour
	{
		public Camera camera;
		public RectTransform panel;
		// Use this for initialization
		void Start ()
		{
			float height = Screen.height;
			float width = Screen.width ;
			float cameraWidth = (height / 2) / width;
			var rect = new Rect (0.5f - cameraWidth, 0, cameraWidth, 1);
			camera.rect = rect;

		}
	
		// Update is called once per frame
		void Update ()
		{
		
		}
	}
}
