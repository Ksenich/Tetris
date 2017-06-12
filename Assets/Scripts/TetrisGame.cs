using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;

namespace Tetris
{
	public class TetrisGame : MonoBehaviour
	{
		public bool useSpriteRenderer;

		//setup
		[Header ("Setup")]
		public float forceFallDelay = 0.1f;
		public int width = 10, height = 20;
		public Transform boxPrefab;
		public float stepDuration = 0.5f;
		public int linesPerLevel = 10;
		public Transform nextTransform;
		public Transform nextBoxPrefab;

		//view
		public Transform background;
		Transform piece;
		Transform nextPiece;
		public Text scoreText;
		public Text levelText;
		public Text linesText;
		public Text gameOverScoreText;
		public Text gameOverTopScoreText;
		public GameObject gameOverView;
		public GameObject pauseView;

		//state
		[Header ("State")]
		public Position piecePos;
		public Box[,] field = new Box[0, 0];
		public Position spawn = new Position (6, 0);
		public Figure falling;
		Figure next;
		float fallCooldown;
		Transform reference;
		public int score;
		public bool lost;
		public bool paused;
		public int level;
		public int lines;
		public int levelLines;

		public int TopScore {
			get {
				return PlayerPrefs.GetInt ("top_score");
			}
			set {
				PlayerPrefs.SetInt ("top_score", value);
			}
		}

		//Dificulty


		//input
		bool left, right, rotate, fall;
		Coroutine stepCoroutine;

		enum CheckResult
		{
			None,
			Collision,
			OutOfBounds,
			Wall,
		}

		// Use this for initialization
		void Start ()
		{
			StartGame ();
		}

		void StartGame ()
		{
			gameOverView.SetActive (false);
			lost = false;
			field = new Box[width, height];
			if (useSpriteRenderer) {
				reference = new GameObject ().GetComponent<Transform> ();
				reference.parent = this.transform;
				reference.localPosition = new Vector3 (-width / 2, -height / 2);
				if (background) {
					var bg = Instantiate (background);
					bg.transform.localScale = new Vector3 (width, height);
					bg.transform.parent = this.transform;
					bg.localPosition = new Vector3 (-width / 2, height / 2);
				}
				GetComponent<TetrisRenderer> ().enabled = false;
			} else {
				GetComponent<TetrisRenderer> ().enabled = true;
			}
			Spawn ();
			stepCoroutine = StartCoroutine (Step ());
		}

		public string inputLeft, inputRight, inputUp, inputDown;

		void CollectInput ()
		{
			if (Input.GetButtonDown (inputLeft)) {
				left = true;
			}
			if (Input.GetButtonDown (inputRight)) {
				right = true;
			}
			if (Input.GetButtonDown (inputUp)) {
				rotate = true;
			}
			if (Input.GetButton (inputDown)) {
				fall = true;
			}
		}

		// Update is called once per frame
		void Update ()
		{
			CollectInput ();
			if (!paused) {
				//Process input
				if (left && !right) {
					Move (-1, 0);
				} else if (right) {
					Move (1, 0);
				}

				if (rotate) {
					Rotate ();
				}

				if (fall && Time.time > fallCooldown) {
					Fall ();
					fallCooldown = Time.time + forceFallDelay;
				}
			}
			//clear input
			left = false;
			right = false;
			rotate = false;
			fall = false;
			//debug

		}


		void UpdateView ()
		{
			if (useSpriteRenderer) {
				piece.localPosition = new Vector3 (piecePos.x, piecePos.y);
			}
		}

		public void Spawn ()
		{
			if (useSpriteRenderer) {
				piece = new GameObject ().transform;
				piece.parent = reference;
			}
			if (next == null) {
				next = NextFigure ();
			}
			falling = new Figure (next);
			next = NextFigure ();
			piecePos.x = spawn.x;
			piecePos.y = spawn.y;
			int i = 0;
			if (useSpriteRenderer) {
				falling.views = new Transform[falling.points.Length];
				if (boxPrefab) {
					foreach (var p in falling.points) {
						var box = Instantiate (boxPrefab, piece.transform);
						box.transform.localPosition = new Vector3 (p.x, p.y);
						box.GetComponentInChildren<SpriteRenderer> ().color = falling.color;
						falling.views [i++] = box;
					}
					if (nextPiece)
						Destroy (nextPiece.gameObject);
					nextPiece = new GameObject ().transform;
					nextPiece.parent = nextTransform;
					nextPiece.localPosition = new Vector3 ();
					foreach (var p in next.points) {
						var box = Instantiate (nextBoxPrefab, nextPiece.transform);
						var rect = nextTransform.GetComponent<RectTransform> ();
						//magic constants that kinda work
						var dy = (float)rect.rect.height * rect.lossyScale.y / 4;
						box.transform.localScale = new Vector3 (dy / 10, dy / 10, 1);
						box.transform.localPosition = new Vector3 (p.x * dy, p.y * dy);
						box.GetComponentInChildren<Image> ().color = next.color;
						box.gameObject.layer = nextTransform.gameObject.layer;
					}
				}
			}
			UpdateView ();
		}

