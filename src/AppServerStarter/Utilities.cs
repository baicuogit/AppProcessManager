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
using System.IO;

namespace SimonGong.AppProcessManage.AppServerStarter
{
    internal struct APP_CONSTS
    {
        public const string SERVICE_NAME = "SERVICE_NAME";

        public const string LOGGING_CONFIG_FILENAME = "LOGGING_CONFIG_FILENAME";
        public const string APP_SERVERS_DATA_PATH = "APP_SERVERS_DATA_PATH";

        public const string DATA_CENTER_FLAG = "DATA_CENTER_FLAG";
        public const string USE_DB_CONFIG = "USE_DB_CONFIG";
        public const string CONFIGURATION_DB = "CONFIGURATION_DB";

        public const string SERVERS_CONFIG_FILENAME = "servers.config";
    }

    internal static class Utility
    {
        public static string GatFullPath(this string s)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, s);

            return path;
        }

        public static string ToArguments(this Tuple<int, string, string, string> tuple)
        {
            string arg = tuple.Item1.ToString() + " " + tuple.Item2 + " " + tuple.Item3 + " " + tuple.Item3;

            return arg;
        }
    }
}
