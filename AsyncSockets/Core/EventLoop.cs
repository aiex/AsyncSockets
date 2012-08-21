using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncSockets.Core
{
    public class EventLoop
    {
        #region Singleton
        private static EventLoop instance;
        internal static EventLoop Instance
        {
            get { return instance; }
        }
        static EventLoop()
        {
            instance = new EventLoop();
        }
        #endregion

        private Queue<Action> events;
        private AutoResetEvent eventPushed;
        private bool running = true;

        private Thread ticker;

        private EventLoop()
        {
            events = new Queue<Action>();
            eventPushed = new AutoResetEvent(false);
            Console.CancelKeyPress += cancelPressed;

            ticker = new Thread(tickerLoop);
        }

        internal void Push(Action action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            lock (events)
                events.Enqueue(action);

            lock (events)
                Console.WriteLine("Queue depth: {0}", events.Count);

            eventPushed.Set();
        }

        internal void RunInternal()
        {
            ticker.Start();
            Console.WriteLine("Starting main loop");
            while (true)
            {
                eventPushed.WaitOne();

                if (events.Count > 0)
                    while (events.Count > 0)
                        events.Dequeue()();

                if (!running)
                    break;
            }
            Console.WriteLine("Main loop halted");
        }

        public static void Run()
        {
            Instance.RunInternal();
        }

        void cancelPressed(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            Console.WriteLine("Control-C received");
            running = false;
            eventPushed.Set();
        }

        void tickerLoop()
        {
            while (true)
            {
                if (!running)
                    break;

                eventPushed.Set();

                Thread.Sleep(10);
            }
        }
    }
}
