using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;

namespace StartProgramBySocketsServer
{
    internal class SocketHandler
    {
        private class ProgramInstance
        {
            public readonly string Path;

            public string ShortName { get; }

            public ProgramInstance(string path)
            {
                Path = path;
                ShortName = GetShortName(Path);
            }

            private static string GetShortName(string path)
            {
                var units = path.Split('\\');

                return units[units.Length - 1];
            }
        }

        private readonly TcpClient _tcpClient;

        public string Name { get; private set; }
        public bool ThreadActive => _clientWorker.IsAlive;

        private Thread _clientWorker;

        private readonly List<ProgramInstance> _listOfProgramInfo;
        private readonly List<string> _listOfTasks;
        private readonly object _locker;
        private NetworkStream _streamHandler;
        private readonly MainWindow _mainWindow;

        public SocketHandler(TcpClient client, MainWindow window)
        {
            _tcpClient = client;
            _mainWindow = window;

            _locker = new object();
            _listOfProgramInfo = new List<ProgramInstance>();
            _listOfTasks = new List<string>();
            StartWorker();
        }

        private void StartWorker()
        {
            _clientWorker = new Thread(Worker);
            _clientWorker.Start();
        }

        public void StopWorker()
        {
            _clientWorker?.Abort();
            _streamHandler?.Close();
            _tcpClient.Close();
        }

        public void SetProgramNameToProcess(string name)
        {
            lock (_locker)
            {
                if (_listOfTasks.Contains(name))
                {
                    _listOfTasks.Remove(name);
                }
                else
                {
                    _listOfTasks.Add(name);
                }
            }
        }

        private string GetProgramNameToProcess()
        {
            lock (_locker)
            {
                if (_listOfTasks.Count == 0)
                {
                    return null;
                }

                var name = _listOfTasks[0];
                _listOfTasks.RemoveAt(0);
                return name;
            }
        }

        private string GetProgramPath(string name)
        {
            return _listOfProgramInfo.Find(p => p.ShortName == name)?.Path;
        }

        private void UpdateListOfPrograms(List<string> progPaths)
        {
            _listOfProgramInfo.RemoveAll(pr => !progPaths.Exists(path => path == pr.Path));

            foreach (var path in progPaths)
            {
                if (!_listOfProgramInfo.Exists(prog => prog.Path == path))
                {
                    _listOfProgramInfo.Add(new ProgramInstance(path));
                }
            }
        }

        private static bool ProcessServiceCode(string code)
        {
            return code.ToLower().Equals("ok");
        }

        public List<string> GetPrograms()
        {
            return _listOfProgramInfo.Select(prog => prog.ShortName).ToList();
        }

        private void Worker()
        {
            try
            {
                _streamHandler = _tcpClient.GetStream();
                var sr = new StreamReader(_streamHandler);
                var sw = new StreamWriter(_streamHandler) { AutoFlush = true };
                
                // Get first line
                var line = sr.ReadLine();
                if (string.IsNullOrEmpty(line)) return;
                
                var words = line.Split(';').ToList();

                words.RemoveAll(word => word.Equals(""));

                // Set name
                Name = words[0];

                // Set list of programs
                UpdateListOfPrograms(words.GetRange(1, words.Count - 1));
                
                // Hold connection
                while (true)
                {
                    
                    var programName = GetProgramNameToProcess();
                    if (programName == null)
                    {
                        Thread.Sleep(1000);
                        continue;
                    }

                    var pathToSend = GetProgramPath(programName);
                    if (pathToSend == null)
                    {
                        Thread.Sleep(1000);
                        continue;
                    }

                    // Send path to process
                    sw.WriteLine(pathToSend);


                    line = sr.ReadLine();
                    if (string.IsNullOrEmpty(line)) return;
                    
                    if (ProcessServiceCode(line)) continue;

                    _mainWindow.ShowMessage("Could not execute!");
                }
            }

            catch (Exception e)
            {
                // Catch if you can
                _mainWindow.ShowMessage("Stopped working!\n" + e.Message);
            }

        }
    }
}