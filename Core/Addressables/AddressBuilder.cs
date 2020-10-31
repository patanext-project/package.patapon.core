namespace PataNext.Client.Core.Addressables
{
	public static class AddressBuilder
	{
		public static AddressBuilderClient Client(string author = "st", string modpack = "pn")
		{
			return new AddressBuilderClient {m_CurrentAddress = $"cr://{author}.{modpack}/"};
		}

		public static AddressBuilderMixed Mixed(string author = "st", string modpack = "pn")
		{
			return new AddressBuilderMixed {m_CurrentAddress = $"cr://{author}.{modpack}/Mixed/"};
		}

		public static AddressBuilderServer Server(string author = "st", string modpack = "pn")
		{
			return new AddressBuilderServer {m_CurrentAddress = $"cr://{author}.{modpack}/Server/"};
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
		public AddressBuilderInterfaceMenu Menu()
		{
			return new AddressBuilderInterfaceMenu {m_CurrentAddress = m_CurrentAddress}.Folder("Menu");
		}
		
		public AddressBuilderInterfaceInGame InGame()
		{
			return new AddressBuilderInterfaceInGame {m_CurrentAddress = m_CurrentAddress}.Folder("InGame");
		}

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
	
	public class AddressBuilderInterfaceInGame : AddressBuilder<AddressBuilderInterfaceInGame>
	{
	}

	public class AddressBuilderInterfaceGameMode : AddressBuilder<AddressBuilderInterfaceGameMode>
	{
	}
	
	public class AddressBuilderInterfaceMenu : AddressBuilder<AddressBuilderInterfaceMenu>
	{
	}
}