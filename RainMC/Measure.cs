using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using MinecraftClientAPI;
using Rainmeter;

namespace Plugin
{
    public class Measure
    {
        public enum MeasureType { }

        public virtual void Init(Rainmeter.API api) { }
        public virtual void Reload(Rainmeter.API api, ref double maxValue) { }
        public virtual double Update() { return 0.0; }
        public virtual string GetString() { return null; }
        public virtual void ExecuteBang(string args) { }
        public virtual void Dispose() { }
    }

    internal class Login : Measure
    {
        internal static readonly List<string> History = new List<string>();

        private static string Username = "";
        private static string Password = "";
        private static string ServerIP = "";
        private static string Path = "";

        private static Wrapper _wrapper;

        protected static bool WrapperIsNull
        {
            get
            {
                if (_wrapper == null)
                    return true;
                return false;
            }
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

            Username = api.ReadString("Username", "ChatBot");
            Password = api.ReadString("Password", "-");
            ServerIP = api.ReadString("ServerIP", "localhost");
        }

        /// <summary>
        /// Called by Rainmeter when a !CommandMeasure bang is sent to the measure.
        /// </summary>
        /// <param name="args">String containing the arguments to parse.</param>
        public override void ExecuteBang(string args)
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

        public override void Dispose()
        {
            History.Clear();
            if (_wrapper != null)
            {
                _wrapper.Dispose();
                _wrapper = null;
            }

        }
    }

    internal class Answer : Login
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
            if (!WrapperIsNull)
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
                    measure = new Login();
                    break;

                case "ANSWER":
                    measure = new Answer();
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
