using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Tetris
{
	public class TetrisRenderer:MonoBehaviour
	{
		Rect oneRect;
		Rect bgRect;
		float dx, dy;
		public Texture blockTexture;
		public Texture bgTexture;
		public Material material;
		int width, height;
		TetrisGame game;

		void Start ()
		{
			game = GetComponent<TetrisGame> ();
			width = game.width;
			height = game.height;
			float ox = 0, oy = 0;
			float hr = (float)Screen.height / height, wr = (float)Screen.width / width;
			dx = dy = Mathf.Min (hr, wr);
			var rect = new Rect (0, 0, 1, 1);
			var bgRect = new Rect (0, 0, width, height);
			bgTexture.wrapMode = TextureWrapMode.Repeat;
		}

		void OnGUI ()
		{
			if (Event.current.type.Equals (EventType.Repaint)) {
				
				Graphics.DrawTexture (new Rect (0, 0, width * dx, height * dy), bgTexture, bgRect, 0, 0, 0, 0, Color.black);
				for (var x = 0; x < width; ++x) {
					for (var y = 0; y < height; ++y) {
						if (!game.IsFree (x, y)) {
							GUI.DrawTexture (new Rect (x * dx, (height - y) * dy, dx, dy), blockTexture);//, oneRect, 0, 0, 0, 0, game.field [x, y].color);
						}
					}
				}

				for (var i = 0; i < game.falling.points.Length; ++i) {
					var p = game.falling.points [i];
					var x = game.piecePos.x + p.x;
					var y = game.piecePos.y + p.y;
					GUI.DrawTexture (new Rect (x * dx, (height - y) * dy, dx, dy), blockTexture);//, oneRect, 0, 0, 0, 0, game.falling.color);
				}
			}

			if (game.paused && !game.lost) {
				GUI.Label (new Rect (100, 100, 100, 100), "Paused");
			}
			if (game.lost) {
				GUI.Box (new Rect (100, 100, 100, 120), bgTexture);
				GUI.Label (new Rect (100, 100, 100, 30), "Game Over");

				GUI.Label (new Rect (100, 130, 100, 30), "" + game.score);
				GUI.Label (new Rect (100, 160, 100, 30), "" + game.TopScore);

				if (GUI.Button (new Rect (100, 200, 100, 30), "Restart")) {
					game.Restart ();
				}
			}
		}
	}
}