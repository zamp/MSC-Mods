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
		const string outDir = "E:/Steam/steamapps/common/My Summer Car/Mods/Dynamite/";
		const string tempDir = "Assets/AssetBundles/";

		Directory.CreateDirectory(tempDir);
		BuildPipeline.BuildAssetBundles(tempDir, BuildAssetBundleOptions.ForceRebuildAssetBundle, BuildTarget.StandaloneWindows);
		File.Copy(tempDir + "hrt4vy97nsvgy9h7sbgy9h7bdg597y", outDir + "bundle-windows", true);
		BuildPipeline.BuildAssetBundles(tempDir, BuildAssetBundleOptions.ForceRebuildAssetBundle, BuildTarget.StandaloneLinux);
		File.Copy(tempDir + "hrt4vy97nsvgy9h7sbgy9h7bdg597y", outDir + "bundle-linux", true);
		BuildPipeline.BuildAssetBundles(tempDir, BuildAssetBundleOptions.ForceRebuildAssetBundle, BuildTarget.StandaloneOSXIntel);
		File.Copy(tempDir + "hrt4vy97nsvgy9h7sbgy9h7bdg597y", outDir + "bundle-osx", true);
	}
}