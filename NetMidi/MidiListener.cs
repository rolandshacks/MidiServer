using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace netmidi
{
    public interface IMidiListener
    {
        void onMidiData(uint data1, uint data2);
    }
}
