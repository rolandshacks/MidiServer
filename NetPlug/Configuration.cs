using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace netplug
{
    public interface IConfiguration
    {
        void setProperty(string name, string value);
        string getProperty(string name);
    }

    public class Configuration : IService, IConfiguration
    {
        private string[] args;

        ConcurrentDictionary<string, string> properties = new ConcurrentDictionary<string, string>();

        public Configuration(string[] args)
        {
            this.args = args;

            setProperty("argCount", args.Length.ToString());

            int i=0;
            foreach (string arg in args)
            {
                setProperty("arg" + i, arg);
                i++;
            }

            setProperty("currentDirectory", System.IO.Directory.GetCurrentDirectory());
            setProperty("documentRoot", System.IO.Directory.GetCurrentDirectory() + "\\web");

        }

        public void start()
        {
        }

        public void stop()
        {
        }

        public void setProperty(string name, string value)
        {
            properties[name] = value;
        }

        public string getProperty(string name)
        {
            string value;

            if (properties.TryGetValue(name, out value))
            {
                return value;
            }

            return "";
        }
    }
}
