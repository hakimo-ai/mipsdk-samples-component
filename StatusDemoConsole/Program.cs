﻿// StatusDemoConsole. Milestone XProtect MIP SDK sample Program
// Connects to an XProtect Corporate Management Server to log on and obtain a token plus system information
// Then connects to one XProtect Corporate Recording Server to pass the token and query status information

using System;
using System.Collections.Generic;
using System.Net;

// Added manually
using System.Threading;
using VideoOS.Common.Integration.Definition;
using VideoOS.Platform;
using VideoOS.Platform.SDK.Proxy.Status2;

// RecorderStatusService.cs can be generated by "svcutil.exe http://<host>:7563/RecorderStatusService/RecorderStatusService.asmx?wsdl"
// <host> is the hostname of a computer with an XProtect Corporate Recording Server v2.0 or later installed
// svcutil.exe can be found in a Microsoft Windows SDK

namespace StatusDemoConsole
{
    class Program
    {
        private static readonly Guid IntegrationId = new Guid("A67884C7-4088-4E56-8C14-B53A69865B1A");
        private const string IntegrationName = "Status Demo Console";
        private const string Version = "1.0";
        private const string ManufacturerName = "Sample Manufacturer";

        // Take name of XPCO Management Server from command line or use a hardcoded default.
        // This is the only value you must change to modify this to run on your site.

