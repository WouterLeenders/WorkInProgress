using ITGlobal.CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using static NativeMethods;

namespace WorkInProgress
{
    public enum GameState : int
    {
        Idle = 0,       
        Start = 1,
        Playing = 2,
        GameOver = 3
    }

    class Program
    {
        private static GameState gameState = GameState.Idle;

        private const string fileName = "Level1.txt";
        private static int gameSpeed = 1000;
        private static int timerValue = 0;
        private static bool updatingHeader = false;

        private static int lives = 3;
        private static int score = 0;

        private readonly static char[] characterBrushes = new char[] { '░', '▒', '▓', '█', '■', '♥', '◊', '♪', '♫' };
        private static ConsoleColor activeColor = ConsoleColor.Red;
        private static char activeBrush = characterBrushes.First();

        private static int previousMousePositionX = -1;
        private static int previousMousePositionY = -1;

        private static System.Timers.Timer timer = new System.Timers.Timer();

        private static List<ConsoleCommand> gameBuffer = new List<ConsoleCommand>();

        private static List<ConsoleCommand> commandBuffer = new List<ConsoleCommand>();

        private static void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            switch (gameState)
            {
                case GameState.Idle:
                    {
                        gameState = GameState.Start;
                        LoadScreen("Start.txt");
                        break;
                    }
                case GameState.Playing:
                    {
                        timerValue += 1;
                        var random = new Random();

                        // Spawn food items
                        var foodCommand = new ConsoleCommand(random.Next(0, 79), random.Next(1, 39), AnsiString.Create(characterBrushes[4].ToString()).Blue());
                        foodCommand.Execute();
                        gameBuffer.Add(foodCommand);

                        // Spawn lives
                        if (random.Next(10) == 0)
                        {
                            var liveCommand = new ConsoleCommand(random.Next(0, 79), random.Next(1, 39), AnsiString.Create(characterBrushes[5].ToString()).Red());
                            liveCommand.Execute();
                            gameBuffer.Add(liveCommand);
                        }

                        // Spawn enemies
                        if (random.Next(3) == 0)
                        {
                            AnsiString enemy;

                            if(random.Next(1) == 0)
                            {
                                enemy = AnsiString.Create(characterBrushes[random.Next(6, 8)].ToString()).Blue();
                            } else
                            {
                                enemy = AnsiString.Create(characterBrushes[random.Next(6, 8)].ToString()).Red();
                            }

                            var enemyCommand = new ConsoleCommand(random.Next(0, 79), random.Next(1, 39), enemy);
                            enemyCommand.Execute();
                            gameBuffer.Add(enemyCommand);
                        }

                        // Remove items
                        if (gameBuffer.Count > 0 && (timerValue > 5) && (timerValue % 2 == 0))
                        {
                            var first = gameBuffer.First();
                            first.Clear();
                            gameBuffer.Remove(first);
                        }

                        // Increase game speed
                        if (gameSpeed > 100)
                        {
                            gameSpeed -= 1;
                            timer.Interval = gameSpeed;
                        }

                        UpdateHeader();
                        break;
                    }
                case GameState.GameOver:
                    {
                        break;
                    }
                default:
                    break;
            }

  

        }

        static void Main(string[] args)
        {
            IntPtr inHandle = GetStdHandle(STD_INPUT_HANDLE);
            uint mode = 0;
            GetConsoleMode(inHandle, ref mode);
            mode &= ~ENABLE_QUICK_EDIT_MODE;
            mode |= ENABLE_WINDOW_INPUT;
            mode |= ENABLE_MOUSE_INPUT;
            SetConsoleMode(inHandle, mode);
            Console.SetWindowSize(80, 40);

            using var _ = Terminal.Initialize();

            ConsoleListener.Start();
            ConsoleListener.MouseEvent += ConsoleListener_MouseEvent;
            ConsoleListener.KeyEvent += ConsoleListener_KeyEvent;
            Console.CursorVisible = false;
            Console.SetCursorPosition(0, 0);
            Console.Title = "Work in progress";

            timer.Elapsed += OnTimedEvent;
            timer.Interval = gameSpeed;
            timer.AutoReset = true;
            timer.Enabled = true;
        }

