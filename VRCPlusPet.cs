
using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Collections;

using Harmony;
using MelonLoader;
using MethodBase = System.Reflection.MethodBase;

using UnityEngine;
using UnityEngine.UI;

using UIExpansionKit.API;

namespace VRCPlusPet
{
    public static class BuildInfo
    {
        public const string Name = "VRCPlusPet";
        public const string Description = "Hides VRC+ advertising, can replace default pet, his phrases and poke sounds.";
        public const string Author = "Taxin2012";
        public const string Company = null;
        public const string Version = "1.0.3";
        public const string DownloadLink = "https://github.com/Taxin2012/VRCPlusPet";
    }

    public class VRCPlusPet : MelonMod
    {
        static string
            configPath = "VRCPlusPet_Config",
            fullconfigPath = Path.Combine(MelonLoaderBase.UserDataPath, configPath),
            uixURL = "https://raw.githubusercontent.com/Taxin2012/VRCPlusPet/master/uix_link.txt",
            uixPath = Path.Combine(Environment.CurrentDirectory, "Mods/UIExpansionKit.dll"),
            uixVersionPath = Path.Combine(fullconfigPath, "uixVersion"),

            mlCfgNameHideAds = "Hide Ads",
            mlCfgNameReplacePet = "Replace Pet",
            mlCfgNameReplacePhrases = "Replace Phrases",
            mlCfgNameReplaceSounds = "Replace Sounds";
            
        static Il2CppSystem.Collections.Generic.List<string> petNormalPhrases = new Il2CppSystem.Collections.Generic.List<string>();
        static Il2CppSystem.Collections.Generic.List<string> petPokePhrases = new Il2CppSystem.Collections.Generic.List<string>();
        static Il2CppSystem.Collections.Generic.List<string> emptyList = null;
        static Il2CppSystem.Collections.Generic.List<AudioClip> audioClips = new Il2CppSystem.Collections.Generic.List<AudioClip>();
        static HarmonyInstance modHarmonyInstance = HarmonyInstance.Create(BuildInfo.Name);
        static Sprite petSprite;

        static void Patch(MethodBase TargetMethod, HarmonyMethod Prefix, HarmonyMethod Postfix)
        {
            modHarmonyInstance.Patch(TargetMethod, Prefix, Postfix);
        }

        static HarmonyMethod GetLocalPatch(string name)
        {
            return new HarmonyMethod(typeof(VRCPlusPet).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic));
        }

        static void InitUI()
        {
            GameObject.DestroyImmediate(GameObject.Find("UserInterface/MenuContent/Screens/UserInfo/User Panel/Supporter"));
            GameObject.DestroyImmediate(GameObject.Find("UserInterface/MenuContent/Screens/Avatar/Vertical Scroll View/Viewport/Content/Favorite Avatar List/GetMoreFavorites"));

            Transform tabTransform = GameObject.Find("UserInterface/MenuContent/Backdrop/Header/Tabs/ViewPort/Content").transform;

            for(int i = 0; i < tabTransform.childCount; i++)
            {
                Transform childTransform = tabTransform.GetChild(i);
                string childName = childTransform.name;

                if (childName != "Search")
                {
                    if (childName == "UserIconTab" || childName == "VRC+PageTab")
                        GameObject.DestroyImmediate(childTransform.gameObject);
                    else
                        childTransform.GetComponent<LayoutElement>().preferredWidth = 250f;
                }
            }
        }

        //from VRC-Minus (bottom of the README file)
        static bool ShortcutMenuPatch(ShortcutMenu __instance)
        {
            __instance.vrcplusThankYou?.SetActive(true);
            __instance.userIconButton?.SetActive(false);
            __instance.userIconCameraButton?.SetActive(false);
            __instance.userIconLearnMoreButton?.SetActive(false);
            __instance.vrcplusBanner?.SetActive(false);
            __instance.vrcplusMiniBanner?.SetActive(false);

            return false;
        }

