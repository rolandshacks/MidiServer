using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace netplug
{
    public interface ILogger
    {
        void log(string text);
    }

    public class Logger
    {
        public Logger()
        {
        }

        public void log(string text)
        {
            Console.Out.WriteLine(text);
        }
    }
}
