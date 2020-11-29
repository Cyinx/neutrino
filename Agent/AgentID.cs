using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace neutrino {
    public struct AgentID : IComparable {
        UInt64 s;
        static Int64 autoIncrAgentID = 0;
        AgentID(UInt64 rhs) {
            s = rhs;
        }

        public static implicit operator UInt64(AgentID obj) => obj.s;
        public static implicit operator Int64(AgentID obj) => (Int32)obj.s;

        public static implicit operator AgentID(UInt64 i) => new AgentID(i);
        public static implicit operator AgentID(Int64 i) => new AgentID((UInt64)i);

        public int CompareTo(object obj) {
            AgentID rhs = (AgentID)obj;
            return s.CompareTo(rhs.s);
        }

        public static AgentID Gen() {
            return Interlocked.Add(ref autoIncrAgentID, 1);
        }
    }
}
