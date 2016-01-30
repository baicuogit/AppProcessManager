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
 * Description:        AppProcess
 * 
 * Created by:         Simon Gong
 * Created on:         December 25, 2013

 * Modified By         Date           Description 
 *  
 * 
******************************************************************************/
#endregion 


using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;

using SimonGong.AppProcessManage.PipeChannel;

namespace SimonGong.AppProcessManage.ProcessControl
{
    public sealed class AppProcess : IDisposable
    {
        private log4net.ILog logger = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private Process process = null;
        private PipeClientChannel clientChannel = null;
        private ProcessStartInfo processStartInfo = null;

        private int instanceID = -1;
        private string appServerName = null;
        private string processGuid = null;
        private bool autoRestartProcess = false;
        private string workProcessName = null;
        private string workingDirectory = null;
        private string newCommand = null;

        private object locker = new object();

        public AppProcess(int instanceId, string processFilePath, string arguments, bool autoRestart = false)
        {
            this.instanceID = instanceId;

            if (string.IsNullOrWhiteSpace(processFilePath))
                throw new ArgumentNullException(processFilePath);

            var startInfo = new ProcessStartInfo(processFilePath, arguments);

            this.processGuid = this.SetupStartInfo(startInfo, autoRestart);

            this.logger.InfoFormat("Instantiated the AppProcess with the parameters. ProcessGuid - {0},  processFilePath - {1}, autoRestart - {2}, arguments: {3}",
                this.processGuid, processFilePath, autoRestart, arguments);
        }

        public AppProcess(int instanceId, string serverName, string processFilePath, string arguments, bool autoRestart = false) : 
            this(instanceId, processFilePath, arguments, autoRestart)
        {
            this.appServerName = serverName;
        }

        public AppProcess(int instanceId, ProcessStartInfo startInfo, bool autoRestart = true)
        {
            this.instanceID = instanceId;

            if (startInfo == null)
                throw new ArgumentNullException("ProcessStartInfo - startInfo");

            this.processGuid = this.SetupStartInfo(startInfo, autoRestart);

            this.logger.InfoFormat("Instantiated the AppProcess with the ProcessStartInfo. ProcessGuid - {0}, FileName - {1}, autoRestart - {2}, Arguments: {3}",
                this.processGuid, startInfo.FileName, autoRestart, startInfo.Arguments);
        }

        public AppProcess(int instanceId, string serverName, ProcessStartInfo startInfo, bool autoRestart) :
            this(instanceId, startInfo, autoRestart)
        {
            this.appServerName = serverName;
        }

        private string SetupStartInfo(ProcessStartInfo startInfo, bool autoRestart)
        {
            string guid = Guid.NewGuid().ToString().ToLower();
            this.workingDirectory = startInfo.FileName.ParseFilePath(out workProcessName);

            this.processStartInfo = startInfo;
            this.processStartInfo.Arguments = this.processStartInfo.Arguments.AddArgument(PROCESS_ARG_KEY.PIPENAME, guid);
            this.processStartInfo.WorkingDirectory = this.workingDirectory;
            this.processStartInfo.UseShellExecute = false;
            this.processStartInfo.CreateNoWindow = false;
            this.processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;

            this.autoRestartProcess = autoRestart;
            this.clientChannel = new PipeClientChannel(guid);

            return guid;
        }

        public void Close()
        {
            lock (this.locker)
            {
                if (this.process != null)
                {
                    try
                    {
                        if (this.IsRunning)
                        {
                            this.KillProcess(this.process);
                        }

                        this.process.Close();
                    }
                    catch { }
                }

                if (this.clientChannel != null)
                {
                    try
                    {
                        this.clientChannel.Dispose();
                    }
                    catch { }
                }
            }
        }

        public int InstanceID
        {
            get
            {
                return this.instanceID;
            }
        }

        public string AppServerName
        {
            get
            {
                return this.appServerName;
            }
            set
            {
                this.appServerName = value;
            }
        }

        public string ProcessGuid
        {
            get
            {
                return this.processGuid;
            }
        }

        public string ProcessFileName
        {
            get
            {
                return this.workProcessName;
            }
        }

        public string WorkingDirectory
        {
            get
            {
                return this.workingDirectory;
            }
        }

        public int ProcessID
        {
            get
            {
                if (this.process == null)
                    return NO_PROCESS;

                lock (this.locker)
                {
                    return GetProcessID;
                }
            }
        }

        private int GetProcessID
        {
            get
            {
                try
                {
                    return this.process.Id;
                }
                catch(Exception e)
                {
                    var ie = e.InnerException ?? e;
                    this.logger.DebugFormat("{0}: {1}", ie.GetType().Name, ie.Message);

                    return NO_PROCESS;
                }
            }
        }

        public Process WorkProcess
        {
            get
            {
                lock (this.locker)
                    return this.process;
            }
        }

        public int Start()
        {
            lock (this.locker)
            {
                this.newCommand = CommandMessage.START;

                try
                {
                    this.process = Process.Start(this.processStartInfo);

                    this.SetProcessEvent(this.process);

                    this.logger.DebugFormat("Started the process and return the process ID - [{0}]", this.GetProcessID);

                    return this.GetProcessID;
                }
                catch { throw; }
            }
        }