        static void Main(string[] args)
        {
        	VideoOS.Platform.SDK.Environment.Initialize();

			string hostManagementService = "http://localhost";
            if (args.GetLength(0) > 0)
                hostManagementService = args[0];
			if (hostManagementService.StartsWith("http://") == false)
				hostManagementService = "http://" + hostManagementService;

        	Uri uri = new UriBuilder(hostManagementService).Uri;
        	VideoOS.Platform.SDK.Environment.AddServer(uri, CredentialCache.DefaultNetworkCredentials);
			try
			{
				VideoOS.Platform.SDK.Environment.Login(uri, IntegrationId, IntegrationName, Version, ManufacturerName);
			}
			catch (Exception ex)
			{
				Console.WriteLine("Could not logon to management server: " + ex.Message);
				Console.WriteLine("");
				Console.WriteLine("Press any key");
				Console.ReadKey();
				return;
			}

			if (EnvironmentManager.Instance.CurrentSite.ServerId.ServerType != ServerId.CorporateManagementServerType)
            {
                Console.WriteLine("{0} is not an XProtect Corporate Management Server", hostManagementService);
				Console.WriteLine("");
				Console.WriteLine("Press any key");
				Console.ReadKey();
				return;
            }

        	VideoOS.Platform.Login.LoginSettings loginSettings =
        		VideoOS.Platform.Login.LoginSettingsCache.GetLoginSettings(hostManagementService);
        	Console.WriteLine("... Token=" + loginSettings.Token);

            // Limit this to 1 recording server. Here I select the last one.
            // If there are XPE servers acting as recording servers, you must filter them away by having an explicit list
            // With XPE, you don't use the Status API, but the Central API". See the sample "CentralDemo".

        	Item serverItem = Configuration.Instance.GetItem(EnvironmentManager.Instance.CurrentSite);
        	List<Item> serverItems = serverItem.GetChildren();
			Item recorder = null;
			foreach (Item item in serverItems)
			{
				if (item.FQID.Kind == Kind.Server && item.FQID.ServerId.ServerType == ServerId.CorporateRecordingServerType)
				{
					recorder = item;
				}
			}
        	List<Item> allItems = recorder.GetChildren();

        	//string hostRecorderService = recorder.Properties["Address"];
            VideoOS.Platform.SDK.Proxy.Status2.RecorderStatusService2 client = new VideoOS.Platform.SDK.Proxy.Status2.RecorderStatusService2(recorder.FQID.ServerId.Uri);
			
            // Start a status session with a recording server
            Guid sessionId = client.StartStatusSession(loginSettings.Token);

            // Set up to subscribe to all the user defined events on that recording server
        	List<Guid> subscribeTheseEventIds = new List<Guid>();
			List<Guid> subscribeTheseDeviceIds = new List<Guid>();

            List<Item> allEvents = FindAllEvents(allItems, serverItem.FQID.ObjectId);
			foreach (Item item in allEvents)
            {
                subscribeTheseEventIds.Add(item.FQID.ObjectId);
				subscribeTheseDeviceIds.Add(item.FQID.ObjectId);
            }
            allEvents = FindAllEvents(serverItems, serverItem.FQID.ObjectId);
			foreach (Item item in allEvents)
			{
				subscribeTheseEventIds.Add(item.FQID.ObjectId);
				subscribeTheseDeviceIds.Add(item.FQID.ObjectId);
			}
			
            // Set up to subscribe to almost all built-in events
            subscribeTheseEventIds.Add(BuiltInEventTypes.EventIDs.ArchiveDiskAvailable);
            subscribeTheseEventIds.Add(BuiltInEventTypes.EventIDs.ArchiveDiskUnavailable);
            subscribeTheseEventIds.Add(BuiltInEventTypes.EventIDs.CommunicationError);
            subscribeTheseEventIds.Add(BuiltInEventTypes.EventIDs.CommunicationStarted);
            subscribeTheseEventIds.Add(BuiltInEventTypes.EventIDs.CommunicationStopped);
            subscribeTheseEventIds.Add(BuiltInEventTypes.EventIDs.DatabaseDiskAvailable);
            subscribeTheseEventIds.Add(BuiltInEventTypes.EventIDs.DatabaseDiskFull);
            subscribeTheseEventIds.Add(BuiltInEventTypes.EventIDs.DatabaseDiskUnavailable);
            subscribeTheseEventIds.Add(BuiltInEventTypes.EventIDs.DatabaseRepair);
            subscribeTheseEventIds.Add(BuiltInEventTypes.EventIDs.DeviceSettingsChanged);
            subscribeTheseEventIds.Add(BuiltInEventTypes.EventIDs.DeviceSettingsChangedError);
            subscribeTheseEventIds.Add(BuiltInEventTypes.EventIDs.FeedOverflowStarted);
            subscribeTheseEventIds.Add(BuiltInEventTypes.EventIDs.FeedOverflowStopped);
            subscribeTheseEventIds.Add(BuiltInEventTypes.EventIDs.HardwareSettingsChanged);
            subscribeTheseEventIds.Add(BuiltInEventTypes.EventIDs.HardwareSettingsChangedError);
            subscribeTheseEventIds.Add(BuiltInEventTypes.EventIDs.InputActivated);
            subscribeTheseEventIds.Add(BuiltInEventTypes.EventIDs.InputChanged);
            subscribeTheseEventIds.Add(BuiltInEventTypes.EventIDs.InputDeactivated);
            subscribeTheseEventIds.Add(BuiltInEventTypes.EventIDs.LiveClientFeedRequested);
            subscribeTheseEventIds.Add(BuiltInEventTypes.EventIDs.LiveClientFeedTerminated);
            subscribeTheseEventIds.Add(BuiltInEventTypes.EventIDs.MotionStarted);
            subscribeTheseEventIds.Add(BuiltInEventTypes.EventIDs.MotionStopped);
            subscribeTheseEventIds.Add(BuiltInEventTypes.EventIDs.OutputActivated);
            subscribeTheseEventIds.Add(BuiltInEventTypes.EventIDs.OutputChanged);
            subscribeTheseEventIds.Add(BuiltInEventTypes.EventIDs.OutputDeactivated);
            subscribeTheseEventIds.Add(BuiltInEventTypes.EventIDs.PTZManualSessionStarted);
            subscribeTheseEventIds.Add(BuiltInEventTypes.EventIDs.PTZManualSessionStopped);
            subscribeTheseEventIds.Add(BuiltInEventTypes.EventIDs.RecordingStarted);
            subscribeTheseEventIds.Add(BuiltInEventTypes.EventIDs.RecordingStopped);

        	Guid[] eventArray = new Guid[subscribeTheseEventIds.Count];
			subscribeTheseEventIds.CopyTo(eventArray);

            // Now, do the actual event subscription
            // Every time an event occurs, you will be informed next time you query the status
			client.SubscribeEventStatus(loginSettings.Token, sessionId, eventArray);
            
            // Subscribe to configuration changes
            // Every time a change occurs, you will be informed next time you query the status
       		client.SubscribeConfigurationStatus(loginSettings.Token, sessionId, true);

            // Set up to subscribe to device descriptions
            // First time you query the status, you will get information on all devices subsribed to
            // Subsequent queries will return changed values only
            // You may subscribe to a device once again to force information to be returned on that device
        	Dictionary<Guid, Item> allCameras = new Dictionary<Guid, Item>();
			FindAllCameras(allItems, recorder.FQID.ServerId.Id, allCameras);
			foreach (Item item in allCameras.Values)
			{
				subscribeTheseDeviceIds.Add(item.FQID.ObjectId);
			}

            // Now, do the actual device subscription
			client.SubscribeDeviceStatus(loginSettings.Token,sessionId,subscribeTheseDeviceIds.ToArray());
            
            // Now we are ready to enter a loop querying for status information with 2 seconds sleep after each
            int count = 30;
            int intr = 2000;
		    while (count > 0)
		    {
		        count--;

		        Status stats = client.GetStatus(loginSettings.Token, sessionId, 5000);

		        foreach (EventStatus eStat in stats.EventStatusArray)
		        {
		            Console.WriteLine(eStat.Time.ToLocalTime().ToString() + " Event " + eStat.EventId.ToString() + " guid " +
		                              eStat.EventId.ToString() +
		                              " Source " + eStat.SourceId.ToString());
		            if (eStat.Metadata != null)
		                foreach (KeyValue keyValue in eStat.Metadata)
		                    Console.WriteLine("    ---> Metadata:  " + keyValue.Key + " = " + keyValue.Value);
		        }
		        if (stats.ConfigurationChangedStatus != null)
		        {
		            ConfigurationChangedStatus eStat = stats.ConfigurationChangedStatus;
		            Console.WriteLine(eStat.Time.ToLocalTime().ToString() + " Configuration change ");
		        }

		        foreach (SpeakerDeviceStatus eStat in stats.SpeakerDeviceStatusArray)
		        {
		            Console.WriteLine(eStat.Time.ToLocalTime().ToString() + " Speaker " + eStat.DeviceId.ToString() +
		                              " Started " + eStat.Started.ToString());
		        }

		        foreach (OutputDeviceStatus eStat in stats.OutputDeviceStatusArray)
		        {
		            Console.WriteLine(eStat.Time.ToLocalTime().ToString() + " Output " + eStat.DeviceId.ToString() +
		                              " Started " + eStat.Started.ToString() +
		                              " State " + eStat.State.ToString());
		        }
                
		        foreach (CameraDeviceStatus eStat in stats.CameraDeviceStatusArray)
		        {
		            Console.WriteLine(eStat.Time.ToLocalTime().ToString() + " Camera " + allCameras[eStat.DeviceId].Name +
		                              " Started " + eStat.Started.ToString() +
		                              " Recording " + eStat.Recording.ToString());
		        }
                
		        foreach (MicrophoneDeviceStatus eStat in stats.MicrophoneDeviceStatusArray)
		        {
		            Console.WriteLine(eStat.Time.ToLocalTime().ToString() + " Microphone " + eStat.DeviceId.ToString() +
		                              " Started " + eStat.Started.ToString() +
		                              " Error " + eStat.Error.ToString());
		        }

		        foreach (InputDeviceStatus eStat in stats.InputDeviceStatusArray)
		        {
		            Console.WriteLine(eStat.Time.ToLocalTime().ToString() + " Input " + eStat.DeviceId.ToString() +
		                              " Started " + eStat.Started.ToString());
		        }

		        Console.WriteLine("{0} queries remaining", count);
		        Thread.Sleep(intr);
		    }
		    Console.WriteLine("");
			Console.WriteLine("Press any key");
			Console.ReadKey();

            // Stop the status session. 
            client.StopStatusSession(loginSettings.Token, sessionId);

            // The token will time out automatically a little later.
        	VideoOS.Platform.SDK.Environment.RemoveAllServers();
        }

