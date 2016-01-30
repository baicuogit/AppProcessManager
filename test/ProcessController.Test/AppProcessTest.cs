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
 * Description:        AppProcessTest
 * 
 * Created by:         Simon Gong
 * Created on:         December 25, 2013

 * Modified By         Date           Description 
 *  
 * 
******************************************************************************/
#endregion 


using System;
using System.Threading;
using System.Diagnostics;
using System.Reflection;

//using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;

using log4net.Config;

using SimonGong.AppProcessManage.ProcessControl;

namespace ProcessController.Test
{
    [TestFixture]
    public class AppProcessTest
    {
        private string workProcess = @"C:\Workspace\GitHub\AppProcessManager\bin\WorkerProcess.exe";
        private string args = ""; 

        [TestFixtureSetUp]
        public void TestInitialize()
        {
            XmlConfigurator.Configure();
        }

        [TestCase]
        public void Test_Constractor1()
        {
            AppProcess module = new AppProcess(1, this.workProcess, this.args, true);

            Console.WriteLine("{0}, {1}, {2}, {3}, {4}", 
                module.AppServerName, module.InstanceID, module.ProcessGuid, module.ProcessFileName, module.WorkingDirectory);
        }

        [TestCase]
        public void Test_Constractor2()
        {
            AppProcess module = new AppProcess(1, "AppServerName", this.workProcess, this.args, true);

            Console.WriteLine("{0}, {1}, {2}, {3}, {4}",
                module.AppServerName, module.InstanceID, module.ProcessGuid, module.ProcessFileName, module.WorkingDirectory);
        }

        [TestCase]
        public void Test_Constractor3()
        {
            ProcessStartInfo info = new ProcessStartInfo(this.workProcess, this.args);

            AppProcess module = new AppProcess(1, info, true);

            Console.WriteLine("{0}, {1}, {2}, {3}, {4}",
                module.AppServerName, module.InstanceID, module.ProcessGuid, module.ProcessFileName, module.WorkingDirectory);
        }

        [TestCase]
        public void Test_Constractor4()
        {
            ProcessStartInfo info = new ProcessStartInfo(this.workProcess, this.args);

            AppProcess module = new AppProcess(1, "AppServerName", info, true);

            Console.WriteLine("{0}, {1}, {2}, {3}, {4}",
                module.AppServerName, module.InstanceID, module.ProcessGuid, module.ProcessFileName, module.WorkingDirectory);
        }

        [TestCase]
        public void Test_Start()
        {
            AppProcess module = new AppProcess(1, this.workProcess, this.args, true);

            int id = module.Start();

            Thread.Sleep(10000);
        }

        [TestCase]
        public void Test_IsRunning()
        {
            AppProcess module = new AppProcess(1, this.workProcess, this.args, true);

            module.Start();

            bool running = module.IsRunning;

            Console.WriteLine(running);

            Thread.Sleep(10000);
        }

        [TestCase]
        public void Test_Start_IsRunning()
        {
            AppProcess module = new AppProcess(1, this.workProcess, this.args, true);
            int id = module.Start();

            bool run = true;
            int cnt = 0;
            while (run)
            {
                cnt++;

                Console.WriteLine("{0}, {1}", module.ProcessID,  module.IsRunning);

                Thread.Sleep(1000);

                if (cnt > 60)
                    run = false;
            }
        }

        [TestCase]
        public void Test_Start_IsRunning2()
        {
            AppProcess module = new AppProcess(1, this.workProcess, this.args, false);

            Console.WriteLine("{0}, {1}, {2}, {3}, {4}",
                module.AppServerName, module.InstanceID, module.ProcessGuid, module.ProcessFileName, module.WorkingDirectory);

            int id = module.Start();

            bool running = module.IsRunning;
            Console.WriteLine("{0}, {1}", id, running);
            Thread.Sleep(1000);

            running = module.IsRunning;
            Console.WriteLine("{0}, {1}", id, running);
            Thread.Sleep(3000);

            module.Reconfigure();

            Thread.Sleep(10000);

            running = module.IsRunning;
            Console.WriteLine("{0}, {1}", id, running);

            module.Stop(); 

            Thread.Sleep(2000);
        }
    }
}
