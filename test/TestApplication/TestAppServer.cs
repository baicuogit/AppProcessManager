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
 * Description:        TestAppServer
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
using System.Xml;
using System.Xml.Linq;
using SimonGong.AppProcessManage.AppServerInterface;


using log4net;

namespace SG.TestApplication
{
    public class TestAppServer : IAppServer
    {
        ILog logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        string databaseConnectionString = null;

        ManualResetEvent stopEvent = new ManualResetEvent(false);

        int processID = -1;
        public TestAppServer()
        {
            try
            {
                this.processID = Process.GetCurrentProcess().Id;
            }
            catch { }
        }

        public void Close()
        {
            if (this.stopEvent != null)
                this.stopEvent.Close();

            Thread.Sleep(500);

            logger.DebugFormat("TestAppServer Close. ProcessID - {0}", this.processID);
        }

        public void Dispose()
        {
            logger.DebugFormat("TestAppServer Close. ProcessID - {0}", this.processID);
        }

        public void Initialize(string dbConnectionString)
        {
            this.databaseConnectionString = dbConnectionString;

            logger.DebugFormat("TestAppServer Initialized. ProcessID - {0}, DBConnectionString - {1}",
                this.processID, dbConnectionString);
        }

        public void Initialize(XmlDocument configuration = null)
        {
            logger.DebugFormat("TestAppServer Initialized. ProcessID - {0}, Configuration - {1}", 
                this.processID, configuration.DocumentElement.OuterXml);

            try
            {
                string dbConString = GetElementValue(configuration, "CONFIGURATION_DB");
                if (string.IsNullOrWhiteSpace(dbConString))
                    this.databaseConnectionString = dbConString;

                string ip = GetElementValue(configuration, "IP_ADDRESS");
                int port = int.Parse(GetElementValue(configuration, "PORT"));

            }
            catch (Exception e)
            {
                var ie = e.InnerException ?? e;

                logger.ErrorFormat("Exception while initializing. {0} {1}", ie.GetType().Name, ie.Message);

                throw e;
            }
        }

        public void Reconfigure()
        {
            Thread.Sleep(2000);

            logger.DebugFormat("TestAppServer Reconfigured. ProcessID - {0}", this.processID);
        }

        public void Start()
        {
            logger.DebugFormat("TestAppServer Starting ... ProcessID - {0}", this.processID);

            while (!this.stopEvent.WaitOne(5000))
            {
                logger.DebugFormat("TestAppServer is running. ProcessID - {0}", this.processID);
            }
        }

        public void Stop()
        {
            this.stopEvent.Set();

            Thread.Sleep(2000);

            logger.DebugFormat("TestAppServer Stopped. ProcessID - {0}", this.processID);
        }

        public string GetElementValue(XmlDocument xml, string tag)
        {
            if (xml == null)
                return null;

            XDocument xdoc = null;
            using (var reader = new XmlNodeReader(xml))
            {
                reader.MoveToContent();

                xdoc = XDocument.Load(reader);
            }

            XName xn = tag;
            if (!string.IsNullOrWhiteSpace(xdoc.Root.GetDefaultNamespace().NamespaceName))
                xn = XName.Get(tag, xdoc.Root.GetDefaultNamespace().NamespaceName);

            if (xdoc.Root.Element(xn) != null)
            {
                return xdoc.Root.Element(xn).Value;
            }
            else
                return string.Empty;
        }
    }
}
