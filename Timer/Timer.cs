using System;

namespace neutrino {
    public class Timer {
        public Timer() { }
        public delegate void TimerCallback(object[] parameters);

        public uint seqID;
        public bool running;
        public TimerCallback callback;

        public object[] parameters;
        public UInt64 runTick;
        public int delay;
        public Timer next;
        public ulong timerID;

        internal void Reset() {
            seqID = 0;
            running = false;
            callback = null;
            parameters = null;
            next = null;
        }
    }

    // Timer列表
    public class TimerList {
        public Timer head;
        public Timer tail;

        public bool Execute(UInt64 now, ref int maxCount) {
            Timer runningTimer = null;
            while (head != null && maxCount > 0) {
                runningTimer = head;
                maxCount--;
                runningTimer.running = true;
                runningTimer.callback(runningTimer.parameters);
                runningTimer.running = false;
                head = runningTimer.next;
                runningTimer.Reset();
            }
            if (head == null) {
                tail = null;
                return false;
            }
            return true;
        }

        public void Clear() {
            Timer currTimer = null;
            while (head != null) {
                currTimer = head;
                head = currTimer.next;
                currTimer.Reset();
            }
            head = null;
            tail = null;
        }

        public void AddTimer(Timer timer) {
            timer.next = null;
            if (tail == null) {
                head = tail = timer;
                return;
            }

            tail.next = timer;
            tail = timer;
        }

        public Timer GetTimer(uint timerSeqID) {
            Timer currTimer = head;
            while (currTimer != null) {
                if (currTimer.seqID == timerSeqID)
                    return currTimer;
                currTimer = currTimer.next;
            }
            return null;
        }

        public bool DeleteTimer(uint timerSeqID) {
            Timer prevTimer = null;
            Timer currTimer = head;
            Timer nextTimer = null;
            while (currTimer != null) {
                nextTimer = currTimer.next;
                if (currTimer.seqID != timerSeqID) {
                    prevTimer = currTimer;
                    currTimer = nextTimer;
                    continue;
                }

                // 正在执行中,忽略删除操作
                if (currTimer.running)
                    return true;

                if (prevTimer == null)
                    head = nextTimer;
                else
                    prevTimer.next = nextTimer;

                if (nextTimer == null)
                    tail = prevTimer;

                currTimer.Reset();
                return true;
            }
            return false;
        }
    }
}
