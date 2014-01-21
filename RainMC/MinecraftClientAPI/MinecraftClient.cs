using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Threading;

namespace MinecraftClientAPI
{
    /// <summary>
    /// This class acts as a wrapper for MinecraftClient.exe
    /// Allows the rest of the program to consider this class as the Minecraft client itself.
    /// </summary>
    internal sealed class MinecraftClient : IDisposable
    {
        public bool Disconnected { get; private set; }

        private const string ExeName = "MinecraftClient.exe";
        private static string FolderPath { get; set; }
        private static string ExePath { get; set; }

        private readonly LinkedList<string> _outputBuffer = new LinkedList<string>();

        private Process _client;
        private Thread _reader;

        private bool _disposed;

        /// <summary>
        /// Start the client using username, password and server IP
        /// </summary>
        /// <param name="username">Username or email</param>
        /// <param name="password">Password for the given username</param>
        /// <param name="serverIp">Server IP to join</param>
        /// <param name="folderPath">Path to the exe file</param>
        public MinecraftClient(string username, string password, string serverIp, string folderPath)
        {
            FolderPath = folderPath;
            ExePath = FolderPath + ExeName;

            InitClient('"' + username + "\" \"" + password + "\" \"" + serverIp + "\" BasicIO");
        }

        /// <summary>
        /// Inner function for launching the external console application
        /// </summary>
        /// <param name="args">Arguments to pass</param>
        private void InitClient(string args)
        {
            if (File.Exists(ExePath))
            {
                _client = new Process
                {
                    StartInfo =
                    {
                        FileName = ExePath,
                        Arguments = args,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardInput = true,
                        CreateNoWindow = true,
                        StandardOutputEncoding = Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.ANSICodePage)
                    }
                };
                _client.OutputDataReceived += _client_OutputDataReceived;
                _client.Start();
                _client.BeginOutputReadLine();
            }
            else throw new FileNotFoundException("Cannot find Minecraft Client Executable!", ExePath);
        }

        private void _client_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!String.IsNullOrEmpty(e.Data))
            {
                var line = e.Data;
                switch (line.Trim())
                {
                    case "Server was successfully joined.":
                        Disconnected = false;
                        break;
                    case "You have left the server.":
                        Disconnected = true;
                        break;
                }
                _outputBuffer.AddLast(line);
            }
        }

        /// <summary>
        /// Get the first queuing output line to print.
        /// </summary>
        /// <returns>Console Output</returns>
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
        /// Print a Minecraft-Formatted string to the console area
        /// </summary>
        /// <param name="str">String to print</param>
        public string FormatString(string str)
        {
            if (!String.IsNullOrEmpty(str))
            {
                string line = "";
                string[] subs = str.Split('§');
                if (subs[0].Length > 0)
                    line += subs[0];
                
                for (int i = 1; i < subs.Length; i++)
                {
                    if (subs[i].Length > 1)
                        line += (subs[i].Substring(1, subs[i].Length - 1));
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
            if (!String.IsNullOrEmpty(text) && text.Length > 0)
            {
                text = text.Replace("\t", "");
                text = text.Replace("\r", "");
                text = text.Replace("\n", "");
                text = text.Trim();
                _client.StandardInput.WriteLine(text);
            }
        }


        /// <summary>
        /// Properly disconnect from the server and dispose the client
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _client.StandardInput.WriteLine("/quit");

                    if (!_client.WaitForExit(1000))
                        _client.Kill();
                    
                }
                _disposed = true;
            }
        }

        ~MinecraftClient()
        {
            Dispose(false);
        }
    }
}
