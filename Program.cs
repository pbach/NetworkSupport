﻿namespace NetworkSupport
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var srv = new TcpSupport(6000);
            srv.Run();
            var srv2 = new TcpSupport(6001);
            srv2.Run();
            var srv3 = new TcpSupport(6002);
            srv3.Run();
            // StartListener();
            // ConnectAsTcpClient();
            Console.ReadKey();
        }

        private static async void ConnectAsTcpClient()
        {
            using (var tcpClient = new TcpClient())
            {
                Console.WriteLine("[Client] Connecting to server");
                await tcpClient.ConnectAsync("127.0.0.1", 1234);
                Console.WriteLine("[Client] Connected to server");
                using (var networkStream = tcpClient.GetStream())
                {
                    Console.WriteLine("[Client] Writing request {0}", ClientRequestString);
                    await networkStream.WriteAsync(ClientRequestBytes, 0, ClientRequestBytes.Length);

                    var buffer = new byte[4096];
                    var byteCount = await networkStream.ReadAsync(buffer, 0, buffer.Length);
                    var response = Encoding.UTF8.GetString(buffer, 0, byteCount);
                    Console.WriteLine("[Client] Server response was {0}", response);
                }
            }
        }

        private static readonly string ClientRequestString = "Some HTTP request here";
        private static readonly byte[] ClientRequestBytes = Encoding.UTF8.GetBytes(ClientRequestString);

        private static readonly string ServerResponseString = "<?xml version=\"1.0\" encoding=\"utf-8\"?><document><userkey>key</userkey> <machinemode>1</machinemode><serial>0000</serial><unitname>Device</unitname><version>1</version></document>\n";
        private static readonly byte[] ServerResponseBytes = Encoding.UTF8.GetBytes(ServerResponseString);

        private static async void StartListener()
        {
            var tcpListener = TcpListener.Create(1234);
            tcpListener.Start();
            var tcpClient = await tcpListener.AcceptTcpClientAsync();
            Console.WriteLine("[Server] Client has connected");
            using (var networkStream = tcpClient.GetStream())
            {
                var buffer = new byte[4096];
                Console.WriteLine("[Server] Reading from client");
                while (true)
                {
                    var byteCount = await networkStream.ReadAsync(buffer, 0, buffer.Length);
                    var request = Encoding.UTF8.GetString(buffer, 0, byteCount);
                    if (request != Environment.NewLine)
                        Console.WriteLine("[Server] Client wrote: {0}", request);
                }
                // await networkStream.WriteAsync(ServerResponseBytes, 0, ServerResponseBytes.Length);
                // Console.WriteLine("[Server] Response has been written");
            }
        }
    }
}