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
 * Description:        AppProcessController
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
using System.Collections.Concurrent;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Threading;

using SimonGong.AppProcessManage.PipeChannel;
using SimonGong.AppProcessManage.ProcessControl.Configuration;

namespace SimonGong.AppProcessManage.ProcessControl
{
    public static class AppProcessController
    {
        private static log4net.ILog logger = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static Timer monitorTimerTask = null;

        private static ProcessControlConfiguration currentAppServerConfiguration = null;
        private static ConcurrentDictionary<string, AppProcess> appAppProcesses = new ConcurrentDictionary<string, AppProcess>();

        private static string serverConfigFilePath = null;
        private static bool watchingFile = false;
        private static bool monitoring = false;
        private static int monitorTimerInterval = 60;

        private static object eventLocker = new object();

        public static void SetupAppProcesses(string serverConfigFile)
        {
            if (string.IsNullOrWhiteSpace(serverConfigFile))
                throw new ArgumentNullException(serverConfigFile);

            serverConfigFilePath = serverConfigFile;

            // Load Controller Configuration 
            currentAppServerConfiguration = ProcessConfigurationManager.GetConfiguration(serverConfigFile);
            if (currentAppServerConfiguration == null)
                throw new ConfigurationErrorsException(serverConfigFile);

            var watch = ProcessConfigurationManager.EnableRaisingFileWatcherEvent;
            var monitor = currentAppServerConfiguration.MonitorProcess();
            var interval = currentAppServerConfiguration.MonitorTimeInterval();

            SetUpController(watch, monitor, interval);

            appAppProcesses = SetupAppProcesses(currentAppServerConfiguration);
        }

        private static void SetUpController(bool watch, bool monitor, int interval)
        {
            if (watch)
            {
                if (!watchingFile)
                    ProcessConfigurationManager.AppServerConfigurationChangeHandler = HandleAppServerConfigurationUpdate;
            }
            else
            {
                if (watchingFile)
                    ProcessConfigurationManager.AppServerConfigurationChangeHandler = null;
            }

            watchingFile = watch;

            if (monitor)
            {
                if (!monitoring)
                {
                    monitorTimerTask.CloseTimer();

                    monitorTimerTask = new Timer(new TimerCallback(TimerTaskHandler), null, TimeSpan.FromSeconds(interval), TimeSpan.FromSeconds(interval));
                }
            }
            else
            {
                if (monitoring)
                {
                    monitorTimerTask.CloseTimer();
                }
            }

            monitoring = monitor;
            monitorTimerInterval = interval;
        }

        public static void TimerTaskHandler(object state)
        {
            lock (monitorTimerTask)
            {
                var updatedConfig = ProcessConfigurationManager.GetConfiguration(serverConfigFilePath);

                //logger.DebugFormat("Received the updated ProcessControlConfiguration from the monitor timer event. {0}", updatedConfig.SerializeToString());
                logger.DebugFormat("Loaded the ProcessControlConfiguration from the file.");

                var watch = ProcessConfigurationManager.EnableRaisingFileWatcherEvent;
                var monitor = updatedConfig.MonitorProcess();
                var interval = updatedConfig.MonitorTimeInterval();

                //SetUpController(watch, monitor, interval);

                ManageAppServers(updatedConfig);

                LogProcessStatus();
            }
        }

        private static void LogProcessStatus()
        {
            foreach(var pro in appAppProcesses.Values)
            {
                logger.DebugFormat("Process status: IsRunning - {5}, InstanceID - {0}, ProcessID - {1}, ProcessGuid - {2}, AppServerName - {3}, WorkingDirectory - {4}.", 
                    pro.InstanceID, pro.ProcessID, pro.ProcessGuid, pro.AppServerName, pro.WorkingDirectory, pro.IsRunning);

            }
        }

        private static void HandleAppServerConfigurationUpdate(ProcessControlConfiguration updatedConfig)
        {
            lock (eventLocker)
            {
                logger.DebugFormat("Received the updated ProcessControlConfiguration from the file change event. {0}", updatedConfig.SerializeToString());

                if ((monitorTimerTask != null) && (monitoring))
                {
                    monitorTimerTask.StopTimer();
                }

                var watch = ProcessConfigurationManager.EnableRaisingFileWatcherEvent;
                var monitor = updatedConfig.MonitorProcess();
                var interval = updatedConfig.MonitorTimeInterval();

                SetUpController(watch, monitor, interval);

                ManageAppServers(updatedConfig);

                if ((monitorTimerTask != null) && (monitoring))
                {
                    monitorTimerTask.StartTimer(interval);
                }
            }
        }

