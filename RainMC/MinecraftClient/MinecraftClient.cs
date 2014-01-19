using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Threading;
using Plugin;

namespace MinecraftClientGUI
{
    /// <summary>
    /// This class acts as a wrapper for MinecraftClient.exe
    /// Allows the rest of the program to consider this class as the Minecraft client itself.
    /// </summary>

    class MinecraftClient
    {
        public static string ExePath = Measure.Path + "MinecraftClient.exe";
        public bool Disconnected { get; private set; }

        private readonly LinkedList<string> _outputBuffer = new LinkedList<string>();
        private readonly LinkedList<string> _tabAutoCompleteBuffer = new LinkedList<string>();
        private Process _client;
        private Thread _reader;

        /// <summary>
        /// Start the client using username, password and server IP
        /// </summary>
        /// <param name="username">Username or email</param>
        /// <param name="password">Password for the given username</param>
        /// <param name="serverIp">Server IP to join</param>
        public MinecraftClient(string username, string password, string serverIp)
        {
            InitClient('"' + username + "\" \"" + password + "\" \"" + serverIp + "\" BasicIO");
        }

        /// <summary>
        /// Inner function for launching the external console application
        /// </summary>
        /// <param name="arguments">Arguments to pass</param>
        private void InitClient(string arguments)
        {
            if (File.Exists(ExePath))
            {
                _client = new Process
                {
                    StartInfo =
                    {
                        FileName = ExePath,
                        Arguments = arguments,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        StandardOutputEncoding = Encoding.GetEncoding(850),
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardInput = true,
                        CreateNoWindow = true
                        #if !DEBUG
                        RedirectStandardError = false;
                        #endif
                    }
                };
                _client.Start();

                _reader = new Thread(t_reader) {Name = "InputReader"};
                _reader.Start();
            }
            else throw new FileNotFoundException("Cannot find Minecraft Client Executable!", Measure.Path + ExePath);
        }

        /// <summary>
        /// Thread for reading output and app messages from the console
        /// </summary>
        private void t_reader()
        {
            while (true)
            {
                try
                {
                    string line = "";
                    while (line.Trim() == "")
                    {
                        line = _client.StandardOutput.ReadLine() + _client.MainWindowTitle;
                        if (line == "Server was successfully joined.") { Disconnected = false;}
                        if (line == "You have left the server.") { Disconnected = true;}
                        if (line[0] == (char)0x00)
                        {
                            //App message from the console
                            string[] command = line.Substring(1).Split((char)0x00);
                            switch (command[0].ToLower())
                            {
                                case "autocomplete":
                                    _tabAutoCompleteBuffer.AddLast(command.Length > 1 ? command[1] : "");
                                    break;
                            }
                        }
                        else _outputBuffer.AddLast(line);
                    }
                }
                catch (NullReferenceException) { break; }
            }
        }

        /// <summary>
        /// Get the first queuing output line to print.
        /// </summary>
        /// <returns></returns>
        public string Read()
        {
            if (_outputBuffer.Count >= 1)
            {
                string line = _outputBuffer.First.Value;
                _outputBuffer.RemoveFirst();
                return line;
            }
            return null;
        }

        /// <summary>
        /// Complete a playername or a command, usually by pressing the TAB key
        /// </summary>
        /// <param name="text_behindcursor">Text to complete</param>
        /// <returns>Returns an autocompletion for the provided text</returns>
        public string TabAutoComplete(string text_behindcursor)
        {
            _tabAutoCompleteBuffer.Clear();
            if (text_behindcursor != null && text_behindcursor.Trim().Length > 0)
            {
                text_behindcursor = text_behindcursor.Trim();
                SendText((char)0x00 + "autocomplete" + (char)0x00 + text_behindcursor);
                int maxwait = 30; while (_tabAutoCompleteBuffer.Count < 1 && maxwait > 0) { Thread.Sleep(100); maxwait--; }
                if (_tabAutoCompleteBuffer.Count > 0)
                {
                    string text_completed = _tabAutoCompleteBuffer.First.Value;
                    _tabAutoCompleteBuffer.RemoveFirst();
                    return text_completed;
                }
                return text_behindcursor;
            }
            return "";
        }

        /// <summary>
        /// Print a Minecraft-Formatted string to the console area
        /// </summary>
        /// <param name="str">String to print</param>
        public string FormatString(string str)
        {
            if (!String.IsNullOrEmpty(str))
            {
                string line = "";
                string[] subs = str.Split(''); // Char is just invincible.
                if (subs[0].Length > 0) { line += subs[0]; }
                for (int i = 1; i < subs.Length; i++)
                {
                    if (subs[i].Length > 1)
                    {
                        line += (subs[i].Substring(1, subs[i].Length - 1));
                    }
                }
                return line;
            }
            return null;
        }

        /// <summary>
        /// Send a message or a command to the server
        /// </summary>
        /// <param name="text">Text to send</param>
        public void SendText(string text)
        {
            if (text != null)
            {
                text = text.Replace("\t", "");
                text = text.Replace("\r", "");
                text = text.Replace("\n", "");
                text = text.Trim();
                if (text.Length > 0)
                {
                    _client.StandardInput.WriteLine(text);
                }
            }
        }

        /// <summary>
        /// Properly disconnect from the server and dispose the client
        /// </summary>
        public void Close()
        {
            _client.StandardInput.WriteLine("/quit");
            if (_reader.IsAlive) { _reader.Abort(); }
            if (!_client.WaitForExit(3000))
            {
                try { _client.Kill(); }
                catch { }
            }
        }
    }
}
