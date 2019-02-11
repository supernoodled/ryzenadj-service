using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices; //Service status
namespace ryzenadj_service {
    public partial class Service1 : ServiceBase {
        //structs needed to send status
        public enum ServiceState {
            SERVICE_STOPPED = 0x00000001,
            SERVICE_START_PENDING = 0x00000002,
            SERVICE_STOP_PENDING = 0x00000003,
            SERVICE_RUNNING = 0x00000004,
            SERVICE_CONTINUE_PENDING = 0x00000005,
            SERVICE_PAUSE_PENDING = 0x00000006,
            SERVICE_PAUSED = 0x00000007,
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct ServiceStatus {
            public int dwServiceType;
            public ServiceState dwCurrentState;
            public int dwControlsAccepted;
            public int dwWin32ExitCode;
            public int dwServiceSpecificExitCode;
            public int dwCheckPoint;
            public int dwWaitHint;
        };
        //pinvoke the setservicestatus func
        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceStatus(System.IntPtr handle, ref ServiceStatus serviceStatus);
        //XD
        int monitorTime = 60000; //1 min
        public Service1(string[] args) {
            InitializeComponent();
            //logging
            string eventSourceName = "ryzenadj-service-source";
            string logName = "ryzenadj-service-log";
            if(args.Length == 0) {
                monitorTime = 60000; //1 min
            }
            else {
                monitorTime = Convert.ToInt32(args[0]); //lol
                if (monitorTime.Equals(0)) {
                    monitorTime = 60000; //check if null lol
                }
            }
            eventLog1 = new EventLog();
            if (!EventLog.SourceExists(eventSourceName)) {
                EventLog.CreateEventSource(eventSourceName, logName);
            }
            eventLog1.Source = eventSourceName;
            eventLog1.Log = logName;
        }
        //timer
        private System.Timers.Timer timer;
        //start of service
        protected override void OnStart(string[] args) {
            eventLog1.Clear(); //cleanup
            // Set up a timer
            try {
                System.Timers.Timer timer = new System.Timers.Timer();
                timer.Interval = monitorTime; //default 1 min
                timer.Elapsed += OnTimer;
                timer.AutoReset = true;
                timer.Start();
            }
            catch (Exception) {
                eventLog1.WriteEntry("error with starting timer");
            }
            // Update the service state to Start Pending.
            ServiceStatus serviceStatus = new ServiceStatus {
                dwCurrentState = ServiceState.SERVICE_START_PENDING,
                dwWaitHint = 100000
            };
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
            // Update the service state to Running.
            serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
            //goto OnTimer :)
        }
        //stopping service
        protected override void OnStop() {
            // Service stopped. Also stop the timer.
            try {
                timer.Stop();
                timer.Dispose();
                timer = null;
            }
            catch(Exception) {
                eventLog1.WriteEntry("error with stopping timer");
            }
            // Update the service state to stop pending.
            ServiceStatus serviceStatus = new ServiceStatus {
                dwCurrentState = ServiceState.SERVICE_STOP_PENDING,
                dwWaitHint = 100000
            };
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
            // Update the service state to stopped.
            serviceStatus.dwCurrentState = ServiceState.SERVICE_STOPPED;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
        }
        //thanks microsoft.com... xD
        public void OnTimer(object sender, System.Timers.ElapsedEventArgs args) {
            //ok, so every minute or so we are gonna set the power limit from bat
            try {
                Process.Start(System.IO.Path.GetDirectoryName(new Uri(this.GetType().Assembly.GetName().CodeBase).LocalPath) + "//autostart.bat");
            }
            catch (Exception) {
                eventLog1.WriteEntry("failed to run autostart.bat");
            }
            finally {
                timer.Start();
            }
        }
    }
}