        private static ConcurrentDictionary<string, AppProcess> SetupAppProcesses(ProcessControlConfiguration serverConfig)
        {
            var appServers = new ConcurrentDictionary<string, AppProcess>();

            foreach (var val in serverConfig.ProcessInstances)
            {
                AppProcess module = SetupAppProcess(val);
                if (module != null)
                {
                    string guid = module.ProcessGuid;

                    appServers.TryAdd(guid, module);
                }
            }

            logger.DebugFormat("Setup [{0}] worker processes.", appServers.Count());

            return appServers;
        }

        private static AppProcess SetupAppProcess(ProcessInstance val)
        {
            if (!string.Equals(val.Command.ToUpper(), CommandMessage.START))
            {
                return null;
            }

            string args = val.ProcessArgs.
                AddArgument(PROCESS_ARG_KEY.LOG_CONFIG, val.LogConfigFilePath).
                AddArgument(PROCESS_ARG_KEY.AOTU_RESTART, val.AutoRestart.ToString()).
                AddArgument(PROCESS_ARG_KEY.NAME, val.Name).
                AddArgument(PROCESS_ARG_KEY.ID, val.InstanceID.ToString());

            AppProcess module = new AppProcess(val.InstanceID, val.Name,
                val.WorkProcessPath, args, val.AutoRestart);

            logger.DebugFormat("Setup worker processes for the setting.  {0}", val.SerializeToString());

            return module;
        }

        private static void ManageAppServers(ProcessControlConfiguration updatedConfig)
        {
            foreach (var val in updatedConfig.ProcessInstances)
            {
                var module = appAppProcesses.GetAppProcessByInstanceID(val.InstanceID);
                if (module == null)   // New ProcessInstance
                {
                    logger.DebugFormat("Found the mew worker process setting with the InstanceID - {0} and Command - {1}",
                        val.InstanceID, val.Command);

                    if (! string.Equals(val.Command.ToUpper(), CommandMessage.STOP))
                    {
                        var newmodule = SetupAppProcess(val);
                        string guid = newmodule.ProcessGuid;
                        appAppProcesses.TryAdd(guid, newmodule);

                        logger.DebugFormat("Starting the new worker process with the InstanceID - {0}", val.InstanceID);

                        StartAppProcess(guid);
                    }
                }
                else // Existing ProcessInstance
                {
                    string guid = module.ProcessGuid;

                    if (module.IsRunning)
                    {
                        if (string.Equals(val.Command.ToUpper(), CommandMessage.STOP))
                        {
                            logger.DebugFormat("Stopping the existing worker process with the InstanceID - {0}", val.InstanceID);

                            StopAppProcess(guid);
                        }

                        if (string.Equals(val.Command.ToUpper(), CommandMessage.RECONFIGURE))
                        {
                            logger.DebugFormat("Reconfiguring the existing worker process with the InstanceID - {0}", val.InstanceID);

                            ReconfigureAppProcess(guid);
                        }
                    }
                    else
                    {
                        if (string.Equals(val.Command.ToUpper(), CommandMessage.START))
                        {
                            logger.DebugFormat("Starting the existing worker process with the InstanceID - {0}", val.InstanceID);

                            StartAppProcess(guid);
                        }
                    }
                }
            }

            List<string> removeList = new List<string>();
            foreach (var val in appAppProcesses.Values)
            {
                var setting = updatedConfig.ProcessInstances.Get(val.InstanceID);

                if (setting == null)
                    removeList.Add(val.ProcessGuid);
            }

            foreach(var val in removeList)
            {
                StopAppProcess(val);

                logger.DebugFormat("Removed the worker process not in the updated AppServerConfiguration. ");
            }
        }

        public static void AddAppProcess(AppProcess AppProcess)
        {
            string guid = AppProcess.ProcessGuid;
            appAppProcesses.TryAdd(guid, AppProcess);
        }

        public static void StartAppProcesses()
        {
            foreach (var module in appAppProcesses.Values)
            {
                try
                {
                    module.Start();
                }
                catch (Exception ex)
                {
                    logger.ErrorFormat(ex.Message);
                }
            }

            logger.DebugFormat("Started [{0}] ServerPocesses.", appAppProcesses.Count());

            if (monitorTimerTask != null)
            {
                monitorTimerTask.StartTimer(monitorTimerInterval);

                logger.DebugFormat("Started monitoring.");
            }
        }

        public static int StartAppProcess(int processId)
        {
            var module = appAppProcesses.GetAppProcessByProcessID(processId);
            if (module != null)
            {
                logger.DebugFormat("Starting the AppProcess and return ProcessID - [{0}].", processId);

                try
                {
                    return module.Start();
                }
                catch (Exception ex)
                {
                    logger.ErrorFormat(ex.Message);

                    return -1;
                }
            }
            else
            {
                logger.DebugFormat("Not found the AppProcess with the ProcessID - [{0}].", processId);

                return -1;
            }
        }

