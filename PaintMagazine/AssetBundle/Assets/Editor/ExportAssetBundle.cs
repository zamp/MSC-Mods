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
		const string outDir = "E:/Steam/steamapps/common/My Summer Car/Mods/MSCPaintMagazine/";
		const string tempDir = "Assets/AssetBundles/";

		Directory.CreateDirectory(tempDir);
		BuildPipeline.BuildAssetBundles(tempDir, BuildAssetBundleOptions.ForceRebuildAssetBundle, BuildTarget.StandaloneWindows);
		File.Copy(tempDir + "98h4tvy9svgehysvgyihgfidbghy5boy8dr5obiy7h5ro", outDir + "bundle-windows", true);
		BuildPipeline.BuildAssetBundles(tempDir, BuildAssetBundleOptions.ForceRebuildAssetBundle, BuildTarget.StandaloneLinux);
		File.Copy(tempDir + "98h4tvy9svgehysvgyihgfidbghy5boy8dr5obiy7h5ro", outDir + "bundle-linux", true);
		BuildPipeline.BuildAssetBundles(tempDir, BuildAssetBundleOptions.ForceRebuildAssetBundle, BuildTarget.StandaloneOSXIntel);
		File.Copy(tempDir + "98h4tvy9svgehysvgyihgfidbghy5boy8dr5obiy7h5ro", outDir + "bundle-osx", true);
	}
}