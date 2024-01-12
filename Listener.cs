/*
* Filename: Listener.cs
* Project: Word Guessing Game
* By: Salma Rageh
* Date: 2023-11-19
* Description: This file is the server listener, that connects to clients
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Microsoft.SqlServer.Server;
using System.Configuration;

namespace ConsoleApp1
{
    internal class Listener
    {

        private List<game_session> game_Sessions = new List<game_session>();
 /*   Name	:  StartListening 
*	Purpose :	this function connects to a client and creates a new game session for them
*	Inputs	:    none
*	            
*	Outputs	:	none
*	Returns	:	none
*/
        internal void StartListeneing()
        {
            TcpListener server = null;
            try
            {
                Int32 port = int.Parse(ConfigurationManager.AppSettings["port"]); ;
                IPAddress localAddress = IPAddress.Parse(ConfigurationManager.AppSettings["ipAddress"]);
                server = new TcpListener(localAddress, port);
                server.Start();

                while (true)
                {
                    Console.WriteLine("Waiting for a connection");
                    TcpClient client = server.AcceptTcpClient();

                    game_session gameSession = new game_session(client, this);
                    game_Sessions.Add(gameSession);

                    ParameterizedThreadStart ts = new ParameterizedThreadStart(gameSession.Worker);
                    Thread clientThread = new Thread(ts);
                    clientThread.Start(client);

                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);

            }
            finally
            {
                server.Stop();
            }
        }
        /*   Name	:  RemoveGameSession
        *	Purpose :	this function removes the game session 
        *	Inputs	:   game_session gameSession
        *	            
        *	Outputs	:	none
        *	Returns	:	none
        */
        public void RemoveGameSession(game_session gameSession)
        {
            game_Sessions.Remove(gameSession);
        }
    } 
}

