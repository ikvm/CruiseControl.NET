using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Threading;

using tw.ccnet.core;
using tw.ccnet.core.configuration;
using tw.ccnet.remote;

namespace tw.ccnet.core 
{
	
	public class CruiseManager : MarshalByRefObject, ICruiseManager 
	{
		private CruiseControl _cruiseControl; 
		private Thread _cruiseControlThread;
		public const int TCP_PORT = 1234;

		public void InitializeCruiseControl(String configFileName)
		{
			ConfigurationLoader configLoader = new ConfigurationLoader(configFileName);
			_cruiseControl = new CruiseControl(configLoader);
		}
		
		public void InitializeThread()
		{
			_cruiseControlThread = new Thread(new ThreadStart(_cruiseControl.Start));
			_cruiseControlThread.Start();
		}
		
		public void StartCruiseControl()
		{
			if(_cruiseControlThread == null || !_cruiseControlThread.IsAlive)
			{
				InitializeThread();
			}
			_cruiseControl.Stopped = false;
		}
		
		public void StopCruiseControl()
		{
			Trace.WriteLine("CruiseControl is stopping");
			_cruiseControl.Stopped = true;
		}
		
		public void StopCruiseControlNow()
		{
			Trace.WriteLine("CruiseControl stopped");
			_cruiseControlThread.Abort();
			_cruiseControlThread = null;
		}
		
		public CruiseControlStatus GetStatus()
		{
			if(_cruiseControlThread == null)	
				return CruiseControlStatus.Stopped;
			else
			{
				if(_cruiseControl.Stopped)
				{
					if(_cruiseControlThread.IsAlive)
					{
						return CruiseControlStatus.WillBeStopped;
					}
					else
					{
						return CruiseControlStatus.Stopped;
					}
				}
				else
				{
					return CruiseControlStatus.Running;
				}
			}
		}				

		public ProjectStatus GetProjectStatus() 
		{
			IEnumerator e =_cruiseControl.Projects.GetEnumerator();
			e.MoveNext();
			Project p = (Project)e.Current;
			return new ProjectStatus(GetStatus(), p.GetLastBuildStatus(), p.CurrentActivity, p.Name); 
		}

		public override object InitializeLifetimeService() 
		{
			return null;
		}

		public void Run(string project, ISchedule schedule) 
		{
		}

        public string Configuration 
        {
            get { return "bob's your uncle"; }
            set { }
        }

        public void RegisterForRemoting() 
        {
            string configFile = System.Reflection.Assembly.GetEntryAssembly().Location + ".config";
            string uri = "CruiseManager.rem";

            RemotingConfiguration.Configure(configFile);
            RemotingServices.Marshal(this, uri);
 

            string url = uri;
            try 
            {
                IChannelReceiver channel = (IChannelReceiver)ChannelServices.RegisteredChannels[0];
                url = channel.GetUrlsForUri(uri)[0];

                ICruiseManager marshalledObject = (ICruiseManager) RemotingServices.Connect(typeof(ICruiseManager), url);
                marshalledObject.GetStatus(); // this will throw an exception if it didn't connect
                Console.WriteLine("listening on " + url);
            } 
            catch 
            {
                throw new Exception("couldn't listen on " + url);
            }
        }
    }
}