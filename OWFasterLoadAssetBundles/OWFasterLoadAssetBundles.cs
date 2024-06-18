using HarmonyLib;
using OWML.Common;
using OWML.ModHelper;
using System.Reflection;

namespace OWFasterLoadAssetBundles;

public class OWFasterLoadAssetBundles : ModBehaviour
{
	public static OWFasterLoadAssetBundles Instance;
	public static Harmony Harmony;

	public void Awake()
	{
		Instance = this;
		Harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());

		Patcher.ChainloaderInitialized(); // lol just call it manually there are no chain smokers here
	}
}
