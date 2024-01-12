/*
* Filename: gamesession.cs
* Project: Word Guessing Game
* By: Salma Rageh 
* Date: 2023-11-19
* Description: This file contains all the game logic
*/

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ConsoleApp1
{
    internal class game_session
    {
        private List<game_session> game_Sessions = new List<game_session>();
        private TcpClient client;
        private Listener listener;
        private string currentGuessString;
        private int currentNoOfWords;
        private List<string> wordsToFind;
        private List<string> guessedWords;
        private bool alreadyGuessed;
        private Stopwatch gameTimer;
        private bool isShuttingDown = false;

        /*   Name	:	game_session
        *	Purpose :	when a user connects to the server, a new game session is created for that client.
        *	            
        *	Inputs	:	TcpClient client: The client which is connected to the server
        *	            Listener listener: the server which is listening
        *	Outputs	:	none
        *	Returns	:	none
        */
        internal game_session(TcpClient client, Listener listener)
        {
            this.client = client;
            this.listener = listener;
            Console.CancelKeyPress += new ConsoleCancelEventHandler(Console_CancelKeyPressed);
        }
        /*   Name	:	Console_CanecelKeyPressed
          *	Purpose :	checks if the cancel key is pressed
          *	Inputs	:	object			         sender	        	    the oject
          *				ConsoleCancelEventArgs          e                      
          *	Outputs	:	none
          *	Returns	:	Nothing
        */
        private void Console_CancelKeyPressed(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            isShuttingDown = true;
        }
        /*   Name	:	checkForShutdown
          *	Purpose :	checks if the cancel key is pressed and it needs to shutdown
          *	Inputs	:	none               
          *	Outputs	:	sends response to the client that server is chutting down
          *	Returns	:	Nothing
        */
        private void checkForShutdown()
        {
            if (isShuttingDown)
            {
                sendResponseToClient(client.GetStream(), "Server is shutting down");

            }
        }
        /*   Name	:   StartNewGame	
         *	Purpose :	Starts a new Game
         *	Inputs	:	none               
         *	Outputs	:	none
         *	Returns	:	Nothing
       */
        internal void StartNewGame()
        {
            try
            {
                string directory = ConfigurationManager.AppSettings["directory"];
                string[] textFiles = Directory.GetFiles(directory);
                if (textFiles.Length == 0)
                {
                    Console.WriteLine("No text files found");
                    return;
                }
                Random random = new Random();
                string randomTextFile = textFiles[random.Next(textFiles.Length)];
                using (StreamReader reader = new StreamReader(randomTextFile))
                {
                    currentGuessString = reader.ReadLine();
                    if (currentGuessString == null)
                    {
                        Console.WriteLine("Invalid format: Please specify the guessing string");
                        return;
                    }
                    string noOfWords = reader.ReadLine();
                    if (int.TryParse(noOfWords, out int numberOfWords))
                    {
                        currentNoOfWords = numberOfWords;
                    }
                    else
                    {
                        Console.WriteLine("Invalid Format: Please specify the number of words to be guessed");
                        return;
                    }
                    wordsToFind = new List<string>();
                    while (!reader.EndOfStream)
                    {
                        string word = reader.ReadLine();
                        wordsToFind.Add(word);
                    }
                    guessedWords = new List<string>();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
        /*   Name	:   sendInitialGameInfo	
       *	Purpose :	sends the inital game information to the client
       *	Inputs	:   NetworkStream stream: the client's stream         
       *	Outputs	:	none
       *	Returns	:	Nothing
      */
        private void sendInitialGameInfo(NetworkStream stream)
        {
            byte[] guessStringBytes = Encoding.ASCII.GetBytes(currentGuessString);
            stream.Write(guessStringBytes, 0, guessStringBytes.Length);

            string intRes = recieveResponseFromClient(stream);

            byte[] noOfWordsBytes = Encoding.ASCII.GetBytes(currentNoOfWords.ToString());
            stream.Write(noOfWordsBytes, 0, noOfWordsBytes.Length);
        }
        /*   Name	:  updateGameState 
*	Purpose :	updates the game state
*	Inputs	:    bool isWordCorrect: checks if the guessed word is correct       
*	Outputs	:	none
*	Returns	:	Nothing
*/
        private void updateGameState(bool isWordCorrect)
        {
            if (isWordCorrect)
            {
                alreadyGuessed = guessedWords.Contains(currentGuessString);
                if (!alreadyGuessed)
                {
                    guessedWords.Add(currentGuessString);
                    currentNoOfWords--;
                }
            }
        }
        /*   Name	:   Worker 
        *	Purpose :	the main function which runs the game
        *	Inputs	:   Object o: the tcp client    
        *	Outputs	:	none
        *	Returns	:	Nothing
        */
        public void Worker(Object o)
        {

            TcpClient client = (TcpClient)o;
            try
            {
                NetworkStream stream = client.GetStream();

                byte[] timeByte = new byte[200];
                int bytesRead = stream.Read(timeByte, 0, timeByte.Length);
                string time_Limit = Encoding.ASCII.GetString(timeByte, 0, bytesRead);
                float timeLimit = float.Parse(time_Limit);

                StartNewGame();
                sendInitialGameInfo(stream);

                gameTimer = new Stopwatch();
                gameTimer.Start();

                Byte[] bytes = new byte[1046];
                int i;
                while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                {
                    checkForShutdown();
                    if (gameTimer.Elapsed.TotalSeconds >= timeLimit)
                    {
                        string timeOutMessage = "Your time is up!!! Do you want to play again?";

                        sendResponseToClient(stream, timeOutMessage);

                        string timeUpStat = recieveResponseFromClient(stream);

                        if (timeUpStat == "y")
                        {
                            Worker(client);
                        }
                        else if (timeUpStat == "n")
                        {
                            sendResponseToClient(stream, "Closed");
                            stream.Close();
                            client.Close();
                        }
                    }
                    currentGuessString = System.Text.Encoding.ASCII.GetString(bytes, 0, i);

                    if (currentGuessString == "EndSession")
                    {
                        endSession(stream);
                    }
                    else
                    {
                        bool isWordCorrect = wordsToFind.Contains(currentGuessString);

                        updateGameState(isWordCorrect);

                        string response = null;
                        if (isWordCorrect)
                        {
                            if (!alreadyGuessed)
                            {
                                response = "Correct Guess";
                            }
                            else
                            {
                                response = "You've already guessed this word!";
                            }
                        }
                        else
                        {
                            response = "Incorrect Guess";
                        }

                        sendResponseToClient(stream, response);

                        string wordStat = recieveResponseFromClient(stream);

                        if (wordStat == "EndSession")
                        {
                            endSession(stream);
                        }

                        sendResponseToClient(stream, guessedWords.Count.ToString());

                        if (currentNoOfWords == 0)
                        {
                            string playAgainMessage = "All Words are guessed!!Do you want to play again?";
                            sendResponseToClient(stream, playAgainMessage);

                            string playAgain = recieveResponseFromClient(stream);
                            if (playAgain == "y")
                            {
                                Worker(client);
                            }
                            else if (playAgain == "n")
                            {
                                sendResponseToClient(stream, "Closed");
                                stream.Close();
                            }
                        }
                    }

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                client.Close();
            }
        }
        /*   Name	:    sendResponseToClient
       *	Purpose :	this function sends the response to the client
       *	Inputs	:    NetworkStream clientStream: the client stream
       *	             string response: the response which needs to be sent to the client
       *	Outputs	:	none
       *	Returns	:	Nothing
       */
        private void sendResponseToClient(NetworkStream clientStream, string response)
        {
            byte[] responseBytes = Encoding.ASCII.GetBytes(response);
            clientStream.Write(responseBytes, 0, responseBytes.Length);
        }
        /*   Name	:  recieveResponseFromClient  
*	Purpose :	this function receives the response from the client
*	Inputs	:    NetworkStream clientStream: the client stream
*	            
*	Outputs	:	none
*	Returns	:	string response: response sent by the client
*/
        private string recieveResponseFromClient(NetworkStream clientStream)
        {
            byte[] responseByte = new byte[256];
            int bytesRead = clientStream.Read(responseByte, 0, responseByte.Length);
            string response = Encoding.ASCII.GetString(responseByte, 0, bytesRead);
            return response;
        }
        /*   Name	:  endSession  
*	Purpose :	this function ends the session of the client
*	Inputs	:    NetworkStream clientStream: the client stream
*	            
*	Outputs	:	none
*	Returns	:	string response: response sent by the client
*/
        private void endSession(NetworkStream stream)
        {
            string confirmation = "Are you sure you want to end the game?";
            sendResponseToClient(stream, confirmation);

            string confirmResponse = recieveResponseFromClient(stream);

            if (confirmResponse == "y")
            {
                sendResponseToClient(stream, "SessionEnded");
                stream.Close();
            }
            else if (confirmResponse == "n")
            {
                sendResponseToClient(stream, "Continue");
            }
        }

    }
}

