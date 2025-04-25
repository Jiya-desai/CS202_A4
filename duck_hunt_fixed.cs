using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

Exception? exception = null;

const char NULL_CHAR = '\0';
const char EMPTY_CHAR = '-';
const int BARREL_LENGTH = 10;

// Menu Settings
int gameDelay;
double gunXStretch;
int crosshairSpeed;
bool gunEnabled;
bool bulletsEnabled;
bool gunOutlineEnabled;

bool inMenu;
bool fireGun;
bool gunSelected;
bool crosshairSelected;
bool gameOver;
int frame;
int score;
int ammoCount;
int spawnDelay;
int grassLevel;
char[,] screenBuffer;
Random rng;
List<Bird> birds;
List<Bullet> bullets;
StringBuilder screenGraphic;
Stopwatch timer;
Point crosshair;
Point LeftAncor;
Point middleAncor;
Point RightAncor;

Stopwatch frameRateTimer;

try
{
	Initialize();
	while (true)
	{
		if (inMenu)
		{
			Console.Clear();
			Console.CursorVisible = true;
			Console.WriteLine("Duck Hunt");
			Console.WriteLine();
			Console.WriteLine("Settings:");
			Console.WriteLine();
			Console.WriteLine($"1. Game Delay: {gameDelay}ms");
			Console.WriteLine($"2. Gun X Stretch: {gunXStretch}");
			Console.WriteLine($"3. Crosshair Speed: {crosshairSpeed}");
			Console.WriteLine($"4. Gun Enabled: {gunEnabled}");
			Console.WriteLine($"5. Bullets Enabled: {bulletsEnabled}");
			Console.WriteLine($"6. Gun Outline Enabled: {gunOutlineEnabled}");
			Console.WriteLine();
			Console.WriteLine("7. Start Game");
			Console.WriteLine("8. Exit Game");
			Console.WriteLine();
			Console.Write("Select an option: ");
			string input = Console.ReadLine() ?? "";
			if (int.TryParse(input, out int option))
			{
				switch (option)
				{
					case 1:
						Console.Write("Enter Game Delay (ms): ");
						if (int.TryParse(Console.ReadLine(), out int delay))
						{
							gameDelay = delay;
						}
						break;
					case 2:
						Console.Write("Enter Gun X Stretch: ");
						if (double.TryParse(Console.ReadLine(), out double stretch))
						{
							gunXStretch = stretch;
						}
						break;
					case 3:
						Console.Write("Enter Crosshair Speed: ");
						if (int.TryParse(Console.ReadLine(), out int speed))
						{
							crosshairSpeed = speed;
						}
						break;
					case 4:
						gunEnabled = !gunEnabled;
						break;
					case 5:
						bulletsEnabled = !bulletsEnabled;
						break;
					case 6:
						gunOutlineEnabled = !gunOutlineEnabled;
						break;
					case 7:
						inMenu = false;
						break;
					case 8:
						Console.Clear();
						Console.WriteLine("Duck Hunt was closed.");
						return;
				}
			}
		}
		else
		{
			Console.Clear();
			Console.CursorVisible = false;
			inMenu = false;
			fireGun = false;
			gunSelected = true;
			crosshairSelected = false;
			gameOver = false;
			frame = 0;
			score = 0;
			ammoCount = 5;
			spawnDelay = 100;
			grassLevel = Sprites.ScreenHeight - 4;
			screenBuffer = new char[Sprites.ScreenWidth, Sprites.ScreenHeight];
			rng = new();
			birds = new();
			bullets = new();
			screenGraphic = new();
			timer = new();
			crosshair = new(Sprites.ScreenWidth / 2, Sprites.ScreenHeight / 3);
			LeftAncor = new(Sprites.ScreenWidth / 2 - 3, Sprites.ScreenHeight - 2);
			middleAncor = new(Sprites.ScreenWidth / 2, Sprites.ScreenHeight - 2);
			RightAncor = new(Sprites.ScreenWidth / 2 + 3, Sprites.ScreenHeight - 2);
			frameRateTimer = new();
			frameRateTimer.Restart();
			timer.Restart();

			while (!gameOver)
			{
				if (Console.WindowWidth != Sprites.ScreenWidth || Console.WindowHeight != Sprites.ScreenHeight)
				{
					Console.Clear();
					Console.WriteLine("Console was resized. Duck Hunt was closed.");
					return;
				}

				for (int y = 0; y < Sprites.ScreenHeight; y++)
				{
					for (int x = 0; x < Sprites.ScreenWidth; x++)
					{
						screenBuffer[x, y] = ' ';
					}
				}

				for (int y = 0; y < Sprites.ScreenHeight; y++)
				{
					for (int x = 0; x < Sprites.ScreenWidth; x++)
					{
						if (y == grassLevel)
						{
							screenBuffer[x, y] = '═';
						}
						else if (y > grassLevel)
						{
							screenBuffer[x, y] = '█';
						}
					}
				}

				while (Console.KeyAvailable)
				{
					switch (Console.ReadKey(true).Key)
					{
						case ConsoleKey.UpArrow: crosshair.Y -= crosshairSpeed; break;
						case ConsoleKey.DownArrow: crosshair.Y += crosshairSpeed; break;
						case ConsoleKey.LeftArrow: crosshair.X -= crosshairSpeed; break;
						case ConsoleKey.RightArrow: crosshair.X += crosshairSpeed; break;
						case ConsoleKey.Spacebar: fireGun = true; break;
						case ConsoleKey.Enter: inMenu = true; break;
						case ConsoleKey.Escape:
							Console.Clear();
							Console.WriteLine("Duck Hunt was closed.");
							return;
					}
				}

				crosshair.X = Math.Min(Sprites.ScreenWidth - Sprites.Enviroment.CrosshairWidth + 2, Math.Max(crosshair.X, 2));
				crosshair.Y = Math.Min(Sprites.ScreenHeight - Sprites.Enviroment.CrosshairHeight, Math.Max(crosshair.Y, 2));

				double theta = Math.Atan2(crosshair.Y - middleAncor.Y, crosshair.X - middleAncor.X);
				Point gunTopOffset = new(0, -1);

				if (gunEnabled)
				{
					int gunLength = (int)(BARREL_LENGTH * gunXStretch);
					int gunX = (int)(gunLength * Math.Cos(theta));
					int gunY = (int)(gunLength * Math.Sin(theta));
					Point gunEnd = new(gunX, gunY);

					if (gunOutlineEnabled)
					{
						for (int i = 0; i < gunLength; i++)
						{
							int x = (int)(i * Math.Cos(theta));
							int y = (int)(i * Math.Sin(theta));
							DrawToScreen(middleAncor.X + x, middleAncor.Y + y, '|');
						}
					}
				}

				DrawToScreen(screenBuffer);
				DrawGUI();

				if (bulletsEnabled)
				{
					if (fireGun && ammoCount > 0)
					{
						bullets.Add(new Bullet(middleAncor + gunTopOffset, theta));
						ammoCount--;
					}

					for (int i = 0; i < bullets.Count; i++)
					{
						bullets[i].UpdatePosition();

						if (bullets[i].OutOfBounds)
						{
							bullets.RemoveAt(i);
							i--; // Adjust index after removal
							continue;
						}

						// Check for valid indices before accessing
						if (i < bullets.Count)
						{
							foreach (Bird bird in birds)
							{
								if (!bird.IsDead &&
									(bird.Contains((int)bullets[i].X[0], (int)bullets[i].Y[0]) ||
									bird.Contains((int)bullets[i].X[1], (int)bullets[i].Y[1])))
								{
									bird.IsDead = true;
									ammoCount += 2;
									score += 350;
								}
							}

							DrawToScreenWithColour((int)bullets[i].X[0], (int)bullets[i].Y[0], ConsoleColor.DarkGray, '█');
							DrawToScreenWithColour((int)bullets[i].X[1], (int)bullets[i].Y[1], ConsoleColor.DarkGray, '█');
						}
					}
				}
				else
				{
					if (fireGun && ammoCount > 0)
					{
						foreach (Bird bird in birds)
						{
							if (!bird.IsDead && bird.Contains(crosshair.X, crosshair.Y))
							{
								bird.IsDead = true;
								ammoCount += 2;
								score += 150;
							}
						}
						ammoCount--;
					}
				}

				fireGun = false;

				foreach (Bird bird in birds)
				{
					DrawToScreenWithColour(bird.X, bird.Y, ConsoleColor.Red, bird.Direction is -1 ? Sprites.Bird.LeftSprites[bird.Frame] : Sprites.Bird.RightSprites[bird.Frame]);
					if (frame % 2 is 0)
					{
						bird.IncrementFrame();
						if (bird.IsDead)
						{
							bird.Y++;
						}
						else
						{
							bird.X += bird.Direction;
						}
					}
				}

				for (int i = birds.Count - 1; i >= 0; i--)
				{
					if (birds[i].Y > Sprites.ScreenHeight ||
						(birds[i].Direction is -1 && birds[i].X < -Sprites.Bird.Width) ||
						(birds[i].Direction is 1 && birds[i].X > Sprites.ScreenWidth + Sprites.Bird.Width))
					{
						birds.RemoveAt(i);
					}
				}

				if (frame % spawnDelay is 0)
				{
					if (rng.Next(50) > 25)
					{
						birds.Add(new Bird(Sprites.ScreenWidth, rng.Next(1, grassLevel - Sprites.Bird.Height), -1));
					}
					else
					{
						birds.Add(new Bird(-Sprites.Bird.Width, rng.Next(1, grassLevel - Sprites.Bird.Height), 1));
					}
					if (spawnDelay > 60)
					{
						spawnDelay--;
					}
				}

				if (ammoCount > 5)
				{
					ammoCount = 5;
				}

				DrawToScreenWithColour(crosshair.X - Sprites.Enviroment.CrosshairHeight / 2, crosshair.Y - Sprites.Enviroment.CrosshairWidth / 2, fireGun ? ConsoleColor.DarkYellow : ConsoleColor.Blue, Sprites.Enviroment.Crosshair);
				Thread.Sleep(TimeSpan.FromMilliseconds(gameDelay));
				frame++;

				gameOver = ammoCount is 0 && bullets.Count is 0;
			}

			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.SetCursorPosition(1, 1);
			Console.WriteLine("Game Over!     ");
			Console.SetCursorPosition(1, 2);
			Console.WriteLine($"Score: {score}");
			Console.SetCursorPosition(1, 3);
			Console.WriteLine("Press [ESC] to quit");

			while (Console.ReadKey(true).Key != ConsoleKey.Escape)
			{
				continue;
			}
		}
	}
}
catch (Exception e)
{
	exception = e;
	throw;
}
finally
{
	Console.CursorVisible = true;
	Console.ResetColor();
	Console.Clear();
	Console.WriteLine(exception?.ToString() ?? "Duck Hunt was closed.");
}