        private static List<Item> FindAllEvents(List<Item> items, Guid managementServerGuid)
        {
            List<Item> result = new List<Item>();
            foreach (Item item in items)
            {
                if (item.FQID.Kind == Kind.TriggerEvent && 
                    item.FQID.ParentId == managementServerGuid &&
                    item.FQID.FolderType == FolderType.No)
                {
                    result.Add(item);
                }
                else if (item.FQID.Kind == Kind.InputEvent && 
                    item.FQID.FolderType == FolderType.No)
                {
                    result.Add(item);
                }
                else if (item.FQID.FolderType != FolderType.No)
                {
                    result.AddRange(FindAllEvents(item.GetChildren(), managementServerGuid));
                }
            }
            return result;
        }

        private static void FindAllCameras(List<Item> items, Guid recorderGuid, Dictionary<Guid, Item> result)
		{
			foreach (Item item in items)
			{
				if (item.FQID.Kind == Kind.Camera && item.FQID.ParentId == recorderGuid && item.FQID.FolderType== FolderType.No)
				{
					if (result.ContainsKey(item.FQID.ObjectId)==false)
						result[item.FQID.ObjectId] = item;
				}
				else
					if (item.FQID.FolderType != FolderType.No)
					{
						FindAllCameras(item.GetChildren(), recorderGuid, result);
					}
			}
		}
	}
}

// This is a repetition of internal definitions, which ought to be exposed in a more official way later on
// For now, this is better than not having access to these events
namespace VideoOS.Common.Integration.Definition
{
    public sealed class BuiltInEventTypes
    {
        public sealed class EventIDs
        {
            // PTZ Events
            public static readonly Guid PTZManualSessionStarted = new Guid("2EEB582A-1506-4D73-96C9-AD356ACF85FB");
            public static readonly Guid PTZManualSessionStopped = new Guid("EED47283-694F-4683-9E4D-18E7D93CF796");

