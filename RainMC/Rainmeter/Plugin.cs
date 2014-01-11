using System;
using System.Collections.Generic;
using Plugin;

namespace Rainmeter
{
    public static class Plugin
    {
        static readonly Dictionary<uint, Measure> Measures = new Dictionary<uint, Measure>();

        [DllExport]
        public static unsafe void Initialize(void** data, void* rm)
        {
            var id = (uint)*data;
            Measures.Add(id, new Measure());
            Measures[id].Initialize(new API((IntPtr)rm));
        }

        [DllExport]
        public static unsafe void Reload(void* data, void* rm, double* maxValue)
        {
            var id = (uint)data;
            Measures[id].Reload(new API((IntPtr)rm), ref *maxValue);
        }

        [DllExport]
        public static unsafe double Update(void* data)
        {
            var id = (uint)data;
            return Measures[id].Update();
        }

        [DllExport]
        public static unsafe char* GetString(void* data)
        {
            var id = (uint)data;
            fixed (char* s = Measures[id].GetString()) return s;
        }

        [DllExport]
        public static unsafe void ExecuteBang(void* data, char* args)
        {
            var id = (uint)data;
            Measures[id].ExecuteBang(new string(args));
        }

        [DllExport]
        public static unsafe void Finalize(void* data)
        {
            var id = (uint)data;
            Measures[id].Finalize();
            Measures.Remove(id);
        }

    }
}