void Initialize()
{
	gameDelay = 20;
	gunXStretch = 0.5;
	crosshairSpeed = 1;
	gunEnabled = true;
	bulletsEnabled = true;
	gunOutlineEnabled = true;
	inMenu = true;
	Sprites.Initialize();
}

void DrawToScreen(char[,] buffer)
{
	Console.SetCursorPosition(0, 0);
	screenGraphic.Clear();
	for (int y = 0; y < Sprites.ScreenHeight; y++)
	{
		for (int x = 0; x < Sprites.ScreenWidth; x++)
		{
			screenGraphic.Append(buffer[x, y]);
		}
		screenGraphic.Append('\n');
	}
	Console.Write(screenGraphic.ToString());
}

void DrawToScreen(int x, int y, char c)
{
	if (x >= 0 && x < Sprites.ScreenWidth && y >= 0 && y < Sprites.ScreenHeight)
	{
		screenBuffer[x, y] = c;
	}
}

void DrawToScreenWithColour(int x, int y, ConsoleColor color, char c)
{
	if (x >= 0 && x < Sprites.ScreenWidth && y >= 0 && y < Sprites.ScreenHeight)
	{
		Console.SetCursorPosition(x, y);
		Console.ForegroundColor = color;
		Console.Write(c);
		Console.ForegroundColor = ConsoleColor.White;
	}
}

