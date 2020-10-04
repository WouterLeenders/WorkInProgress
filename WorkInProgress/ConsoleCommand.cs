using ITGlobal.CommandLine;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace WorkInProgress
{
    class ConsoleCommand
    {
        public ConsoleCommand(int x, int y, AnsiString value)
        {
            X = x;
            Y = y;
            Value = value;
        }

        public int X { get; set; }
        public int Y { get; set; }
        public AnsiString Value { get; set; }

        public void Execute()
        {
            Console.SetCursorPosition(X, Y);
            Console.Write(Value);
        }

        public void Clear()
        {
            Console.SetCursorPosition(X, Y);
            var clearValue = AnsiString.Empty; 
            for (int i = 0; i <= Value.Length; i++)
            {
                clearValue += new AnsiChar(' ', ConsoleColor.Black, ConsoleColor.Black);
            }
            Console.Write(clearValue);
        }
    }
}
