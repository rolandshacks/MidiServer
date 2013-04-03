using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using netplug;

namespace netserver
{
    public interface IDispatch
    {
        Object dispatch(string command, string args);
    }
}
