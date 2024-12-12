using System;
using WMPLib;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Numerics;

Console.OutputEncoding = Encoding.UTF8;
Exception? exception = null;
int speedInput;
string prompt = $"Select speed [1], [2] (default), or [3]: ";
string? input;
Console.Write(prompt);
while (!int.TryParse(input = Console.ReadLine(), out speedInput) || speedInput < 1 || 3 < speedInput)
{
    if (string.IsNullOrWhiteSpace(input))
    {
        speedInput = 2;
        break;
    }
    else
    {
        Console.WriteLine("Invalid Input. Try Again...");
        Console.Write(prompt);
    }
}

int[] velocities = { 100, 70, 50 }; 
int velocity = velocities[speedInput - 1];
char[] DirectionChars = { '■', '■', '■', '■' };
char[] listOfFoodChars = { '◉', '◆', '●', '◈' };
TimeSpan sleep = TimeSpan.FromMilliseconds(velocity);
int width = Console.WindowWidth;
int height = Console.WindowHeight;

int headerHeight = 1;
int footerHeight = 1;
int sideWidth = 1;

Tile[,] map = new Tile[width, height];
Direction? direction = null;
Queue<(int X, int Y)> snake = new();
(int X, int Y) = (2, 1);
bool closeRequested = false;
Random random = new Random();

(int specialX, int specialY) = (-1, -1); //special food variable
DateTime specialFoodSpawnTime = DateTime.MinValue;
TimeSpan specialFoodLifetime = TimeSpan.FromSeconds(7);
bool specialFoodActive = false;
int normalFoodCounter = 0; //count the number of times normal food is eaten
bool specialFoodBlinking = false; //to control the blinking effect

try
{
    Console.CursorVisible = false;
    Console.Clear();
    DrawConsole(0, velocity);
    snake.Enqueue((X, Y));
    map[X, Y] = Tile.Snake;
    PositionFood();
    Console.SetCursorPosition(X, Y);
    Console.ForegroundColor = ConsoleColor.Green;
    Console.Write("▶︎");
    Console.ResetColor();

    //add blinking feature while waiting for the player to press a move key
    WaitForInputAndBlink();

    while (!closeRequested)
    {
        if (Console.WindowWidth != width || Console.WindowHeight != height)
        {
            Console.Clear();
            Console.Write("Console was resized. Snake game has ended.");
            return;
        }

        //handle snake movement
        switch (direction)
        {
            case Direction.Up: Y--; break;
            case Direction.Down: Y++; break;
            case Direction.Left: X--; break;
            case Direction.Right: X++; break;
        }
        if (Y < headerHeight || Y >= (height - footerHeight - 1) || X <= sideWidth || X >= (width - sideWidth) ||
            map[X, Y] is Tile.Snake)
        {
            //add sound when the snake dies
            WindowsMediaPlayer crashSound = new WindowsMediaPlayer();
            crashSound.URL = @"C:\GiaoDienChoiGame\GiaoDienChoiGame\snakeHittingSound.wav";
            crashSound.controls.play();

            Console.Clear();
            Console.Write("Game Over. Score: " + (snake.Count - 1) + ".");
            return;
        }


        Console.SetCursorPosition(X, Y);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write(DirectionChars[(int)direction!]);
        Console.ResetColor();
        snake.Enqueue((X, Y));

        if (map[X, Y] is Tile.Food)
        {
            //handle eating normal food
            WindowsMediaPlayer eatSound = new WindowsMediaPlayer();
            eatSound.URL = @"C:\GiaoDienChoiGame\GiaoDienChoiGame\snakeEatingSound.wav";
            eatSound.controls.play();
            PositionFood();

            normalFoodCounter++;

            //spawn special food after eating 5 normal foods
            if (normalFoodCounter % 5 == 0)
            {
                PositionSpecialFood();
            }
        }
        else if (specialFoodActive && X == specialX && Y == specialY)
        {
            //handle eating special food
            specialFoodActive = false;

            map[specialX, specialY] = Tile.Open;
            Console.SetCursorPosition(specialX, specialY);
            Console.Write(' ');
            snake.Enqueue((X, Y)); //the snake grows by 2 more units
            snake.Enqueue((X, Y));

            WindowsMediaPlayer specialEatSound = new WindowsMediaPlayer();
            specialEatSound.URL = @"C:\GiaoDienChoiGame\GiaoDienChoiGame\snakeEatingSound.wav";
            specialEatSound.controls.play();
        }
        else
        {
            (int x, int y) = snake.Dequeue();
            map[x, y] = Tile.Open;
            Console.SetCursorPosition(x, y);
            Console.Write(' ');
        }

        map[X, Y] = Tile.Snake;

        //check special food's lifetime
        if (specialFoodActive)
        {
            var timeElapsed = DateTime.Now - specialFoodSpawnTime;

            if (timeElapsed > specialFoodLifetime)
            {
                specialFoodActive = false;
                Console.SetCursorPosition(specialX, specialY);
                Console.Write(' ');
            }
            else if (timeElapsed > specialFoodLifetime - TimeSpan.FromSeconds(1))
            {
                //make the special food blink during the last 1 sec
                specialFoodBlinking = !specialFoodBlinking;
                Console.SetCursorPosition(specialX, specialY);
                Console.ForegroundColor = specialFoodBlinking ? ConsoleColor.Yellow : ConsoleColor.Black;
                Console.Write('★');
                Console.ResetColor();
            }
        }


        if (Console.KeyAvailable)
        {
            GetDirection();
        }

        DrawConsole(snake.Count - 1, velocity);
        System.Threading.Thread.Sleep(sleep);
    }
}
catch (Exception e)
{
    exception = e;
    throw;
}
//finally
//{
//    Console.CursorVisible = true;
//    Console.Clear();
//    Console.WriteLine(exception?.ToString() ?? "Snake was closed.");
//}

