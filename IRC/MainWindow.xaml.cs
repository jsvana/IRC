using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;

using ChatSharp;
using System.Collections.Generic;

namespace IRC
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private IrcClient client;

        private string SERVER = "irc.freenode.net";
        private string USERNAME = "jsvana-test";
        private string[] CHANNELS = new string[] {
            "#encoded",
            "#encoded-test",
        };

        private string currentChannel = "Server";
        private Dictionary<string, ArrayList> messages = new Dictionary<string, ArrayList>();

        private List<string> channelNames {
            get
            {
                List<string> chans = new List<string>() { "Server" };
                foreach (IrcChannel chan in client.Channels)
                {
                    chans.Add(chan.Name);
                }
                return chans;
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            titleBar.MouseLeftButtonDown += (o, e) => DragMove();

            AddMessage("Connecting...", "Server", "Server");
            log.ItemsSource = messages[currentChannel];
            client = new IrcClient(SERVER, new IrcUser(USERNAME, USERNAME));
            channels.ItemsSource = channelNames;

            client.NoticeRecieved += NoticeReceived;
            client.MOTDPartRecieved += MotdPartReceived;
            client.ConnectionComplete += ConnectionComplete;
            
            client.UserJoinedChannel += UserJoinedChannel;
            client.ChannelMessageRecieved += ChannelMessageReceived;

            client.ConnectAsync();
        }

        private void ChannelMessageReceived(object sender, ChatSharp.Events.PrivateMessageEventArgs e)
        {
            if (e.IrcMessage.Parameters.Length < 2)
            {
                // Can't operate on a message with fewer than two parameters
                return;
            }
            string channel = e.IrcMessage.Parameters[0];
            string message = "";
            for (int i = 1; i < e.IrcMessage.Parameters.Length; i++)
            {
                message += e.IrcMessage.Parameters[i];
            }
            AddMessage(message, channel, e.PrivateMessage.User.Nick);
        }

        private void SelectChannel(object sender, SelectionChangedEventArgs e)
        {
            string channel = channels.SelectedItem as string;
            if (!messages.ContainsKey(channel))
            {
                // Ignore bad channels
                return;
            }
            log.ItemsSource = messages[channel];
            currentChannel = channel;
            Dispatcher.BeginInvoke((Action)(() =>
            {
                log.Items.Refresh();
                scrollView.ScrollToBottom();
            }));
        }

        private void UserJoinedChannel(object sender, ChatSharp.Events.ChannelUserEventArgs e)
        {
            if (!messages.ContainsKey(e.Channel.Name))
            {
                messages.Add(e.Channel.Name, new ArrayList());
            }
            Dispatcher.BeginInvoke((Action)(() =>
            {
                channels.ItemsSource = channelNames;
                channels.Items.Refresh();
            }));
        }

        private void NoticeReceived(object sender, ChatSharp.Events.IrcNoticeEventArgs e)
        {
            AddMessage(e.Notice, "Server", "Server");
        }

        private void MotdPartReceived(object sender, ChatSharp.Events.ServerMOTDEventArgs e)
        {
            AddMessage(e.MOTD, "Server", "Server");
        }

        private void ConnectionComplete(object sender, System.EventArgs e)
        {
            foreach (string channel in CHANNELS)
            {
                client.JoinChannel(channel);
            }
        }

        private void AddMessage(string content, string channel, string user)
        {
            if (!messages.ContainsKey(channel))
            {
                messages.Add(channel, new ArrayList());
            }
            messages[channel].Add(new Message() { Time = DateTime.Now.ToString("h:mm:ss tt"), User = user, Content = content });
            if (channel == currentChannel)
            {
                Dispatcher.BeginInvoke((Action)(() =>
                {
                    log.Items.Refresh();
                    scrollView.ScrollToBottom();
                }));
            }
        }

        private void KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (currentChannel == "Server")
                {
                    // Ignore server things for now
                    Console.WriteLine("Ignoring server message");
                    return;
                }
                client.SendRawMessage("PRIVMSG " + currentChannel + " :" + input.Text);
                AddMessage(input.Text, currentChannel, USERNAME);
                input.Text = "";
            }
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }

    public class Message {
        public string Time { get; set; }
        public string User { get; set; }
        public string Content { get; set; }
    }
}
