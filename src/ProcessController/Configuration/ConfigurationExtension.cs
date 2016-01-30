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
 * Description:        ConfigurationExtension
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
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace SimonGong.AppProcessManage.ProcessControl.Configuration
{
    /// <summary>
    /// A class to handler the serialization of a configuration file.
    /// </summary>
    public static class ConfigurationExtension
    {
        /// <summary>
        /// Gets the deserialized object of the type from the XML file.
        /// </summary>
        /// <typeparam name="T">The type for deserialization.</typeparam>
        /// <param name="fileInfo">the file info</param>
        /// <param name="sectionName">The name of the section.</param>
        /// <returns>
        /// The deserialized object of the type.
        /// </returns>
        public static T GetSection<T>(this FileInfo fileInfo, string sectionName = null)
        {
            if (fileInfo == null)
                return default(T);

            XmlDocument xdocument = GetXmlDocument(fileInfo);

            if (xdocument == null)
                return default(T);

            XmlNode xNode = null;

            if (string.IsNullOrEmpty(sectionName))
                xNode = (XmlNode)xdocument.DocumentElement;
            else
                xNode = xdocument.SelectSingleNode("//" + sectionName);

            if (xNode == null)
                throw new ConfigurationErrorsException("The xml document does not have the section " + sectionName);

            return (T)DeserializeToObject<T>(xNode);
        }

        internal static XmlDocument GetXmlDocument(this FileInfo configFile)
        {
            if (configFile == null)
                throw new ArgumentNullException("FileInfo - configFile");

            if (!File.Exists(configFile.FullName))
                throw new FileNotFoundException(configFile.FullName);

            XmlReaderSettings xrSettings = new XmlReaderSettings();
            xrSettings.DtdProcessing = DtdProcessing.Parse;
            xrSettings.IgnoreWhitespace = false;

            XmlDocument xmldoc = new XmlDocument();

            using (FileStream fs = GetFileStream(configFile))
            {
                if (fs == null)
                    throw new ConfigurationErrorsException("Failed opening and reading the configuration file " + configFile.FullName);

                using (XmlReader xr = XmlReader.Create(fs, xrSettings))
                {
                    xmldoc.Load(xr);
                }

                fs.Close();
            }

            if (!xmldoc.HasChildNodes)
                throw new ConfigurationErrorsException("Incorrect content of the configuration file " + configFile.FullName);

            return xmldoc;
        }

        private static FileStream GetFileStream(FileInfo fi)
        {
            FileStream fs = null;

            for (int i = 3; --i >= 0;)
            {
                try
                {
                    fs = fi.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
                    break;
                }
                catch (IOException)
                {
                    if (i == 0)
                    {
                        fs = null;
                    }

                    System.Threading.Thread.Sleep(250);
                }
            }

            return fs;
        }

        private static T DeserializeToObject<T>(XmlNode xNode)
        {
            Type objType = typeof(T);

            if (xNode.LocalName != objType.Name)
                throw new ConfigurationErrorsException("The element name in the section must be '" + objType.Name + "'.");
            try
            {
                object obj = null;
                using (StringReader sr = new StringReader(xNode.OuterXml))
                {
                    XmlSerializer serializer = new XmlSerializer(objType);
                    obj = serializer.Deserialize(sr);
                }

                return (T)obj;
            }
            catch (Exception exp)
            {
                throw new ConfigurationErrorsException(exp.Message, exp);
            }
        }
    }
}