void PositionSpecialFood()
{
    List<(int X, int Y)> possibleCoordinates = new();
    for (int i = sideWidth + 1; i <= (width - sideWidth - 1); i++)
    {
        for (int j = headerHeight + 2; j <= (height - footerHeight - 2); j++)
        {
            if (map[i, j] is Tile.Open)
            {
                possibleCoordinates.Add((i, j));
            }
        }
    }

    if (possibleCoordinates.Count > 0)
    {
        (specialX, specialY) = possibleCoordinates[random.Next(possibleCoordinates.Count)];
        specialFoodActive = true;
        specialFoodSpawnTime = DateTime.Now;

        Console.SetCursorPosition(specialX, specialY);
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write('★'); //special food's icon
        Console.ResetColor();
    }
}

void WaitForInputAndBlink()
{
    int blinkInterval = 300; //interval between blinks (ms)
    DateTime lastBlinkTime = DateTime.Now;
    bool isVisible = true;

    //draw a blinking triangle at the current position (X, Y)
    while (!direction.HasValue && !closeRequested)
    {
        DateTime currentTime = DateTime.Now;

        //check if it's time to blink
        if ((currentTime - lastBlinkTime).TotalMilliseconds >= blinkInterval)
        {
            isVisible = !isVisible;  //toggle the visibility state of the triangle
            lastBlinkTime = currentTime;

            //clear the triangle before redrawing it
            Console.SetCursorPosition(X, Y);
            Console.Write(' ');

            //if the triangle is still visible, draw it
            if (isVisible)
            {
                Console.SetCursorPosition(X, Y);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("▶︎");
                Console.ResetColor();
            }
        }

        //check if the player has pressed a movement key
        if (Console.KeyAvailable)
        {
            GetDirection();
        }

        System.Threading.Thread.Sleep(50);
    }
}

void GetDirection()
{
    //takes direction from arrow keys
    switch (Console.ReadKey(true).Key)
    {
        case ConsoleKey.UpArrow: direction = Direction.Up; break;
        case ConsoleKey.DownArrow: direction = Direction.Down; break;
        case ConsoleKey.LeftArrow: direction = Direction.Left; break;
        case ConsoleKey.RightArrow: direction = Direction.Right; break;
        case ConsoleKey.Escape: closeRequested = true; break;
    }
}

void PositionFood()
{
    Random random = new Random();

    List<(int X, int Y)> possibleCoordinates = new();
    for (int i = sideWidth + 1; i <= (width - sideWidth - 1); i++)
    {
        for (int j = headerHeight + 2; j <= (height - footerHeight - 2); j++)
        {
            if (map[i, j] is Tile.Open)
            {
                possibleCoordinates.Add((i, j));
            }
        }
    }

    if (possibleCoordinates.Count > 0)
    {
        var (X, Y) = possibleCoordinates[random.Next(possibleCoordinates.Count)];
        map[X, Y] = Tile.Food;

        char foodChar = listOfFoodChars[random.Next(listOfFoodChars.Length)];
        var colors = new List<ConsoleColor> { ConsoleColor.Red, ConsoleColor.Green, ConsoleColor.Blue, ConsoleColor.Yellow, ConsoleColor.Cyan, ConsoleColor.Magenta, ConsoleColor.DarkYellow };
        Console.SetCursorPosition(X, Y);
        Console.ForegroundColor = colors[random.Next(colors.Count)];
        Console.Write(foodChar);
        Console.ResetColor();
    }
}
void DrawConsole(int score, int velocity)
{
    string title = "[HUNTING SNAKE]🐍";
    string speed = velocity == 100 ? "Slow" : velocity == 70 ? "Normal" : "Fast";
    string headerSpeed = $"Speed: {speed}";
    string footerPause = "[Space]: Pause the game";
    string footerScore = $"Score: {score}";

    //top border
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.SetCursorPosition(1, 0);
    Console.Write('╔' + new string('═', width - 3) + '╗');

    //header
    Console.SetCursorPosition((width - title.Length) / 2, 0);
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.Write(title);

    Console.SetCursorPosition(width - headerSpeed.Length - 2, 0);
    Console.ForegroundColor = ConsoleColor.Magenta;
    Console.Write(headerSpeed);

    //bottom border
    Console.SetCursorPosition(1, height - 2);
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.Write('╚' + new string('═', width - 3) + '╝');

    //side borders
    for (int y = 1; y < height - 2; y++)
    {
        Console.SetCursorPosition(1, y);
        Console.Write('║');
        Console.SetCursorPosition(width - 1, y);
        Console.Write('║');
    }

    //footer
    Console.SetCursorPosition(2, height - 1);
    Console.ForegroundColor = ConsoleColor.Green;
    Console.Write("[ESC]: Exit");

    Console.SetCursorPosition((width - footerPause.Length) / 2, height - 1);
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.Write(footerPause);

    Console.SetCursorPosition(width - footerScore.Length - 3, height - 1);
    Console.ForegroundColor = ConsoleColor.Magenta;
    Console.Write($"{footerScore} 🌟");

    //reset color
    Console.ResetColor();
}
enum Direction
{
    Up = 0,
    Down = 1,
    Left = 2,
    Right = 3,
}

enum Tile
{
    Open = 0,
    Snake,
    Food,
}