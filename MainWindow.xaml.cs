/*
* Filename: MainWindow.xaml.cs
* Project: Word Guessing Game
* By: Salma Rageh  
* Date: 2023-11-19
* Description: This file and it's functions correspond with the Main Window.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /*   Name	:	MainWindow()
	    *	Purpose :	initialize main window
        *	Inputs	:	none
	    *	Outputs	:	NONE
	    *	Returns	:	Nothing
	    */
        public MainWindow()
        {
            InitializeComponent();
        }

        /*   Name	:	Connect_Click()
	    *	Purpose :	when a user submits their game information, create client connection
        *	Inputs	:	object			         sender	        	    the oject
        *				RoutedEventArgs          e                      
	    *	Outputs	:	NONE
	    *	Returns	:	Nothing
	    */
        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            // Get user information
            string IPaddress = userIP.Text;
            int port = int.Parse(userPort.Text);
            string userName = userNameBox.Text;
            float userTime = float.Parse(userTimeBox.Text);

            try
            {
                // Create connection with server
                TcpClient client = new TcpClient(IPaddress, port);

                NetworkStream stream = client.GetStream();

                // Open game window
                GameWindow gameWindow = new GameWindow(client, stream, userTime);
                gameWindow.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error connecting to server, {0}", ex.Message);
            }
            
        }

    }
}
