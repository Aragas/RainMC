﻿using System;

namespace MinecraftClientAPI
{
    public delegate void WrapperEventHandler<in TEventArgs>(object sender, TEventArgs e);

    public class DataReceived : EventArgs
    {
        public string DataRaw { get; private set; }
        public string Data { get { return Wrapper.FormatRaw(DataRaw); } private set {} }

        public DataReceived(string data)
        {
            DataRaw = data;
        }
    }
}
