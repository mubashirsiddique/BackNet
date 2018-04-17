﻿using Master.AdvancedConsole;
using Shared;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace Master.Core
{
    public class MasterNetworkManager : GlobalNetworkManager
    {
        Socket socketForServer { get; set; }

        TcpListener tcpListener { get; set; }

        bool cleanedUp;

        /// <summary>
        /// Call the SetStreamTransfertEventHandlers() method to set
        /// the event handlers required to display a completion meter
        /// </summary>
        public MasterNetworkManager()
        {
            SetStreamTransfertEventHandlers();
        }

        /// <summary>
        /// Set the event handlers to display a completion meter
        /// </summary>
        public void SetStreamTransfertEventHandlers()
        {
            StreamTransfertStartEvent += StreamTransfertStartEventHandler;
            StreamTransfertProgressEvent += StreamTransfertProgressEventHandler;
            StreamTransfertFailEvent += StreamTransfertFailEventHandler;
        }

        /// <summary>
        /// Unset the event handlers to avoid displaying a completion meter
        /// </summary>
        public void UnsetStreamTransfertEventHandlers()
        {
            StreamTransfertStartEvent -= StreamTransfertStartEventHandler;
            StreamTransfertProgressEvent -= StreamTransfertProgressEventHandler;
            StreamTransfertFailEvent -= StreamTransfertFailEventHandler;
        }

        /// <summary>
        /// Listen for a connection request on the given port,
        /// when the connection succeed, instanciate the network stream,
        /// and based on it StreamReaders and StreamWriters
        /// </summary>
        /// <param name="port">Port to listen to</param>
        public void ListenAndConnect(int port)
        {
            tcpListener = new TcpListener(IPAddress.Any, port);
            tcpListener.Start();

            Console.Clear();
            ColorTools.WriteCommandMessage($"Listening on port {port} ...");
            // Wait for an incoming connection
            socketForServer = tcpListener.AcceptSocket();

            cleanedUp = false;

            // Initiate streams
            networkStream = new NetworkStream(socketForServer);
            streamReader = new StreamReader(networkStream);
            streamWriter = new StreamWriter(networkStream);
            binaryWriter = new BinaryWriter(networkStream);
            binaryReader = new BinaryReader(networkStream);

            ColorTools.WriteCommandSuccess("Connected to " + (IPEndPoint)socketForServer.RemoteEndPoint + "\n");
        }

        /// <summary>
        /// Close the Socket and TcpListener, call GlobalNetworkManager.Cleanup() for the stream cleanup
        /// </summary>
        /// <param name="processingCommand">Was a command beeing processed</param>
        public void Cleanup(bool processingCommand)
        {
            cleanedUp = true;

            ColorTools.WriteWarning(processingCommand ? "\nDisconnected, operation stopped" : "\n\nDisconnected");

            try
            {
                base.Cleanup();
                socketForServer.Close();
                tcpListener.Stop();
            }
            catch (Exception)
            {
                // ignored
            }
        }

        /// <summary>
        /// Send a simple line to know if the other end of the connection is still connected
        /// This will throw an exception if the other end isn't connected
        /// </summary>
        /// <returns>Boolean stating the status of the connection</returns>
        public bool IsConnected()
        {
            try
            {
                WriteLine(".");
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Inform if the cleanup method as been called since the beggining of the connection
        /// </summary>
        /// <returns>Boolean</returns>
        public bool CleanupMade() => cleanedUp;

        /// <summary>
        /// GlobalNetworkManager StreamTransfertStartEvent handler.
        /// Calls ProgressDisplayer.Init()
        /// </summary>
        /// <param name="total">Total number of bytes</param>
        void StreamTransfertStartEventHandler(long total)
            => ProgressDisplayer.Init(total);

        /// <summary>
        /// GlobalNetworkManager StreamTransfertProgressEvent handler.
        /// Calls ProgressDisplayer.Update()
        /// </summary>
        /// <param name="current">Number of bytes copied</param>
        void StreamTransfertProgressEventHandler(long current)
            => ProgressDisplayer.Update(current);

        /// <summary>
        /// GlobalNetworkManager StreamTransfertFailEvent handler.
        /// Calls ProgressDisplayer.End()
        /// </summary>
        void StreamTransfertFailEventHandler()
            => ProgressDisplayer.End();
    }
}
