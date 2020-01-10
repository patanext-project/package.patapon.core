using System;
using Grpc.Core;

namespace Patapon4TLB.Core.MasterServer
{
	public static class StaticMasterServer
	{
		public static Channel channel => MasterServerSystem.Instance.channel;

		public static bool HasClient<T>() where T : ClientBase             => MasterServerSystem.Instance.HasClient<T>();
		public static void AddClient<T>(Func<T> func) where T : ClientBase => MasterServerSystem.Instance.AddClient<T>(func);
		public static T    GetClient<T>() where T : ClientBase             => MasterServerSystem.Instance.GetClient<T>();

		public static bool TryGetClient<T>(out T client) where T : ClientBase
		{
			if (!HasClient<T>())
			{
				client = null;
				return false;
			}

			client = GetClient<T>();
			return true;
		}
	}
}