﻿/*
  Copyright (C) 2013-2014 ORelio

  See CDDL-1.0 for license.

*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using Plugin;

namespace Minecraft
{
    /// <summary>
    /// This class parses JSON chat data from MC 1.6+ and returns the appropriate string to be printed.
    /// </summary>
    internal static class ChatParser
    {
        public static bool TagEnabled = true;
        private static readonly string TranslationsFileFromMcDir =
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) +
            @"\.minecraft\assets\objects\9e\9e2fdc43fc1c7024ff5922b998fadb2971a64ee0"; //MC 1.7.4 en_GB.lang

        private const string TranslationsFileWebsiteIndex = "https://s3.amazonaws.com/Minecraft.Download/indexes/1.7.4.json";
        private const string TranslationsFileWebsiteDownload = "http://resources.download.minecraft.net";
        private static string TranslationsFile = "translations.lang";

        private static readonly string SavePath = MinecraftBotWrapper.Path + "\\translations.lang";

        /// <summary>
        /// The main function to convert text from MC 1.6+ JSON to MC 1.5.2 formatted text
        /// </summary>
        /// <param name="json">JSON serialized text</param>
        /// <returns>Returns the translated text</returns>
        public static string ParseText(string json)
        {
            int cursorpos = 0;
            JSONData jsonData = String2Data(json, ref cursorpos);
            return JSONData2String(jsonData, "");
        }

        /// <summary>
        /// An internal class to store unserialized JSON data
        /// The data can be an object, an array or a string
        /// </summary>
        private class JSONData
        {
            public enum DataType
            {
                Object,
                Array,
                String
            };

            private DataType type;

            public DataType Type
            {
                get { return type; }
            }

            public Dictionary<string, JSONData> Properties;
            public List<JSONData> DataArray;
            public string StringValue;

            public JSONData(DataType datatype)
            {
                type = datatype;
                Properties = new Dictionary<string, JSONData>();
                DataArray = new List<JSONData>();
                StringValue = String.Empty;
            }
        }

        /// <summary>
        /// Get the classic color tag corresponding to a color name
        /// </summary>
        /// <param name="colorname">Color Name</param>
        /// <returns>Color code</returns>
        private static string color2tag(string colorname)
        {
            if (!TagEnabled)
                return "";

            switch (colorname.ToLower())
            {
                case "black":
                    return "§0";
                case "dark_blue":
                    return "§1";
                case "dark_green":
                    return "§2";
                case "dark_aqua":
                    return "§3";
                case "dark_red":
                    return "§4";
                case "dark_purple":
                    return "§5";
                case "gold":
                    return "§6";
                case "gray":
                    return "§7";
                case "dark_gray":
                    return "§8";
                case "blue":
                    return "§9";
                case "green":
                    return "§a";
                case "aqua":
                    return "§b";
                case "red":
                    return "§c";
                case "light_purple":
                    return "§d";
                case "yellow":
                    return "§e";
                case "white":
                    return "§f";
                default:
                    return "";
            }
        }

        private static bool init = false;
        private static Dictionary<string, string> TranslationRules = new Dictionary<string, string>();

        public static void InitTranslations()
        {
            if (!init)
            {
                InitRules();
                init = true;
            }
        }

        /// <summary>
        /// Rules for text translation
        /// </summary>
        private static void InitRules()
        {
            //Small default dictionnary of translation rules
            TranslationRules["chat.type.admin"] = "[%s: %s]";
            TranslationRules["chat.type.announcement"] = "§d[%s] %s";
            TranslationRules["chat.type.emote"] = " * %s %s";
            TranslationRules["chat.type.text"] = "<%s> %s";
            TranslationRules["multiplayer.player.joined"] = "§e%s joined the game.";
            TranslationRules["multiplayer.player.left"] = "§e%s left the game.";
            TranslationRules["commands.message.display.incoming"] = "§7%s whispers to you: %s";
            TranslationRules["commands.message.display.outgoing"] = "§7You whisper to %s: %s";


            //Use translations from Minecraft assets if translation file is not found but a copy of the game is installed?
            if (!File.Exists(TranslationsFile) //Try en_GB.lang
                && File.Exists(TranslationsFileFromMcDir))
            {
                TranslationsFile = TranslationsFileFromMcDir;
                MinecraftBotWrapper.AddString("Using en_GB.lang from your Minecraft directory.");
            }

            //Still not found? try downloading en_GB from Mojang's servers?
            if (!File.Exists(TranslationsFile))
            {
                MinecraftBotWrapper.AddString("Downloading en_GB.lang from Mojang's servers...");
                try
                {
                    string assets_index = downloadString(TranslationsFileWebsiteIndex);
                    string[] tmp = assets_index.Split(new string[] {"lang/en_GB.lang"}, StringSplitOptions.None);
                    tmp = tmp[1].Split(new string[] {"hash\": \""}, StringSplitOptions.None);
                    string hash = tmp[1].Split('"')[0]; //Translations file identifier on Mojang's servers
                    File.WriteAllText(SavePath,
                        downloadString(TranslationsFileWebsiteDownload + '/' + hash.Substring(0, 2) + '/' +
                                       hash));
                    MinecraftBotWrapper.AddString("Done. File saved as \"" + SavePath + '"');
                }
                catch
                {
                    MinecraftBotWrapper.AddString("Failed to download the file.");
                }
            }

            //Load the external dictionnary of translation rules or display an error message
            if (File.Exists(TranslationsFile))
            {
                string[] translations = File.ReadAllLines(TranslationsFile);
                foreach (string line in translations)
                {
                    if (line.Length > 0)
                    {
                        string[] splitted = line.Split('=');
                        if (splitted.Length == 2)
                        {
                            TranslationRules[splitted[0]] = splitted[1];
                        }
                    }
                }

                MinecraftBotWrapper.AddString("Translations file loaded.");
            }
            else //No external dictionnary found.
            {
                MinecraftBotWrapper.AddString("Translations file not found: \"" + TranslationsFile + "\""
                                              + "\nYou can pick a translation file from .minecraft\\assets\\lang\\"
                                              + "\nSome messages won't be properly printed without this file.");
            }
            //*/
        }

        /// <summary>
        /// Format text using a specific formatting rule.
        /// Example : * %s %s + ["ORelio", "is doing something"] = * ORelio is doing something
        /// </summary>
        /// <param name="rulename">Name of the rule, chosen by the server</param>
        /// <param name="using_data">Data to be used in the rule</param>
        /// <returns>Returns the formatted text according to the given data</returns>
        private static string TranslateString(string rulename, List<string> using_data)
        {
            if (!init)
            {
                InitRules();
                init = true;
            }
            if (TranslationRules.ContainsKey(rulename))
            {
                if ((TranslationRules[rulename].IndexOf("%1$s", StringComparison.Ordinal) >= 0 &&
                     TranslationRules[rulename].IndexOf("%2$s", StringComparison.Ordinal) >= 0)
                    &&
                    (TranslationRules[rulename].IndexOf("%1$s", StringComparison.Ordinal) >
                     TranslationRules[rulename].IndexOf("%2$s", StringComparison.Ordinal)))
                {
                    while (using_data.Count < 2)
                    {
                        using_data.Add("");
                    }
                    string tmp = using_data[0];
                    using_data[0] = using_data[1];
                    using_data[1] = tmp;
                }
                string[] syntax = TranslationRules[rulename].Split(new string[] {"%s", "%d", "%1$s", "%2$s"},
                    StringSplitOptions.None);
                while (using_data.Count < syntax.Length - 1)
                {
                    using_data.Add("");
                }
                string[] using_array = using_data.ToArray();
                string translated = "";
                for (int i = 0; i < syntax.Length - 1; i++)
                {
                    translated += syntax[i];
                    translated += using_array[i];
                }
                translated += syntax[syntax.Length - 1];
                return translated;
            }
            else
                return "[" + rulename + "] " + String.Join(" ", using_data.ToArray());
        }

        /// <summary>
        /// Parse a JSON string to build a JSON object
        /// </summary>
        /// <param name="toparse">String to parse</param>
        /// <param name="cursorpos">Cursor start (set to 0 for function init)</param>
        /// <returns></returns>
        private static JSONData String2Data(string toparse, ref int cursorpos)
        {
            try
            {
                JSONData data;
                switch (toparse[cursorpos])
                {
                        //Object
                    case '{':
                        data = new JSONData(JSONData.DataType.Object);
                        cursorpos++;
                        while (toparse[cursorpos] != '}')
                        {
                            if (toparse[cursorpos] == '"')
                            {
                                JSONData propertyname = String2Data(toparse, ref cursorpos);
                                if (toparse[cursorpos] == ':')
                                    cursorpos++;
                                
                                else
                                {
                                    /* parse error ? */
                                }
                                JSONData propertyData = String2Data(toparse, ref cursorpos);
                                data.Properties[propertyname.StringValue] = propertyData;
                            }
                            else 
                                cursorpos++;
                        }
                        cursorpos++;
                        break;

                        //Array
                    case '[':
                        data = new JSONData(JSONData.DataType.Array);
                        cursorpos++;
                        while (toparse[cursorpos] != ']')
                        {
                            if (toparse[cursorpos] == ',')
                                cursorpos++;
                            
                            JSONData arrayItem = String2Data(toparse, ref cursorpos);
                            data.DataArray.Add(arrayItem);
                        }
                        cursorpos++;
                        break;

                        //String
                    case '"':
                        data = new JSONData(JSONData.DataType.String);
                        cursorpos++;
                        while (toparse[cursorpos] != '"')
                        {
                            if (toparse[cursorpos] == '\\')
                            {
                                try //Unicode character \u0123
                                {
                                    if (toparse[cursorpos + 1] == 'u'
                                        && isHex(toparse[cursorpos + 2])
                                        && isHex(toparse[cursorpos + 3])
                                        && isHex(toparse[cursorpos + 4])
                                        && isHex(toparse[cursorpos + 5]))
                                    {
                                        //"abc\u0123abc" => "0123" => 0123 => Unicode char n°0123 => Add char to string
                                        data.StringValue +=
                                            Char.ConvertFromUtf32(Int32.Parse(toparse.Substring(cursorpos + 2, 4),
                                                NumberStyles.HexNumber));
                                        cursorpos += 6;
                                        continue;
                                    }
                                    else 
                                        cursorpos++; //Normal character escapement \"
                                }
                                catch (IndexOutOfRangeException)
                                {
                                    cursorpos++;
                                } // \u01<end of string>
                                catch (ArgumentOutOfRangeException)
                                {
                                    cursorpos++;
                                } // Unicode index 0123 was invalid
                            }
                            data.StringValue += toparse[cursorpos];
                            cursorpos++;
                        }
                        cursorpos++;
                        break;

                        //Boolean : true
                    case 't':
                        data = new JSONData(JSONData.DataType.String);
                        cursorpos++;
                        if (toparse[cursorpos] == 'r')
                            cursorpos++;
                        
                        if (toparse[cursorpos] == 'u')
                            cursorpos++;
                        
                        if (toparse[cursorpos] == 'e')
                        {
                            cursorpos++;
                            data.StringValue = "true";
                        }
                        break;

                        //Boolean : false
                    case 'f':
                        data = new JSONData(JSONData.DataType.String);
                        cursorpos++;
                        if (toparse[cursorpos] == 'a')
                            cursorpos++;
                       
                        if (toparse[cursorpos] == 'l')
                            cursorpos++;
                        
                        if (toparse[cursorpos] == 's')
                            cursorpos++;
                        
                        if (toparse[cursorpos] == 'e')
                        {
                            cursorpos++;
                            data.StringValue = "false";
                        }
                        break;

                        //Unknown data
                    default:
                        cursorpos++;
                        return String2Data(toparse, ref cursorpos);
                }
                return data;
            }
            catch (IndexOutOfRangeException)
            {
                return new JSONData(JSONData.DataType.String);
            }
        }

        /// <summary>
        /// Use a JSON Object to build the corresponding string
        /// </summary>
        /// <param name="data">JSON object to convert</param>
        /// <param name="colorcode">Allow parent color code to affect child elements (set to "" for function init)</param>
        /// <returns>returns the Minecraft-formatted string</returns>
        private static string JSONData2String(JSONData data, string colorcode)
        {
            string extra_result = "";
            switch (data.Type)
            {
                case JSONData.DataType.Object:
                    if (data.Properties.ContainsKey("color"))
                        colorcode = color2tag(JSONData2String(data.Properties["color"], ""));
                    
                    if (data.Properties.ContainsKey("extra"))
                    {
                        string tag = "";
                        if (TagEnabled)
                            tag = "§r";
                        JSONData[] extras = data.Properties["extra"].DataArray.ToArray();
                        foreach (JSONData item in extras)
                            extra_result = extra_result + JSONData2String(item, colorcode) + tag;
                    }

                    if (data.Properties.ContainsKey("text"))
                        return colorcode + JSONData2String(data.Properties["text"], colorcode) + extra_result;
                    
                    else if (data.Properties.ContainsKey("translate"))
                    {
                        List<string> using_data = new List<string>();

                        if (data.Properties.ContainsKey("using") && !data.Properties.ContainsKey("with"))
                            data.Properties["with"] = data.Properties["using"];

                        if (data.Properties.ContainsKey("with"))
                        {
                            JSONData[] array = data.Properties["with"].DataArray.ToArray();
                            for (int i = 0; i < array.Length; i++)
                            {
                                using_data.Add(JSONData2String(array[i], colorcode));
                            }
                        }

                        return colorcode +
                               TranslateString(JSONData2String(data.Properties["translate"], ""), using_data) +
                               extra_result;
                    }

                    else
                        return extra_result;

                case JSONData.DataType.Array:
                    string result = "";
                    foreach (JSONData item in data.DataArray)
                    {
                        result += JSONData2String(item, colorcode);
                    }
                    return result;

                case JSONData.DataType.String:
                    return colorcode + data.StringValue;
            }

            return "";
        }

        /// <summary>
        /// Small function for checking if a char is an hexadecimal char (0-9 A-F a-f)
        /// </summary>
        /// <param name="c">Char to test</param>
        /// <returns>True if hexadecimal</returns>
        private static bool isHex(char c)
        {
            return ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f'));
        }

        /// <summary>
        /// Do a HTTP request to get a webpage or text data from a server file
        /// </summary>
        /// <param name="url">URL of resource</param>
        /// <returns>Returns resource data if success, otherwise a WebException is raised</returns>
        private static string downloadString(string url)
        {
            HttpWebRequest myRequest = (HttpWebRequest) WebRequest.Create(url);
            myRequest.Method = "GET";
            WebResponse myResponse = myRequest.GetResponse();
            StreamReader sr = new StreamReader(myResponse.GetResponseStream(),
                Encoding.UTF8);
            string result = sr.ReadToEnd();
            sr.Close();
            myResponse.Close();
            return result;
        }
    }
}