        static void SetupMenuPetPatch(VRCPlusThankYou __instance)
        {
            __instance.oncePerWorld = false;

            if (petNormalPhrases.Count > 0)
                __instance.normalPhrases = petNormalPhrases;

            if (petPokePhrases.Count > 0)
                __instance.pokePhrases = petPokePhrases;

            if (audioClips.Count > 0)
                __instance.sounds = audioClips;

            __instance.gameObject.SetActive(true);
            __instance.transform.parent.gameObject.SetActive(true);

            if (petSprite != null)
            {
                Image imageComponent = __instance.transform.FindChild("Character")?.GetComponent<Image>();

                if (imageComponent != null)
                    imageComponent.sprite = petSprite;
            }
        }

        static IEnumerator SetupAudioFile(string fileName)
        {
            WWW www = new WWW(fileName);
            yield return www;

            AudioClip audioClip = www.GetAudioClip();
            audioClip.hideFlags |= HideFlags.DontUnloadUnusedAsset;

            audioClips.Add(audioClip);
        }

        static string SetupConfigFiles(string fileName, ref Il2CppSystem.Collections.Generic.List<string> phrasesArray, bool isDirectory = false)
        {
            if (!Directory.Exists(fullconfigPath))
                Directory.CreateDirectory(fullconfigPath);

            string filePath = Path.Combine(fullconfigPath, fileName);

            if (isDirectory)
            {
                if (!Directory.Exists(filePath))
                    Directory.CreateDirectory(filePath);

                return filePath;
            }

            if (File.Exists(filePath))
            {
                if (phrasesArray != null)
                    foreach (string line in File.ReadAllLines(filePath))
                        if (!string.IsNullOrEmpty(line))
                            phrasesArray.Add(line);

                return filePath;
            }
            else
            {
                if (phrasesArray != null)
                    File.Create(filePath);

                return null;
            }
        }

        static void SetupToggleButton(string displayText, string configName)
        {
            ExpansionKitApi.GetExpandedMenu(ExpandedMenu.UiElementsQuickMenu).AddToggleButton(displayText,
            (bool boolVar) => MelonPrefs.SetBool(BuildInfo.Name, configName, boolVar),
            () => MelonPrefs.GetBool(BuildInfo.Name, configName));
        }

        public override void OnApplicationStart()
        {
            string spaces = new string('-', 40);
            MelonLogger.Log(spaces);
            MelonLogger.Log("Initializing...");

            MelonPrefs.RegisterCategory(BuildInfo.Name, BuildInfo.Name);
            MelonPrefs.RegisterBool(BuildInfo.Name, mlCfgNameHideAds, true);
            MelonPrefs.RegisterBool(BuildInfo.Name, mlCfgNameReplacePet, false);
            MelonPrefs.RegisterBool(BuildInfo.Name, mlCfgNameReplacePhrases, false);
            MelonPrefs.RegisterBool(BuildInfo.Name, mlCfgNameReplaceSounds, false);

            MelonLogger.Log("Checking UIExpansionKit version... ");
            MelonLogger.Log(string.Format("Fetching UIX URL: [{0}]", uixURL));

            HttpWebResponse response = (HttpWebResponse)WebRequest.Create(uixURL).GetResponse();

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string[] versionAndUIXUrl = new StreamReader(response.GetResponseStream()).ReadToEnd().Split('|');

                if (!File.Exists(uixPath) || !File.Exists(uixVersionPath) || File.ReadAllText(uixVersionPath) != versionAndUIXUrl[0])
                {
                    MelonLogger.Log("Downloading UIExpansionKit...");

                    new WebClient().DownloadFile(versionAndUIXUrl[1], uixPath);
                    File.WriteAllText(uixVersionPath, versionAndUIXUrl[0]);

                    MelonLogger.LogWarning("UIExpansionKit successfully downloaded! Restart needed!");

                    //UIX working strange :(
                    /*Assembly uiExpansionKitAssembly = Assembly.LoadFile(uixPath);
                    var uixMainClass = uiExpansionKitAssembly.GetType("UIExpansionKit.UiExpansionKitMod");

                    var method = uixMainClass.GetMethod("OnApplicationStart");
                    method.Invoke(Activator.CreateInstance(uixMainClass), new object[0]);*/
                }
                else
                {
                    MelonLogger.Log("UIExpansionKit is up to date!");

                    SetupToggleButton("Hide VRC+ adverts?", mlCfgNameHideAds);
                    SetupToggleButton("Replace pet image?", mlCfgNameReplacePet);
                    SetupToggleButton("Replace pet phrases?", mlCfgNameReplacePhrases);
                    SetupToggleButton("Replace pet poke sounds?", mlCfgNameReplaceSounds);
                }
            }
            else
                MelonLogger.LogError("Got error in HTTP request");

