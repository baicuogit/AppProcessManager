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
 * Description:        ConfigureFileWatcher
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
    public sealed class ConfigureFileWatcher : IDisposable
    {
        private const int TimeoutMilliseconds = 500;
        private FileSystemWatcher fileSystemWatcher = null;
        private FileInfo configurationFile = null;

        private DateTime fileLastWriteTimeUtc;
        private object locker = new object();

        public Action<FileInfo> FileChangeEventHandler;

        [System.Security.SecuritySafeCritical]
        public ConfigureFileWatcher(FileInfo configFileInfo)
        {
            if (configFileInfo == null)
                throw new ArgumentNullException("configFileInfo");

            if (!File.Exists(configFileInfo.FullName))
                throw new FileNotFoundException(configFileInfo.FullName);

            this.configurationFile = configFileInfo;

            this.Init();
        }

        [System.Security.SecuritySafeCritical]
        public void Dispose()
        {
            fileSystemWatcher.EnableRaisingEvents = false;
            fileSystemWatcher.Dispose();
        }

        private void Init()
        {
            this.fileSystemWatcher = new FileSystemWatcher();
            
            this.fileSystemWatcher.Path = configurationFile.DirectoryName;
            this.fileSystemWatcher.Filter = configurationFile.Name;

            this.fileSystemWatcher.NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite | NotifyFilters.FileName;

            this.fileSystemWatcher.Changed += new FileSystemEventHandler(ConfigureAndWatchHandler_OnChanged);
            this.fileSystemWatcher.Created += new FileSystemEventHandler(ConfigureAndWatchHandler_OnChanged);
            this.fileSystemWatcher.Deleted += new FileSystemEventHandler(ConfigureAndWatchHandler_OnChanged);
            this.fileSystemWatcher.Renamed += new RenamedEventHandler(ConfigureAndWatchHandler_OnRenamed);

            this.fileSystemWatcher.EnableRaisingEvents = true;
        }

        private void ConfigureAndWatchHandler_OnChanged(object source, FileSystemEventArgs e)
        {
            lock (this.locker)
            {
                this.OnWatchedFileChange(this.configurationFile);
            }
        }

        private void ConfigureAndWatchHandler_OnRenamed(object source, RenamedEventArgs e)
        {
            lock (this.locker)
            {
                this.OnWatchedFileChange(this.configurationFile);
            }
        }

        private void OnWatchedFileChange(FileInfo state)
        {
            try
            {
                DateTime newTime = File.GetLastWriteTimeUtc(state.FullName);
                TimeSpan ts = newTime - this.fileLastWriteTimeUtc;
                if (ts.TotalMilliseconds > TimeoutMilliseconds)
                {
                    if (this.FileChangeEventHandler != null)
                    {
                        this.FileChangeEventHandler.Invoke(state);
                    }

                    this.fileLastWriteTimeUtc = newTime;
                }
            }
            catch { }
        }

    }
}