void DrawToScreenWithColour(int x, int y, ConsoleColor color, char[] sprite)
{
	int width = 0;
	int height = 0;
	int newLineCount = 0;

	for (int i = 0; i < sprite.Length; i++)
	{
		if (sprite[i] == '\n')
		{
			newLineCount++;
			width = Math.Max(width, i - newLineCount);
		}
	}
	height = newLineCount + 1;

	Console.ForegroundColor = color;
	for (int i = 0, dx = 0, dy = 0; i < sprite.Length; i++)
	{
		if (sprite[i] == '\n')
		{
			dx = 0;
			dy++;
		}
		else
		{
			int drawX = x + dx;
			int drawY = y + dy;
			if (drawX >= 0 && drawX < Sprites.ScreenWidth && drawY >= 0 && drawY < Sprites.ScreenHeight)
			{
				Console.SetCursorPosition(drawX, drawY);
				Console.Write(sprite[i]);
			}
			dx++;
		}
	}
	Console.ForegroundColor = ConsoleColor.White;
}

void DrawGUI()
{
	Console.ForegroundColor = ConsoleColor.Yellow;
	Console.SetCursorPosition(1, 1);
	Console.Write($"Score: {score}");
	Console.SetCursorPosition(1, 2);
	Console.Write($"Ammo: {ammoCount}");
	Console.ForegroundColor = ConsoleColor.White;
}