            if (MelonPrefs.GetBool(BuildInfo.Name, mlCfgNameReplacePet))
            {
                MelonLogger.Log(string.Format("Option \"{0}\" | Pet image will be replaced", mlCfgNameReplacePet));

                string texturePath = SetupConfigFiles("pet.png", ref emptyList);

                if (texturePath != null)
                {
                    Texture2D newTexture = new Texture2D(2, 2);
                    byte[] imageByteArray = File.ReadAllBytes(texturePath);

                    //poka-yoke
                    if (imageByteArray.Length < 67 || !ImageConversion.LoadImage(newTexture, imageByteArray))
                    {
                        MelonLogger.LogError(string.Format("Option \"{0}\" | Image loading error", mlCfgNameReplacePet));
                    }
                    else
                    {
                        petSprite = Sprite.CreateSprite(newTexture, new Rect(.0f, .0f, newTexture.width, newTexture.height), new Vector2(.5f, .5f), 100f, 0, 0, new Vector4(), false);
                        petSprite.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                    }
                }
                else
                    MelonLogger.LogWarning(string.Format("Option \"{0}\" | Image not found (UserData/{1}/pet.png)", mlCfgNameReplacePet, configPath));
            }

            if (MelonPrefs.GetBool(BuildInfo.Name, mlCfgNameReplacePhrases))
            {
                MelonLogger.Log(string.Format("Option \"{0}\" | Pet phrases will be replaced", mlCfgNameReplacePhrases));
                SetupConfigFiles("normalPhrases.txt", ref petNormalPhrases);
                SetupConfigFiles("pokePhrases.txt", ref petPokePhrases);
            }

            if (MelonPrefs.GetBool(BuildInfo.Name, mlCfgNameReplaceSounds))
            {
                MelonLogger.Log(string.Format("Option \"{0}\" | Pet sounds will be replaced", mlCfgNameReplaceSounds));

                foreach (string fileName in Directory.GetFiles(SetupConfigFiles("audio", ref emptyList, true), "*.*", SearchOption.TopDirectoryOnly))
                {
                    if (fileName.Contains(".ogg") || fileName.Contains(".wav"))
                        MelonCoroutines.Start(SetupAudioFile(Path.Combine("file://", fileName)));
                    else
                        MelonLogger.LogWarning("Option \"aud\" | File has wrong audio format (Only .ogg/.wav are supported), will be ignored");
                }
            }

            MelonLogger.Log(spaces);

            if (MelonPrefs.GetBool(BuildInfo.Name, mlCfgNameHideAds))
                Patch(typeof(ShortcutMenu).GetMethod("Method_Private_Void_0"), GetLocalPatch("ShortcutMenuPatch"), null);

            Patch(typeof(VRCPlusThankYou).GetMethod("OnEnable"), GetLocalPatch("SetupMenuPetPatch"), null);
        }

        public override void VRChat_OnUiManagerInit()
        {
            if (MelonPrefs.GetBool(BuildInfo.Name, mlCfgNameHideAds))
                InitUI();
        }
    }
}
