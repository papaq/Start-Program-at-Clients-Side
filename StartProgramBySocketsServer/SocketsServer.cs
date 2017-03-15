using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace StartProgramBySocketsServer
{
    internal class SocketsServer
    {
        private const int Port = 28282;

        private readonly List<SocketHandler> _listOfHandlers;

        private Thread _commissionaire;
        private Thread _updateClients;

        private readonly TcpListener _tcpListener;

        private readonly MainWindow _mainWindow;

        private bool _work = true;

        public SocketsServer(MainWindow window)
        {
            _mainWindow = window;
            _listOfHandlers = new List<SocketHandler>();

            try
            {
                //_tcpListener = new TcpListener(Dns.GetHostAddresses("")[0], Port);
                _tcpListener = new TcpListener(IPAddress.Any, Port);
                _tcpListener.Start();
            }
            catch (Exception e)
            {
                window.ShowMessage(e.Message);
                return;
            }

            StartCommissionaire();
            StartUpdater();
        }

        private void StartCommissionaire()
        {
            _commissionaire = new Thread(ConnectNewSockets);
            _commissionaire.Start();
        }

        private void StartUpdater()
        {
            _updateClients = new Thread(UpdateAll);
            _updateClients.Start();
        }

        public void CloseConnections()
        {
            _work = false;
            _tcpListener?.Stop();
            //_commissionaire?.Abort();
            _commissionaire.Join();
            _updateClients.Join();

            foreach (var handler in _listOfHandlers)
            {
                // Stop each handler
                handler.StopWorker();
            }
        }

        public void SendCommand(string client, string program)
        {
            var handler = _listOfHandlers.Find(h => h.Name == client);
            handler?.SetProgramNameToProcess(program);
        }

        private void UpdateComboboxes()
        {
            var clientProgram = (from handler in _listOfHandlers
                //where handler.ThreadActive
                select new ClientPrograms(handler.Name, handler.GetPrograms())).ToList();


            _mainWindow.UpdateComboBoxes(clientProgram);
        }

        private void UpdateAll()
        {
            while (_work)
            {

                // CHECK THREADS IF ACTIVE
                var i = 0;
                while (i < _listOfHandlers.Count)
                {
                    var handler = _listOfHandlers[i];

                    if (!handler.ThreadActive)
                    {
                        _listOfHandlers.RemoveAt(i);
                        continue;
                    }

                    i++;
                }

                // UPDATE COMBOBOXES
                UpdateComboboxes();

                Thread.Sleep(1000);
            }
        }

        private void ConnectNewSockets()
        {
            while (_work)
            {
                TcpClient tcpClient;

                try
                {
                    tcpClient = _tcpListener.AcceptTcpClient();
                }
                catch (Exception)
                {
                    return;
                }

                var passMeNewSocket = new SocketHandler(tcpClient, _mainWindow);
                _listOfHandlers.Add(passMeNewSocket);

                Thread.Sleep(1000);
            }
        }
    }

    public class ClientPrograms
    {
        public string Name { get; private set; }

        public List<string> ListOfPrograms { get; private set; }

        public ClientPrograms(string name, List<string> programs)
        {
            Name = name;
            ListOfPrograms = programs;
        }
    }
}