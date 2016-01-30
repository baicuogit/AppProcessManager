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
 * Description:        PipeClientChannelTest
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
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Principal;

//using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;

using SimonGong.AppProcessManage.PipeChannel;

namespace PipeChannel.Test
{
    //[TestClass]
    [TestFixture]
    public class PipeClientChannelTest
    {
        [TestCase]
        public void Test_Constructor()
        {
            PipeClientChannel clientChannel = new PipeClientChannel("testpipe");

            clientChannel.Dispose();
        }

        [TestCase]
        public void Test_SendReceiveMessage()
        {
            Thread t = new Thread(this.RunPipeServer);
            t.Start();

            PipeClientChannel clientChannel = new PipeClientChannel("testpipe");

            string response = clientChannel.SendReceive("Hello Server, this is client 1 !");
            Console.WriteLine(response);

            Thread.Sleep(1000);

            response = clientChannel.SendReceive(data);
            Console.WriteLine(response);

            Thread.Sleep(1000);

            clientChannel.Dispose();
        }

        [TestCase]
        public void Test_SendMessage()
        {
            PipeClientChannel clientChannel = new PipeClientChannel("testpipe");
            clientChannel.SendAsyncReceive("Hello Server, this is client !", this.ResponseCallback);

            Thread t = new Thread(this.RunPipeServer);
            t.Start();

            Thread.Sleep(1000);

            string response = clientChannel.SendReceive("Hello Server, this is client !");
            Console.WriteLine(response);

            Thread.Sleep(1000);

            clientChannel.Dispose();
        }

        private void ResponseCallback(string response)
        {
            Console.WriteLine("ResponseCallback - {0}", response);
        }

        private void RunPipeServer()
        {
            var pipeServer = new NamedPipeServerStream("testpipe", PipeDirection.InOut, 1,
                                     PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            try
            {
                pipeServer.BeginWaitForConnection(asyncResult =>
                {
                    using (var pipe = (NamedPipeServerStream)asyncResult.AsyncState)
                    {
                        try
                        {
                            pipe.EndWaitForConnection(asyncResult);

                            ChannelStream stream = new ChannelStream(pipe);
                            string message = stream.Receive();
                            Console.WriteLine(message);
                            stream.Send("This is server, received your message, client.");
                            pipe.WaitForPipeDrain();

                            pipe.Disconnect();
                            pipe.Close();
                        }
                        catch(Exception e)
                        {
                            Console.WriteLine("EndWaitForConnection: {0}", e.Message);
                        }

                        RunPipeServer();
                    }

                },  pipeServer);
            }
            catch (Exception ee)
            {
                Console.WriteLine("Exception: {0}", ee.Message);
            }
        }

        string data = "The active solution has been temporarily disconnected from source control because the server is unavailable.  To attempt to reconnect to source control, close and then re-open the solution when the server is available.  If you want to connect this solution to another server, use the Change Source Control dialog.";
    }
}
