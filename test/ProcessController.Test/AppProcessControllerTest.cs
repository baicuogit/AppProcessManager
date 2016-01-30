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
 * Description:        AppProcessControllerTest
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
using System.Threading.Tasks;
using System.IO;

//using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;

using log4net.Config;

using SimonGong.AppProcessManage.ProcessControl;

namespace ProcessController.Test
{
    [TestFixture]
    public partial class AppProcessControllerTest
    {
        private string serverConfig = @"C:\Workspace\GitHub\AppProcessManager\bin\servers.config";

        [TestFixtureSetUp]
        public void TestInitialize()
        {
            XmlConfigurator.Configure();
        }

        [TestCase]
        public void Test_SetupAppProcesses()
        {
            AppProcessController.SetupAppProcesses(serverConfig);

            var modules = AppProcessController.AppServers;

            foreach(var v in modules)
            {
                Console.WriteLine("{0}, {1}, {2}", v.Key, v.Value.ProcessID, v.Value.IsRunning);
            }
        }

        [TestCase]
        public void Test_StartAppProcesses()
        {
            this.Test_SetupAppProcesses();

            var modules = AppProcessController.AppServers;

            AppProcessController.StartAppProcesses();

            foreach (var v in modules)
            {
                Console.WriteLine("{0}, {1}, {2}", v.Key, v.Value.ProcessID, v.Value.IsRunning);
            }

            Thread.Sleep(5000);
        }

        [TestCase]
        public void Test_IsRunning()
        {
            //Task.Run( () => { this.Test_StartAppProcesses(); });

            this.Test_StartAppProcesses();

            var modules = AppProcessController.AppServers;

            bool run = true;
            int cnt = 0;
            while (run)
            {
                cnt++;

                foreach (var v in modules.Values)
                {
                    if ((cnt % 20) == 0)
                    {
                        v.Reconfigure();
                    }

                    Console.WriteLine("{0}, {1}, {2}", v.ProcessGuid, v.ProcessID, v.IsRunning);
                }

                Thread.Sleep(5000);

                if (cnt > 100)
                    run = false;
            }

            AppProcessController.ReconfigAppProcesses();

            Thread.Sleep(2000);

            AppProcessController.StopAppProcesses();

            Thread.Sleep(1000);
        }

        [TestCase]
        public void Test_StopAppProcesses()
        {
            this.Test_StartAppProcesses();

            Thread.Sleep(2000);

            AppProcessController.ReconfigAppProcesses();

            Thread.Sleep(2000);

            var modules = AppProcessController.AppServers;

            AppProcessController.StopAppProcesses();

            foreach (var v in modules)
            {
                Console.WriteLine("{0}, {1}, {2}", v.Key, v.Value.ProcessID, v.Value.IsRunning);
            }
        }

        [TestCase]
        public void Test_AutoRestart()
        {
            this.Test_SetupAppProcesses();

            AppProcessController.StartAppProcesses();

            var modules = AppProcessController.AppServers;

            bool run = true;
            int cnt = 0;
            while (run)
            {
                cnt++;

                foreach (var v in modules.Values)
                {
                    if ((cnt % 20) == 0)
                    {
                        Console.WriteLine("{0}, {1}", v.ProcessID, v.IsRunning);

                        v.Reconfigure();
                    }
                }

                Thread.Sleep(1000);

                //if (cnt > 180)
                //    run = false;
            }

            AppProcessController.StopAppProcesses();

            Thread.Sleep(1000);
        }
    }
}
