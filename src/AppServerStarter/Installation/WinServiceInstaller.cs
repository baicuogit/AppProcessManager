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
 * Description:        WinServiceInstaller
 * 
 * Created by:         Simon Gong
 * Created on:         December 25, 2013

 * Modified By         Date           Description 
 *  
 * 
******************************************************************************/
#endregion 


using System;
using System.Collections;
using System.ComponentModel;
using System.Configuration;
using System.Configuration.Install;
using System.ServiceProcess;
using System.IO;
using System.Xml;
using System.Reflection;
using System.Linq;

using Microsoft.Win32;

namespace SimonGong.AppProcessManage.AppServerStarter.Installation
{
    [RunInstaller(true)]
    public partial class WinServiceInstaller : Installer
    {
        private ServiceInstaller svcInstaller;
        private ServiceProcessInstaller svcProInstaller;

        private string serviceName = String.Empty;
        private string serviceDisplayName = String.Empty;
        private string serviceDescription = String.Empty;

        public WinServiceInstaller()
        {
            try
            {
                InitializeComponent();

                this.serviceName = this.GetValueFromAppSettings("SERVICE_NAME");
                this.serviceDisplayName = this.GetValueFromAppSettings("SERVICE_DISPLAYNAME");
                this.serviceDescription = this.GetValueFromAppSettings("SERVICE_DESC");

                if (string.IsNullOrEmpty(this.serviceName))
                {
                    Console.WriteLine(SERVICENAME_MISSING_MSG);
                    throw new ConfigurationErrorsException(SERVICENAME_MISSING_MSG);
                }

                this.svcInstaller = new ServiceInstaller();
                this.svcInstaller.ServiceName = this.serviceName;
                this.svcInstaller.DisplayName = this.serviceDisplayName;
                this.svcInstaller.Description = this.serviceDescription;

                this.svcInstaller.StartType = ServiceStartMode.Manual;

                this.svcInstaller.ServicesDependedOn = this.GetServiceDependency("STOP_DEPENDENCY"); 

                this.svcProInstaller = new ServiceProcessInstaller();
                this.svcProInstaller.Account = ServiceAccount.LocalSystem;

                base.Installers.Add(this.svcInstaller);
                base.Installers.Add(this.svcProInstaller);

                base.AfterInstall += new InstallEventHandler(AfterInstallEventHandler);
            }
            catch (Exception excp)
            {
                Console.WriteLine(string.Format(INSTALL_FAIL_MSG, this.serviceName, excp.Message));
            }
        }

        public override void Uninstall(System.Collections.IDictionary savedState)
        {
            if (null == savedState)
            {
                Console.WriteLine(INSTALL_LOG_MISSING_MSG);
                return;
            }

            try
            {
                base.Uninstall(savedState);
            }
            catch (Exception excp)
            {
                Console.WriteLine(string.Format(UNINSTALL_FAIL_MSG, this.serviceName, excp.Message));
            }
        }

        private void ToDoSomething()
        {
        
        }
        private void AfterInstallEventHandler(object sender, InstallEventArgs e)
        {
            string path = REG_SERVICE_DESC_PATH + this.serviceName;

            RegistryKey regKey = Registry.LocalMachine.OpenSubKey(path, true);
            regKey.SetValue("Description", this.serviceDescription);

            regKey.Close();
        }

        private string[] GetServiceDependency(string key)
        {
            string strValue = this.GetValueFromAppSettings(key);

            if (string.IsNullOrEmpty(strValue))
                return null;

            string[] strValues = strValue.Split(new char[] {'|'}, StringSplitOptions.RemoveEmptyEntries);

            string[] dependency = strValues.Select(s => s.Trim()).ToArray<string>();

            return dependency;
        }

        private string GetValueFromAppSettings(string key)
        {
            if (string.IsNullOrEmpty(key))
                return String.Empty;

            string value = String.Empty;

            Type ty = Type.GetType(this.ToString());
            Assembly asm = ty.Assembly;
            string configFile = asm.Location + ".config";

            XmlTextReader reader = new XmlTextReader(configFile);
            while (reader.Read())
            {
                if (reader.HasAttributes)
                {
                    if (reader["key"] == key)
                    {
                        string temp = reader["value"];
                        
                        value = temp.Trim();

                        break;
                    }
                }
            }

            return value;
        }

        private const string REG_SERVICE_DESC_PATH = @"System\CurrentControlSet\Services\";

        private const string SERVICENAME_MISSING_MSG = "The required SERVICE_NAME attribute was NOT found " +
            "in the 'appSettings' section of the .config file located in the working directory.";

        private const string INSTALL_FAIL_MSG = "Installation of the service {0} failed, the exception: {1}";

        private const string INSTALL_LOG_MISSING_MSG = "The installLog, which is needed to uninstall the service, " + 
            "could NOT be found. The service MAY NOT have successfully uninstalled.";

        private const string UNINSTALL_FAIL_MSG = "Failed uninstalling the service {0}, The exception: {1}";
    }
}
