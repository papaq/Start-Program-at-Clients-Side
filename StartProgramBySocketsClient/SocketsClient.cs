using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace StartProgramBySocketsClient
{
    internal class SocketsClient
    {
        private class ProgramStarted
        {
            public readonly string Path;

            public readonly Process Process;

            public ProgramStarted(string path, Process process)
            {
                Path = path;
                Process = process;
            }
        }

        private const int Port = 28282;
        private readonly Thread _guestThread;
        private readonly TcpClient _tcpClient;
        private NetworkStream _streamHandler;
        private readonly string _name;
        private readonly MainWindow _window;

        private readonly List<ProgramStarted> _listOfProgramState;

        public SocketsClient(byte[] ip, MainWindow window)
        {
            _name = MakeRandomName(10);
            _window = window;

            _listOfProgramState = new List<ProgramStarted>();

            try
            {
                var ipAddress = new IPAddress(ip);
                var ipEndPoint = new IPEndPoint(ipAddress, Port);
                
                //_tcpClient = new TcpClient(Dns.GetHostName(), Port);
                //_tcpClient = new TcpClient(ipEndPoint);
                _tcpClient = new TcpClient();
                _tcpClient.Connect(ipEndPoint);
            }
            catch (Exception e)
            {
                _window.ShowMessage("Server is unreachable!\n" + e.Message);
                return;
            }

            _guestThread = new Thread(CommunicateWithServer);
            _guestThread.Start();
        }

        private static string MakeRandomName(int digits)
        {
            var rnd = new Random((int)DateTime.Now.Ticks);
            var name = new List<byte>();

            for (var i = 0; i < digits; i++)
            {
                var ch = rnd.Next(46);
                name.Add(ch > 25 ? (byte)(ch-25 + 97) : (byte)(ch + 65));
            }

            return Encoding.UTF8.GetString(name.ToArray());
        }

        public void CloseConnection()
        {
            if (_streamHandler != null)
            {
                var writer = new StreamWriter(_streamHandler) { AutoFlush = true };
                writer.WriteLine("bad");

                _streamHandler.Close();
            }

            _guestThread?.Abort();

            _tcpClient?.Close();


            // Close all programs
            foreach (var program in _listOfProgramState)
            {
                try
                {
                    program.Process.Kill();
                }
                catch (Exception)
                {
                    // No need to throw exception
                }
            }
        }

        private static IEnumerable<string> GetListOfPrograms()
        {
            var programPaths = GetAllFiles("*.exe");
            programPaths.AddRange(GetAllFiles("*.jar"));

            programPaths.RemoveAll(path => path.Contains(AppDomain.CurrentDomain.FriendlyName.Split('.')[0] + "."));

            return programPaths;
        }

        private static List<string> GetAllFiles(string template)
        {
            return Directory.GetFiles(
                Directory.GetCurrentDirectory(),
                template,
                SearchOption.TopDirectoryOnly
            ).ToList();
        }

        private bool StartOrStopApplication(string path)
        {
            // Check if still available

            if (_listOfProgramState.Exists(val => val.Path.Equals(path)))
            {
                // Stop program
                var programInfo = _listOfProgramState.Find(p => p.Path == path);
                try
                {
                    programInfo.Process.Kill();
                }
                catch (Exception)
                {
                    _window.ShowMessage("Could not close opened program!");
                    return false;
                }
                finally
                {
                    _listOfProgramState.Remove(programInfo);
                }
            }
            else
            {
                Process process;

                // Start program
                try
                {
                    process = Process.Start(path);
                }
                catch (Exception)
                {
                    _window.ShowMessage("Could not start the program!");
                    return false;
                }

                _listOfProgramState.Add(new ProgramStarted(path, process));
            }

            return true;
        }

        private void CommunicateWithServer()
        {
            _streamHandler = _tcpClient.GetStream();
            var sr = new StreamReader(_streamHandler);
            var sw = new StreamWriter(_streamHandler) {AutoFlush = true};

            var programs = GetListOfPrograms();
            var message = new StringBuilder();

            message.Append(_name).Append(";");

            foreach (var word in programs)
            {
                message.Append(word).Append(";");
            }

            // Send first message
            sw.WriteLine(message);

            while (true)
            {
                var line = sr.ReadLine();
                if (string.IsNullOrEmpty(line)) return;

                // Received command
                if (line.Equals("idle"))
                {
                    Thread.Sleep(1000);
                    continue;
                }

                var result = StartOrStopApplication(line);
                sw.WriteLine(result ? "ok" : "bad");
            }
        }

    }
}