		Figure NextFigure ()
		{
			return figures [Random.Range (0, figures.Length)];
		}

		public void Pause ()
		{
			if (!paused) {
				pauseView.SetActive (true);
				paused = true;
				Time.timeScale = 0;
			} else {
				pauseView.SetActive (false);
				Time.timeScale = 1;
				paused = false;
			}
		}


		public void Quit ()
		{
			Application.Quit ();
		}

		public void Move (int dx, int dy)
		{
			var nx = piecePos.x + dx;
			var ny = piecePos.y + dy;
			var check = CheckCollision (nx, ny, falling);
			switch (check) {
			case CheckResult.None:
				piecePos.x += dx;
				piecePos.y += dy;
				UpdateView ();
				break;
			case CheckResult.Collision:
				if (dy != 0) {
					Solidify (piecePos.x, piecePos.y, falling);
					var linesCleared = 0;
					for (var i = height - 1; i >= 0; --i) {
						if (CheckLine (i)) {
							ClearLine (i);
							++linesCleared;
						}
					}
					score += linesCleared * linesCleared;
					lines += linesCleared;
					levelLines += linesCleared;
					if (levelLines > linesPerLevel) {
						level++;
						levelLines = levelLines % linesPerLevel;
					}
					scoreText.text = "" + score;
					linesText.text = "" + lines;
					levelText.text = "" + level;
					if (!lost) {
						Spawn ();
					} else {
						Lose ();
					}
				}
				break;
			case CheckResult.OutOfBounds:
				break;
			case CheckResult.Wall:
				break;
			}
		}

		void Rotate ()
		{
			var newPoints = falling.Rotate ();
			foreach (var p in newPoints) {
				var x = piecePos.x + p.x;
				var y = piecePos.y + p.y;
				if (x < 0 || x >= width || y < 0 || y >= height || !IsFree (x, y)) {
					return;
				}
			}
			falling.Assign (newPoints);
			int i = 0;
			if (useSpriteRenderer) {
				foreach (var p in falling.points) {
					if (falling.views [i])
						falling.views [i++].localPosition = new Vector3 (p.x, p.y);
				}
			}
			UpdateView ();
		}

		public void Fall ()
		{
			Move (0, -1);
		}

		void Lose ()
		{
			StopCoroutine (stepCoroutine);
			paused = true;
			var topScore = TopScore;
			if (score > topScore) {
				TopScore = score;
			}
			gameOverScoreText.text = "" + score;
			gameOverTopScoreText.text = "" + TopScore;
			gameOverView.SetActive (true);
		}

		public void Restart ()
		{
			paused = false;
			score = 0;
			lines = 0;
			level = 0;
			scoreText.text = "0";
			linesText.text = "0";
			levelText.text = "0";
			if (useSpriteRenderer) {
				Destroy (reference.gameObject);
				Destroy (piece.gameObject);
			}
			StartGame ();
		}

		bool CheckLine (int y)
		{
			for (var x = 0; x < width; ++x) {
				if (IsFree (x, y))
					return false;
			}
			return true;
		}

		void ClearLine (int y)
		{
			if (useSpriteRenderer) {
				for (var x = 0; x < width; ++x) {
					if (field [x, y].view)
						Destroy (field [x, y].view.gameObject);
				}
			}
			for (var uy = y; uy < height - 1; ++uy) {
				for (var x = 0; x < width; ++x) {
					field [x, uy] = field [x, uy + 1];
					if (useSpriteRenderer) {
						if (!IsFree (x, uy + 1)) {
							if (field [x, uy + 1].view)
								field [x, uy + 1].view.transform.Translate (0, -1, 0);
						}
					}
				}
			}
		}

