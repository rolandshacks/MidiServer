using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;

using netplug;

namespace netserver
{
    public class Server : IService
    {
        volatile bool is_active = true;
        private string rootDirectory;
        Task serverTask;
        IDispatch dispatch;

        public Server()
        {
        }

        public void start()
        {
            dispatch = ServiceManager.get<IDispatch>(netplug.Services.Dispatch);

            IConfiguration configuration = ServiceManager.get<IConfiguration>(netplug.Services.Configuration);
            this.rootDirectory = configuration.getProperty("documentRoot");

            serverTask = Task.Run(() => run());
        }

        public void stop()
        {
            is_active = false;

            while (null != serverTask && !serverTask.IsCanceled)
            {
                if (serverTask.Wait(10))
                {
                    break;
                }
            }

            serverTask = null;
        }

        public void run()
        {
            TcpListener listener = new TcpListener(IPAddress.Any, 80);
            listener.Start();

            while (is_active && Thread.CurrentThread.IsAlive)
            {
                TcpClient tcpClient = listener.AcceptTcpClient();

                handleClientConnection(tcpClient);
            }
        }

        public void handleClientConnection(TcpClient tcpClient)
        {
            // new Handler(this, dispatch, tcpClient).run();
            Task.Run(() => new Handler(this, dispatch, tcpClient).run());
        }

        public string getRootDirectory()
        {
            return rootDirectory;
        }
    }
}