        public void Stop()
        {
            if (this.process == null)
                return;

            lock (this.locker)
            {
                this.newCommand = CommandMessage.STOP;

                if (this.GetProcessID == NO_PROCESS)
                {
                    return;
                }

                this.SetProcessEvent(this.process, false);

                string response = this.clientChannel.SendReceive(CommandMessage.STOP, 3000);

                this.logger.DebugFormat("Sent [STOP] command message to process [{0}] through pipe channel. Return - [{1}]",
                    this.GetProcessID, response);

                if (string.IsNullOrWhiteSpace(response))
                {
                    Thread.Sleep(3000);

                    this.KillProcess(this.process);
                }

                this.logger.DebugFormat("Stopped the process [{0}]", this.GetProcessID);
            }
        }

        private void KillProcess(Process pro)
        {
            try
            {
                pro.Kill();
            }
            catch { }
        }

        public void Reconfigure()
        {
            if (this.process == null)
                return;

            lock (this.locker)
            {
                this.newCommand = CommandMessage.RECONFIGURE;

                if (this.GetProcessID == NO_PROCESS)
                {
                    return;
                }

                this.clientChannel.SendAsyncReceive(CommandMessage.RECONFIGURE, ChannelResponse);

                this.logger.DebugFormat("Sent [RECONFIGURE] command message to the process [{0}] through pipe channel.", this.GetProcessID);
            }
        }

        private void ChannelResponse(string response)
        {
            this.logger.DebugFormat("Response for the [RECONFIGURE] command from the process [{0}]. {1}", this.GetProcessID, response);
        }

        public bool IsRunning
        {
            get
            {
                if (this.process == null)
                    return false;

                lock (this.locker)
                {
                    if (this.GetProcessID == NO_PROCESS)
                        return false;

                    try
                    {
                        Process pro = Process.GetProcessById(this.GetProcessID);

                        //this.logger.DebugFormat("Get the process [{0}] with the process ID - [{0}]", pro.Id, this.GetProcessID);

                        try
                        {
                            this.SetProcessEvent(this.process, false);
                            this.process.Close();
                        }
                        catch { }

                        this.process = pro;
                        this.SetProcessEvent(this.process);

                        //this.logger.DebugFormat("Updated the process with the return query result.");

                        return true;
                    }
                    catch (ArgumentException ae)
                    {
                        this.logger.DebugFormat("ArgumentException: {0}", ae.Message);

                        return false;
                    }
                    catch (InvalidOperationException ioe)
                    {
                        this.logger.DebugFormat("InvalidOperationException: {0}", ioe.Message);

                        return false;
                    }
                }
            }
        }

        private void SetProcessEvent(Process pro, bool isAdd = true)
        {
            if (isAdd)
            {
                pro.EnableRaisingEvents = true;
                pro.Exited += new EventHandler(this.Process_Exited);
                pro.ErrorDataReceived += new DataReceivedEventHandler(Process_ErrorDataReceived);
                pro.OutputDataReceived += new DataReceivedEventHandler(Process_OutputDataReceived);
            }
            else
            {
                pro.Exited -= new EventHandler(this.Process_Exited);
                pro.ErrorDataReceived -= new DataReceivedEventHandler(Process_ErrorDataReceived);
                pro.OutputDataReceived -= new DataReceivedEventHandler(Process_OutputDataReceived);
            }
        }

        private void Process_Exited(object sender, EventArgs e)
        {
            lock (this.locker)
            {
                if (sender != null)
                {
                    Process proc = sender as Process;

                    this.logger.DebugFormat("Received the process exited event. ProcessID - {0}, ExitCode - {1}, ExitTime - {2:yyyy/MM/dd HH:mm:ss.ffffff}.",
                        proc.Id, proc.ExitCode, proc.ExitTime);

                    try
                    {
                        proc.Exited -= new EventHandler(Process_Exited);
                        proc.Close();
                    }
                    catch { }
                }
                else
                {
                    this.logger.DebugFormat("Received the process exited event.");
                }

                if (string.Equals(this.newCommand, CommandMessage.STOP))
                    return;

                if (!this.autoRestartProcess)
                    return;

                try
                {
                    this.process = Process.Start(this.processStartInfo);

                    this.SetProcessEvent(this.process);

                    this.logger.DebugFormat("Restarted the new instance of the process, the new process ID - [{0}]", this.GetProcessID);
                }
                catch (Exception exp)
                {
                    var ie = exp.InnerException ?? exp;

                    this.logger.DebugFormat("Exception while restarting the new instance of the process. {0} {1}",
                        ie.GetType().Name, ie.Message);
                }
            }
        }

        private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            this.logger.DebugFormat("Received the error data from the process [{0}]. {1}", this.GetProcessID, e.Data);
        }

        private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            this.logger.DebugFormat("Received the output data from the process [{0}]. {1}", this.GetProcessID, e.Data);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    this.Close();
                }

                disposedValue = true;
            }
        }

       ~AppProcess()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }
        #endregion

        private const int NO_PROCESS = -1;
    }
}
