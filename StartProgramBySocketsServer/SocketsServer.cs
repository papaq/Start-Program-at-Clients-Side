using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StartProgramBySocketsServer
{
    internal class SocketsServer
    {
        private const int PORT = 3324;

        private List<SocketHandler> _listOfHandlers;
        private Thread _commissionaire;
        private TcpListener _tcpListener; 

        public SocketsServer()
        {
            _listOfHandlers = new List<SocketHandler>();

            _tcpListener = new TcpListener(Dns.GetHostAddresses("")[0], PORT);
            StartCommissionaire();
        }

        private void StartCommissionaire()
        {
            _commissionaire = new Thread(ConnectNewSockets);
        }

        public void StopConnections()
        {
            _commissionaire?.Abort();

            foreach (var handler in _listOfHandlers)
            {
                // Stop each handler
            }
        }

        private static void ConnectNewSockets()
        {
            
        }
    }
}
