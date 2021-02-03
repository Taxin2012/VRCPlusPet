
using System;
using System.IO;
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
        public const string Description = "Hides VRC+ advertising, can replace default pet, his phrases, poke sounds and chat bubble.";
        public const string Author = "Taxin2012";
        public const string Company = null;
        public const string Version = "1.0.6";
        public const string DownloadLink = "https://github.com/Taxin2012/VRCPlusPet";
    }

    public class VRCPlusPet : MelonMod
    {
        static string
            configPath = "VRCPlusPet_Config",
            fullconfigPath = Path.Combine(MelonUtils.UserDataDirectory, configPath),
            uixPath = Path.Combine(Environment.CurrentDirectory, "Mods/UIExpansionKit.dll"),

            mlCfgNameHideAds = "Hide Ads",
            mlCfgNameReplacePet = "Replace Pet",
            mlCfgNameReplaceBubble = "Replace Bubble",
            mlCfgNameReplacePhrases = "Replace Phrases",
            mlCfgNameReplaceSounds = "Replace Sounds";

        static Il2CppSystem.Collections.Generic.List<string> petNormalPhrases = new Il2CppSystem.Collections.Generic.List<string>();
        static Il2CppSystem.Collections.Generic.List<string> petPokePhrases = new Il2CppSystem.Collections.Generic.List<string>();
        static Il2CppSystem.Collections.Generic.List<string> emptyList = null;
        static Il2CppSystem.Collections.Generic.List<AudioClip> audioClips = new Il2CppSystem.Collections.Generic.List<AudioClip>();
        static HarmonyInstance modHarmonyInstance = HarmonyInstance.Create(BuildInfo.Name);
        static Sprite
            petSprite,
            bubbleSprite;
        static Transform petTransform;
        static Image
            bubbleImageComponent,
            petImageComponent;

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
            GameObject.Destroy(GameObject.Find("UserInterface/MenuContent/Screens/UserInfo/User Panel/Supporter"));
            GameObject.Destroy(GameObject.Find("UserInterface/MenuContent/Screens/Avatar/Vertical Scroll View/Viewport/Content/Favorite Avatar List/GetMoreFavorites"));

            Transform tabTransform = GameObject.Find("UserInterface/MenuContent/Backdrop/Header/Tabs/ViewPort/Content").transform;

            for (int i = 0; i < tabTransform.childCount; i++)
            {
                Transform childTransform = tabTransform.GetChild(i);
                string childName = childTransform.name;

                if (childName != "Search")
                {
                    if (childName == "VRC+PageTab") //childName == "UserIconTab" ||
                        GameObject.Destroy(childTransform.gameObject);
                    else
                        childTransform.GetComponent<LayoutElement>().preferredWidth = 250f;
                }
            }
        }

        //from VRC-Minus (bottom of the README file)
        static bool ShortcutMenuPatch(ShortcutMenu __instance)
        {
            __instance.field_Public_GameObject_5?.SetActive(true); //VRCPlusThankYou
            __instance.field_Public_GameObject_0?.SetActive(false); //UserIconButton
            __instance.field_Public_GameObject_2?.SetActive(false); //Learn More
            __instance.field_Public_GameObject_3?.SetActive(false); //VRCPlusBanner
            __instance.field_Public_GameObject_4?.SetActive(false); //VRCPlusMiniBanner

            return false;
        }

        static void SetupMenuPetPatch(VRCPlusThankYou __instance)
        {
            __instance.field_Public_Boolean_0 = false; //oncePerWorld

            if (petNormalPhrases.Count > 0)
                __instance.field_Public_List_1_String_0 = petNormalPhrases; //normalPhrases

            if (petPokePhrases.Count > 0)
                __instance.field_Public_List_1_String_1 = petPokePhrases; //pokePhrases

            if (audioClips.Count > 0)
                __instance.field_Public_List_1_AudioClip_0 = audioClips; //sounds

            __instance.gameObject.SetActive(true);

            if (petTransform == null)
                petTransform = __instance.transform;

            petTransform.parent.gameObject.SetActive(true);

            if (bubbleSprite != null)
            {
                if (bubbleImageComponent == null)
                    bubbleImageComponent = petTransform.Find("Dialog Bubble").Find("Bubble").GetComponent<Image>();

                bubbleImageComponent.sprite = bubbleSprite;
            }
            
            if (petSprite != null)
            {
                if (petImageComponent == null)
                    petImageComponent = petTransform.FindChild("Character").GetComponent<Image>();

                petImageComponent.sprite = petSprite;
            }
        }

        static IEnumerator SetupAudioFile(string filePath)
        {
            WWW www = new WWW(filePath);
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
            (bool boolVar) => MelonPreferences.SetEntryValue(BuildInfo.Name, configName, boolVar),
            () => MelonPreferences.GetEntryValue<bool>(BuildInfo.Name, configName));
        }

        static void SetupSprite(string fileName, string configName, ref Sprite sprite, bool specialBorder = false)
        {
            string texturePath = SetupConfigFiles(fileName, ref emptyList);

            if (texturePath != null)
            {
                Texture2D newTexture = new Texture2D(2, 2);
                byte[] imageByteArray = File.ReadAllBytes(texturePath);

                //poka-yoke
                if (imageByteArray.Length < 67 || !ImageConversion.LoadImage(newTexture, imageByteArray))
                {
                    MelonLogger.Error(string.Format("Option \"{0}\" | Image loading error", configName));
                }
                else
                {
                    sprite = Sprite.CreateSprite(newTexture, new Rect(.0f, .0f, newTexture.width, newTexture.height), new Vector2(.5f, .5f), 100f, 0, 0, specialBorder ? new Vector4(35f, 55f, 62f, 41f) : new Vector4(), false);
                    sprite.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                }
            }
            else
                MelonLogger.Warning(string.Format("Option \"{0}\" | Image not found (UserData/{1}/{2})", configName, configPath, fileName));
        }

        public override void OnApplicationStart()
        {
            string spaces = new string('-', 40);
            MelonLogger.Msg(spaces);
            MelonLogger.Msg("Initializing...");

            MelonPreferences.CreateCategory(BuildInfo.Name, BuildInfo.Name);
            MelonPreferences.CreateEntry(BuildInfo.Name, mlCfgNameHideAds, true);
            MelonPreferences.CreateEntry(BuildInfo.Name, mlCfgNameReplacePet, false);
            MelonPreferences.CreateEntry(BuildInfo.Name, mlCfgNameReplaceBubble, false);
            MelonPreferences.CreateEntry(BuildInfo.Name, mlCfgNameReplacePhrases, false);
            MelonPreferences.CreateEntry(BuildInfo.Name, mlCfgNameReplaceSounds, false);

            if (File.Exists(uixPath))
            {
                MelonLogger.Msg("UIExpansionKit found, creating visual settings...");

                SetupToggleButton("Hide VRC+ adverts?", mlCfgNameHideAds);
                SetupToggleButton("Replace pet image?", mlCfgNameReplacePet);
                SetupToggleButton("Replace bubble image?", mlCfgNameReplaceBubble);
                SetupToggleButton("Replace pet phrases?", mlCfgNameReplacePhrases);
                SetupToggleButton("Replace pet poke sounds?", mlCfgNameReplaceSounds);
            }
            else
                MelonLogger.Warning("UIExpansionKit not found");

            if (MelonPreferences.GetEntryValue<bool>(BuildInfo.Name, mlCfgNameReplacePet))
            {
                MelonLogger.Msg(string.Format("Option \"{0}\" | Pet image will be replaced", mlCfgNameReplacePet));

                SetupSprite("pet.png", mlCfgNameReplacePet, ref petSprite);
            }

            if (MelonPreferences.GetEntryValue<bool>(BuildInfo.Name, mlCfgNameReplaceBubble))
            {
                MelonLogger.Msg(string.Format("Option \"{0}\" | Bubble image will be replaced", mlCfgNameReplaceBubble));

                SetupSprite("bubble.png", mlCfgNameReplaceBubble, ref bubbleSprite, true);
            }

            if (MelonPreferences.GetEntryValue<bool>(BuildInfo.Name, mlCfgNameReplacePhrases))
            {
                MelonLogger.Msg(string.Format("Option \"{0}\" | Pet phrases will be replaced", mlCfgNameReplacePhrases));
                SetupConfigFiles("normalPhrases.txt", ref petNormalPhrases);
                SetupConfigFiles("pokePhrases.txt", ref petPokePhrases);
            }

            if (MelonPreferences.GetEntryValue<bool>(BuildInfo.Name, mlCfgNameReplaceSounds))
            {
                MelonLogger.Msg(string.Format("Option \"{0}\" | Pet sounds will be replaced", mlCfgNameReplaceSounds));

                foreach (string fileName in Directory.GetFiles(SetupConfigFiles("audio", ref emptyList, true), "*.*", SearchOption.TopDirectoryOnly))
                {
                    if (fileName.Contains(".ogg") || fileName.Contains(".wav"))
                        MelonCoroutines.Start(SetupAudioFile(Path.Combine("file://", fileName)));
                    else
                        MelonLogger.Warning("Option \"aud\" | File has wrong audio format (Only .ogg/.wav are supported), will be ignored");
                }
            }

            MelonLogger.Msg(spaces);

            if (MelonPreferences.GetEntryValue<bool>(BuildInfo.Name, mlCfgNameHideAds))
                Patch(typeof(ShortcutMenu).GetMethod("Method_Private_Void_0"), GetLocalPatch("ShortcutMenuPatch"), null);

            Patch(typeof(VRCPlusThankYou).GetMethod("OnEnable"), GetLocalPatch("SetupMenuPetPatch"), null);
        }

        public override void VRChat_OnUiManagerInit()
        {
            if (MelonPreferences.GetEntryValue<bool>(BuildInfo.Name, mlCfgNameHideAds))
                InitUI();
        }
    }
}
