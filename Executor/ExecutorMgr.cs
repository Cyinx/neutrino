using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace neutrino {

    public interface Executor : Agent {
        string Name();
        void RegisterEventHandler(EventID typeID, MsgHandler handler);
        void RegisterRpcHandler(string rpcName, RpcHandler handler);
        void Enqueue(EventMsg eventMsg);
        void Enqueue(Agent sender, EventID msgID, object data = null);
        void Enqueue(Agent sender, ActionDoEvent action, params object[] args);
        void RpcCall(string rpcName, params object[] args);
        UInt64 AddTimer(double delay, Timer.TimerCallback op, params object[] parameters);
        bool RemoveTimer(UInt64 id);
        void Close();
    }
    public static class ExecutorMgr {

        public static Executor Get(string name) {
            var m = workerMap.GetOrAdd(name, (string name) => {
                ExecutorWorker worker = new ExecutorWorker(name);
                if (Interlocked.CompareExchange(ref inited, 1, 1) == 1) {
                    worker.Init();
                }
                return worker;
            });
            return m;
        }

        public static void Start() {
            foreach (var x in workerMap) {
                x.Value.Init();
            }
        }

        public static void Close() {
            foreach (var x in workerMap) {
                x.Value.Close();
            }
        }

        static ConcurrentDictionary<string, ExecutorWorker> workerMap = new ConcurrentDictionary<string, ExecutorWorker>();
        static int inited = 0;
    }
}
