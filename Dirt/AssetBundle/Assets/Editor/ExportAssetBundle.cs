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
		const string outDir = "E:/Steam/steamapps/common/My Summer Car/Mods/MSCDirtMod/";
		const string tempDir = "Assets/AssetBundles/";

		Directory.CreateDirectory(tempDir);
		BuildPipeline.BuildAssetBundles(tempDir, BuildAssetBundleOptions.ForceRebuildAssetBundle, BuildTarget.StandaloneWindows);
		File.Copy(tempDir + "t9ytiy8dtyi87dg5rvbyi7rbdg5iy7hg5rdbyi7h", outDir + "bundle-windows", true);
		BuildPipeline.BuildAssetBundles(tempDir, BuildAssetBundleOptions.ForceRebuildAssetBundle, BuildTarget.StandaloneLinux);
		File.Copy(tempDir + "t9ytiy8dtyi87dg5rvbyi7rbdg5iy7hg5rdbyi7h", outDir + "bundle-linux", true);
		BuildPipeline.BuildAssetBundles(tempDir, BuildAssetBundleOptions.ForceRebuildAssetBundle, BuildTarget.StandaloneOSXIntel);
		File.Copy(tempDir + "t9ytiy8dtyi87dg5rvbyi7rbdg5iy7hg5rdbyi7h", outDir + "bundle-osx", true);
	}
}