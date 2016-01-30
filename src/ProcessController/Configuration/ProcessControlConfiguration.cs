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
 * Description:        ProcessControlConfiguration
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
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace SimonGong.AppProcessManage.ProcessControl.Configuration
{
    [Serializable]
    public class ProcessControlConfiguration
    {
        [XmlArray("ProcessInstances")]
        [XmlArrayItem("ProcessInstance")]
        public ProcessInstances ProcessInstances;

        [XmlElement("ControllerSetting")]
        public ControllerSetting ControllerSetting;

        public bool WatchFile()
        {
            return (this.ControllerSetting == null) ? false : this.ControllerSetting.WatchFile;
        }

        public bool MonitorProcess()
        {
            return (this.ControllerSetting == null) ? false : this.ControllerSetting.MonitorProcess;
        }

        public int MonitorTimeInterval()
        {
            return (this.ControllerSetting == null) ? 0 : this.ControllerSetting.MonitorTimeInterval;
        }
    }

    [Serializable]
    public class ProcessInstances : List<ProcessInstance>
    {
        public ProcessInstances() { }

        public ProcessInstance Get(int instanceId)
        {
            foreach (var v in this)
            {
                if (v.InstanceID == instanceId)
                    return v;
            }

            return null;
        }

        [XmlIgnore]
        public ProcessInstance this[string name]
        {
            get
            {
                foreach (var v in this)
                {
                    if (string.Equals(v.Name, name, StringComparison.OrdinalIgnoreCase))
                        return v;
                }

                return null;
            }
        }
    }

    [Serializable]
    public class ProcessInstance
    {
        public ProcessInstance() { }

        [XmlAttribute(AttributeName = "instanceID", DataType = "int")]
        public int InstanceID { get; set; }

        [XmlAttribute(AttributeName = "autoRestart", DataType = "boolean")]
        public bool AutoRestart { get; set; }

        [XmlElement("Name")]
        public string Name { get; set; }

        [XmlElement("WorkProcess")]
        public string WorkProcess { get; set; }

        [XmlElement("ProcessArgs")]
        public string ProcessArgs { get; set; }

        [XmlElement("DirectoryName")]
        public string DirectoryName { get; set; }

        [XmlElement("WorkingDirectory")]
        public string WorkingDirectory { get; set; }

        [XmlElement("LogConfigFilePath")]
        public string LogConfigFilePath { get; set; }

        [XmlElement("Command")]
        public string Command { get; set; }

        [XmlIgnore]
        public string WorkProcessPath
        {
            get
            {
                return Path.Combine((this.WorkingDirectory ?? string.Empty), (this.WorkProcess ?? string.Empty));
            }
        }
    }

    [Serializable]
    public class ControllerSetting
    {
        public ControllerSetting() { }

        [XmlElement("WatchFile", DataType = "boolean")]
        public bool WatchFile { get; set; }

        [XmlElement("MonitorProcess", DataType = "boolean")]
        public bool MonitorProcess { get; set; }

        [XmlElement("MonitorTimeInterval", DataType = "int")]
        public int MonitorTimeInterval { get; set; }
    }
}
