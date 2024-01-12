/*
* Filename: GameWindow.xaml.cs
* Project: Word Guessing Game
* By: Salma Rageh  
* Date: 2023-11-19
* Description: This file and it's functions correspond with the Game Window.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Annotations;
using System.Threading;
using System.ComponentModel;

namespace Client
{
    /// <summary>
    /// Interaction logic for GameWindow.xaml
    /// </summary>
    public partial class GameWindow : Window
    {
        private TcpClient client;
        private NetworkStream stream;
        private float userTime;
        private int noOfWords;
        private DateTime startTime;
        private DispatcherTimer timer;
        private string statusSend;

        /*   Name	:	GameWindow()
	    *	Purpose :	initialize game window and get game information
        *	Inputs	:	TcpClient               client              the tcp client
        *				NetworkStream           stream              the client's stream
        *				float                   userTime            the user's time limit
	    *	Outputs	:	NONE
	    *	Returns	:	Nothing
	    */
        public GameWindow(TcpClient client, NetworkStream stream, float userTime)
        {
            InitializeComponent();

            // Get the variables to use in the function
            this.client = client;
            this.stream = stream;
            this.userTime = userTime;
          
            try
            {
                // Set a status variable
                statusSend = "0";

                // Send the user time to the server
                sendResponseToServer(stream, (userTime.ToString()));

                // Get the guess string from the server
                string guessString = recieveResponseFromServer(stream);

                // Send the status to confirm the client got the guess string
                sendResponseToServer(stream, statusSend);

                // Get the number of words from the server
                string noOfWordsStr = recieveResponseFromServer(stream);

                // Start a timer
                timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(1);
                timer.Tick += timer_Tick;
                startTime = DateTime.Now;
                timer.Start();

                noOfWords = int.Parse(noOfWordsStr);

                // Display information in the window
                wordString.Text = guessString;
                numWordsBox.Text = noOfWords.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error connecting to server, {0}", ex.Message);
            }
            
        }


        /*   Name	:	SubmitGuess_Click()
	    *	Purpose :	when a user submits their word guess, send it to the 
	    *	            server to check and update then the window information
        *	Inputs	:	object			         sender	        	    the oject
        *				RoutedEventArgs          e                      
	    *	Outputs	:	Updates guess status and number of correct words in window
	    *	Returns	:	Nothing
	    */
        private void SubmitGuess_Click(object sender, RoutedEventArgs e)
        {
            // Get the user's guess
            string wordGuess = wordGuessBox.Text;

            // Clear the textbox
            wordGuessBox.Text = String.Empty;

            // Make sure the user's guess is not empty
            if (wordGuess != "")
            {
                try
                {
                    // Send user guess to server
                    sendResponseToServer(stream, wordGuess);

                    // Get if guess was correct or not and display in window
                    string responseString = recieveResponseFromServer(stream);
                    if (responseString == "Correct Guess")
                    {
                        statusOfGuess.Text = responseString;
                        statusOfGuess.Foreground = Brushes.Green;
                        statusOfGuess.Foreground = Brushes.DarkGreen;
                    }
                    else if (responseString == "Your time is up!!! Do you want to play again?")
                    {
                        // Check that the user didn't press submit as the time was up
                        timer.Stop();
                        finalScreen();
                        playAgainBlock.Text = responseString;
                        playAgainBlock.Foreground = Brushes.DarkRed;
                        return;
                    }
                    else
                    {
                        statusOfGuess.Text = responseString;
                        statusOfGuess.Foreground = Brushes.Red;
                        statusOfGuess.Foreground = Brushes.DarkRed;
                    }

                    // Send status to say it worked
                    sendResponseToServer(stream, statusSend);

                    // Get the correct number of words
                    string numOfWordsString = recieveResponseFromServer(stream);
                    numCorrectWordsBox.Text = numOfWordsString;
                    int numberWords = int.Parse(numOfWordsString);

                    // Check if the user has guessed all the words
                    if (numberWords == noOfWords)
                    {
                        timer.Stop();

                        // Get play again message
                        string playString = recieveResponseFromServer(stream);

                        // Display the play again screen
                        finalScreen();
                        playAgainBlock.Text = playString;
                        playAgainBlock.Foreground = Brushes.DarkGreen;
                    }



                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error connecting to server, {0}", ex.Message);
                }
            }
            else
            {
                statusOfGuess.Text = "You're guess can't be blank";
                statusOfGuess.Foreground = Brushes.DarkRed;
            }
           
            
        }

        /*   Name	:	EndGame_Click() 
        *	Purpose :	when a user clicks end game, confirms if they want to and ends session
        *	Inputs	:	object			         sender	        	    the object
        *				RoutedEventArgs          e                      
        *	Outputs	:	NONE
        *	Returns	:	Nothing
        */
        private void EndGame_Click(object sender, RoutedEventArgs e)
        {
            // Let server know the user wants to end the game
            sendResponseToServer(stream, "EndSession");

            // Get message from server
            string message = recieveResponseFromServer(stream);

            // Display a message box with message from server
            MessageBoxResult answer = MessageBox.Show(message, "Confirmation", MessageBoxButton.YesNo);

            if (answer == MessageBoxResult.Yes)
            {
                // Let server know user confirmed
                sendResponseToServer(stream, "y");

                // Get message from server
                string finalConfirm = recieveResponseFromServer(stream);

                if (finalConfirm == "SessionEnded")
                {
                    // End game
                    stream.Close();
                    Application.Current.Shutdown();
                }
            }
            else if (answer == MessageBoxResult.No)
            {
                // Let server know user said no
                sendResponseToServer(stream, "n");

                // Get message from server
                string finalConfirm = recieveResponseFromServer(stream);

                if (finalConfirm == "Continue")
                {
                    return;
                }
            }
            
        }

        /*   Name	:	playAgainButton_Click() 
        *	Purpose :	when a user clicks the play again button, give them a new game
        *	Inputs	:	object			         sender	        	    the object
        *				RoutedEventArgs          e                      
        *	Outputs	:	new game
        *	Returns	:	Nothing
        */
        private void playAgainButton_Click(object sender, RoutedEventArgs e)
        {
            // Let server know user want to play again
            string playAgainAnswer = "y";

            sendResponseToServer(stream, playAgainAnswer);

            // Get new game
            GameWindow gameWindow = new GameWindow(client, stream, userTime);
            gameWindow.Show();
            this.Close();
        }

        /*   Name	:	timer_Tick() 
        *	Purpose :	checks the timer, sends message to server when user time is up
        *	Inputs	:	object			         sender	        	    the object
        *				EventArgs                e                      
        *	Outputs	:	NONE
        *	Returns	:	Nothing
        */
        private void timer_Tick(object sender, EventArgs e)
        {
            // Get time and display to the user
            TimeSpan elapsedTime = DateTime.Now - startTime;
            timeLabel.Content = elapsedTime.ToString(@"mm\:ss");

            // Check if user;s time is up
            if (elapsedTime.TotalSeconds >= (userTime + 1))
            {
                timer.Stop();

                string status = "Time up!";

                // Send messgae to server that time is up
                Byte[] data = System.Text.Encoding.ASCII.GetBytes(status);
                stream.Write(data, 0, data.Length);

                // Get server message
                Byte[] numOfWordsBytes = new Byte[256];
                Int32 numOfWordsRead = stream.Read(numOfWordsBytes, 0, numOfWordsBytes.Length);
                string stat = System.Text.Encoding.ASCII.GetString(numOfWordsBytes, 0, numOfWordsRead);

                if (stat == "Your time is up!!! Do you want to play again?")
                {
                    finalScreen();
                    playAgainBlock.Text = stat;
                    playAgainBlock.Foreground = Brushes.DarkRed;
                }
            }


        }

        /*   Name	:	finalScreen() 
        *	Purpose :	updates the UI, for when the user is presented a play again option
        *	Inputs	:	NONE                     
        *	Outputs	:	NONE
        *	Returns	:	Nothing
        */
        private void finalScreen()
        {
            numWordsBox.Visibility = Visibility.Collapsed;
            numWordsLabel.Visibility = Visibility.Collapsed;
            wordGuessBox.Visibility = Visibility.Collapsed;
            statusOfGuess.Visibility = Visibility.Collapsed;
            submitGuess.Visibility = Visibility.Collapsed;
            wordString.Visibility = Visibility.Collapsed;
            correctWordLabel.Visibility = Visibility.Collapsed;
            numCorrectWordsBox.Visibility = Visibility.Collapsed;
            timeLabel.Visibility = Visibility.Collapsed;
            endGame.Visibility = Visibility.Collapsed;
            exitButton.Visibility = Visibility.Visible;
            playAgainButton.Visibility = Visibility.Visible;
            playAgainBlock.Visibility = Visibility.Visible;
        }

        /*   Name	:	sendResponseToServer() 
        *	Purpose :	send message to server
        *	Inputs	:	NetworkStream       clientStream        the client stream to send information
        *	            string              response            the string you want to send to server
        *	Outputs	:	NONE
        *	Returns	:	Nothing
        */
        private void sendResponseToServer(NetworkStream clientStream, string response)
        {
            byte[] responseBytes = Encoding.ASCII.GetBytes(response);
            clientStream.Write(responseBytes, 0, responseBytes.Length);
        }

        /*   Name	:	recieveResponseFromServer() 
        *	Purpose :	receive message from server
        *	Inputs	:	NetworkStream       clientStream        the client stream to send information
        *	Outputs	:	NONE
        *	Returns	:	string             response             message from server
        */
        private string recieveResponseFromServer(NetworkStream clientStream)
        {
            byte[] responseByte = new byte[256];
            int bytesRead = clientStream.Read(responseByte, 0, responseByte.Length);
            string response = Encoding.ASCII.GetString(responseByte, 0, bytesRead);
            if(response == "Server is shutting down")
            {
                MessageBox.Show("server is shutting down");
                stream.Close();
                client.Close();
                Application.Current.Shutdown();
            }
            return response;

        }

        /*   Name	:	Exit_Click() 
        *	Purpose :	when a user clicks the exit button, end the session
        *	Inputs	:	object			         sender	        	    the object
        *				RoutedEventArgs          e                      
        *	Outputs	:	NONE
        *	Returns	:	Nothing
        */
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            string playAgainAnswer = "n";

            sendResponseToServer(stream, playAgainAnswer);

            string confirm = recieveResponseFromServer(stream);

            if (confirm == "Closed")
            {
                stream.Close();
                Application.Current.Shutdown();
            }
            
        }

        
    }
}
