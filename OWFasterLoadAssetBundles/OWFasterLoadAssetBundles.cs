using HarmonyLib;
using OWML.Common;
using OWML.ModHelper;
using System.Reflection;

namespace OWFasterLoadAssetBundles;

public class OWFasterLoadAssetBundles : ModBehaviour
{
	public static OWFasterLoadAssetBundles Instance;

	public void Awake()
	{
		Instance = this;
		Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
	}
}