        public static int StartAppProcess(string guid)
        {
            var module = appAppProcesses[guid];
            if (module != null)
            {
                logger.DebugFormat("Starting the AppProcess with the ProcessGuid - [{0}].", guid);

                try
                {
                    return module.Start();
                }
                catch (Exception ex)
                {
                    logger.ErrorFormat(ex.Message);

                    return -1;
                }
            }
            else
            {
                logger.DebugFormat("Not found the AppProcess with the ProcessGuid - [{0}].", guid);

                return -1;
            }
        }

        public static void StopAppProcesses(bool dispose = true, bool clean = true)
        {
            monitorTimerTask.CloseTimer();
            logger.DebugFormat("Stopped and closed the monitor timer.");

            foreach (var module in appAppProcesses.Values)
            {
                try
                {
                    module.Stop();
                }
                catch (Exception ex)
                {
                    logger.ErrorFormat(ex.Message);
                }
                finally
                {
                    if (dispose)
                    {
                        module.Dispose();
                    }
                }
            }

            logger.DebugFormat("Stopped [{0}] ServerPocesses.", appAppProcesses.Count());

            if (watchingFile)
            {
                ProcessConfigurationManager.AppServerConfigurationChangeHandler = null;
                ProcessConfigurationManager.ReleaseConfigureFileWatcher();
            }

            if (clean)
            {
                try
                {
                    appAppProcesses.Clear();
                }
                catch { }
            }
        }

        public static void StopAppProcess(int processId, bool dispose = true)
        {
            var module = appAppProcesses.GetAppProcessByProcessID(processId);
            if (module != null)
            {
                logger.DebugFormat("Stopping the AppProcess and return ProcessID - [{0}].", processId);

                try
                {
                    module.Stop();
                }
                catch (Exception ex)
                {
                    logger.ErrorFormat(ex.Message);
                }
                finally
                {
                    if (dispose)
                    {
                        module.Dispose();
                    }
                }
            }
            else
            {
                logger.DebugFormat("Not found the AppProcess with the ProcessID - [{0}].", processId);
            }
        }

        public static void StopAppProcess(string guid, bool dispose = true)
        {
            var module = appAppProcesses[guid];
            if (module != null)
            {
                logger.DebugFormat("Stopping the AppProcess with the ProcessGuid - [{0}].", guid);

                try
                {
                    module.Stop();
                }
                catch (Exception ex)
                {
                    logger.ErrorFormat(ex.Message);
                }
                finally
                {
                    if (dispose)
                    {
                        appAppProcesses.TryRemove(guid, out module);
                        module.Dispose();
                    }
                }
            }
            else
            {
                logger.DebugFormat("Not found the AppProcess with the ProcessGuid - [{0}].", guid);
            }
        }

        public static void ReconfigAppProcesses()
        {
            foreach (var module in appAppProcesses.Values)
            {
                try
                {
                    module.Reconfigure();
                }
                catch (Exception ex)
                {
                    logger.ErrorFormat(ex.Message);
                }
            }

            logger.DebugFormat("Reconfigured [{0}] ServerPocesses.", appAppProcesses.Count());
        }

        public static void ReconfigureAppProcess(int processId)
        {
            var module = appAppProcesses.GetAppProcessByProcessID(processId);
            if (module != null)
            {
                logger.DebugFormat("Starting the AppProcess and return ProcessID - [{0}].", processId);

                try
                {
                    module.Reconfigure();
                }
                catch (Exception ex)
                {
                    logger.ErrorFormat(ex.Message);
                }
            }
            else
            {
                logger.DebugFormat("Not found the AppProcess with the ProcessID - [{0}].", processId);
            }
        }

        public static void ReconfigureAppProcess(string guid)
        {
            var module = appAppProcesses[guid];
            if (module != null)
            {
                logger.DebugFormat("Starting the AppProcess and return ProcessGuid - [{0}].", guid);

                try
                {
                    module.Reconfigure();
                }
                catch (Exception ex)
                {
                    logger.ErrorFormat(ex.Message);
                }
            }
            else
            {
                logger.DebugFormat("Not found the AppProcess with the ProcessGuid - [{0}].", guid);
            }
        }

        public static void ReconfigureAppProcessWithServerName(string serverName)
        {
            foreach(var val in appAppProcesses.Values)
            {
                if (val.AppServerName == serverName)
                {
                    try
                    {
                        val.Reconfigure();
                    }
                    catch (Exception ex)
                    {
                        logger.ErrorFormat(ex.Message);
                    }
                }
            }
        }

        public static ConcurrentDictionary<string, AppProcess> AppServers
        {
            get
            {
                return appAppProcesses;
            }
        }
    }
}
