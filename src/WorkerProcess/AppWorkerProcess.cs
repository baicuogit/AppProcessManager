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
 * Description:        AppWorkerProcess.cs
 * 
 * Created by:         Simon Gong
 * Created on:         December 25, 2013

 * Modified By         Date           Description 
 *  
 * 
******************************************************************************/
#endregion 


using System;
using System.Configuration;
using System.Diagnostics;
using System.Xml;
using System.Reflection;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using log4net;
using log4net.Config;

using SimonGong.AppProcessManage.PipeChannel;
using SimonGong.AppProcessManage.AppServerInterface;

namespace SimonGong.AppProcessManage.WorkerProcess
{
    public class AppWorkerProcess
    {
        private IAppServer appServerImpl = null;
        private PipeServerChannel pipeServerChannel = null;

        static AppWorkerProcess Program = new AppWorkerProcess();
        static ILog Logger = null;
        static int? InstanceID = null;
        static string AppServerName = null;
        static int ProcessID = -1;
        static string SessionGuid = null;
        static string WorkingDirectory = null;
        static Task MainTask = null;
        static CancellationTokenSource MainTaskSource = new CancellationTokenSource();

        #region Entry point
        public static int Main(string[] args)
        {
            int exitCode = 0;
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                ProcessID = Process.GetCurrentProcess().Id;

                Program.Init(args);

                Logger.InfoFormat("Initialized the AppAppProcess. " + 
                    "WorkProcessID - {0}, SessionGuid - {1} InstanceID - {2}, AppServerName - {3}, WorkingDirectory - {4}, BaseDirectory - {5}",
                    ProcessID, SessionGuid, InstanceID, AppServerName, WorkingDirectory, baseDir);

                Program.appServerImpl = Program.InitializeAppServer();

                Logger.InfoFormat("AppProcess starting. ProcessID - {0}, SessionGuid - {1}, InstanceID - {2}", ProcessID, SessionGuid, InstanceID);

                MainTask = Task.Factory.StartNew(() => { Program.Start(); Thread.Sleep(Timeout.Infinite); }, MainTaskSource.Token);

                MainTask.Wait(MainTaskSource.Token);

                Logger.InfoFormat("AppProcess exits. ProcessID - {0}, SessionGuid - {1}, InstanceID - {2}", ProcessID, SessionGuid, InstanceID);
            }
            catch (OperationCanceledException)
            {
                Logger.InfoFormat("AppProcess aborted. ProcessID - {0}, SessionGuid - {1}, InstanceID - {2}", ProcessID, SessionGuid, InstanceID);

                exitCode = 0;
            }
            catch (Exception e)
            {
                string msg = e.Message;

                exitCode = -1;
            }
            finally
            {
                if (Program != null)
                    Program.Close();

                try
                {
                    MainTaskSource.Dispose();
                }
                catch { }
            }

            return exitCode;
        }
        #endregion

        #region Instance members
        private void Close()
        {
            if (this.appServerImpl != null)
            {
                try
                {
                    this.appServerImpl.Close();
                }
                catch { }

                try
                {
                    this.appServerImpl.Dispose();
                }
                catch { }
            }

            if (this.pipeServerChannel != null)
            {
                try
                {
                    this.pipeServerChannel.Close();
                }
                catch { }

                try
                {
                    this.pipeServerChannel.Dispose();
                }
                catch { }
            }
        }

        private void Start()
        {
            if (appServerImpl != null)
                appServerImpl.Start();
        }

        private void Reconfigure()
        {
            if (appServerImpl != null)
                appServerImpl.Reconfigure();
        }

        private void AbortProcess()
        {
            if (this.appServerImpl != null)
                appServerImpl.Stop();

            MainTaskSource.Cancel();
        }

        private void Init(string[] args)
        {
            WorkingDirectory = Environment.CurrentDirectory;
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(AssemblyResolveEvent);

            var kv = args.ParseToDictionary();
            InstanceID = kv.GetValue<int?>(PROCESS_ARG_KEY.ID);
            AppServerName = kv.GetValue(PROCESS_ARG_KEY.NAME);

            string logConfig = kv.GetValue(PROCESS_ARG_KEY.LOG_CONFIG);
            Logger = this.ConfigureLogging(logConfig);

            Logger.InfoFormat("Receiving the arguments: {0}", args.AddToString());

            SessionGuid = kv.GetValue(PROCESS_ARG_KEY.PIPENAME);

            this.SetupServerChannel(SessionGuid, InstanceID, AppServerName);
        }

        private ILog ConfigureLogging(string logConfigFile)
        {
            if (string.IsNullOrWhiteSpace(logConfigFile))
                logConfigFile = ConfigurationManager.AppSettings[CONFIG_KEY.LOGGING_CONFIG_FILENAME];

            if (!string.IsNullOrWhiteSpace(logConfigFile))
            {
                string logConfigFilePath = Path.Combine(WorkingDirectory, logConfigFile);
                FileInfo fi = new FileInfo(logConfigFilePath);
                if (fi.Exists)
                {
                    try
                    {
                        XmlConfigurator.ConfigureAndWatch(fi);
                    }
                    catch { }
                }
                else
                {
                    try
                    {
                        XmlConfigurator.Configure();
                    }
                    catch { }
                }
            }
            else
            {
                try
                {
                    XmlConfigurator.Configure();
                }
                catch { }
            }

            var log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

            return log;
        }