struct Point
{
	public int X;
	public int Y;

	public Point(int x, int y)
	{
		X = x;
		Y = y;
	}

	public static Point operator +(Point a, Point b) => new(a.X + b.X, a.Y + b.Y);
}

class Bird
{
	public int X;
	public int Y;
	public int Frame = 0;
	public int Direction = 0;
	public bool IsDead = false;
	public Bird(int x, int y, int direction)
	{
		X = x;
		Y = y;
		Direction = direction;
	}
	public void IncrementFrame()
	{
		if (IsDead)
		{
			Frame = 4;
		}
		else
		{
			Frame++;
			Frame %= 4;
		}
	}
	public bool Contains(int x, int y)
	{
		return
			(x >= X) &&
			(y >= Y) &&
			(y < Y + Sprites.Bird.Height) &&
			(x < X + Sprites.Bird.Width);
	}
}

class Bullet
{
	public bool OutOfBounds = false;
	public double[] X = new double[2];
	public double[] Y = new double[2];

	private readonly double XOffset;
	private readonly double YOffset;
	public Bullet(Point position, double angle)
	{
		for (int i = 0; i < 2; i++)
		{
			X[i] = position.X;
			Y[i] = position.Y;
		}

		XOffset = -Math.Cos(angle);
		YOffset = -Math.Sin(angle);
	}
	public void UpdatePosition()
	{
		X[1] = X[0];
		Y[1] = Y[0];

		X[0] += XOffset;
		Y[0] += YOffset;

		if (X[0] < 0 || X[0] >= Console.WindowWidth ||
			Y[0] < 0 || Y[0] >= Console.WindowHeight)
		{
			OutOfBounds = true;
		}
	}
}

static class Sprites
{
	public const int ScreenWidth = 80;
	public const int ScreenHeight = 25;
	private const char NEWLINE_CHAR = '\n';

