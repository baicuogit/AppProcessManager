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
 * Description:        ProcessTest
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
using System.Threading;
using System.Threading.Tasks;

//using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;

using SimonGong.AppProcessManage.PipeChannel;

namespace ProcessController.Test
{
    //[TestClass]
    [TestFixture]
    public class ProcessTest
    {
        //[TestMethod]
        [TestCase]
        public void Process_Start()
        {
            PipeClientChannel channel = new PipeClientChannel("testPipe");

            ProcessStartInfo processStartInfo = new ProcessStartInfo();
            processStartInfo.FileName = @"C:\Workspace\GitHub\AppProcessManager\bin\WorkerProcess.exe";
            processStartInfo.Arguments = "-pipeName testPipe";
            processStartInfo.UseShellExecute = false;
            processStartInfo.WorkingDirectory = @"C:\Workspace\GitHub\AppProcessManager\bin";
            processStartInfo.CreateNoWindow = false;
            processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            processStartInfo.RedirectStandardOutput = true;

            Task.Run(() => { 
                var process = Process.Start(processStartInfo);
                process.EnableRaisingEvents = true;
                process.OutputDataReceived += Process_OutputDataReceived;
                process.ErrorDataReceived += Process_ErrorDataReceived;
                process.Exited += Process_Exited;

                Console.WriteLine(process.Id);
            });

            Thread.Sleep(10000);

            channel.SendAsyncReceive(CommandMessage.STOP, this.ResponseHandler);
            Console.WriteLine("End of Process_Start");

            Thread.Sleep(1000);
        }

        private void ResponseHandler(string response)
        {
            Console.WriteLine("ResponseHandler {0}", response);
        }

        private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine("Process_OutputDataReceived {0}", e.Data);
        }

        private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine("Process_ErrorDataReceived {0}", e.Data);
        }

        private void Process_Exited(object sender, EventArgs e)
        {
            Console.WriteLine("Process_Exited");

            if (sender != null)
            {
                Process pro = sender as Process;

                Console.WriteLine("Process_Exited - {0}", pro.Id);
            }
        }
    }
}
