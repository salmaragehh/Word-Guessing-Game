/*
* Filename: Program.cs
* Project: Word Guessing Game
* By: Salma Rageh  
* Date: 2023-11-19
* Description: This file is the starts the listener, and the server
*/

using ConsoleApp1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Listener listener = new Listener();
            listener.StartListeneing();

        }
    }
}
