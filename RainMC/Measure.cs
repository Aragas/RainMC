using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Minecraft;
using MineLib.Network.Enums;
using MineLib.Network.Packets.Client;
using Rainmeter;

namespace Plugin
{
    internal class Measure
    {
        public enum MeasureType { }

        public virtual void Init(Rainmeter.API api) { }
        public virtual void Reload(Rainmeter.API api, ref double maxValue) { }
        public virtual double Update() { return 0.0; }
        public virtual string GetString() { return null; }
        public virtual void ExecuteBang(string args) { }
        public virtual void Dispose() { }
    }

    internal class MinecraftBotWrapper : Measure
    {
        internal static readonly List<string> History = new List<string>();

        private static string _login = "";
        private static string _username = "";
        private static string _password = "";
        private static string _serverIp = "";
        public static string Path { get; private set; }

        private static Bot _client;

        internal static bool ClientIsNull
        {
            get { return _client == null; }
        }


        /// <summary>
        /// Called when a measure is created (i.e. when Rainmeter is launched or when a skin is refreshed).
        /// Initialize your measure object here.
        /// </summary>
        /// <param name="api">Rainmeter API</param>
        public override void Init(Rainmeter.API api)
        {
            if (String.IsNullOrEmpty(Path))
            {
                string path = api.ReadPath("Type", "");
                if (!String.IsNullOrEmpty(path))
                    Path = path.Replace("\\" + path.Split('\\')[7], "\\");
            }

            _login    = api.ReadString("Login", "ChatBot");
            _username = api.ReadString("Username", "ChatBot");
            _password = api.ReadString("Password", "");
            _serverIp = api.ReadString("ServerIP", "localhost");

            // Load all .dll from libs folder.
            AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
            {
                string strTempAssmbPath = "";

                Assembly objExecutingAssemblies = Assembly.GetExecutingAssembly();
                AssemblyName[] arrReferencedAssmbNames = objExecutingAssemblies.GetReferencedAssemblies();

                foreach (AssemblyName strAssmbName in arrReferencedAssmbNames)
                {
                    if (strAssmbName.FullName.Substring(0, strAssmbName.FullName.IndexOf(",", StringComparison.Ordinal)) ==
                        e.Name.Substring(0, e.Name.IndexOf(",", StringComparison.Ordinal)))
                    {
                        strTempAssmbPath = Path + "\\libs\\" +
                                           e.Name.Substring(0, e.Name.IndexOf(",", StringComparison.Ordinal)) +
                                           ".dll";
                        break;
                    }

                }			
                Assembly myAssembly = Assembly.LoadFrom(strTempAssmbPath);

                return myAssembly;
            };

            ChatParser.TagEnabled = false;

        }

        /// <summary>
        /// Called by Rainmeter when a !CommandMeasure bang is sent to the measure.
        /// </summary>
        /// <param name="args">String containing the arguments to parse.</param>
        public override void ExecuteBang(string args)
        {
            if (args.ToUpperInvariant() == "START")
            {
                if (ClientIsNull)
                {
                    _client = new Bot(_login, _password, _username);
                    _client.OnChatMessageReceived += _client_OnChatMessageReceived;
                    _client.Connect(_serverIp);
                }
            }

            else if (args.ToUpperInvariant() == "RESTART")
                Restart();
            

            else if (args.ToUpperInvariant() == "QUIT")
                Quit();
            

            else if (args.ToUpperInvariant().StartsWith("TEXT:"))
            {
                string text = args.Substring(5);

                if (!ClientIsNull)
                    HandleText(text);
            }

            else
                API.Log(API.LogType.Error, "RainMC.dll Command " + args + " not valid");

        }

        private void Restart()
        {
            if (!ClientIsNull)
                _client.Dispose();

            _client = new Bot(_login, _password, _username);
            _client.OnChatMessageReceived += _client_OnChatMessageReceived;
            _client.Connect(_serverIp);

            History.Clear();
        }

        private void Quit()
        {
            if (!ClientIsNull)
                _client.Dispose();
            _client = null;


            SaveHistory();
            History.Clear();
        }

        private void HandleText(string text)
        {
            text = text.Trim();

            if (text.ToLower() == "/quit")
                Quit();

            else if (text.ToLower() == "/reco" || text.ToLower() == "/reconnect")
                Restart();

            else if (text.ToLower() == "/resp" ||text.ToLower() == "/respawn")
            {
                _client.SendRespawnPacket();
                AddString("You have respawned.");
            }

            else if (text != "")
                _client.SendChatMessage(text);
            
        }

