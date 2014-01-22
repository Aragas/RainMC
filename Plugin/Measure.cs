using System.IO;
using System.Runtime.InteropServices;
using MinecraftClientAPI;
using Rainmeter;
using System;
using System.Collections.Generic;

namespace Plugin
{
    internal class Measure
    {
        private static readonly List<string> History = new List<string>();

        private static string Username = "";
        private static string Password = "";
        private static string ServerIP = "";
        private static string Path = "";

        private static Wrapper _wrapper;

        private enum MeasureType { Login, Answer }
        private MeasureType _type;

        private enum CountType { One, Two, Three, Four, Five, Six, Seven }
        private CountType _countType;

        /// <summary>
        /// Called when a measure is created (i.e. when Rainmeter is launched or when a skin is refreshed).
        /// Initialize your measure object here.
        /// </summary>
        /// <param name="api">Rainmeter API</param>
        public Measure(Rainmeter.API api)
        {
            if (String.IsNullOrEmpty(Path))
            {
                string path = api.ReadPath("Type", "");
                if (!String.IsNullOrEmpty(path))
                    Path = path.Replace("\\" + path.Split('\\')[7], "\\");
            }

            string type = api.ReadString("Type", "");
            switch (type.ToUpperInvariant())
            {

                case "ANSWER":
                    break;

                case "LOGIN":
                    _type = MeasureType.Login;

                    Username = api.ReadString("Username", "ChatBot");
                    Password = api.ReadString("Password", "-");
                    ServerIP = api.ReadString("ServerIP", "localhost");
                    break;

                default:
                    API.Log
                        (API.LogType.Error, "RainMC.dll Type=" + type + " not valid");
                    break;
            }
        }

        /// <summary>
        ///  Called when the measure settings are to be read directly after Initialize.
        ///  If DynamicVariables=1 is set on the measure, Reload is called on every update cycle (usually once per second).
        ///  Read and store measure settings here. To set a default maximum value for the measure, assign to maxValue.
        /// </summary>
        /// <param name="api">Rainmeter API</param>
        /// <param name="maxValue">Max Value</param>
        internal void Reload(Rainmeter.API api, ref double maxValue)
        {
            string type = api.ReadString("Type", "");
            switch (type.ToUpperInvariant())
            {
                case "ANSWER":
                    _type = MeasureType.Answer;

                    int countType = api.ReadInt("Count", 1);
                    switch (countType)
                    {
                        case 1: _countType = CountType.One; break;
                        case 2: _countType = CountType.Two; break;
                        case 3: _countType = CountType.Three; break;
                        case 4: _countType = CountType.Four; break;
                        case 5: _countType = CountType.Five; break;
                        case 6: _countType = CountType.Six; break;
                        case 7: _countType = CountType.Seven; break;
                    }

                    break;

                case "LOGIN":
                    break;

                default:
                    API.Log
                        (API.LogType.Error, "RainMC.dll Type=" + type + " not valid");
                    break;
            }
        }

        /// <summary>
        /// Called on every update cycle (usually once per second).
        /// </summary>
        /// <returns>Return the numerical value for the measure here.</returns>
        internal double GetDouble()
        {
            return 0.0;
        }

        internal string GetString()
        {
            switch (_type)
            {
                case MeasureType.Answer:

                    if (_wrapper != null)
                    {
                        switch (_countType)
                        {
                            case CountType.One:
                                try { return History[0]; }
                                catch (ArgumentOutOfRangeException) { }
                                break;

                            case CountType.Two:
                                try { return History[1]; }
                                catch (ArgumentOutOfRangeException) { }
                                break;

                            case CountType.Three:
                                try { return History[2]; }
                                catch (ArgumentOutOfRangeException) { }
                                break;

                            case CountType.Four:
                                try { return History[3]; }
                                catch (ArgumentOutOfRangeException) { }
                                break;

                            case CountType.Five:
                                try { return History[4]; }
                                catch (ArgumentOutOfRangeException) { }
                                break;

                            case CountType.Six:
                                try { return History[5]; }
                                catch (ArgumentOutOfRangeException) { }
                                break;

                            case CountType.Seven:
                                try { return History[6]; }
                                catch (ArgumentOutOfRangeException) { }
                                break;
                        }
                    }
                    break;

            }
            return null;
        }

        /// <summary>
        /// Called by Rainmeter when a !CommandMeasure bang is sent to the measure.
        /// </summary>
        /// <param name="command">String containing the arguments to parse.</param>
        internal void ExecuteBang(string args)
        {
            if (args.ToUpperInvariant() == "START")
            {
                if (_wrapper == null)
                {
                    _wrapper = new Wrapper(Username, Password, ServerIP, Path);
                    _wrapper.DataReceived += _wrapper_DataReceived;
                }
            }

            else if (args.ToUpperInvariant() == "RESTART")
            {
                if (_wrapper != null)
                {
                    _wrapper.Dispose();
                    _wrapper = new Wrapper(Username, Password, ServerIP, Path);
                    _wrapper.DataReceived += _wrapper_DataReceived;
                    History.Clear();
                }
            }

            else if (args.ToUpperInvariant() == "EXIT")
            {
                if (_wrapper != null)
                {
                    _wrapper.Dispose();
                    _wrapper = null;
                }
                SaveHistory();
                History.Clear();
            }

            else if (args.ToUpperInvariant().StartsWith("TEXT:"))
            {
                if (_wrapper != null)
                    _wrapper.SendText(args.Substring(5));
            }

            else
                API.Log(API.LogType.Error, "RainMC.dll Command " + args + " not valid");

        }

        private static void SaveHistory()
        {
            string filename = String.Format("{0:yyyy-MM-dd-HH.mm.ss}.{1}", DateTime.Now, "log");
            string directory = @"logs\";
            if (!Directory.Exists(Path + directory))
                Directory.CreateDirectory(Path + directory);

            using (FileStream fileStream = new FileStream(Path + directory + filename, FileMode.OpenOrCreate))
            using (StreamWriter streamWriter = new StreamWriter(fileStream))
            {
                History.Reverse();
                History.ForEach(streamWriter.WriteLine);
            }
        }

        private static void _wrapper_DataReceived(object sender, DataReceived e)
        {
            if (!String.IsNullOrEmpty(e.Data))
                History.Insert(0, e.Data);
        }

        ~Measure()
        {
            History.Clear();
            if (_wrapper != null)
                _wrapper.Dispose();
        }
    }

    public static class Plugin
    {
        public static void Initialize(ref IntPtr data, IntPtr rm)
        {
            data = GCHandle.ToIntPtr(GCHandle.Alloc(new Measure(new API(rm))));
        }

        public static void Finalize(IntPtr data)
        {
            GCHandle.FromIntPtr(data).Free();
        }

        public static void Reload(IntPtr data, IntPtr rm, ref double maxValue)
        {
            ((Measure)GCHandle.FromIntPtr(data).Target).Reload(new API(rm), ref maxValue);
        }

        public static double Update(IntPtr data)
        {
            return ((Measure)GCHandle.FromIntPtr(data).Target).GetDouble();
        }

        public static string GetString(IntPtr data)
        {
            return ((Measure)GCHandle.FromIntPtr(data).Target).GetString();
        }

        public static void ExecuteBang(IntPtr data, string args)
        {
            ((Measure)GCHandle.FromIntPtr(data).Target).ExecuteBang(args);
        }
    }
}
