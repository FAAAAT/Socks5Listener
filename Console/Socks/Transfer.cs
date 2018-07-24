using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ConsoleApp.LogService;
using socks5.Socks5Client;
using System.Diagnostics;


namespace ConsoleApp.Socks
{
    public class TransferAdapter
    {



        private Socket opendSocket;

        private Socks5Client opendSocks5Client;

        private ProtocolType type = ProtocolType.Tcp;

        private Task loopTask;

        public BaseLogger logger;

        public event EventHandler<AdapterOnCloseEventArgs> OnCloseHandler;
        public class AdapterOnCloseEventArgs : EventArgs
        {
            public Socket S { get; set; }
            public Socks5Client Client { get; set; }
            public TransferAdapter Adapter { get; set; }
        }

        public bool SocksClientConnected = false;

        CancellationTokenSource source = new CancellationTokenSource();

        public int BufferSize = 4096;

        private List<object> tmpArgs = new List<object>();

        public Exception LastError { get { return tmpArgs[2] as Exception; } }

        public TransferAdapter(Socket s, Socks5Client client, BaseLogger logger)
        {
            this.logger = logger;

            this.opendSocks5Client = client;
            this.opendSocket = s;
            tmpArgs.Add(opendSocket);
            tmpArgs.Add(opendSocks5Client);
        }

        private bool breakLoopTrace = false;


        private bool check()
        {
            return opendSocket.ProtocolType == type;
        }

        public bool Start()
        {
            if (!check())
            {
                return false;
            }

            //			opendSocks5Client.OnDataReceived += OpendSocks5Client_OnDataReceived;
            opendSocks5Client.OnDisconnected += OpendSocks5Client_OnDisconnected;

            logger.Info(this.GetHashCode()+"=>"+ (opendSocks5Client.Client.Sock.LocalEndPoint as IPEndPoint) + "<->" + (opendSocks5Client.Client.Sock.RemoteEndPoint as IPEndPoint));

            var tasks = new[]{
                Task.Run(() =>
                {
                    while (true)
                    {
                        LoopTranClient();

                    }
                    logger.Info("opendSocks5Client:" + opendSocks5Client.Connected + "");


                }, source.Token)
                .ContinueWith((x, y) =>
                {
                    var args = (y as List<object>);

                    logger.Info("closing client");
                    if (x.IsFaulted)
                    {
                        args.Add(x.Exception);
                    }

                }, tmpArgs),
                Task.Run(() =>
                {
                    while (!this.breakLoopTrace)
                    {
                        LoopTran();
                    }

                    logger.Info("opendSocket:" + this.opendSocket.Connected + "");

                }, source.Token)
                .ContinueWith((x, y) =>
                {
                    var args = (y as List<object>);
                    
                    //(args[1] as Socks5Client)?.Close();
                    logger.Info("closing socket");
                    //OnCloseHandler?.Invoke(this, new AdapterOnCloseEventArgs()
                    //{
                    //	S = (args[0] as Socket),
                    //	Client = (args[1] as Socks5Client),
                    //	Adapter = this

                    //});
                    if (x.IsCanceled)
                    {

                    }
                    else if (x.IsFaulted)
                    {
                        args.Add(x.Exception);
                    }
                }, tmpArgs)};

            Task.WhenAll(tasks).ContinueWith(x =>
            {
                this.opendSocket.Shutdown(SocketShutdown.Both);
                this.opendSocks5Client.Client.Sock.Shutdown(SocketShutdown.Both);
                this.opendSocket.Close();
                this.opendSocks5Client.Close();
                OnCloseHandler?.Invoke(this, new AdapterOnCloseEventArgs()
                {
                    S = this.opendSocket,
                    Client = this.opendSocks5Client,
                    Adapter = this

                });
            });


            return true;
        }

        private void OpendSocks5Client_OnDisconnected(object sender, Socks5ClientArgs e)
        {
            SocksClientConnected = false;
        }

        private void OpendSocks5Client_OnDataReceived(object sender, Socks5ClientDataArgs e)
        {
            //			logger.Info("data receive:" + Encoding.UTF8.GetString(e.Buffer));
            //			logger.Info("send begin");
            //			opendSocket.Send(e.Buffer, e.Offset, e.Count, SocketFlags.None);
            //			logger.Info("send end");


        }

