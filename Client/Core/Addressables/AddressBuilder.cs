namespace PataNext.Client.Core.Addressables
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
		public             string Result => m_CurrentAddress;

		public virtual T      Folder(string  folder)   => new T {m_CurrentAddress = m_CurrentAddress + folder + "/"};
		public virtual string GetFile(string filePath) => m_CurrentAddress + filePath;
	}

	public class AddressBuilderClient : AddressBuilder<AddressBuilderClient>
	{
		public AddressBuilderInterface Interface()
		{
			return new AddressBuilderInterface {m_CurrentAddress = m_CurrentAddress}.Folder("Interface");
		}
	}

	public class AddressBuilderMixed : AddressBuilder<AddressBuilderMixed>
	{
	}

	public class AddressBuilderServer : AddressBuilder<AddressBuilderServer>
	{
	}

	public class AddressBuilderInterface : AddressBuilder<AddressBuilderInterface>
	{
		public AddressBuilderInterfaceGameMode GameMode()
		{
			return new AddressBuilderInterfaceGameMode {m_CurrentAddress = m_CurrentAddress}.Folder("GameMode");
		}
		
		public AddressBuilderInterfaceGameMode GameMode(string gameModeFolder)
		{
			return new AddressBuilderInterfaceGameMode {m_CurrentAddress = m_CurrentAddress}.Folder("GameMode")
			                                                                                .Folder(gameModeFolder);
		}
	}

	public class AddressBuilderInterfaceGameMode : AddressBuilder<AddressBuilderInterfaceGameMode>
	{
	}
}