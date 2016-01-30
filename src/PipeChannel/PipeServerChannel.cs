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
 * Description:        PipeServerChannel
 * 
 * Created by:         Simon Gong
 * Created on:         December 25, 2013

 * Modified By         Date           Description 
 *  
 * 
******************************************************************************/
#endregion 


using System;
using System.IO.Pipes;

namespace SimonGong.AppProcessManage.PipeChannel
{
    public class PipeServerChannel : IDisposable
    {
        public Func<string, string> ReceiveResponseEventHandler = null;

        private NamedPipeServerStream pipeServerStream = null;
        private string pipeName = null;

        private IAsyncResult pendingAccept = null;

        public PipeServerChannel(string pipeName)
        {
            if (string.IsNullOrWhiteSpace(pipeName))
                throw new ArgumentNullException("Pipe name cannot by null or empty.");

            this.pipeName = pipeName;

            this.pipeServerStream = new NamedPipeServerStream(this.pipeName, PipeDirection.InOut, 1, 
                PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        }

        public void Dispose()
        {
            if (this.pipeServerStream != null)
            {
                try
                {
                    this.pipeServerStream.Dispose();
                }
                catch { }
            }
        }

        public string PipeName
        {
            get
            {
                return PipeName;
            }
        }

        public void StartListening()
        {
            this.pendingAccept = this.pipeServerStream.BeginWaitForConnection(AcceptConnectionCallback, this.pipeServerStream);
        }

        public void Close()
        {
            if (this.pipeServerStream != null)
            {
                try
                {
                    if (this.pipeServerStream.IsConnected)
                        this.pipeServerStream.Disconnect();
                }
                catch { }

                try
                {
                    this.pipeServerStream.Close();
                }
                catch { }
            }

            //if (this.pendingAccept != null)
            //{
            //    try
            //    {
            //        this.pendingAccept.AsyncWaitHandle.Close();
            //    }
            //    catch { }
            //}
        }

        private void AcceptConnectionCallback(IAsyncResult ar)
        {
            try
            {
                var pipeServer = (NamedPipeServerStream)ar.AsyncState;

                pipeServer.EndWaitForConnection(ar);

                this.ReadWriteMessage(pipeServer);

                pipeServer.Disconnect();
            }
            catch(Exception)
            {
                this.pipeServerStream = new NamedPipeServerStream(this.pipeName, PipeDirection.InOut, 1,
                    PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            }

            this.StartListening();
        }

        private void ReadWriteMessage(NamedPipeServerStream pipeStream)
        {
            try
            {
                ChannelStream channelStream = new ChannelStream(pipeStream);

                string inMessage = channelStream.Receive();

                if (this.ReceiveResponseEventHandler != null)
                {
                    try
                    {
                        string response = this.ReceiveResponseEventHandler.Invoke(inMessage);

                        if (!string.IsNullOrWhiteSpace(response))
                        {
                            channelStream.Send(response);

                            pipeStream.WaitForPipeDrain();
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }
    }
}