		public bool IsFree (int x, int y)
		{
			return field [x, y] == null;
		}

		CheckResult CheckCollision (int x, int y, Figure fig)
		{
			foreach (var p in fig.points) {
				var nx = x + p.x;
				var ny = y + p.y;
				if (nx < 0 || nx >= width) {
					return CheckResult.Wall;
				} 
				if (ny >= height) {
					return CheckResult.OutOfBounds;
				}
				if (ny <= 0) {
					return CheckResult.Collision;
				}
				if (!IsFree (nx, ny)) {
					return CheckResult.Collision;
				}
			}
			return CheckResult.None;
		}

		void Solidify (int x, int y, Figure fig)
		{
			int i = 0;
			foreach (var p in fig.points) {
				var box = new Box ();
				box.color = fig.color;
				if (y + p.y >= height) {
					lost = true;
					return;
				}
				field [x + p.x, y + p.y] = box;
				if (useSpriteRenderer) {
					box.view = fig.views [i++];
				}
			}
		}

		IEnumerator Step ()
		{
			while (true) {
				yield return new WaitForSeconds (stepDuration);
				Fall ();
			}
		}

		//		void OnDrawGizmos ()
		//		{
		//			if (Application.isEditor) {
		//				return;
		//			}
		//			if (falling != null) {
		//				foreach (var p in falling.points) {
		//					Gizmos.color = falling.color;
		//					Gizmos.DrawCube (new Vector3 (reference.position.x + piecePos.x + p.x + 0.5f, reference.position.y + piecePos.y + p.y + 0.5f), Vector3.one);
		//				}
		//			}
		//			for (var x = 0; x < width; ++x) {
		//				for (var y = 0; y < height; ++y) {
		//					if (!IsFree (x, y)) {
		//						Gizmos.color = field [x, y].color;
		//						Gizmos.DrawCube (new Vector3 (reference.position.x + x + 0.5f, reference.position.y + y + 0.5f), Vector3.one);
		//					}
		//				}
		//			}
		//		}

		Figure[] figures = {
			new Figure (1, 0, Color.red,
				"****"
			),//I
			new Figure (1, 1, Color.cyan,
				"**",
				"**"
			),//O
			new Figure (1, 1, Color.gray,
				" * ",
				"***"
			),
			new Figure (1, 1, Color.green,
				"** ",
				" **"
			),//Z
			new Figure (1, 1, Color.blue,
				" **",
				"** "
			),//S
			new Figure (1, 1, Color.magenta,
				"*  ",
				"***"
			),//J
			new Figure (1, 1, Color.yellow,
				"  *",
				"***"
			),//L
		};
	}

	public class Figure
	{
		public Color color;
		public Position[] points;
		public Transform[] views;
		int ox, oy;

		public Figure (params int[] coords)
		{
			color = Color.red;
			points = new Position[coords.Length / 2];
			for (var i = 0; i < coords.Length; i += 2) {
				points [i / 2] = new Position (coords [i], coords [i + 1]);
			}
		}

		public Figure (Figure original)
		{
			color = original.color;
			points = new Position[original.points.Length];
			int i = 0;
			foreach (var p in  original.points) {
				points [i++] = new Position (p.x, p.y);
			}
		}

		public Figure (int ox, int oy, Color color, params string[] drawing)
		{
			this.ox = ox;
			this.oy = oy;
			this.color = color;
			List<Position> points = new List<Position> ();
			for (int y = 0; y < drawing.Length; ++y) {
				var line = drawing [y];
				for (var x = 0; x < line.Length; ++x) {
					if (line [x] == '*') {
						points.Add (new Position (x - ox, y - oy));
					}
				}
			}
			this.points = points.ToArray ();
		}

		public void Assign (Position[] newPoints)
		{
			points = newPoints;
		}

		public Position[] Rotate ()
		{
			Position[] result = new Position[points.Length];
			for (var i = 0; i < points.Length; ++i) {
				var p = points [i];
				result [i] = new Position (-p.y, p.x);
			}
			return result;
		}

		string ToString ()
		{
			return string.Join (", ", points.Select (p => "(" + p.x + "," + p.y + ")").ToArray ());
		}

	}

	[System.Serializable]
	public class Position
	{
		public int x, y;

		public Position (int x, int y)
		{
			this.x = x;
			this.y = y;
		}
	}

	public class Box
	{
		public Transform view;
		public Color color = Color.white;
	}
		

}