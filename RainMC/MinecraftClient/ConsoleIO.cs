using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Rainmeter;

namespace MinecraftClient
{
    /// <summary>
    /// Allows simultaneous console input and output without breaking user input
    /// (Without having this annoying behaviour : User inp[Some Console output]ut)
    /// </summary>

    public static class ConsoleIO
    {
        public static string Text = "";
        //public static bool BasicIo = false;
        private static IAutoComplete _autocompleteEngine;
        private static readonly LinkedList<string> Previous = new LinkedList<string>();

        public static void SetAutoCompleteEngine(IAutoComplete engine)
        {
            _autocompleteEngine = engine;
        }

        #region Read User Input

        public static string ReadLine()
        {
            string buffer;

            while (String.IsNullOrEmpty(Text))
            {
                Thread.Sleep(1000);
            }

            buffer = Text;
            Text = "";
            Previous.AddLast(buffer);
            return buffer;
        }

        #endregion

        #region Console Output

        public static void Write(string text)
        {
            if (text != null && text.Contains("\n"))
                text.Replace("\n", "  ");
            API.Log(API.LogType.Warning, text);
        }

        //public static void WriteLine(string line)
        //{
        //    //Write(line + '\n');
        //    Write(line);
        //}

        public static void Write(char c)
        {
            Write("" + c);
        }
        #endregion

    }

    /// <summary>
    /// Interface for TAB autocompletion
    /// Allows to use any object which has an AutoComplete() method using the IAutocomplete interface
    /// </summary>
    public interface IAutoComplete
    {
        string AutoComplete(string BehindCursor);
    }
}
