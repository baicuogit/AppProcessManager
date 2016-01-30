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
 * Description:        ChannelStream
 * 
 * Created by:         Simon Gong
 * Created on:         December 25, 2013

 * Modified By         Date           Description 
 *  
 * 
******************************************************************************/
#endregion 


using System;
using System.Text;
using System.IO;
using System.Xml;

namespace SimonGong.AppProcessManage.PipeChannel
{
    public class ChannelStream
    {
        private Stream ioStream = null;
        private UnicodeEncoding streamEncoding = null;

        public ChannelStream(Stream stream)
        {
            this.ioStream = stream;
            this.streamEncoding = new UnicodeEncoding();
        }

        public string Receive()
        {
            // Two bytes header for length, so total 65536 bytes.
            int length = this.ioStream.ReadByte() * 0x100;
            length += this.ioStream.ReadByte();

            byte[] inBuffer = new byte[length];

            this.ioStream.Read(inBuffer, 0, length);

            string s = this.streamEncoding.GetString(inBuffer);

            return s;
        }

        public int Send(string s)
        {
            byte[] outBuffer = this.streamEncoding.GetBytes(s);
            int length = outBuffer.Length;

            // Two bytes integer - so max value 65536 bytes
            if (length > UInt16.MaxValue)
                length = (int)UInt16.MaxValue;

            // 2 bytes header for length, so max 65536 bytes.
            byte b1 = (byte)(length / 0x100);  // (byte)(len >> 8);
            byte b2 = (byte)(length & 0xff);

            this.ioStream.WriteByte(b1);
            this.ioStream.WriteByte(b2);

            this.ioStream.Write(outBuffer, 0, length);
            this.ioStream.Flush();

            int sent = outBuffer.Length + 2;

            return sent;
        }

        public string SendReceive(string request)
        {
            this.Send(request);

            string response = this.Receive();

            return response;
        }

        public int SendXml(XmlDocument xml)
        {
            string s = xml.OuterXml;

            return this.Send(s);
        }

        public XmlDocument ReceiveXml()
        {
            string s = this.Receive();

            XmlDocument xml = new XmlDocument();
            xml.LoadXml(s);

            return xml;
        }

        public XmlDocument SendReceiveXml(XmlDocument request)
        {
            this.SendXml(request);

            XmlDocument response = this.ReceiveXml();

            return response;
        }
    }
}
