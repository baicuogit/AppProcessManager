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
 * Description:        Utility
 * 
 * Created by:         Simon Gong
 * Created on:         December 25, 2013

 * Modified By         Date           Description 
 *  
 * 
******************************************************************************/
#endregion 


using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace SimonGong.AppProcessManage.ProcessControl
{
    internal static class Utility
    {
        /// <summary>
        /// Serializes to string.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns></returns>
        public static string SerializeToString(this object o)
        {
            if (o == null)
                return null;

            if (o is String)
                return (String)o;

            string objString = null;
            using (StringWriter wr = new StringWriter())
            {
                XmlSerializer serializer = new XmlSerializer(o.GetType());
                serializer.Serialize(wr, o);
                objString = wr.ToString();
            }

            return objString;
        }

        public static string ParseFilePath(this string filePath, out string fileName)
        {
            fileName = null;

            if (string.IsNullOrWhiteSpace(filePath))
                return null;

            fileName = Path.GetFileName(filePath);

            return Path.GetDirectoryName(filePath);
        }

        public static string AddArgument(this string args, string key, string val)
        {
            if (string.IsNullOrWhiteSpace(val))
                return args;

            string arg = args ?? string.Empty;

            string result = key + " " + val + " " + arg;

            return result.Trim();
        }

        public static AppProcess GetAppProcessByProcessID(this ConcurrentDictionary<string, AppProcess> dic, int processId)
        {
            if (dic == null)
                return null;

            foreach (var val in dic.Values)
            {
                if (val.ProcessID == processId)
                    return val;
            }

            return null;
        }

        public static AppProcess GetAppProcessByInstanceID(this ConcurrentDictionary<string, AppProcess> dic, int instanceId)
        {
            if (dic == null)
                return null;

            foreach (var val in dic.Values)
            {
                if (val.InstanceID == instanceId)
                    return val;
            }

            return null;
        }

        public static void CloseTimer(this Timer timer)
        {
            if (timer != null)
            {
                try
                {
                    timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                }
                catch { }

                try
                {
                    timer.Dispose();
                }
                catch { }

                timer = null;
            }
        }

        public static void StartTimer(this Timer timer, int interval)
        {
            if (timer != null)
            {
                try
                {
                    timer.Change(TimeSpan.FromSeconds(interval), TimeSpan.FromSeconds(interval));
                }
                catch { }
            }
        }

        public static void StopTimer(this Timer timer)
        {
            if (timer != null)
            {
                try
                {
                    timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                }
                catch { }
            }
        }
    }

    internal struct PROCESS_ARG_KEY
    {
        public const string PIPENAME = "-pipeName";
        public const string ID = "-id";
        public const string NAME = "-name";
        public const string LOG_CONFIG = "-logConfig";
        public const string AOTU_RESTART = "-autoRestart";
    }

}