        private static void ConsoleListener_KeyEvent(KEY_EVENT_RECORD r)
        {
            if (updatingHeader)
            {
                return;
            }
            if (gameState == GameState.Start && r.UnicodeChar == ' ')
            {
                Console.Clear();
                UpdateHeader();
                gameState = GameState.Playing;
            }
            if (gameState == GameState.GameOver && r.UnicodeChar == (char)27)
            {
                Environment.Exit(0);
            }
            if (gameState == GameState.GameOver && r.UnicodeChar == ' ')
            {
                gameBuffer.Clear();
                commandBuffer.Clear();
                Console.Clear();
                gameSpeed = 1000;
                score = 0;
                lives = 3;
                timer.Interval = gameSpeed;
                gameState = GameState.Idle;
            }
            if (r.UnicodeChar == 'c')
            {
                activeColor = (System.ConsoleColor)new Random().Next(1, 15);
            }
            if (r.UnicodeChar == 'b')
            {
                activeBrush = characterBrushes[new Random().Next(0, 3)];
            }
            if (r.UnicodeChar == 'u')
            {
                if (commandBuffer.Count > 0)
                {
                    var lastCommand = commandBuffer.Last();
                    commandBuffer.Remove(lastCommand);

                    var previousCommandAtPosition = commandBuffer.LastOrDefault(cc => cc.X == lastCommand.X && cc.Y == lastCommand.Y);
                    if (previousCommandAtPosition != null)
                    {
                        previousCommandAtPosition.Execute();
                    }
                    else
                    {
                        lastCommand.Clear();
                    }
                }
            }
            if (r.UnicodeChar == 's')
            {
                using (var sw = new System.IO.StreamWriter(fileName))
                {
                    foreach (var cc in commandBuffer)
                    {
                        sw.WriteLine(cc.X);
                        sw.WriteLine(cc.Y);
                        sw.WriteLine(cc.Value);
                    }
                }
            }
            if (r.UnicodeChar == 'l')
            {
                if (System.IO.File.Exists(fileName))
                {
                    // Load
                    commandBuffer = new List<ConsoleCommand>();
                    Console.Clear();
                    using (var sw = new System.IO.StreamReader(fileName))
                    {
                        while (!sw.EndOfStream)
                        {
                            int x = int.Parse(sw.ReadLine());
                            int y = int.Parse(sw.ReadLine());
                            string value = sw.ReadLine();
                            commandBuffer.Add(new ConsoleCommand(x, y, value));
                        }
                    }
                    // Replay buffer
                    foreach (var cc in commandBuffer)
                    {
                        cc.Execute();
                    }
                }
            }
        }
        private static void ConsoleListener_MouseEvent(MOUSE_EVENT_RECORD r)
        {
            if (!updatingHeader && r.dwButtonState == 1 && (r.dwMousePosition.X != previousMousePositionX || r.dwMousePosition.Y != previousMousePositionY))
            {
                var gameObjectFound = gameBuffer.FirstOrDefault(cc => cc.X == r.dwMousePosition.X && cc.Y == r.dwMousePosition.Y);
                if (gameObjectFound != null)
                {
                    gameObjectFound.Clear();
                    gameBuffer.Remove(gameObjectFound);

                    if (gameObjectFound.Value.Text[0] == characterBrushes[4])
                    {
                        score += 1;
                    }
                    else if (gameObjectFound.Value.Text[0] == characterBrushes[5])
                    {
                        lives += 1;
                    }
                    else if (gameObjectFound.Value.Text[0] == characterBrushes[6] || gameObjectFound.Value.Text[0] == characterBrushes[7] || gameObjectFound.Value.Text[0] == characterBrushes[8])
                    {
                        lives -= 1;
                        Console.Beep();
                        if (lives == 0)
                        {
                            LoadScreen("End.txt");
                            gameState = GameState.GameOver;
                        }
                    }
                }

                var newCommand = new ConsoleCommand(r.dwMousePosition.X, r.dwMousePosition.Y, AnsiString.Create(activeBrush.ToString()).Colored(activeColor, ConsoleColor.Black));

                newCommand.Execute();
                commandBuffer.Add(newCommand);
                previousMousePositionX = r.dwMousePosition.X;
                previousMousePositionY = r.dwMousePosition.Y;
            }
        }

        private static void UpdateHeader()
        {
            updatingHeader = true;
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.SetCursorPosition(0, 0);
            Console.Write($"WORK IN PROGRESS - Score: {score:000000} Lives: {lives:00} Food: {gameBuffer.Count():0000}");
            updatingHeader = false;
        }

        private static void LoadScreen(string fileName)
        {
            if (System.IO.File.Exists(fileName))
            {
                // Load
                commandBuffer = new List<ConsoleCommand>();
                Console.Clear();
                using (var sw = new System.IO.StreamReader(fileName))
                {
                    while (!sw.EndOfStream)
                    {
                        int x = int.Parse(sw.ReadLine());
                        int y = int.Parse(sw.ReadLine());
                        string value = sw.ReadLine();
                        commandBuffer.Add(new ConsoleCommand(x, y, value));
                    }
                }
                // Replay buffer
                foreach (var cc in commandBuffer)
                {
                    cc.Execute();
                }
            }
        }
    }
}
