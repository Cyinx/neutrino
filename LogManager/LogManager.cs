using System;
using System.Collections.Generic;
using System.Text;

namespace neutrino {
    enum LogLevel {
        DBUG,
        WARN,
        EROR,
        INFO,
    }
    public static class sLog {
        static ObjectPool<LogData> objectPool = new ObjectPool<LogData>(() => new LogData());
        enum LogWorkerEvent {
            QUEUE_LOG_OBJECT = 1,
        }
        static sLog() {
            worker.Init();
            worker.RegisterEventHandler((uint)LogWorkerEvent.QUEUE_LOG_OBJECT, OnQueueLogMsg);
        }

        internal static void Close() {
            worker.Close();
        }

        public static void Debug(string title, string content, params object[] args) {
            LogData xlog = objectPool.New();
            xlog.title = title;
            xlog.body.AppendFormat(content, args);
            xlog.logLevel = LogLevel.DBUG;
            worker.Enqueue(null, (uint)LogWorkerEvent.QUEUE_LOG_OBJECT, xlog);
        }

        public static void Error(string title, string content, params object[] args) {
            LogData xlog = objectPool.New();
            xlog.title = title;
            xlog.body.AppendFormat(content, args);
            xlog.logLevel = LogLevel.EROR;
            worker.Enqueue(null, (uint)LogWorkerEvent.QUEUE_LOG_OBJECT, xlog);
        }

        public static void OnQueueLogMsg(Context ctx, object arg) {
            LogData xlog = (LogData)arg;
            Console.WriteLine("[{0}] {1} {2}", xlog.logLevel.ToString(), DateTime.Now.ToString("hh:mm:ss"), xlog.body);
            xlog.Reset();
            objectPool.Free(xlog);
        }

        static ExecutorWorker worker = new ExecutorWorker("slog");
    }
}