	public static void Initialize()
	{
		Bird.Width = 10;
		Bird.Height = 3;
	}

	public static class Enviroment
	{
		public static char[] Crosshair = (
			@"┌─┐" + NEWLINE_CHAR +
			@"│+│" + NEWLINE_CHAR +
			@"└─┘").ToCharArray();
		public static int CrosshairWidth = 3;
		public static int CrosshairHeight = 3;

		public static char[] Tree = (
			@"       ####         " + NEWLINE_CHAR +
			@"      ######        " + NEWLINE_CHAR +
			@"     ########       " + NEWLINE_CHAR +
			@"    ##########      " + NEWLINE_CHAR +
			@"   ############     " + NEWLINE_CHAR +
			@"  ##############    " + NEWLINE_CHAR +
			@" ################   " + NEWLINE_CHAR +
			@"##################  " + NEWLINE_CHAR +
			@"       ||||         " + NEWLINE_CHAR +
			@"       ||||         " + NEWLINE_CHAR +
			@"       ||||         " + NEWLINE_CHAR +
			@"       ||||         " + NEWLINE_CHAR +
			@"       ||||         " + NEWLINE_CHAR +
			@"       ||||         " + NEWLINE_CHAR +
			@"       ||||         " + NEWLINE_CHAR +
			@"       ||||         " + NEWLINE_CHAR +
			@"       ||||         " + NEWLINE_CHAR +
			@"       ||||         " + NEWLINE_CHAR +
			@"       ||||         " + NEWLINE_CHAR +
			@"       ||||         ").ToCharArray();
		public static int TreeHeight = 20;
		public static int TreeWidth = 20;
	}

	public static class Bird
	{
		public static char[][] LeftSprites =
		[ ( @"  _(nn)_  " + NEWLINE_CHAR +
			@"<(o----_)=" + NEWLINE_CHAR +
			@"   (UU)   ").ToCharArray(),

		  ( @"  ______  " + NEWLINE_CHAR +
			@"<(o(UU)_)=" + NEWLINE_CHAR +
			@"          ").ToCharArray(),

		  ( @"  _(nn)_  " + NEWLINE_CHAR +
			@"<(o----_)=" + NEWLINE_CHAR +
			@"   (UU)   ").ToCharArray(),

		  ( @"  ______  " + NEWLINE_CHAR +
			@"<(o(UU)_)=" + NEWLINE_CHAR +
			@"          ").ToCharArray(),

		  ( @"    _    " + NEWLINE_CHAR +
			@" _<(x)__ " + NEWLINE_CHAR +
			@"(--(-)--)" + NEWLINE_CHAR +
			@"(__(_)__)" + NEWLINE_CHAR +
			@"  _/ \_  " ).ToCharArray()
		];

		public static char[][] RightSprites =
		[ ( @"  _(nn)_  " + NEWLINE_CHAR +
			@"=(o----_)>" + NEWLINE_CHAR +
			@"   (UU)   ").ToCharArray(),

		  ( @"  ______  " + NEWLINE_CHAR +
			@"=(_UU)o)>" + NEWLINE_CHAR +
			@"          ").ToCharArray(),

		  ( @"  _(nn)_  " + NEWLINE_CHAR +
			@"=(o----_)>" + NEWLINE_CHAR +
			@"   (UU)   ").ToCharArray(),

		  ( @"  ______  " + NEWLINE_CHAR +
			@"=(_UU)o)>" + NEWLINE_CHAR +
			@"          ").ToCharArray(),

		  ( @"    _    " + NEWLINE_CHAR +
			@" __<(x)_ " + NEWLINE_CHAR +
			@"(--(-)--)" + NEWLINE_CHAR +
			@"(__(_)__)" + NEWLINE_CHAR +
			@"  _/ \_  " ).ToCharArray()
		];

		public static int Width;
		public static int Height;
	}
}