        private void SetupServerChannel(string pipeName, int? id, string name)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(pipeName))
                {
                    this.pipeServerChannel = new PipeServerChannel(pipeName);
                    this.pipeServerChannel.StartListening();

                    this.pipeServerChannel.ReceiveResponseEventHandler += this.HandleNotification;

                    Logger.InfoFormat("Setup the PipeServerChannel with the pipe name - {0}", pipeName);
                }
                else
                {
                    if ((id != null) && (!string.IsNullOrWhiteSpace(name)))
                    {
                        string pn = name + id.Value.ToString();
                        this.pipeServerChannel = new PipeServerChannel(pn);
                        this.pipeServerChannel.StartListening();

                        this.pipeServerChannel.ReceiveResponseEventHandler += this.HandleNotification;

                        Logger.InfoFormat("Setup the PipeServerChannel with the pipe name - {0}", pn);
                    }
                }
            }
            catch { }
        }

        private string HandleNotification(string message)
        {
            Logger.InfoFormat("Received the notification command - [{3}]. ProcessID - {0}, SessionGuid - {1}, InstanceID - {2}.",
                ProcessID, SessionGuid, InstanceID, message);

            if (string.Equals(message, CommandMessage.RECONFIGURE))
            {
                this.Reconfigure();

                return CommandMessage.RECONFIGURE;
            }

            if (string.Equals(message, CommandMessage.STOP))
            {
                this.AbortProcess();

                return CommandMessage.EXIT;
            }

            return null;
        }

        private IAppServer InitializeAppServer()
        {
            string appServerModule = ConfigurationManager.AppSettings[CONFIG_KEY.SERVICE_MODULE];
            IAppServer appServer = null;

            try
            {
                appServer = ObjectCreator.CreateInstance<IAppServer>(appServerModule);
            }
            catch(Exception e)
            {
                var ie = e.InmostException();

                string error = string.Format("Exception while instancing the object of type implementing IAppServer. {0}  {1}",
                    ie.GetType().Name, ie.Message);

                Logger.ErrorFormat("ProcessID - {0}, SessionGuid - {1}, InstanceID - {2}, AppServerName - {3}.  {4}",
                    ProcessID, SessionGuid, InstanceID, AppServerName, error);

                throw new NullReferenceException(error);
            }

            if (appServer != null)
            {
                string dbConnection = ConfigurationManager.AppSettings[CONFIG_KEY.CONFIGURATION_DB];

                XmlDocument appServerConfig = null;
                try
                {
                    appServerConfig = (XmlDocument)ConfigurationManager.GetSection(CONFIG_KEY.Section_AppServerConfiguration);
                    if (appServerConfig != null)
                    {
                        XmlElement elem = appServerConfig.CreateElement(CONFIG_KEY.CONFIGURATION_DB);
                        elem.InnerText = dbConnection ?? string.Empty;
                        appServerConfig.DocumentElement.AppendChild(elem);
                    }
                }
                catch (Exception e)
                {
                    var ie = e.InmostException();

                    string error = string.Format("Error while loading the AppServerConfiguration section. {0}  {1}",
                        ie.GetType().Name, ie.Message);

                    Logger.ErrorFormat("ProcessID - {0}, SessionGuid - {1}, InstanceID - {2}, AppServerName - {3}.  {4}",
                        ProcessID, SessionGuid, InstanceID, AppServerName, error);
                }

                try
                {
                    appServer.Initialize(appServerConfig);

                    if (appServerConfig == null)
                        appServer.Initialize(dbConnection);
                }
                catch { }
            }

            return appServer;
        }

        private Assembly AssemblyResolveEvent(object sender, ResolveEventArgs args)
        {
            string strTempAssmbPath = "";

            Assembly executingAssemblies = Assembly.GetExecutingAssembly();
            AssemblyName[] arrReferencedAssmbNames = executingAssemblies.GetReferencedAssemblies();

            foreach (AssemblyName strAssmbName in arrReferencedAssmbNames)
            {
                if (strAssmbName.FullName.Substring(0, strAssmbName.FullName.IndexOf(",")) == args.Name.Substring(0, args.Name.IndexOf(",")))
                {
                    strTempAssmbPath = Path.Combine(WorkingDirectory, args.Name.Substring(0, args.Name.IndexOf(",")) + ".dll");

                    break;
                }
            }

            Assembly assembly = Assembly.LoadFrom(strTempAssmbPath);

            return assembly;
        }
        #endregion

        private static void WriteOutput(string someString)
        {
            Console.SetOut(Console.Out);
            using (Stream output = Console.OpenStandardOutput())
            {
                using (StreamWriter sw = new StreamWriter(output))
                {
                    sw.Write(someString);
                    sw.Flush();

                    sw.Close();
                }
                output.Close();
            }
            Console.SetOut(TextWriter.Null);
        }
    }

    internal struct CONFIG_KEY
    {
        public const string SERVICE_NAME = "SERVICE_NAME";
        public const string SERVICE_DISPLAYNAME = "SERVICE_DISPLAYNAME";
        public const string SERVICE_DESCRIPTION = "SERVICE_DESCRIPTION";
        public const string SERVICE_MODULE = "SERVICE_MODULE";
        public const string LOGGING_CONFIG_FILENAME = "LOGGING_CONFIG_FILENAME";
        public const string DATA_CENTER_FLAG = "DATA_CENTER_FLAG";
        public const string CONFIGURATION_DB = "CONFIGURATION_DB";
        public const string USE_DB_CONFIG = "USE_DB_CONFIG";

        public const string Section_AppServerConfiguration = "AppServerConfiguration";
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
