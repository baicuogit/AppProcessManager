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
 * Description:        WindowsService
 * 
 * Created by:         Simon Gong
 * Created on:         December 25, 2013

 * Modified By         Date           Description 
 *  
 * 
******************************************************************************/
#endregion 


using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.ServiceProcess;
using System.IO;
using System.Collections.Generic;

using log4net;
using log4net.Config;

using SimonGong.AppProcessManage.ProcessControl;

namespace SimonGong.AppProcessManage.AppServerStarter
{
    public partial class WindowsService : ServiceBase
    {
        private ILog logger = null;
        private string loggingID = null;

        private string serviceName = String.Empty;

        public WindowsService()
        {
            InitializeComponent();
        }

        #region ServiceBase Methods
        protected override void OnStart(string[] args)
        {
            try
            {
                EventLog.WriteEntry("Instantiating the application service module ...", EventLogEntryType.Information);

                string server_data_path = ConfigurationManager.AppSettings[APP_CONSTS.APP_SERVERS_DATA_PATH];

                AppProcessController.SetupAppProcesses(server_data_path);
                AppProcessController.StartAppProcesses();

                this.logger.InfoFormat(this.loggingID + "Started the application service module.");
                EventLog.WriteEntry("Started the application service module.", EventLogEntryType.Information);

                string msg = string.Format(SUCCESSFUL_ACTION_MSG, "START", this.serviceName);
                EventLog.WriteEntry(msg, EventLogEntryType.Information);
            }
            catch (Exception excp)
            {
                string msg = String.Format(MAIN_OPERATION_EXCP_MSG, "START",
                  this.serviceName, Environment.MachineName, excp.Message, excp.StackTrace);

                EventLog.WriteEntry(msg, EventLogEntryType.Error);

                throw excp;
            }
        }

        protected override void OnContinue()
        {
            try
            {
                string msg = string.Format(SUCCESSFUL_ACTION_MSG, "CONTINUE", this.serviceName);
                EventLog.WriteEntry(msg, EventLogEntryType.Information);
            }
            catch (Exception excp)
            {
                string msg = String.Format(MAIN_OPERATION_EXCP_MSG, "CONTINUE",
                  this.serviceName, Environment.MachineName, excp.Message, excp.StackTrace);

                EventLog.WriteEntry(msg, EventLogEntryType.Error);
            }
        }

        protected override void OnPause()
        {
            try
            {
                string msg = string.Format(SUCCESSFUL_ACTION_MSG, "PAUSE", this.serviceName);
                EventLog.WriteEntry(msg, EventLogEntryType.Information);
            }
            catch (Exception excp)
            {
                string msg = String.Format(MAIN_OPERATION_EXCP_MSG, "PAUSE",
                  this.serviceName, Environment.MachineName, excp.Message, excp.StackTrace);

                EventLog.WriteEntry(msg, EventLogEntryType.Error);
            }
        }

        protected override void OnStop()
        {
            try
            {
                AppProcessController.StopAppProcesses();

                string msg = string.Format(SUCCESSFUL_ACTION_MSG, "STOP", this.serviceName);
                EventLog.WriteEntry(msg, EventLogEntryType.Information);
            }
            catch (Exception excp)
            {
                string msg = String.Format(MAIN_OPERATION_EXCP_MSG, "STOP",
                  this.serviceName, Environment.MachineName, excp.Message, excp.StackTrace);

                EventLog.WriteEntry(msg, EventLogEntryType.Error);
            }
        }

        protected override void OnShutdown()
        {
            string msg = "The system is shutting down, stopping the service ...";
            EventLog.WriteEntry(msg, EventLogEntryType.Warning);

            try
            {

            }
            catch { }

            msg = "Successfully STOP " + this.serviceName + " for the system's shutting down.";
            EventLog.WriteEntry(msg, EventLogEntryType.Warning);
        }
        #endregion ServiceBase Methods