            // ImageServer Events
            public static readonly Guid LiveClientFeedRequested = new Guid("EEDA47FF-4F3D-459E-8143-69896C7C74AD");
            public static readonly Guid LiveClientFeedTerminated = new Guid("66BDD008-B856-44B6-B385-B1F6C3F76F1B");

            // Motion Plugin Events
            public static readonly Guid MotionStarted = new Guid("6EB95DD6-7CCC-4BCE-99F8-AF0D0B582D77");
            public static readonly Guid MotionStopped = new Guid("6F55A7A7-D21C-4629-AC18-AF1975E395A2");

            // Bookmark Events
            public static readonly Guid BookmarkReferenceRequested = new Guid("7CFC6709-2C16-405f-9182-0BDB6F9F7E43");

            // Rule Engine / Timer Events
            public static readonly Guid RelativeTimerEvent = new Guid("6A00FD9A-A7F1-446C-ABC2-29639CECD595");
            public static readonly Guid PeriodicTimerEvent = new Guid("F07AC0E3-9662-4781-9176-EC9E488AA494");

            // Media Database Events
            public static readonly Guid RecordingStarted = new Guid("4577F552-765A-438c-BC7D-E5FF1F754BC3");
            public static readonly Guid RecordingStopped = new Guid("79A94F89-92DE-4fca-8A43-5561D407423D");
            public static readonly Guid DatabaseRepair = new Guid("B6C4B695-7404-414D-A229-33BF83FFE255");
            public static readonly Guid DatabaseDiskFull = new Guid("A4935AA3-C4B6-42A3-B4C5-6D4C20B043CF");
            public static readonly Guid DatabaseDiskUnavailable = new Guid("29986AA1-924B-4c8d-AAC7-A876883DC82C");
            public static readonly Guid DatabaseDiskAvailable = new Guid("FAB03C4D-0589-48c8-9328-E28B76C95AF0");
            public static readonly Guid ArchiveDiskUnavailable = new Guid("72C29AAE-DDB2-46df-B6FD-7F728A85F292");
            public static readonly Guid ArchiveDiskAvailable = new Guid("B7F0003B-1392-450e-B6CC-9BBE6E7DF065");

            // Driver Events
            public static readonly Guid HardwareSettingsChanged = new Guid("A600C492-11C8-4D65-92DB-6D46209CE9EA");
            public static readonly Guid HardwareSettingsChangedError = new Guid("829A93AB-1AC0-4AEB-BDA0-ED8F6D8F514B");
            public static readonly Guid InputActivated = new Guid("836CA458-A833-4742-8EE0-64B2380984BD");
            public static readonly Guid InputDeactivated = new Guid("8666E3DC-57A7-4F38-9611-51D774EE7358");
            public static readonly Guid InputChanged = new Guid("FDA121B3-2070-4F31-A086-9F74E16580C0");
            public static readonly Guid InputSensor = new Guid("F84C8443-BFE3-4ECA-B69B-07DAEBC63A02");
            public static readonly Guid OutputActivated = new Guid("7A78F5BB-D8C3-4997-89B7-CAE72713B7DB");
            public static readonly Guid OutputDeactivated = new Guid("35742498-BCC5-4F0A-9800-827C9388D1CD");
            public static readonly Guid OutputChanged = new Guid("672F9F80-55FE-496F-BEE4-6C870BAB5064");
            public static readonly Guid FeedOverflowStarted = new Guid("536AD730-05AE-42CD-B14C-07B07C293E79");
            public static readonly Guid FeedOverflowStopped = new Guid("247FEF11-15DB-4C99-99DB-DE5475EAE443");
            public static readonly Guid CommunicationError = new Guid("A334AF1C-4B4B-4957-9E5F-AB8CA07FEAB6");
            public static readonly Guid CommunicationStarted = new Guid("DD3E6464-7DC0-405A-A92F-6150587563E8");
            public static readonly Guid CommunicationStopped = new Guid("0EE90664-2924-42A0-A816-4129D0ECABDC");
            public static readonly Guid DeviceSettingsChanged = new Guid("4C8BE5ED-80FA-43E7-BF5B-4E7570AD0025");
            public static readonly Guid DeviceSettingsChangedError = new Guid("87BE2A0D-C33B-4B5F-AE1F-AFA6770A2497");

            // Failover Events
            public static readonly Guid FailoverStarted = new Guid("F951B1F0-2FED-48F7-88D3-49EB5999C923");
            public static readonly Guid FailoverStopped = new Guid("C7B9227A-8E11-451B-BC9C-8EA659731012");
        }
    }
}