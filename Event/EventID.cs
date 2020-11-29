using System;


namespace neutrino {
    public struct EventID : IComparable {
        UInt32 s;
        EventID(UInt32 rhs) {
            s = rhs;
        }

        public static implicit operator UInt32(EventID obj) => obj.s;
        public static implicit operator Int32(EventID obj) => (Int32)obj.s;

        public static implicit operator EventID(UInt32 i) => new EventID(i);
        public static implicit operator EventID(Int32 i) => new EventID((UInt32)i);

        public int CompareTo(object obj) {
            EventID rhs = (EventID)obj;
            return s.CompareTo(rhs.s);
        }
    }
}
