using System;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Linq;

namespace NNTP_Project
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public TcpClient client;
        public StreamReader reader;
        public StreamWriter writer;

        public MainWindow()
        {
            InitializeComponent();
            InitializeConnection();
            ReadCredentialsFromIni();
        }

        private void InitializeConnection()
        {
            try
            {
                client = new TcpClient("news.dotsrc.org", 119);
                if (client.Connected)
                {
                    reader = new StreamReader(client.GetStream());
                    writer = new StreamWriter(client.GetStream());
                    writer.AutoFlush = true;
                    MessageBox.Show("Connected to the news server");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"error connecting to the news server: {ex.Message}");
                Close();
            }
        }
        private void ReadCredentialsFromIni()
        {
            string iniFilePath = "LogIn.ini";

            if (File.Exists(iniFilePath))
            {
                var lines = File.ReadLines(iniFilePath);
                foreach (var line in lines)
                {
                    var parts = line.Split('=');
                    if (parts.Length == 2)
                    {
                        if (parts[0] == "Username")
                        {
                            UsernameTextBox.Text = parts[1];
                        }
                        else if (parts[0] == "Password")
                        {
                            PasswordTextBox.Text = parts[1];
                        }
                    }
                }
            }
        }


        private void LoginButtonClick(object sender, RoutedEventArgs e)
        {
            LogIn();
        }

        private void ListNewsGroupsClick(object sender, RoutedEventArgs e)
        {
            ListNewsGroups();
        }

        public void ListNewsGroups()
        {
            writer.WriteLine("LIST");

            while (true)
            {
                string response = reader.ReadLine();
                if (response == ".")
                    break;

                string[] Groups = response.Split(' ');
                if (Groups.Length > 0)
                {
                    NewsGroupsList.Items.Add(Groups[0] + Environment.NewLine);
                }
            }
        }

        public void LogIn()
        {
            string username = UsernameTextBox.Text;
            string password = PasswordTextBox.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Please enter both username and password.");
                return;
            }

            // send authentication information to the server
            writer.WriteLine($"AUTHINFO USER {username}");
            string userResponse = reader.ReadLine();

            if (!userResponse.StartsWith("381"))
            {
                MessageBox.Show($"User response: {userResponse}");
            }

            writer.WriteLine($"AUTHINFO PASS  {password}");
            string passResponse = reader.ReadLine();

            if (!passResponse.StartsWith("281"))
            {
                MessageBox.Show($"Pass response: {passResponse}");
            }
            SaveCredentialsToIni(username, password);
            MessageBox.Show("Login Successful!");
        }
        private void SaveCredentialsToIni(string username, string password)
        {
            string iniFilePath = "LogIn.ini";

            // Create or overwrite the INI file
            using (StreamWriter sw = new StreamWriter(iniFilePath))
            {
                sw.WriteLine("[Credentials]");
                sw.WriteLine($"Username={username}");
                sw.WriteLine($"Password={password}");
            }
        }

        private void NewsgroupSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // check if an item is selected
            if (NewsGroupsList.SelectedItem != null)
            {
                // get the selected newsgroup
                string selectedNewsgroup = NewsGroupsList.SelectedItem.ToString();

                // perform actions for the selected newsgroup
                ListArticleHeadersInNewsgroup(selectedNewsgroup);
            }
        }

        private void ListArticleHeadersInNewsgroup(string newsgroupName)
        {
            ArticleList.Items.Clear();
            
            // Send a command to list articles in the selected newsgroup
            writer.WriteLine($"group {newsgroupName}");
            writer.WriteLine($"xover 1-");

            string[] articleInfo;
            string fullInfo;
            string response;
            string name;
            while (!(response = reader.ReadLine()).Equals("."))
            {
                articleInfo = response.Split();
                if (articleInfo[1] == "Re:")
                {
                    name = articleInfo[2];
                }
                else
                {
                    name = articleInfo[1];
                }

                fullInfo = articleInfo[0] + " " + name;
                ArticleList.Items.Add(fullInfo);
            }
        }

        private void ArticleHeaderSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(ArticleList.SelectedItem != null)
            {
                string selectedArticle = ArticleList.SelectedItem.ToString();

                DisplayArticleBody(selectedArticle);
            }
        }

        private void DisplayArticleBody(string articleNumber)
        {
            ArticleBody.Clear();

            writer.WriteLine($"body {articleNumber}");
            StringBuilder fullArticleBody = new StringBuilder();
            string response;
            while(!(response = reader.ReadLine()).Equals("."))
            {
               fullArticleBody.AppendLine(response);
            }
            ArticleBody.Text = fullArticleBody.ToString();
        }

        private void WriteArticle(object sender, RoutedEventArgs e)
        {
            ArticleWriterSubject.Focus();
        }

        private void PostArticleClick(object sender, RoutedEventArgs e)
        {

            if(NewsGroupsList.SelectedItem == null)
            {
                MessageBox.Show("Please select a newsgroup");
                return;
            }

            string newsgroup = NewsGroupsList.SelectedItem.ToString();
            string subject = ArticleWriterSubject.Text;
            string author = UsernameTextBox.Text;
            string date = DateTime.UtcNow.ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'");
            string organization = "EASV";
            string content = ArticleWriterContent.Text;

            PostArticle(newsgroup, subject, content, author, date, organization);
        }

        private void PostArticle(string newsgroup, string subject, string content, string author, string date, string organization)
        {
            // Format the article
            string article = $"Newsgroups: {newsgroup}Subject: {subject}\r\nFrom: {author}\r\nDate: {date}\r\nOrganization: {organization}\r\nLines: {content.Split('\n').Length}\r\n\r\n{content}";

            // Send the "POST" command to initiate the article posting process
            writer.WriteLine("POST");

            // Check the response from the server
            string response = reader.ReadLine();

            if (response.StartsWith("340"))
            {
                // The server is ready to accept the article data, so send the article
                writer.WriteLine(article);
                writer.WriteLine("."); // Indicate the end of the article

                // Check the response for a successful posting
                response = reader.ReadLine();

                if (response.StartsWith("240") || response.StartsWith("200"))
                {
                    MessageBox.Show("Article posted successfully.");
                }
                else
                {
                    MessageBox.Show("Failed to post the article. Server response: " + response);
                }
            }
            else
            {
                MessageBox.Show("Server did not request the article data. Server response: " + response);
            }
        }
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e) 
        {

        }

        private void ArticleBody_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
