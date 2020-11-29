using System;
using System.Collections.Generic;
using System.Text;

namespace neutrino
{
    public interface NetworkComponent : Agent
    {
        void Start();
        void Close();
        string Name();
    }
}
