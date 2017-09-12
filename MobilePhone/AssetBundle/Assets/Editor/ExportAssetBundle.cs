using UnityEditor;
using System;
using System.Linq;
using System.IO;
using System.Collections;

public class CreateAssetBundles
{
	[MenuItem ("Assets/Build AssetBundles")]
	static void BuildAllAssetBundles ()
	{
		const string outDir = "E:/Steam/steamapps/common/My Summer Car/Mods/MobilePhone/";
		const string tempDir = "Assets/AssetBundles/";

		Directory.CreateDirectory(tempDir);
		BuildPipeline.BuildAssetBundles(tempDir, BuildAssetBundleOptions.ForceRebuildAssetBundle, BuildTarget.StandaloneWindows);
		File.Copy(tempDir + "vtjgvy9gvdhnkunhvghnusohi9ut4834b092yb0762b98670947", outDir + "bundle-windows", true);
		BuildPipeline.BuildAssetBundles(tempDir, BuildAssetBundleOptions.ForceRebuildAssetBundle, BuildTarget.StandaloneLinux);
		File.Copy(tempDir + "vtjgvy9gvdhnkunhvghnusohi9ut4834b092yb0762b98670947", outDir + "bundle-linux", true);
		BuildPipeline.BuildAssetBundles(tempDir, BuildAssetBundleOptions.ForceRebuildAssetBundle, BuildTarget.StandaloneOSXIntel);
		File.Copy(tempDir + "vtjgvy9gvdhnkunhvghnusohi9ut4834b092yb0762b98670947", outDir + "bundle-osx", true);
	}
}