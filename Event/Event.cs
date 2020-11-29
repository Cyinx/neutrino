
namespace neutrino {
    public delegate void ActionDoEvent(Context ctx, object[] args);
    enum EventType {
        EVENT_ROUTER_MSG,
        EVENT_ROUTE_RPC,
        EVENT_COMPONENT_CREATE,
        EVENT_COMPONENT_ERROR,
        EVENT_WORKER_CLOSE,
        EVENT_WORKER_ACTION,
    }
    public class EventMsg {
        internal EventType eventType;
        internal EventType GetEventType() {
            return eventType;
        }
    }

    class DataEventMsg : EventMsg {
        public EventID typeID;
        public Agent sAgent;
        public object attchData;

        public DataEventMsg() {
            eventType = EventType.EVENT_ROUTER_MSG;
        }
    }

    class RpcEventMsg : EventMsg {
        public string rpcName;
        public Agent sAgent;
        public object[] attachData;

        public RpcEventMsg() {
            eventType = EventType.EVENT_ROUTE_RPC;
        }
    }

    class ActionEventMsg : EventMsg {
        internal Agent sAgent;
        internal ActionDoEvent action;
        internal object[] attachData;

        public ActionEventMsg() {
            eventType = EventType.EVENT_WORKER_ACTION;
        }
    }
}