        private void _client_OnChatMessageReceived(string message)
        {
            if (!String.IsNullOrEmpty(message))
                History.Insert(0, message);
        }

        private static void SaveHistory()
        {
            string filename = String.Format("{0:yyyy-MM-dd-HH.mm.ss}.{1}", DateTime.Now, "log");
            const string directory = @"logs\";
            if (!Directory.Exists(Path + directory))
                Directory.CreateDirectory(Path + directory);

            using (var fileStream = new FileStream(Path + directory + filename, FileMode.OpenOrCreate))
            using (var streamWriter = new StreamWriter(fileStream))
            {
                History.Reverse();
                History.ForEach(streamWriter.WriteLine);
            }
        }

        public static void AddString(string message)
        {
            if (History != null)
                History.Insert(0, message);
        }

        public override void Dispose()
        {
            History.Clear();
            if (!ClientIsNull)
            {
                _client.Dispose();
                _client = null;
            }

        }
    }

    internal class Parser : MinecraftBotWrapper
    {
        private new enum MeasureType { One, Two, Three, Four, Five, Six, Seven }
        private MeasureType _type;

        public override void Reload(API api, ref double maxValue)
        {
            int type = api.ReadInt("Count", 1);
            switch (type)
            {
                case 1:
                    _type = MeasureType.One;
                    break;
                case 2:
                    _type = MeasureType.Two;
                    break;
                case 3:
                    _type = MeasureType.Three;
                    break;
                case 4:
                    _type = MeasureType.Four;
                    break;
                case 5:
                    _type = MeasureType.Five;
                    break;
                case 6:
                    _type = MeasureType.Six;
                    break;
                case 7:
                    _type = MeasureType.Seven;
                    break;
            }

        }

        public override string GetString()
        {
            if (!ClientIsNull)
            {
                switch (_type)
                {
                    case MeasureType.One:
                        try { return History[0]; }
                        catch (ArgumentOutOfRangeException) { }
                        break;

                    case MeasureType.Two:
                        try { return History[1]; }
                        catch (ArgumentOutOfRangeException) { }
                        break;

                    case MeasureType.Three:
                        try { return History[2]; }
                        catch (ArgumentOutOfRangeException) { }
                        break;

                    case MeasureType.Four:
                        try { return History[3]; }
                        catch (ArgumentOutOfRangeException) { }
                        break;

                    case MeasureType.Five:
                        try { return History[4]; }
                        catch (ArgumentOutOfRangeException) { }
                        break;

                    case MeasureType.Six:
                        try { return History[5]; }
                        catch (ArgumentOutOfRangeException) { }
                        break;

                    case MeasureType.Seven:
                        try { return History[6]; }
                        catch (ArgumentOutOfRangeException) { }
                        break;
                }
            }

            return null;
        }
    }

    public static class Plugin
    {
        static IntPtr StringBuffer = IntPtr.Zero;

        private static void GetMeasure(string type, ref Measure measure)
        {
            switch (type.ToUpper())
            {
                case "LOGIN":
                    measure = new MinecraftBotWrapper();
                    break;

                case "ANSWER":
                    measure = new Parser();
                    break;
            }
        }

        [DllExport]
        public static void Initialize(ref IntPtr data, IntPtr rm)
        {
            Rainmeter.API api = new Rainmeter.API(rm);

            string type = api.ReadString("Type", "");

            Measure measure = null;

            GetMeasure(type, ref measure);
            measure.Init(api);

            data = GCHandle.ToIntPtr(GCHandle.Alloc(measure));
        }

        [DllExport]
        public static void Finalize(IntPtr data)
        {
            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
            measure.Dispose();

            GCHandle.FromIntPtr(data).Free();
        }

        [DllExport]
        public static void Reload(IntPtr data, IntPtr rm, ref double maxValue)
        {
            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
            measure.Reload(new Rainmeter.API(rm), ref maxValue);
        }

        [DllExport]
        public static double Update(IntPtr data)
        {
            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
            return measure.Update();
        }

        [DllExport]
        public static IntPtr GetString(IntPtr data)
        {
            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
            if (StringBuffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(StringBuffer);
                StringBuffer = IntPtr.Zero;
            }

            string stringValue = measure.GetString();
            if (stringValue != null)
            {
                StringBuffer = Marshal.StringToHGlobalUni(stringValue);
            }

            return StringBuffer;
        }

        [DllExport]
        public static void ExecuteBang(IntPtr data, IntPtr args)
        {
            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
            measure.ExecuteBang(Marshal.PtrToStringUni(args));
        }
    }
}
