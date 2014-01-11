using Rainmeter;
using System;
using System.Collections.Generic;
using System.Threading;

namespace MinecraftClient
{
    /// <summary>
    /// Interface for TAB autocompletion
    /// Allows to use any object which has an AutoComplete() method using the IAutocomplete interface
    /// </summary>
    public interface IAutoComplete
    {
        string AutoComplete(string behindCursor);
    }

    /// <summary>
    /// Allows simultaneous console input and output without breaking user input
    /// (Without having this annoying behaviour : User inp[Some Console output]ut)
    /// </summary>
    public static class ConsoleIO
    {
        public static readonly LinkedList<string> History = new LinkedList<string>();
        public static bool Debug = false;
        public static string Text = "";
        private static IAutoComplete _autocompleteEngine;

        public static string ReadLine()
        {
            while (String.IsNullOrEmpty(Text))
            {
                Thread.Sleep(1000);
            }

            string buffer = Text;
            Text = "";
            //History.AddLast(buffer);
            return buffer;
        }

        public static void Write(string text)
        {
            if (text != null && text.Contains("\n"))
                History.AddLast(text.Replace("\n", "  "));
            else
                History.AddLast(text);

            if (Debug)
                API.Log(API.LogType.Warning, text);
        }

        public static void Write(char c)
        {
            Write("" + c);
        }

        public static void SetAutoCompleteEngine(IAutoComplete engine)
        {
            _autocompleteEngine = engine;
        }

    }
}