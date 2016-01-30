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
 * Description:        ProcessConfigurationManagerTest
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

using SimonGong.AppProcessManage.ProcessControl.Configuration;

//using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;

namespace ProcessConfiguration.Test
{
    [TestFixture]
    public class ProcessConfigurationManagerTest
    {
        private string filePath = @"C:\Workspace\GitHub\AppProcessManager\bin\servers.config";

        ManualResetEvent stopEvent = new ManualResetEvent(false);

        [TestCase]
        public void Test_GetConfiguration()
        {
            var config = ProcessConfigurationManager.GetConfiguration(this.filePath);

            Console.WriteLine(ProcessConfigurationManager.EnableRaisingFileWatcherEvent);

            foreach (var val in config.ProcessInstances)
            {
                Console.WriteLine("{0}, {1}", val.Name, val.WorkingDirectory);
            }
        }

        [TestCase]
        public void Test_GetConfiguration_Watch()
        {
            var config = ProcessConfigurationManager.GetConfiguration(this.filePath, true);

            ProcessConfigurationManager.AppServerConfigurationChangeHandler = this.HandleAppServerConfigurationChange;
             
            foreach (var val in config.ProcessInstances)
            {
                Console.WriteLine("{0}, {1}", val.Name, val.WorkingDirectory);
            }

            while(!this.stopEvent.WaitOne())
            {
            }

            Console.WriteLine("Done.");
        }

        private void HandleAppServerConfigurationChange(ProcessControlConfiguration config)
        {
            Console.WriteLine("ChangeEvent: ----------------- {0}", DateTime.Now.ToString("HH:mm:ss.ffffff"));

            foreach (var val in config.ProcessInstances)
            {
                Console.WriteLine("{0}, {1}", val.Name, val.WorkingDirectory);
            }

            //this.stopEvent.Set();
        }
    }
}
