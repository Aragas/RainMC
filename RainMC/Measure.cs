using System;
using System.Collections.Generic;
using MinecraftClientGUI;

namespace Rainmeter
{
    /// <summary>
    /// Main part of Measure.
    /// </summary>
    internal class Measure
    {
        static List<string> History = new List<string>();

        private static string Username = "";
        private static string Password = "";
        private static string ServerIP = "";

        public static string Path { get; private set; }

        private static MinecraftClient MC;

        internal enum MeasureType
        {
            Login,
            Answer
        }
        private MeasureType _type;

        internal enum CountType
        {
            One,
            Two,
            Three,
            Four,
            Five,
            Six,
            Seven
        }
        private CountType _countType;

        /// <summary>
        /// Called when Rainmeter is launched. Just once.
        /// Is called before skin gets data.
        /// </summary>
        public Measure()
        {
        }

        /// <summary>
        /// Called when a measure is created (i.e. when Rainmeter is launched or when a skin is refreshed).
        /// Initialize your measure object here.
        /// </summary>
        /// <param name="api">Rainmeter API</param>
        internal void Initialize(Rainmeter.API api)
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
                    _type = MeasureType.Answer;

                    int counttype = api.ReadInt("Count", 1);
                    switch (counttype)
                    {
                        case 1:
                            _countType = CountType.One;
                            break;

                        case 2:
                            _countType = CountType.Two;
                            break;

                        case 3:
                            _countType = CountType.Three;
                            break;

                        case 4:
                            _countType = CountType.Four;
                            break;

                        case 5:
                            _countType = CountType.Five;
                            break;

                        case 6:
                            _countType = CountType.Six;
                            break;

                        case 7:
                            _countType = CountType.Seven;
                            break;
                    }

                    break;

                case "LOGIN":
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
            if (MC != null && !MC.Disconnected)
            {
                string s = MC.Read();
                if (!String.IsNullOrEmpty(s))
                {
                    History.Insert(0, s);
                }

            }

            string type = api.ReadString("Type", "");

            switch (type.ToUpperInvariant())
            {

                case "ANSWER":
                    _type = MeasureType.Answer;

                    int countType = api.ReadInt("Count", 1);
                    switch (countType)
                    {
                        case 1:
                            _countType = CountType.One;
                            break;

                        case 2:
                            _countType = CountType.Two;
                            break;

                        case 3:
                            _countType = CountType.Three;
                            break;

                        case 4:
                            _countType = CountType.Four;
                            break;

                        case 5:
                            _countType = CountType.Five;
                            break;

                        case 6:
                            _countType = CountType.Six;
                            break;

                        case 7:
                            _countType = CountType.Seven;
                            break;
                    }

                    break;

                case "LOGIN":
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
        /// Called on every update cycle (usually once per second).
        /// </summary>
        /// <returns>Return the numerical value for the measure here.</returns>
        internal double Update()
        {
            return 0.0;
        }

        internal string GetString()
        {
            switch (_type)
            {
                case MeasureType.Answer:

                    if (MC != null && !MC.Disconnected)
                    {
                        switch (_countType)
                        {
                            case CountType.One:
                                try { return History[0]; }
                                catch (Exception ex)
                                {
                                    if (ex is ArgumentOutOfRangeException)
                                        break;
                                    
                                    throw;
                                }
                                break;

                            case CountType.Two:
                                try { return History[1]; }
                                catch (Exception ex)
                                {
                                    if (ex is ArgumentOutOfRangeException)
                                        break;

                                    throw;
                                }
                                break;

                            case CountType.Three:
                                try { return History[2]; }
                                catch (Exception ex)
                                {
                                    if (ex is ArgumentOutOfRangeException)
                                        break;

                                    throw;
                                }
                                break;

                            case CountType.Four:
                                try { return History[3]; }
                                catch (Exception ex)
                                {
                                    if (ex is ArgumentOutOfRangeException)
                                        break;

                                    throw;
                                }
                                break;

                            case CountType.Five:
                                try { return History[4]; }
                                catch (Exception ex)
                                {
                                    if (ex is ArgumentOutOfRangeException)
                                        break;

                                    throw;
                                }
                                break;

                            case CountType.Six:
                                try { return History[5]; }
                                catch (Exception ex)
                                {
                                    if (ex is ArgumentOutOfRangeException)
                                        break;

                                    throw;
                                }
                                break;

                            case CountType.Seven:
                                try { return History[6]; }
                                catch (Exception ex)
                                {
                                    if (ex is ArgumentOutOfRangeException)
                                        break;

                                    throw;
                                }
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
        internal void ExecuteBang(string command)
        {
            if (command.ToUpperInvariant() == "START")
            {
                if (MC == null)
                    MC = new MinecraftClient(Username, Password, ServerIP);
            }
                
            else if (command.ToUpperInvariant() == "RESTART")
            {
                if (MC != null)
                {
                    MC.Close();
                    MC = new MinecraftClient(Username, Password, ServerIP);
                }
            }

            else if (command.ToUpperInvariant() == "EXIT")
            {
                if (MC != null)
                {
                    MC.Close();
                    MC = null;
                }
            }

            else if (command.StartsWith("Text:"))
            {
                if (MC != null)
                    MC.SendText(command.Substring(5));
            }

            else
                API.Log(API.LogType.Error, "RainMC.dll Command " + command + " not valid");

        }

        /// <summary>
        /// Called when a measure is disposed (i.e. when Rainmeter is closed or when a skin is refreshed).
        /// Dispose your measure object here.
        /// </summary>
        internal void Finalize()
        {
        }

    }
}