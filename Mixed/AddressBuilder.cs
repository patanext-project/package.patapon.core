namespace DefaultNamespace
{
	public static class AddressBuilder
	{
		public static AddressBuilderClient Client()
		{
			return new AddressBuilderClient {m_CurrentAddress = "core://Client/"};
		}

		public static AddressBuilderMixed Mixed()
		{
			return new AddressBuilderMixed {m_CurrentAddress = "core://Mixed/"};
		}

		public static AddressBuilderServer Server()
		{
			return new AddressBuilderServer {m_CurrentAddress = "core://Server/"};
		}
	}

	public class AddressBuilder<T> where T : AddressBuilder<T>, new()
	{
		protected internal string m_CurrentAddress;
		public    string Result => m_CurrentAddress;

		public virtual T      Folder(string  folder)   => new T {m_CurrentAddress = m_CurrentAddress + folder + "/"};
		public virtual string GetFile(string filePath) => m_CurrentAddress + filePath;
	}

	public class AddressBuilderClient : AddressBuilder<AddressBuilderClient>
	{

	}

	public class AddressBuilderMixed : AddressBuilder<AddressBuilderMixed>
	{
	}

	public class AddressBuilderServer : AddressBuilder<AddressBuilderServer>
	{
	}
}