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
 * Description:        PipeServerChannelTest
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
using System.Security.Principal;

//using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;

using SimonGong.AppProcessManage.PipeChannel;

namespace PipeChannel.Test
{
    //[TestClass]
    [TestFixture]
    public class PipeServerChannelTest
    {
        [TestCase]
        public void Test_Constructor()
        {
            PipeServerChannel serverChannel = new PipeServerChannel("testpipe");

            serverChannel.Dispose();
        }

        [TestCase]
        public void Test_StartListening()
        {
            PipeServerChannel serverChannel = new PipeServerChannel("testpipe");

            serverChannel.StartListening();

            serverChannel.Dispose();
        }

        [TestCase]
        public void Test_StartListening_StopListening()
        {
            PipeServerChannel serverChannel = new PipeServerChannel("testpipe");

            serverChannel.StartListening();

            serverChannel.Close();

            serverChannel.Dispose();
        }

        [TestCase]
        public void Test_StartListening_ReceiveResponseHandler()
        {
            Thread t = new Thread(this.RunPipeClient);
            t.Start();

            PipeServerChannel serverChannel = new PipeServerChannel("testpipe");
            serverChannel.ReceiveResponseEventHandler += ReceiveResponse;

            serverChannel.StartListening();

            Thread.Sleep(1000);

            Thread t2 = new Thread(this.RunPipeClient);
            t2.Start();

            Thread.Sleep(1000);

            serverChannel.Close();

            serverChannel.Dispose();
        }

        [TestCase]
        public void Test_StartListening_ReceiveResponseHandler2()
        {
            PipeServerChannel serverChannel = new PipeServerChannel("testpipe");
            serverChannel.ReceiveResponseEventHandler += ReceiveResponse;

            serverChannel.StartListening();

            Thread t = new Thread(this.RunPipeClient);
            t.Start();

            Thread.Sleep(1000);

            Thread t2 = new Thread(this.RunPipeClient);
            t2.Start();

            Thread.Sleep(1000);

            serverChannel.Close();

            serverChannel.Dispose();
        }

        private string ReceiveResponse(string message)
        {
            Console.WriteLine(message);

            return "This is server, received your message, client.";
        }

        private void RunPipeClient()
        {
            NamedPipeClientStream client = new NamedPipeClientStream(".", "testpipe", PipeDirection.InOut,
                PipeOptions.Asynchronous, TokenImpersonationLevel.Impersonation);

            client.Connect();

            ChannelStream stream = new ChannelStream(client);

            stream.Send("Hello Server, this is client !");

            string response = stream.Receive();

            Console.WriteLine(response);

            client.Close();
        }
    }
}
