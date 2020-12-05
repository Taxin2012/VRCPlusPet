# VRC-Minus-Pet
VRChat Mod that uses ["MelonLoader"](https://github.com/HerpDerpinstine/MelonLoader). <br>
Removes VRC+ advertising and can replace default pet and his phrages (if needed).
 
<h3>Mod uses launch parameters:</h3>

| Parameter  | Description |
| --- | --- |
| -mp.ads | The mod will not touch any VRC+ advertising |
| -mp.pet | Pet will be replaced |
| -mp.phs | Pet phrages will be replaced |


<h3>How to replace Phrases for Pet?</h3>

  1. Use "-mp.phs" parameter, run VRChat once;
  2. Open VRChat game folder;
  3. Change files in "UserData/Custom_Menu_Pet/".

  > "normal_phrases.txt" - Random phrages. <br>
  > "poke_phrases.txt" - Phrases when poking.


<h3>How to replace Pet?</h3>

  > Only for peoples who know how to build project. <br>
  > Mod currently supports only ".png" images.
  > Full AssetBundles build guide: [*Click Me*](https://docs.unity3d.com/Manual/AssetBundles-Workflow.html)
  
  1) Create New Project in Unity3D;
  2) Create a folder with name "VRCMP", rename an image that you want to use as pet to "pet.png" and put to created folder;
  3) Click on image, find "AssetBundle" sign in bottom of Inspector, click on it and click New (if no "vrcmp" in menu), enter "vrcmp";
  4) Set same "AssetBundle" name for "VRCMP" folder;
  
  6) Create a folder called "Editor" in the "Assets" folder, create a script with the following contents in that folder:

  ```csharp
  using UnityEditor;
  using System.IO;

  public class CreateAssetBundles
  {
      [MenuItem("Assets/Build AssetBundles")]
      static void BuildAllAssetBundles()
      {
          string assetBundleDirectory = "Assets/AssetBundles";
          if(!Directory.Exists(assetBundleDirectory))
          {
              Directory.CreateDirectory(assetBundleDirectory);
          }
          BuildPipeline.BuildAssetBundles(assetBundleDirectory, 
                                          BuildAssetBundleOptions.None, 
                                          BuildTarget.StandaloneWindows);
      }
  }
  ```
  
  7) Click right mouse button on folder "VRCMP" and find "Build AssetBundles" variant, select it, after building, find folder "AssetBundles", copy "vrcmp" file and replace same file in the Project Resources. <br><br>


This mod based on ["VRC-Minus"](https://github.com/HerpDerpinstine/VRC-Minus).
