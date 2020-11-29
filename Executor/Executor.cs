using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace neutrino {
    public delegate void MsgHandler(Context ctx, object msg);
    public delegate void RpcHandler(Context ctx, object[] args);
    public class ExecutorContext {
        internal void Init(ExecutorWorker m) {
            this.m = m;
        }

        internal protected Agent s;
        internal ExecutorWorker m;
    }

    class ExecutorWorker : Executor {
        public ExecutorWorker(string name) {
            this.mName = name;
            this.agentID = AgentID.Gen();
        }

        public void Init() {
            workerThread = new Thread(this.Run);
            workerContext.Init(this);
            workerThread.Start();
        }
        public AgentID GetID() {
            return agentID;
        }

        public string Name() {
            return mName;
        }

        public static Context LocalContext() {
            return tlsCtx.Value;
        }

        public void Close() {
            EventMsg eventMsg = new EventMsg();
            eventMsg.eventType = EventType.EVENT_WORKER_CLOSE;
            this.Enqueue(eventMsg);

            workerThread.Join();
        }

        public UInt64 AddTimer(double delay, Timer.TimerCallback op, params object[] parameters) {
            return timerManager.AddTimer(delay, op, parameters);
        }

        public bool RemoveTimer(UInt64 id) {
            return timerManager.DeleteTimer(id);
        }

        public void Enqueue(Agent sender, EventID msgID, object data = null) {
            DataEventMsg eventMsg = new DataEventMsg();
            eventMsg.attchData = data;
            eventMsg.sAgent = sender;
            eventMsg.typeID = msgID;
            this.Enqueue(eventMsg);
        }

        public void Enqueue(Agent sender, ActionDoEvent action, params object[] args) {
            ActionEventMsg eventMsg = new ActionEventMsg();
            eventMsg.sAgent = sender;
            eventMsg.attachData = args;
            eventMsg.action = action;
            this.Enqueue(eventMsg);
        }

        public void Enqueue(EventMsg eventMsg) {
            eConcurrQueue.Add(eventMsg);
        }

        public void RpcCall(string rpcName, params object[] args) {
            RpcEventMsg eventMsg = new RpcEventMsg();
            if (tlsCtx.IsValueCreated == true) {
                eventMsg.sAgent = tlsCtx.Value.m;
            }
            eventMsg.attachData = args;
            eventMsg.rpcName = rpcName;
            this.Enqueue(eventMsg);
        }

        public void RegisterEventHandler(EventID typeID, MsgHandler handler) {
            msgHandlerMap[typeID] = handler;
        }
        public void RegisterRpcHandler(string rpcName, RpcHandler handler) {
            rpcHandlerMap[rpcName] = handler;
        }
        public void Run() {
            isRunning = true;
            do {
                try {
                    tlsCtx.Value = workerContext;
                    EventMsg eventMsg = null;

                    if (!eConcurrQueue.TryTake(out eventMsg, 1))
                        continue;

                    switch (eventMsg.GetEventType()) {
                        case EventType.EVENT_ROUTER_MSG:
                            HandlerRouteMsg(eventMsg);
                            break;
                        case EventType.EVENT_WORKER_CLOSE:
                            HandlerCloseSignal(eventMsg);
                            break;
                        case EventType.EVENT_WORKER_ACTION:
                            HandlerActionEvent(eventMsg);
                            break;
                        case EventType.EVENT_ROUTE_RPC:
                            HandlerRouteRpc(eventMsg);
                            break;
                        default:
                            break;
                    }

                    timerManager.Execute();
                } catch (Exception ex) {
                    sLog.Error("executor", "executor {0} worker exception.", this.mName);
                    sLog.Error("executor", ex.Message);
                    sLog.Error("executor", ex.StackTrace);
                }

            } while (isRunning);
        }

        private void HandlerRouteMsg(EventMsg eventMsg) {
            DataEventMsg dataMsg = (DataEventMsg)eventMsg;

            MsgHandler handler = null;
            if (msgHandlerMap.TryGetValue(dataMsg.typeID, out handler)) {
                workerContext.s = dataMsg.sAgent;
                handler(workerContext, dataMsg.attchData);
            } else {
                sLog.Error("executor", "executor {0} handle route msg type {1} null.", this.mName, dataMsg.typeID);
            }
        }
        private void HandlerRouteRpc(EventMsg eventMsg) {
            RpcEventMsg rpcMsg = (RpcEventMsg)eventMsg;

            RpcHandler handler = null;
            if (rpcHandlerMap.TryGetValue(rpcMsg.rpcName, out handler)) {
                workerContext.s = rpcMsg.sAgent;
                handler(workerContext, rpcMsg.attachData);
            } else {
                sLog.Error("executor", "executor {0} handle route rpc type {1} null.", this.mName, rpcMsg.rpcName);
            }
        }

        private void HandlerActionEvent(EventMsg eventMsg) {
            ActionEventMsg actionMsg = (ActionEventMsg)eventMsg;
            workerContext.s = actionMsg.sAgent;
            actionMsg.action(workerContext, actionMsg.attachData);
        }

        private void HandlerCloseSignal(EventMsg eventMsg) {
            isRunning = false;
        }

        Thread workerThread;
        AgentID agentID;
        string mName;
        bool isRunning = false;

        static ThreadLocal<Context> tlsCtx = new ThreadLocal<Context>();
        TimerManager timerManager = new TimerManager();
        Context workerContext = new Context();
        BlockingCollection<EventMsg> eConcurrQueue = new BlockingCollection<EventMsg>();
        Dictionary<EventID, MsgHandler> msgHandlerMap = new Dictionary<EventID, MsgHandler>();
        Dictionary<string, RpcHandler> rpcHandlerMap = new Dictionary<string, RpcHandler>();
    }
}
