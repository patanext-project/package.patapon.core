using System;
using System.Collections.Generic;
using System.Net;
using P4TLB.MasterServer;
using Unity.Collections;
using Unity.Entities;
using Unity.Networking.Transport;

namespace Patapon4TLB.Core.MasterServer.Data
{
	public struct RequestServerList : IComponentData
	{
		public NativeString512 Query;

		public struct Processing : IComponentData
		{
		}

		public struct CompletionStatus : IRequestCompletionStatus, IComponentData
		{
			public bool error { get; set; }
		}
	}

	public class ResponseServiceList : IComponentData
	{
		public List<ServerInformation> Servers;
	}

	public struct RequestConnectToServer : IComponentData
	{
		public ulong          ServerUserId;
		public NativeString64 ServerLogin;
		public bool           ManualConnectionLabor;

		public struct Processing : IComponentData
		{
		}

		public struct CompletionStatus : IRequestCompletionStatus, IComponentData
		{
			public bool error => ConnectionErrorCode != 0;

			public ConnectionResponse.Types.ErrorCode ConnectionErrorCode;
		}
	}

	public struct ResponseConnectToServer : IComponentData
	{
		public NetworkEndPoint EndPoint;
	}

	public struct RequestDisconnectFromServer : IComponentData
	{
		public int foo;

		public struct Processing : IComponentData
		{
		}

		public struct CompletionStatus : IRequestCompletionStatus, IComponentData
		{
			public bool error { get; set; }
		}
	}

	public struct ResponseDisconnectFromServer : IComponentData
	{
	}

	public struct CurrentServerSingleton : IComponentData
	{
		public ulong ServerId;
	}

	public struct RequestServerInformation : IComponentData
	{
		public ulong          ServerUserId;
		public NativeString64 ServerLogin;

		public struct Processing : IComponentData
		{
		}

		public struct CompletionStatus : IRequestCompletionStatus, IComponentData
		{
			public bool error { get; set; }
		}

		public struct Automated : IAutomaticRequestComponent<RequestServerInformation>
		{
			public ulong          ServerId;
			public NativeString64 ServerLogin;

			public void SetRequest(ref RequestServerInformation original)
			{
				original.ServerUserId = ServerId;
				original.ServerLogin  = ServerLogin;
			}
		}
	}

	public class ResultServerInformation : IComponentData
	{
		public ServerInformation Information;
	}

	public struct RequestUpdateServerInformation : IComponentData
	{
		public NativeString128 Name;
		public int             CurrentUserCount;
		public int             MaxUsers;

		public struct Processing : IComponentData
		{
		}

		public struct CompletionStatus : IRequestCompletionStatus, IComponentData
		{
			public bool error => ErrorCode != SetServerInformationResponse.Types.ErrorCode.Ok;

			public SetServerInformationResponse.Types.ErrorCode ErrorCode;
		}
	}

	public struct ResultUpdateServerInformation : IComponentData
	{

	}
}