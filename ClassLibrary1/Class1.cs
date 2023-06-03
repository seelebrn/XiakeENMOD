using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Text.RegularExpressions;
using System.IO;
using System.Text;
using HarmonyLib;
using System.Reflection;
using System.Reflection.Emit;
using System.Globalization;
using BepInEx;
using BepInEx.Logging;
using Newtonsoft;
using Newtonsoft.Json.Linq;
using FMODUnity;
using UnityEngine.Assertions;
using GameApp;
using AssetBundles;
using XLua;
using XLua.LuaDLL;
using XLua.CSObjectWrap;
using System.Diagnostics;
using System.Collections;
using GCloud;
using WebSocketSharp;

namespace ClassLibrary1
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class Main : BaseUnityPlugin
    {

        public static ManualLogSource log = new ManualLogSource("EN"); 
        public const string pluginGuid = "Cadenza.IWOL.EnMod";
        public const string pluginName = "ENMod Continued";
        public const string pluginVersion = "0.5";
        public static bool enabled;
        public static bool enabledDebugLogging = false;
        public static Dictionary<string, string> translationDict;
        public static string sourceDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location).ToString();
        public static string parentDir = Directory.GetParent(Main.sourceDir).ToString();
        public static string configDir = Path.Combine(parentDir, "config");
        public static Dictionary<string, string> UILabelsDict;
        public static Dictionary<string, string> TextAssetDict;
        public static Dictionary<string, string> TextAssetDict1;
        public static Dictionary<string, string> TextAssetDict2;
        public static Dictionary<string, string> FungusSayDict;
        public static Dictionary<string, string> FungusMenuDict;
        public static Dictionary<string, string> FailedStringsDict;
        public static Dictionary<string, string> etcDict;


        public static List<string> missingta = new List<string>();
        public static List<string> missingtx = new List<string>();
        public static string dumppath = Path.Combine(BepInEx.Paths.PluginPath, "Dump");



        public static void AddFailedStringToDict(string s, string location)
        {


            if (FailedStringsDict.ContainsKey(s))
            {

                return;
            }
            FailedStringsDict.Add(s, location);

        }

        public static Dictionary<string, string> FileToDictionary(string dir)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();

            IEnumerable<string> lines = File.ReadLines(Path.Combine(sourceDir, "Translations", dir));

            foreach (string line in lines)
            {

                var arr = line.Split('¤');
                if (arr[0] != arr[1])
                {
                    var pair = new KeyValuePair<string, string>(Regex.Replace(arr[0], @"\t|\n|\r", ""), arr[1]);

                    if (!dict.ContainsKey(pair.Key))
                        dict.Add(pair.Key, pair.Value);
                    else
                    {

                    }
                        //Debug.Log($"Found a duplicated line while parsing {dir}: {pair.Key}");


                }

              

            }
            return dict;
        }



        public void Awake()
        {
            log = Logger;
            string FailedRegistry = Path.Combine(BepInEx.Paths.PluginPath, "MissedStrings.txt");
            using (FileStream fs = File.Open(FailedRegistry, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                lock (fs)
                {
                    fs.SetLength(0);
                }

            }
            try
            {
              

            }
            catch (Exception e)
            {
                //Debug.Log("Error in generating dicts");
                //Debug.LogException(e);
            }

            Main.log.LogInfo("Logger Online !");

            translationDict = FileToDictionary("KV.txt");
            translationDict = translationDict.MergeLeft(FileToDictionary("UIKV.txt"));

            //Dump.DumpUIKV();





            var harmony = new Harmony("Cadenza.IWOL.EnMod");
                Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
           




        }


       private void Update()
        {
         


            if (Input.GetKeyDown(KeyCode.F2))
            {
                if (File.Exists(Path.Combine(dumppath, "TAKV.txt")))
                {
                    File.Delete(Path.Combine(dumppath, "TAKV.txt"));
                }
                List<TextAsset> ta = Resources.LoadAll<TextAsset>("").ToList().ToList();

                foreach (var t in ta)
                {
                    var dq = "\"(.*?)\"";
                    var dq2 = "\'(.*?)\'";
                    foreach (var line in Regex.Matches(t.text, dq))
                    {
                        if (line != null && line != "")
                        {
                            if (Helpers.IsChinese(line.ToString()))
                            {
                                if (!translationDict.ContainsKey(line.ToString()))
                                {
                                    missingta.Add(line.ToString().Replace("\"", ""));


                                }
                               

                            }
                        }
                    }

                    foreach (var line in Regex.Matches(t.text, dq2))
                    {

                        if (line != null && line != "")
                        {
                            if (Helpers.IsChinese(line.ToString()))
                            {
                                if (!translationDict.ContainsKey(line.ToString()))
                                {
                                    missingta.Add(line.ToString().Replace("\'", ""));


                                }


                            }
                        }


                    }


                }
               
                using (StreamWriter sw = new StreamWriter(Path.Combine(dumppath, "TAKV.txt")))
                {
                    foreach (var s in missingta.Distinct())
                    {
                        sw.WriteLine(s);
                    }
                }
            }
            if(Input.GetKeyDown(KeyCode.F1))
            {
                AssetBundle ab = AssetBundle.LoadFromFile(Path.Combine(Application.dataPath, "resources.assets"));
                List<Text> tx = ab.LoadAllAssets<UnityEngine.UI.Text>().ToList();
                //List<Text> tx = Resources.FindObjectsOfTypeAll<UnityEngine.UI.Text>().ToList();


                foreach (var text in tx)
                {

                    Main.log.LogInfo("Tx ! : " + text.text);
                    if (!translationDict.ContainsKey(text.text.ToString()))
                    {
                        missingtx.Add(text.text.ToString());

                    }


                }
                if (File.Exists(Path.Combine(dumppath, "UIKV.txt")))
                {
                    File.Delete(Path.Combine(dumppath, "UIKV.txt"));
                }

                using (StreamWriter sw = new StreamWriter(Path.Combine(dumppath, "UIKV.txt")))
                {
                    foreach (var s in missingtx.Distinct())
                    {
                        sw.WriteLine(s.Replace("\n", "\\n").Replace("\r", "\\r"));
                    }
                }
            }
        }



    }

    [HarmonyPatch(typeof(XLua.LuaDLL.Lua), "lua_tostring")]
    static class Test_Patch
    {
        
        static void Postfix(Lua __instance, ref string __result)
        {
            if(!__result.IsNullOrEmpty())
            { 
            if (Helpers.IsChinese(__result))
            {
                
                Main.log.LogInfo(__result);
                if(Main.translationDict.ContainsKey(__result))
                {
                    __result = Main.translationDict[__result];
                }
                else
                    {
                        if(Main.translationDict.ContainsKey(__result.Replace("\n", "\\n").Replace("\r", "\\r")))
                        {
                            Main.log.LogInfo("__result : " + __result);
                            Main.log.LogInfo("Dict__result : " + Main.translationDict[__result.Replace("\n", "\\n").Replace("\r", "\\r")]);
                            __result = Main.translationDict[__result.Replace("\n", "\\n").Replace("\r", "\\r")].Replace("\\n", "\n").Replace("\\r", "\r");
                        }
                    }
               
            }
            }
        }
    }
    
        [HarmonyPatch(typeof(XLua.TemplateEngine.Chunk), "Text", MethodType.Getter)]
    static class Test_Patch2
    {

        static void Postfix(ref string __result)
        {
         
                Main.log.LogInfo("Text.text : " + __result);

        }
    }

    public static class DictionaryExtensions
    {
        // Works in C#3/VS2008:
        // Returns a new dictionary of this ... others merged leftward.
        // Keeps the type of 'this', which must be default-instantiable.
        // Example: 
        //   result = map.MergeLeft(other1, other2, ...)
        public static T MergeLeft<T, K, V>(this T me, params IDictionary<K, V>[] others)
            where T : IDictionary<K, V>, new()
        {
            T newMap = new T();
            foreach (IDictionary<K, V> src in
                (new List<IDictionary<K, V>> { me }).Concat(others))
            {
                // ^-- echk. Not quite there type-system.
                foreach (KeyValuePair<K, V> p in src)
                {
                    newMap[p.Key] = p.Value;
                }
            }
            return newMap;
        }

        public static Dictionary<TKey, TValue>
        Merge<TKey, TValue>(IEnumerable<Dictionary<TKey, TValue>> dictionaries)
        {
            var result = new Dictionary<TKey, TValue>(dictionaries.First().Comparer);
            foreach (var dict in dictionaries)
                foreach (var x in dict)
                    result[x.Key] = x.Value;
            return result;
        }

    }


    public static class Helpers
    {
        public static readonly Regex cjkCharRegex = new Regex(@"\p{IsCJKUnifiedIdeographs}");
        public static bool IsChinese(string s)
        {
            return cjkCharRegex.IsMatch(s);
        }
        public static string CustomEscape(string s)
        {
            return s.Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
        }
        public static string CustomUnescape(string s)
        {
            return s.Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\t", "\t");
        }
    }
}


