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
 * Description:        AppServerConfigurationSectionHandler.cs
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
using System.Xml;
using System.IO;
using System.Xml.Serialization;
using System.Reflection;

namespace SimonGong.AppProcessManage.WorkerProcess
{
    public class AppServerConfigurationSectionHandler : IConfigurationSectionHandler
    {
        public virtual object Create(object parent, object configContext, XmlNode sectionNode)
        {
            if (sectionNode == null)
                throw new ConfigurationErrorsException("The section is not found.", sectionNode);

            if (sectionNode.Attributes["type"] == null)
            {
                XmlDocument xdoc = new XmlDocument();

                xdoc.LoadXml(sectionNode.OuterXml);

                return xdoc;
            }

            Type type = GetSectionType(sectionNode);

            if (type == null)
                throw new ConfigurationErrorsException("Failed loading the type from the assembly provided in the 'type' attribute of the element " + sectionNode.Name);

            object obj = DeserializeSection(sectionNode, type);

            return obj;
        }

        private Type GetSectionType(XmlNode xNode)
        {
            if (xNode.Attributes["type"] == null)
                throw new ConfigurationErrorsException("The type attribute is not found.", xNode);

            string typeAsmString = xNode.Attributes["type"].Value;

            if (string.IsNullOrEmpty(typeAsmString))
                throw new ConfigurationErrorsException(
                    "Null or empty value of the type attribute in the element " + xNode.Name);

            TypeLoader.LoadType(typeAsmString);

            try
            {
                Type type = TypeLoader.LoadType(typeAsmString);

                return type;
            }
            catch (Exception exp)
            {
                throw new ConfigurationErrorsException(exp.Message, exp);
            }
        }

        private object DeserializeSection(XmlNode xNode, Type type)
        {
            if (xNode.LocalName != type.Name)
                throw new ConfigurationErrorsException("The element name must be '" + type.Name + "'.");
            try
            {
                object obj = null;
                using (StringReader sr = new StringReader(xNode.OuterXml))
                {
                    XmlSerializer serializer = new XmlSerializer(type);
                    obj = serializer.Deserialize(sr);
                }

                return obj;
            }
            catch (Exception exp)
            {
                throw new ConfigurationErrorsException(exp.Message, exp);
            }
        }
    }
}
