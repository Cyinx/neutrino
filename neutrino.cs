using System;
using System.Threading;
using System.Threading.Tasks;

namespace neutrino
{
    public static class Neutrino
    {
        static Semaphore signalWait = new Semaphore(0, 1);
        public static void Run() {
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionEventHandler;

            TransportManager.Start();
            ExecutorMgr.Start();

            
            Console.CancelKeyPress += (object sender, ConsoleCancelEventArgs args) => {
                signalWait.Release();
            };

            signalWait.WaitOne();

            ExecutorMgr.Close();
            TransportManager.Close();
            sLog.Close();
        }

        private static void UnhandledExceptionEventHandler(object sender, UnhandledExceptionEventArgs e) {

        }
    }
}
