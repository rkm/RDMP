using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using CatalogueLibrary.Data.Automation;
using CommandLine;
using Timer = System.Timers.Timer;

namespace RDMPAutomationService
{
    /// <summary>
    /// Host container for an RDMPAutomationLoop which handles ressurecting it if it crashes and recording starting/stopping events to the console / windows
    /// logs (when running as a windows service).
    /// </summary>
    public class AutoRDMP
    {
        public event EventHandler<ServiceEventArgs> LogEvent;

        private RDMPAutomationLoop host;

        private readonly Action<EventLogEntryType, string> logAction;
        private readonly Timer timer;
        private bool hostStarted;

        public AutoRDMP()
        {
            logAction = (et, msg) => OnLogEvent(new ServiceEventArgs() { EntryType = et, Message = msg });
            timer = new Timer(600000);
            timer.Elapsed += (sender, args) => Start();
            timer.Start();

            InitialiseAutomationLoop();
        }

        private void InitialiseAutomationLoop()
        {
            host = new RDMPAutomationLoop(new AutomationServiceOptions(), logAction);
            host.Failed += OnHostServiceFailure;
            host.StartCompleted += OnHostStarted;
        }

        private void OnHostServiceFailure(object sender, ServiceEventArgs e)
        {
            var serviceEventArgs = new ServiceEventArgs()
            {
                EntryType = e.EntryType,
                Message = "RDMP Automation Loop did not start: \r\n\r\n" + e.Message +
                          "\r\n\r\nWill try again in about " + timer.Interval / 60000 + " minute(s)."
            };
            if (e.Exception != null)
            {
                serviceEventArgs.Message += "\r\n\r\n" + e.Exception.Message +
                                            "\r\n" + e.Exception.StackTrace;
            }

            OnLogEvent(serviceEventArgs);
            Stop();
            Environment.Exit(666);
        }

        private void OnHostStarted(object sender, ServiceEventArgs e)
        {
            OnLogEvent(new ServiceEventArgs() { EntryType = e.EntryType, Message = e.Message });
            hostStarted = true;
        }

        public void Start()
        {
            if (hostStarted)
                return;

            OnLogEvent(new ServiceEventArgs()
            {
                EntryType = EventLogEntryType.Information,
                Message = "Starting Host Container..."
            });

            host.Start();
            
            if (Environment.UserInteractive)
            {
                // running as console app
                Console.WriteLine("Press any key to stop...");
                Console.ReadKey(true);
                Stop();
            }
        }
        
        public void Stop()
        {
            host.Stop = true;
        }

        protected virtual void OnLogEvent(ServiceEventArgs e)
        {
            var handler = LogEvent;
            if (handler != null)
            {
                handler(this, e);
            }
            else
            {
                Console.WriteLine("{0}: {1}", e.EntryType.ToString().ToUpper(), e.Message);
            }
        }
    }
}