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
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Threading;

namespace SimonGong.AppProcessManage.AppServerStarter
{
    public partial class WindowsService
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        public static void Main(string[] args)
        {
            bool runConsole = (args.Contains<string>("-C") || args.Contains<string>("/C") ||
                 args.Contains<string>("-c") || args.Contains<string>("/c"));

            WindowsService service = new WindowsService();
            try
            {
                service.SetServiceName();
                service.SetLoggingConfiguration();
            }
            catch (Exception excp)
            {
                if (runConsole)
                {
                    Console.WriteLine("{0},  {1}", excp.Message, excp.StackTrace);
                    return;
                }

                throw excp;
            }

            service.AutoLog = true;
            service.CanPauseAndContinue = false;
            service.CanStop = true;
            service.CanShutdown = true;

            string svrName = service.ServiceName;
            if (runConsole)
            {
                // =======================================================
                // Run as Console
                // =======================================================
                service.OnStart(args);
                Console.WriteLine("{0} is running.", svrName);
                Console.WriteLine("Press <Enter> to stop.");

                Console.ReadLine();

                Console.WriteLine("{0} is stopping...", svrName);
                service.OnStop();
                Thread.Sleep(3000);
                service.Dispose();
                service = null;

                Console.WriteLine("{0} stopped.", svrName);
                // =======================================================
            }
            else
            {
                // =======================================================
                // Run as service
                // =======================================================
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[] { service };
                ServiceBase.Run(ServicesToRun);
            }
        }
    }
}