        private void LoopTran()
        {

            byte[] buffer = new byte[BufferSize];
            logger.Info($"listener prepare to receive {this.GetHashCode()}");

            var result = opendSocket.Receive(buffer, SocketFlags.None, out var errorCode);

            if (result <= 0 || errorCode != default(SocketError))
            {

                breakLoopTrace = true;
                logger.Error($"Socket closed by client! {errorCode}");
                throw new Exception($"Socket closed by client! {errorCode}");

            }
            else if (result > 0)
            {
                Array.Resize<byte>(ref buffer, result);
                if (!opendSocks5Client.Send(buffer, out var code))
                {
                    logger.Error(code.ToString());
                }
                else
                {
                    logger.Debug("Send data:" + Encoding.UTF8.GetString(buffer));
                }
            }


        }

        private void LoopTranClient()
        {
           
            var buffer = new byte[4096];

            logger.Info($"client prepare to receive {this.GetHashCode()}");
            var revResult = opendSocks5Client.Receive(buffer, 0, 4096, out var recvError);
            if (revResult > 0)
            {
                logger.Debug("data receive:" + Encoding.UTF8.GetString(buffer));
                logger.Info("send begin");
                Array.Resize<byte>(ref buffer, revResult);
                var result = opendSocket.Send(buffer, SocketFlags.None, out var error);
                if (result <= 0 && error != SocketError.Success)
                {
                    logger.Error(error.ToString());
                    throw new Exception(error.ToString());
                }
                else
                {
                    logger.Info("send end");

                }
            }
            else
            {
                throw new Exception(recvError + "");
            }




        }

        public bool Stop()
        {
            source.Cancel();
            logger.Info("adapter stoped");
            return true;
        }

    }

    public class TransferServer
    {
        public IPEndPoint ListenAddress { get; set; }
        public EndPoint ProxyAddress { get; set; }
        public EndPoint DestAddress { get; set; }


        public BaseLogger logger;


        private List<TransferAdapter> adapters = new List<TransferAdapter>();

        public uint MAX_CONN = 512;
        public TransferAdapter[] Adapters;

        public TransferServer(IPEndPoint listen, EndPoint proxy, EndPoint dest, BaseLogger logger)
        {
            this.logger = logger;
            this.ListenAddress = listen;
            this.ProxyAddress = proxy;
            this.DestAddress = dest;
            Init(listen, proxy, dest);
        }

        public Exception LastError;

        private void Init(IPEndPoint listen, EndPoint proxy, EndPoint dest)
        {
            logger.Info("Initializing transfer server");
            var s = new Socket(SocketType.Stream, ProtocolType.Tcp);
            s.Bind(listen);
            s.Listen((int)MAX_CONN);
            Task.Run(() => SocketAccept(s, proxy, dest)).ContinueWith(SocketAcceptContinueWith);

        }

        private void SocketAccept(Socket socket, EndPoint proxy, EndPoint dest)
        {
            var dataSocket = socket.Accept();
            logger.Info($"socket accepted:{dataSocket.RemoteEndPoint as IPEndPoint}<->{dataSocket.LocalEndPoint as IPEndPoint}");
            Socks5Client client = new Socks5Client(proxy, dest);
            if (!client.ConnectWithEp())
            {
                throw new Exception("socks cannot connect");
            }

            TransferAdapter adapter = new TransferAdapter(dataSocket, client, logger);
            adapter.SocksClientConnected = true;
            adapter.OnCloseHandler += (sender, args) =>
            {
                adapter.Stop();
                adapters.Remove(args.Adapter);
                logger.Info("remove adapter");
                
            };
            logger.Info($" adapter starting {adapter.GetHashCode()}"); ;

            if (!adapter.Start())
                logger.Info("adapter start fail!"); ;

            Task.Run(() => SocketAccept(socket, proxy, dest)).ContinueWith(SocketAcceptContinueWith);
        }

        private void SocketAcceptContinueWith(Task task)
        {
            if (task.IsFaulted)
            {
                this.LastError = task.Exception;
#if DEBUG
                throw task.Exception;
#endif
            }
        }

        public void Close()
        {
            foreach (TransferAdapter transferAdapter in adapters)
            {
                transferAdapter.Stop();
            }
        }
    }
}
