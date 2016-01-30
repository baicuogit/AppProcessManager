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
 * Description:        ProcessConfigurationManager
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

namespace SimonGong.AppProcessManage.ProcessControl.Configuration
{
    public static class ProcessConfigurationManager
    {
        public static Action<ProcessControlConfiguration> AppServerConfigurationChangeHandler;

        private static ConfigureFileWatcher configureAndWatchHandler;

        private static bool watchingLogFile = false;

        private static object locker = new object();
        public static ProcessControlConfiguration GetConfiguration(string filePath, bool? watch = null)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException(filePath);

            FileInfo fiInfo = new FileInfo(filePath);
            if (!File.Exists(fiInfo.FullName))
                throw new FileNotFoundException(filePath);

            ProcessControlConfiguration config = null;
            try
            {
                config = ConfigurationExtension.GetSection<ProcessControlConfiguration>(fiInfo);

                if (config == null)
                    return null;

                bool newWatchingFile = watch ?? ((bool)((config.ControllerSetting != null) ? config.ControllerSetting.WatchFile : false));

                SetupConfigureFileWatcher(newWatchingFile, fiInfo);

                return config;
            }
            catch
            {
                throw;
            }
        }

        public static bool EnableRaisingFileWatcherEvent
        {
            get
            {
                lock (locker)
                {
                    return watchingLogFile;
                }
            }
        }

        public static void ReleaseConfigureFileWatcher()
        {
            lock (locker)
            {
                if (configureAndWatchHandler != null)
                    configureAndWatchHandler.Dispose();
            }
        }

        private static void SetupConfigureFileWatcher(bool watch, FileInfo fi)
        {
            if (watch)
            {
                if (!watchingLogFile)
                {
                    if (configureAndWatchHandler != null)
                    {
                        configureAndWatchHandler.FileChangeEventHandler = null;
                        configureAndWatchHandler.Dispose();
                    }

                    configureAndWatchHandler = new ConfigureFileWatcher(fi);
                    configureAndWatchHandler.FileChangeEventHandler = HandleFileChangeEvent;
                }
            }
            else
            {
                if (watchingLogFile)
                {
                    if (configureAndWatchHandler != null)
                    {
                        configureAndWatchHandler.FileChangeEventHandler = null;
                        configureAndWatchHandler.Dispose();
                    }
                }
            }

            watchingLogFile = watch;
        }

        private static void HandleFileChangeEvent(FileInfo fi)
        {
            lock (locker)
            {
                if (AppServerConfigurationChangeHandler != null)
                {
                    try
                    {
                        ProcessControlConfiguration config = ConfigurationExtension.GetSection<ProcessControlConfiguration>(fi);

                        if (config == null)
                            return;

                        bool watch = (config.ControllerSetting != null) ? config.ControllerSetting.WatchFile : false;
                        SetupConfigureFileWatcher(watch, fi);

                        AppServerConfigurationChangeHandler.Invoke(config);
                    }
                    catch { }
                }
            }
        }
    }
}
