﻿using System;
using MinecraftClient;

namespace Rainmeter
{
    /// <summary>
    /// Main part of Measure.
    /// </summary>
    internal partial class Measure
    {
        private static string Username = "";
        private static string Password = "";
        private static string ServerIP = "";

        internal enum MeasureType
        {
            Answer,
        }
        private MeasureType _type;

        /// <summary>
        /// Called when Rainmeter is launched. Just once.
        /// Is called before skin gets data.
        /// </summary>
        public Measure()
        {
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
        }

        private System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string dllName = args.Name.Contains(",")
                ? args.Name.Substring(0, args.Name.IndexOf(','))
                : args.Name.Replace(".dll", "");

            dllName = dllName.Replace(".", "_");

            if (dllName.EndsWith("_resources")) return null;

            System.Resources.ResourceManager rm =
                new System.Resources.ResourceManager(GetType().Namespace + ".Properties.Resources",
                    System.Reflection.Assembly.GetExecutingAssembly());

            byte[] bytes = (byte[])rm.GetObject(dllName);

            return System.Reflection.Assembly.Load(bytes);
        }

        /// <summary>
        /// Called when a measure is created (i.e. when Rainmeter is launched or when a skin is refreshed).
        /// Initialize your measure object here.
        /// </summary>
        /// <param name="api">Rainmeter API</param>
        internal void Initialize(Rainmeter.API api)
        {
            string type = api.ReadString("Type", "");

            Username = api.ReadString("Username", "BotChat");
            Password = api.ReadString("Password", "");
            ServerIP = api.ReadString("ServerIP", "localhost");

            switch (type.ToUpperInvariant())
            {
                
                case "ANSWER":
                    _type = MeasureType.Answer;
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

            Username = api.ReadString("Username", "BotChat");
            Password = api.ReadString("Password", "");
            ServerIP = api.ReadString("ServerIP", "localhost");

            switch (type.ToUpperInvariant())
            {

                case "ANSWER":
                    _type = MeasureType.Answer;
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
                MClient.Initialize(Username, Password, ServerIP);
            }
            else if (command.ToUpperInvariant() == "RESTART")
            {
                MClient.Restart();
            }
            else if (command.ToUpperInvariant() == "EXIT")
            {
                MClient.Exit();
            }
            else if (command.StartsWith("Text:"))
            {
                ConsoleIO.Text = command.Substring(5);
            }
            else
            {
                API.Log(API.LogType.Error, "RainMC.dll Command " + command + " not valid");
            }
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