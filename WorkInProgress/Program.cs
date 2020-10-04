using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using ITGlobal.CommandLine;
using ITGlobal.CommandLine.Table;
using static NativeMethods;

namespace WorkInProgress
{
    class Program
    {
        private static char[] CharacterBrushes = new char[] { '░', '▒', '▓', '█' };
        private static ConsoleColor activeColor = ConsoleColor.Red;
        private static char activeBrush = CharacterBrushes.First();

        private static AnsiChar[] CommandBuffer = new AnsiChar[16538];
        static void Main(string[] args)
        {
            IntPtr inHandle = GetStdHandle(STD_INPUT_HANDLE);
            uint mode = 0;
            GetConsoleMode(inHandle, ref mode);
            mode &= ~ENABLE_QUICK_EDIT_MODE;
            mode |= ENABLE_WINDOW_INPUT;
            mode |= ENABLE_MOUSE_INPUT;
            SetConsoleMode(inHandle, mode);

            using var _ = Terminal.Initialize();

            ConsoleListener.Start();
            ConsoleListener.MouseEvent += ConsoleListener_MouseEvent;
            ConsoleListener.KeyEvent += ConsoleListener_KeyEvent;
            ConsoleListener.WindowBufferSizeEvent += ConsoleListener_WindowBufferSizeEvent;
            Console.SetCursorPosition(0, 0);
            
            Console.Write(AnsiString.Create(Maps.Map));
            Console.Title = "Work in progress";
        }

        private static void ConsoleListener_WindowBufferSizeEvent(WINDOW_BUFFER_SIZE_RECORD r)
        {
            if(r.dwSize.X != Console.BufferWidth || r.dwSize.Y != Console.BufferHeight)
            {
                Console.Clear();
            }
        }

        private static void ConsoleListener_KeyEvent(KEY_EVENT_RECORD r)
        {
            if(r.UnicodeChar == 'c')
            {
                activeColor = (System.ConsoleColor)new Random().Next(1, 15);
            }
            if (r.UnicodeChar == 'b')
            {
                activeBrush = CharacterBrushes[ new Random().Next(0, CharacterBrushes.Length - 1)];
            }
        }

        private static void ConsoleListener_MouseEvent(MOUSE_EVENT_RECORD r)
        {
            if (r.dwButtonState == 1)
            {
                Console.SetCursorPosition(r.dwMousePosition.X, r.dwMousePosition.Y);
                Console.Write(AnsiString.Create(activeBrush.ToString()).Colored(activeColor, ConsoleColor.Black));
            }
        }
    }
}
