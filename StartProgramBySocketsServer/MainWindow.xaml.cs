using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace StartProgramBySocketsServer
{

    public partial class MainWindow
    {
        private readonly SocketsServer _server;
        private List<ClientPrograms> _clientPrograms;

        public MainWindow()
        {
            InitializeComponent();

            _clientPrograms = new List<ClientPrograms>();
            _server = new SocketsServer(this);
        }

        private void buttonStartStopProgram_Click(object sender, RoutedEventArgs e)
        {
            if (comboBoxChooseClient.SelectedIndex < 0)
            {
                MessageBox.Show(this, "Choose client!");
                return;
            }

            if (comboBoxChooseProgram.SelectedIndex < 0)
            {
                MessageBox.Show(this, "Choose program!");
                return;
            }

            _server.SendCommand((string) comboBoxChooseClient.SelectedItem, 
                (string) comboBoxChooseProgram.SelectedItem);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _server?.CloseConnections();
        }

        public void UpdateComboBoxes(List<ClientPrograms> list)
        {
            Dispatcher.Invoke(() =>
            {
                var combo1Item = comboBoxChooseClient.SelectedItem;
                var combo2Item = comboBoxChooseProgram.SelectedItem;
                
                // Update comboboxes
                _clientPrograms = list;
                comboBoxChooseClient.Items.Clear();
                foreach (var client in _clientPrograms)
                {
                    comboBoxChooseClient.Items.Add(client.Name);
                }

                if (combo1Item == null && comboBoxChooseClient.Items.Count > 0)
                {
                    comboBoxChooseClient.SelectedIndex = 0;
                }

                if (!list.Exists(el => el.Name.Equals(combo1Item))) return;

                comboBoxChooseClient.SelectedItem = combo1Item;
                if (list.Find(el => el.Name.Equals(combo1Item)).ListOfPrograms.Contains(combo2Item))
                {
                    comboBoxChooseProgram.SelectedItem = combo2Item;
                }
            });
        }

        private void comboBoxChooseClient_SelectionChanged(object sender,
            System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var client = comboBoxChooseClient.SelectedItem;
            
            ClientPrograms clientRecord = null;
            if (_clientPrograms.Count > 0)
            {

                clientRecord = _clientPrograms.Find(cl => cl.Name.Equals(client));
            }

            if (clientRecord == null)
            {
                comboBoxChooseProgram.SelectedIndex = -1;
                return;
            };

            comboBoxChooseProgram.Items.Clear();
            foreach (var program in clientRecord.ListOfPrograms)
            {
                comboBoxChooseProgram.Items.Add(program);
            }

            comboBoxChooseProgram.SelectedIndex = 0;
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
