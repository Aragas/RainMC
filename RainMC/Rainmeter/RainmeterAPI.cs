/*
  Copyright (C) 2011 Birunthan Mohanathas

  This program is free software; you can redistribute it and/or
  modify it under the terms of the GNU General Public License
  as published by the Free Software Foundation; either version 2
  of the License, or (at your option) any later version.

  This program is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  GNU General Public License for more details.

  You should have received a copy of the GNU General Public License
  along with this program; if not, write to the Free Software
  Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
*/

using System;
using System.Runtime.InteropServices;

namespace Rainmeter
{
    /// <summary>
    ///     Wrapper around the Rainmeter C API.
    /// </summary>
    public class API
    {
        private readonly IntPtr _mRm;

        public API(IntPtr rm)
        {
            _mRm = rm;
        }

        public enum LogType
        {
            Error = 1,
            Warning = 2,
            Notice = 3,
            Debug = 4
        }

        public static unsafe void Execute(IntPtr skin, string command)
        {
            NativeMethods.RmExecute((void*)skin, ToUnsafe(command));
        }

        public static unsafe void Log(LogType type, string message)
        {
            NativeMethods.LSLog((int)type, null, ToUnsafe(message));
        }

        public unsafe string MeasureName
        {
            get
            {
                var value = (char*) NativeMethods.RmGet((void*) _mRm, 0);
                return new string(value);
            }
        }

        public unsafe string SettingsFile
        {
            get
            {
                var value = (char*) NativeMethods.RmGet((void*) _mRm, 2);
                return new string(value);
            }
        }

        public unsafe IntPtr Skin
        {
            get { return (IntPtr) NativeMethods.RmGet((void*) _mRm, 1); }
        }

        public unsafe string SkinName
        {
            get
            {
                var value = (char*) NativeMethods.RmGet((void*) _mRm, 3);
                return new string(value);
            }
        }

        public unsafe IntPtr SkinWindow
        {
            get { return (IntPtr) NativeMethods.RmGet((void*) _mRm, 4); }
        }

        public unsafe double ReadDouble(string option, double defValue)
        {
            return NativeMethods.RmReadFormula((void*)_mRm, ToUnsafe(option), defValue);
        }

        public unsafe int ReadInt(string option, int defValue)
        {
            return (int)NativeMethods.RmReadFormula((void*)_mRm, ToUnsafe(option), defValue);
        }

        public unsafe string ReadPath(string option, string defValue)
        {
            char* relativePath = NativeMethods.RmReadString((void*)_mRm, ToUnsafe(option), ToUnsafe(defValue), 1);
            char* value = NativeMethods.RmPathToAbsolute((void*)_mRm, relativePath);
            return new string(value);
        }

        public unsafe string ReadString(string option, string defValue, bool replaceMeasures = true)
        {
            char* value = NativeMethods.RmReadString((void*)_mRm, ToUnsafe(option), ToUnsafe(defValue), replaceMeasures ? 1 : 0);
            return new string(value);
        }

        public unsafe string ReplaceVariables(string str)
        {
            char* value = NativeMethods.RmReplaceVariables((void*)_mRm, ToUnsafe(str));
            return new string(value);
        }

        private static unsafe char* ToUnsafe(string s)
        {
            fixed (char* p = s) return p;
        }
    }

    internal static class NativeMethods
    {
        [DllImport("Rainmeter.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int LSLog(int type, char* unused, char* message);

        [DllImport("Rainmeter.dll", CharSet = CharSet.Auto)]
        internal static extern unsafe void RmExecute(void* rm, char* command);

        [DllImport("Rainmeter.dll", CharSet = CharSet.Auto)]
        internal static extern unsafe void* RmGet(void* rm, int type);

        [DllImport("Rainmeter.dll", CharSet = CharSet.Auto)]
        internal static extern unsafe char* RmPathToAbsolute(void* rm, char* relativePath);

        [DllImport("Rainmeter.dll", CharSet = CharSet.Auto)]
        internal static extern unsafe double RmReadFormula(void* rm, char* option, double defValue);

        [DllImport("Rainmeter.dll", CharSet = CharSet.Auto)]
        internal static extern unsafe char* RmReadString(void* rm, char* option, char* defValue, int replaceMeasures);

        [DllImport("Rainmeter.dll", CharSet = CharSet.Auto)]
        internal static extern unsafe char* RmReplaceVariables(void* rm, char* str);
    }

    /// <summary>
    ///     Attribute to mark method as exported for DllExporter.exe.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class DllExport : Attribute
    {
    }
}