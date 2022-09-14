namespace NetworkSupport
{
    public class TcpSupport
    {
        private int _numConnectedSockets;
        private bool _isRunning;
        private ConcurrentDictionary<string, TcpClient> _clients;
        TcpListener _listenSocket;
        CancellationTokenSource _cancellationToken;

        public TcpSupport(int port)
        {
            _cancellationToken = new CancellationTokenSource();
            _listenSocket = new TcpListener(IPAddress.Any, port);
            _numConnectedSockets = 0;
            _clients = new ConcurrentDictionary<string, TcpClient>();
        }

        public void Run()
        {
            try
            {
                _listenSocket.Start();
                _isRunning = true;
                var task = Task.Run(() => AcceptClientsAsync(_listenSocket, _cancellationToken.Token));

                if (task.IsFaulted)
                    task.Wait();
            }
            finally { }
        }

        public void Stop()
        {
            try
            {
                _cancellationToken.Cancel();
                _listenSocket.Stop();
                // tq?
                // tq?
            }
            finally { }

            foreach (var client in _clients.Values)
            {
                try
                {
                    client.Client.Close();
                }
                finally { }
            }

            _clients.Clear();

            Console.WriteLine("Server stopped");
        }

        private void RestartListener()
        {
            _isRunning = true;
            _listenSocket.Start();
            var task = Task.Run(() => AcceptClientsAsync(_listenSocket, _cancellationToken.Token));
        }

        async Task AcceptClientsAsync(TcpListener listener, CancellationToken token)
        {
            var ip = string.Empty;

            while (!token.IsCancellationRequested)
            {
                var client = await listener.AcceptTcpClientAsync(token);
                ip = client.Client.RemoteEndPoint.ToString();

                var task = Task.Run(() => EchoAsync(client, ip, token));
            }
        }

        async Task EchoAsync(TcpClient client, string ip, CancellationToken token)
        {
            Console.WriteLine($"New client ({ip}) connected.");
            Memory<byte> buffer = new byte[1024];

            using (client)
            {
                if (client.Client.Poll(0, SelectMode.SelectRead))
                {
                    byte[]? buff = new byte[1];
                    if (client.Client.Receive(buff, SocketFlags.Peek) == 0)
                    {
                        buff = null;
                        return;
                    }

                    buff = null;
                }

                Interlocked.Increment(ref _numConnectedSockets);
                Console.WriteLine($"Client connection accepted. There are {_numConnectedSockets} clients connected to the server.");
                _clients.AddOrUpdate(ip, client, (n, o) => { return o; });

                using (var stream = client.GetStream())
                {
                    while (!token.IsCancellationRequested)
                    {
                        Send(client, Encoding.ASCII.GetBytes("Message from the server"));
                        Thread.Sleep(1000);

                        try
                        {
                            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(610));
                            var amountReadTask = stream.ReadAsync(buffer, token).AsTask();

                            var completedTask = await Task.WhenAny(timeoutTask, amountReadTask).ConfigureAwait(false);

                            if (completedTask == timeoutTask)
                                break;

                            if (amountReadTask.IsFaulted || amountReadTask.IsCanceled)
                                break;

                            var amountRead = amountReadTask.Result;

                            if (amountRead == 0)
                                break;

                            Console.WriteLine($"IP({ip}): '{Encoding.ASCII.GetString(buffer.ToArray())}'");
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"Exception: {e.Message}");
                        }
                    }
                }
            }

            buffer = null;
            Interlocked.Decrement(ref _numConnectedSockets);
            Console.WriteLine($"Client ({ip}) disconnected. There are {_numConnectedSockets} clients connected to the server.");

            _clients.TryRemove(ip, out TcpClient tcpClient);
            tcpClient.Close();
            tcpClient = null;

            if (_isRunning)
                RestartListener();
        }

        #region Send message

        private void Send(TcpClient tcpClient, byte[] data)
        {
            if (tcpClient == null || data == null)
                throw new ArgumentNullException("Parameters can not be null!");

            try
            {
                NetworkStream stream = tcpClient.GetStream();
                if (stream.CanWrite)
                    stream.WriteAsync(data, 0, data.Length);
            }
            catch (ObjectDisposedException e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public void SendToAll(byte[] data)
        {
            foreach (var client in _clients.Values)
                Send(client, data);
        }

        public void SendToAll(string data)
        {
            SendToAll(Encoding.ASCII.GetBytes(data));
        }

        public void SendToSelectedClient(string ip, string data)
        {
            Send(_clients[ip], Encoding.ASCII.GetBytes(data));
        }

        public void SendToSelectedClient(string ip, byte[] data)
        {
            Send(_clients[ip], data);
        }

        #endregion
    }
}
