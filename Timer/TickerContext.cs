using System;
using System.Threading;

namespace neutrino {
    class TickerContext {
        static ThreadLocal<TickerContext> tlsTicker = new ThreadLocal<TickerContext>(() => new TickerContext());
        UInt64 preHiTick = 0;
        uint preTick = 0;
        uint baseTick = 0;
        public TickerContext() {

        }
        private uint GetTick32() {
            return (uint)System.Environment.TickCount;
        }

        private UInt64 DoGetTick() {
            UInt64 ret;
            uint now = GetTick32();

            if (now < preTick)
                ret = ((++preHiTick) << 32) + now;
            else
                ret = (preHiTick << 32) + now;

            preTick = now;
            return ret - baseTick;
        }

        public static UInt64 GetTick() {
            return tlsTicker.Value.DoGetTick();
        }
    }
}