        #region Called by Main
        private void SetServiceName()
        {
            try
            {
                NameValueCollection appsettings = ConfigurationManager.AppSettings;
                if (null == appsettings)
                    throw new ConfigurationErrorsException(APPSETTINGS_MISSING);

                this.serviceName = appsettings[APP_CONSTS.SERVICE_NAME];
                if (string.IsNullOrEmpty(this.serviceName))
                    throw new ConfigurationErrorsException(SERVICE_NAME_MISSING_MSG);

                base.ServiceName = this.serviceName;
            }
            catch (Exception excp)
            {
                string msg = String.Format("Caught Exception on SetServiceName. {0}, {1} {2}",
                    Environment.MachineName, excp.Message, excp.StackTrace);

                string sSource = string.IsNullOrEmpty(this.serviceName) ? DEFAULT_SERVICE_NAME : this.serviceName;

                if (!EventLog.SourceExists(sSource))
                    EventLog.CreateEventSource(sSource, "Application");

                EventLog.WriteEntry(sSource, msg, EventLogEntryType.Error);

                throw excp;
            }
        }

        private void SetLoggingConfiguration()
        {
            try
            {
                this.loggingID = string.Format("{0} -- ", this.serviceName);

                string logConfigFile = ConfigurationManager.AppSettings[APP_CONSTS.LOGGING_CONFIG_FILENAME];
                if (!string.IsNullOrWhiteSpace(logConfigFile))
                {
                    string logConfigFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, logConfigFile);

                    if (!File.Exists(logConfigFilePath))
                        throw new ConfigurationErrorsException(string.Format(LOGCONFIG_FILE_MISSING, logConfigFilePath));

                    FileInfo fi = new FileInfo(logConfigFilePath);

                    XmlConfigurator.ConfigureAndWatch(fi);
                }
                else
                {
                    XmlConfigurator.Configure();
                }

                this.logger = LogManager.GetLogger(typeof(WindowsService));

                this.logger.InfoFormat(this.loggingID + "The server logging configuration is setup and the server is starting ...");
            }
            catch (Exception excp)
            {
                string msg = String.Format("Caught Exception on SetLoggingConfiguration. {0}, {1} {2}",
                    Environment.MachineName, excp.Message, excp.StackTrace);

                string sSource = string.IsNullOrEmpty(this.serviceName) ? DEFAULT_SERVICE_NAME : this.serviceName;

                if (!EventLog.SourceExists(sSource))
                    EventLog.CreateEventSource(sSource, "Application");

                EventLog.WriteEntry(sSource, msg, EventLogEntryType.Error);

                throw excp;
            }
        }
        #endregion Called by Main

        #region Private Methods
        private string ServerDataPath
        {
            get
            {
                string dataPath = ConfigurationManager.AppSettings[APP_CONSTS.APP_SERVERS_DATA_PATH];

                return dataPath;
            }
        }

        private bool IsDependencyRunning(string dependencyService)
        {
            if (string.IsNullOrEmpty(dependencyService))
                return true;

            string dependency = dependencyService.Trim();
            if (string.IsNullOrEmpty(dependency))
                return true;

            bool isRunning = false;

            string[] svcDependency = dependency.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string svc in svcDependency)
            {
                try
                {
                    ServiceController sc = new ServiceController(svc.Trim());
                    isRunning = (sc.Status == ServiceControllerStatus.Running);

                    if (!isRunning)
                        break;
                }
                catch
                {
                    isRunning = false;

                    break;
                }
            }

            return isRunning;
        }
        #endregion Private Methods

        #region Error Message
        private const string DEFAULT_SERVICE_NAME = "SimonGong.AppProcessManage.AppServerStarter";

        private const string APPSETTINGS_MISSING =
            "The required 'appSettings' was NOT found in the .config file.";

        private const string SERVICE_NAME_MISSING_MSG =
            "The required SERVICE_NAME attribute was NOT found in the 'appSettings' section of the .config file.";

        private const string LOGCONFIG_FILE_MISSING =
            "The logging configuration file {0} provided in LOGGING_CONFIG_FILENAME attribute was NOT found in the application's working directory.";

        private const string SUCCESSFUL_ACTION_MSG = "Successfully {0} {1}.";

        private const string MAIN_OPERATION_EXCP_MSG = 
            "An exception occurred while attempting to {0} the {1} Service on {2}. Exception message: {3}  Stack trace: {4}";
        #endregion Error Message
    }
}
