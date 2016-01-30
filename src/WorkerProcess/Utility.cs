#region License
//
// Copyright © 2013-2016  Simon Gong
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
//
/******************************************************************************
 * Description:        Utility.cs
 * 
 * Created by:         Simon Gong
 * Created on:         December 25, 2013

 * Modified By         Date           Description 
 *  
 * 
******************************************************************************/
#endregion 


using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;

namespace SimonGong.AppProcessManage.WorkerProcess
{
    internal static partial class StringExtension
    {
        /// <summary>
        /// Convert the key/value pairs in the string array into a key/value dictionary.
        /// </summary>
        /// <param name="args">Array of the key/value pairs</param>
        /// <param name="keyPrefixChars">Key prefix chars. Default "-". </param>
        /// <returns></returns>
        public static Dictionary<string, string> ParseToDictionary(this string[] args, string keyPrefixChars = "-", bool lowCaseKey = false)
        {
            if (args == null)
                return null;

            Dictionary<string, string> kv = new Dictionary<string, string>();

            string key = null;
            string val = string.Empty;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].StartsWith(keyPrefixChars))
                {
                    string keyValue = args[i];

                    if (lowCaseKey)
                        key = keyValue.ToLower();
                    else
                        key = keyValue;

                    kv[key] = string.Empty;

                    val = string.Empty;
                }
                else
                {
                    val = val + args[i];

                    if (key == null)
                        kv["NULL"] = val;
                    else
                        kv[key] = val;
                }
            }

            return kv;
        }

        public static string AddToString(this string[] args, string keyPrefixChars = "-")
        {
            if (args == null)
                return null;

            string s = string.Empty;
            for (int i = 0; i < args.Length; i++)
            {
                if (i == 0)
                {
                    s = args[i];
                }
                else
                {
                    s = s + " " + args[i];
                }
            }

            return s;
        }

        #region ParaseValue
        internal static T ParseValue<T>(string s)
        {
            Type t = typeof(T);

            if (t == typeof(string))
            {
                object objS = s;

                return (T)objS;
            }

            if (string.IsNullOrWhiteSpace(s))
                return default(T);

            object objVal = default(T);

            string val = s.Trim();

            if ((t == typeof(int)) || (t == typeof(int?)))
            {
                int num = 0;
                if (int.TryParse(val, out num))
                    objVal = num;
            }

            if ((t == typeof(long)) || (t == typeof(long?)))
            {
                long num = 0;
                if (long.TryParse(val, out num))
                    objVal = num;
            }

            if ((t == typeof(decimal)) || (t == typeof(decimal?)))
            {
                decimal num = 0;
                if (decimal.TryParse(val, out num))
                    objVal = num;
            }

            if ((t == typeof(bool)) || (t == typeof(bool?)))
            {
                bool bl = false;
                if (bool.TryParse(val, out bl))
                    objVal = bl;
            }

            return (T)objVal;
        }
        #endregion
    }

    internal static partial class DictionaryExtension
    {
        public static string GetValue(this Dictionary<string, string> dic, string key, string defaultValue = null)
        {
            if ((dic == null) || (dic.Count == 0))
                return null;

            if (!dic.ContainsKey(key))
                return null;

            if (dic[key] == null)
                return null;

            string val = dic[key];

            return val;
        }

        public static T GetValue<T>(this Dictionary<string, string> dic, string key, string defaultValue = null)
        {
            string s = dic.GetValue(key, defaultValue);
            if (s == null)
                return default(T);

            T val = StringExtension.ParseValue<T>(s);

            return val;
        }

        public static TValue GetValue<TKey, TValue>(this Dictionary<TKey, TValue> dic, TKey key, TValue defaultValue = default(TValue))
        {
            if ((dic == null) || (dic.Count == 0))
                return defaultValue;

            if (!dic.ContainsKey(key))
                return defaultValue;

            if (dic[key] == null)
                return defaultValue;

            return dic[key];
        }
    }

    internal static partial class ExceptionExtension
    {
        public static Exception InmostException(this Exception e)
        {
            if (e == null)
                return e;

            Exception ie = e;
            while (ie.InnerException != null)
                ie = ie.InnerException;

            return ie;
        }

    }

    internal static class ObjectCreator
    {
        /// <summary>
        /// A simple object factory to create an instance of object from assembly name and type name.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="assemblyName">Name of the assembly.</param>
        /// <param name="typeName">Name of the type.</param>
        /// <param name="constructorParameters">The constructor parameters.</param>
        /// <returns></returns>
        public static T CreateInstance<T>(string assemblyName, string typeName, params object[] constructorParameters)
        {
            Type type = TypeLoader.LoadType(typeName, assemblyName);

            var messaging = default(T);
            if ((constructorParameters == null) || (constructorParameters.Length == 0))
                messaging = (T)Activator.CreateInstance(type);
            else
                messaging = (T)Activator.CreateInstance(type, constructorParameters);

            return (T)messaging;
        }

        /// <summary>
        /// A simple object factory to create an instance of object from the type and assembly string.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="typeAssemblyName">Name of the type and assembly.</param>
        /// <param name="constructorParameters">The constructor parameters.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">typeAssemblyName</exception>
        public static T CreateInstance<T>(string typeAssemblyName, params object[] constructorParameters)
        {
            Type type = TypeLoader.LoadType(typeAssemblyName);

            var messaging = default(T);
            if ((constructorParameters == null) || (constructorParameters.Length == 0))
                messaging = (T)Activator.CreateInstance(type);
            else
                messaging = (T)Activator.CreateInstance(type, constructorParameters);

            return (T)messaging;
        }
    }

    internal static class TypeLoader
    {
        /// <summary>
        /// Loads the type.
        /// </summary>
        /// <param name="typeName">Name of the type.</param>
        /// <param name="assemblyName">Name of the assembly.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">
        /// assemblyName
        /// or
        /// typeName
        /// </exception>
        /// <exception cref="System.IO.FileNotFoundException"></exception>
        public static Type LoadType(string typeName, string assemblyName)
        {
            if (string.IsNullOrWhiteSpace(assemblyName))
                throw new ArgumentNullException("assemblyName");
            if (string.IsNullOrWhiteSpace(typeName))
                throw new ArgumentNullException("typeName");

            if ((!assemblyName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)) &&
                (!assemblyName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)))
                assemblyName = assemblyName + ".dll";

            string dir = AppDomain.CurrentDomain.BaseDirectory;
            string asmFile1 = Path.Combine(dir, assemblyName);
            string asmFile2 = Path.Combine(dir, "bin", assemblyName);

            string asmFile = asmFile1;
            if (!File.Exists(asmFile))
                asmFile = asmFile2;

            if (!File.Exists(asmFile))
                throw new FileNotFoundException(asmFile);

            Assembly assembly = Assembly.LoadFrom(asmFile);
            Type type = assembly.GetType(typeName, true);

            return type;
        }

        /// <summary>
        /// Loads the type.
        /// </summary>
        /// <param name="typeAssemblyName">Name of the type assembly.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">
        /// typeAssemblyName
        /// or
        /// Wrong format of type and assembly string.
        /// </exception>
        public static Type LoadType(string typeAssemblyName)
        {
            if (string.IsNullOrWhiteSpace(typeAssemblyName))
                throw new ArgumentNullException("typeAssemblyName");

            string[] typeAsm = typeAssemblyName.Split(new char[] { ',' });

            if (typeAsm.Length < 2)
                throw new ArgumentNullException("Wrong format of type and assembly string.");

            string typeName = typeAsm[0].Trim();
            string assemblyName = typeAsm[1].Trim();

            return LoadType(typeName, assemblyName);
        }
    }
}
