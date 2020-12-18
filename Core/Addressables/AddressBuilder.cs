using StormiumTeam.GameBase.Utility.Misc;

namespace PataNext.Client.Core.Addressables
{
	public static class AddressBuilder
	{
		public static AddressBuilderClient Client(string author = "st", string modpack = "pn")
		{
			return new AddressBuilderClient
			{
				AssetPath        = ($"{author}.{modpack}", string.Empty),
				m_CurrentAddress = string.Empty
			};
		}
	}

	public class AddressBuilder<T> where T : AddressBuilder<T>, new()
	{
		protected internal string m_CurrentAddress;
		public             string Result => m_CurrentAddress;

		public AssetPath AssetPath { get; internal set; }

		public virtual T Folder(string folder) =>
			new T
			{
				AssetPath        = AssetPath,
				m_CurrentAddress = m_CurrentAddress + folder + "/"
			};

		public virtual string    GetFile(string  filePath) => m_CurrentAddress + filePath;
		public virtual AssetPath GetAsset(string filePath) => (AssetPath.Bundle, m_CurrentAddress + filePath);
	}

	public class AddressBuilderClient : AddressBuilder<AddressBuilderClient>
	{
		public AddressBuilderInterface Interface()
		{
			return new AddressBuilderInterface {AssetPath = AssetPath, m_CurrentAddress = m_CurrentAddress}.Folder("Interface");
		}
	}

	public class AddressBuilderInterface : AddressBuilder<AddressBuilderInterface>
	{
		public AddressBuilderInterfaceMenu Menu()
		{
			return new AddressBuilderInterfaceMenu {AssetPath = AssetPath, m_CurrentAddress = m_CurrentAddress}.Folder("Menu");
		}

		public AddressBuilderInterfaceInGame InGame()
		{
			return new AddressBuilderInterfaceInGame {AssetPath = AssetPath, m_CurrentAddress = m_CurrentAddress}.Folder("InGame");
		}

		public AddressBuilderInterfaceGameMode GameMode()
		{
			return new AddressBuilderInterfaceGameMode {AssetPath = AssetPath, m_CurrentAddress = m_CurrentAddress}.Folder("GameMode");
		}

		public AddressBuilderInterfaceGameMode GameMode(string gameModeFolder)
		{
			return new AddressBuilderInterfaceGameMode {AssetPath = AssetPath, m_CurrentAddress = m_CurrentAddress}.Folder("GameMode")
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