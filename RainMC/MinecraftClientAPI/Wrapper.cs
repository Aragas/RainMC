using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace MinecraftClientAPI
{
    /// <summary>
    /// This class acts as a wrapper for MinecraftClient.exe
    /// Allows the rest of the program to consider this class as the Minecraft client itself.
    /// </summary>
    public sealed class Wrapper : IDisposable
    {
        #region Events

        /// <summary>
        /// Called when connected to server
        /// </summary>
        public event WrapperEventHandler<EventArgs> Connected;

        /// <summary>
        /// Called when disconnected from server
        /// </summary>
        public event WrapperEventHandler<EventArgs> Disconnected;

        /// <summary>
        /// Called when get data from Minecraft client. 
        /// Returns formatted and raw data, so you don't need to use ReadLine
        /// </summary>
        public event WrapperEventHandler<DataReceived> DataReceived;

        #endregion Events

        public bool ConnectedToServer { get; private set; }

        private const string ExeName = "MinecraftClient.exe";
        private static string FolderPath { get; set; }
        private static string ExePath { get; set; }

        private readonly LinkedList<string> _outputBuffer = new LinkedList<string>();

        private Process _client;

        private bool _disposed;

        public Wrapper(string[] args)
        {
            InitClient("\"" + String.Join("\" \"", args) + "\" BasicIO");
        }

        /// <summary>
        /// Start the client using username, password and server IP
        /// </summary>
        /// <param name="username">Username or email</param>
        /// <param name="password">Password for the given username</param>
        /// <param name="serverIp">Server IP to join</param>
        public Wrapper(string username, string password, string serverIp)
        {
            FolderPath = "";
            ExePath = FolderPath + ExeName;

            InitClient('"' + username + "\" \"" + password + "\" \"" + serverIp + "\" BasicIO");
        }

        /// <summary>
        /// Start the client using username, password, server IP and folder path
        /// </summary>
        /// <param name="username">Username or email</param>
        /// <param name="password">Password for the given username</param>
        /// <param name="serverIp">Server IP to join</param>
        /// <param name="folderPath">Path to the exe file</param>
        public Wrapper(string username, string password, string serverIp, string folderPath)
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
                        StandardOutputEncoding =
                            Encoding.GetEncoding(System.Globalization.CultureInfo.CurrentCulture.TextInfo.ANSICodePage)
                    }
                };
                _client.OutputDataReceived += _client_OutputDataReceived;
                _client.Start();
                _client.BeginOutputReadLine();
            }
            else throw new FileNotFoundException("Cannot find Minecraft Client Executable!", ExePath);
        }

        /// <summary>
        /// Get the first queuing output line to print in raw format.
        /// </summary>
        /// <returns>Raw MinecraftClient first output from the beginning</returns>
        public string ReadLineRaw()
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
        /// Get the first queuing output line to print.
        /// </summary>
        /// <returns>MinecraftClient first output from the beginning</returns>
        public string ReadLine()
        {
            return FormatRaw(ReadLineRaw());
        }

        public static string FormatRaw(string str)
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
        /// Output from Minecraft client is processed here.
        /// </summary>
        private void _client_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!String.IsNullOrEmpty(e.Data))
            {
                var line = e.Data;
                switch (line.Trim())
                {
                    case "Server was successfuly joined.":
                        if (Connected != null)
                            Connected(this, EventArgs.Empty);

                        ConnectedToServer = true;
                        break;

                    case "You have left the server.":
                        if (Disconnected != null)
                            Disconnected(this, EventArgs.Empty);

                        ConnectedToServer = false;
                        break;
                }
                _outputBuffer.AddLast(line);

                if (DataReceived != null)
                    DataReceived(this, new DataReceived(line));
            }
        }

        #region Dispose

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

        ~Wrapper()
        {
            Dispose(false);
        }

        #endregion Dispose
    }
}
