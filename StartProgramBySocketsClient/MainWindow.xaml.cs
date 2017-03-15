using System;
using System.Linq;
using System.Windows;

namespace StartProgramBySocketsClient
{
    public partial class MainWindow
    {
        private SocketsClient _client;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            var ip = textBlock.Text;

            if (ip.Equals(""))
            {
                MessageBox.Show(this, "Fill in server ip, please!");
                return;
            }

            var ipWords = ip.Split('.').ToArray();

            if (ipWords.Length != 4)
            {
                MessageBox.Show(this, "Check the server ip, please!");
                return;
            }

            var ipBytes = new byte[4];
            for (var i = 0; i < ipWords.Length; i++)
            {
                try
                {
                    ipBytes[i] = byte.Parse(ipWords[i]);
                }
                catch (Exception)
                {
                    MessageBox.Show(this, "Check the server ip, please!");
                    return;
                }
            }
            
            _client = new SocketsClient(ipBytes, this);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _client?.CloseConnection();
        }

        public void ShowMessage(string message)
        {
            Dispatcher.Invoke(() =>
            {
                MessageBox.Show(this, message);
            });
        }
    }
}
