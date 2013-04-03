using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;

using netplug;
using netserver;

namespace MidiServer
{
    class Application
    {
        private volatile bool running;
        private string[] args;

        private static Application instance = null;

        public static Application Instance()
        {
            return instance;
        }

        private Application(string[] args)
        {
            running = true;
            instance = this;
            this.args = args;
        }

        public string[] getArgs()
        {
            return args;
        }

        public void start()
        {
            IServiceManager serviceManager = ServiceManager.Instance();

            serviceManager.registerService(Services.Configuration, new Configuration(args));
            serviceManager.registerService(Services.Logger, new Logger());
            serviceManager.registerService(Services.Server, new Server());

            MidiService midiService = new MidiService();
            serviceManager.registerService(Services.Dispatch, midiService);
            serviceManager.registerService(Services.Midi, midiService);

            serviceManager.startServices();
        }

        public void stop()
        {
            running = false;

            IServiceManager serviceManager = ServiceManager.Instance();

            serviceManager.stopServices();
            serviceManager.unregisterAllServices();
        }

        public void run()
        {
            while (running)
            {
                Thread.Sleep(250);

                if (Console.KeyAvailable)
                {
                    ConsoleKeyInfo keyInfo = Console.ReadKey(true);
                    if (keyInfo.KeyChar == 27)
                    {
                        Console.Out.WriteLine("EXIT");
                        break;
                    }
                }
            }

            running = false;
        }

        public void exit()
        {
            running = false;
        }

        static void Main(string[] args)
        {
            Console.Out.WriteLine("MIDI Server");
            Console.Out.WriteLine("v1.0, (C) 2013 Roland Schabenberger");
            Console.Out.WriteLine("----------------------------------------------------------------------");

            if (args.Length == 1 && args[0] == "/?")
            {
                Console.Out.WriteLine("Usage: MidiServer [output device id]");
                return;
            }

            new ServiceManager(); // start service manager

            Application app = new Application(args);

            app.start();
            app.run();
            app.stop();
        }

    }
}
