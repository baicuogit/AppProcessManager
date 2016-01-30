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
 * Description:        PipeClientChannel
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
using System.Threading;
using System.Threading.Tasks;
using System.Security.Principal;

namespace SimonGong.AppProcessManage.PipeChannel
{
    public class PipeClientChannel : IDisposable
    {
        private NamedPipeClientStream pipeClientStream = null;
        private string pipeName = null;
        public PipeClientChannel(string pipeName)
        {
            if (string.IsNullOrWhiteSpace(pipeName))
                throw new ArgumentNullException("Pipe name cannot by null or empty.");

            this.pipeName = pipeName;
        }

        public void Dispose()
        {
            if (this.pipeClientStream != null)
            {
                try
                {
                    this.pipeClientStream.Dispose();
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

        public void SendAsyncReceive(string message, Action<string> receiveHandler = null)
        {
            try
            {
                Task.Run(() =>
                {
                    this.pipeClientStream = new NamedPipeClientStream(".", this.pipeName, PipeDirection.InOut,
                        PipeOptions.Asynchronous, TokenImpersonationLevel.Impersonation);

                    this.pipeClientStream.Connect();

                    string response = ReadWriteMessage(this.pipeClientStream, message);

                    if (receiveHandler != null)
                        receiveHandler.Invoke(response);
                });
            }
            catch { }
        }

        public string SendReceive(string message, int timeoutMS = Timeout.Infinite)
        {
            try
            {
                this.pipeClientStream = new NamedPipeClientStream(".", this.pipeName, PipeDirection.InOut,
                    PipeOptions.Asynchronous, TokenImpersonationLevel.Impersonation);

                this.pipeClientStream.Connect(timeoutMS);

                string response = ReadWriteMessage(this.pipeClientStream, message);

                return response;
            }
            catch
            {
                return null;
            }
        }

        private string ReadWriteMessage(NamedPipeClientStream pipeClient, string message)
        {
            try
            {
                ChannelStream channelStream = new ChannelStream(pipeClient);

                channelStream.Send(message);

                pipeClient.WaitForPipeDrain();

                string response = channelStream.Receive();

                pipeClient.Close();

                return response;
            }
            catch
            {
                return null;
            }
        }
    }
}
