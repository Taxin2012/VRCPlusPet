
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Collections;

using Harmony;
using MelonLoader;
using MethodBase = System.Reflection.MethodBase;

using UnityEngine;
using UnityEngine.UI;

namespace VRCPlusPet
{
    public static class BuildInfo
    {
        public const string Name = "VRCPlusPet";
        public const string Description = "Hides VRC+ advertising, can replace default pet, his phrases, poke sounds and chat bubble.";
        public const string Author = "Taxin2012";
        public const string Company = null;
        public const string Version = "1.1.0";
        public const string DownloadLink = "https://github.com/Taxin2012/VRCPlusPet";
    }

    public class VRCPlusPet : MelonMod
    {
        static string
            configPath = "VRCPlusPet_Config",
            fullconfigPath = Path.Combine(MelonUtils.UserDataDirectory, configPath),

            mlCfgNameHideAds = "Hide Ads",
            mlCfgNameHideUserIconTab = "Hide Menu Icon Tab",
            mlCfgNameHideIconCameraButton = "Hide Icon Camera Button",
            mlCfgNameHideUserIconsButton = "Hide User Icons Button",
            mlCfgNameReplacePet = "Replace Pet",
            mlCfgNameReplaceBubble = "Replace Bubble",
            mlCfgNameReplacePhrases = "Replace Phrases",
            mlCfgNameReplaceSounds = "Replace Sounds";
        static bool
            cachedCfgHideAds,
            cachedCfgHideUserIconTab;

        static Il2CppSystem.Collections.Generic.List<string>
            petNormalPhrases = new Il2CppSystem.Collections.Generic.List<string>(),
            petPokePhrases = new Il2CppSystem.Collections.Generic.List<string>(),
            emptyList = null;
        static Il2CppSystem.Collections.Generic.List<AudioClip> audioClips = new Il2CppSystem.Collections.Generic.List<AudioClip>();
        static Dictionary<string, float> originalSizes = new Dictionary<string, float>();
        static HarmonyInstance modHarmonyInstance = HarmonyInstance.Create(BuildInfo.Name);
        static Sprite
            petSprite,
            bubbleSprite;
        static Transform petTransform;
        static Image
            bubbleImageComponent,
            petImageComponent;

        public override void OnPreferencesSaved() => InitUI();

        static void Patch(MethodBase TargetMethod, HarmonyMethod Prefix, HarmonyMethod Postfix) => modHarmonyInstance.Patch(TargetMethod, Prefix, Postfix);

        static HarmonyMethod GetLocalPatchMethod(string name) => new HarmonyMethod(typeof(VRCPlusPet).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic));

        static void InitUI(bool firstInit = false)
        {
            bool hideAds = MelonPreferences.GetEntryValue<bool>(BuildInfo.Name, mlCfgNameHideAds);
            bool hideUserIconTab = MelonPreferences.GetEntryValue<bool>(BuildInfo.Name, mlCfgNameHideUserIconTab);

            if (firstInit || cachedCfgHideAds != hideAds || cachedCfgHideUserIconTab != hideUserIconTab)
            {
                cachedCfgHideAds = hideAds;
                hideAds = !hideAds;

                cachedCfgHideUserIconTab = hideUserIconTab;

                GameObject.Find("UserInterface/MenuContent/Screens/UserInfo/User Panel/Supporter").SetActiveRecursively(hideAds);
                GameObject.Find("UserInterface/MenuContent/Screens/Avatar/Vertical Scroll View/Viewport/Content/Favorite Avatar List/GetMoreFavorites")?.SetActive(hideAds);

                Transform tabsTransform = GameObject.Find("UserInterface/MenuContent/Backdrop/Header/Tabs/ViewPort/Content").transform;

                for (int i = 0; i < tabsTransform.childCount; i++)
                {
                    Transform childTransform = tabsTransform.GetChild(i);
                    string childName = childTransform.name;

                    if (childName != "Search")
                    {
                        if (childName == "VRC+PageTab")
                            childTransform.gameObject.SetActive(hideAds);
                        else
                        {
                            LayoutElement childLayoutElement = childTransform.GetComponent<LayoutElement>();

                            if (childName == "UserIconTab")
                            {
                                GameObject tabGO = childLayoutElement.gameObject;
                                bool toSet = !hideUserIconTab;

                                //lol
                                tabGO.SetActiveRecursively(toSet);
                                tabGO.GetComponent<Image>().enabled = toSet;
                                tabGO.GetComponent<LayoutElement>().enabled = toSet;
                            }
                            else
                            {
                                if (hideUserIconTab)
                                {
                                    if (!originalSizes.ContainsKey(childName))
                                        originalSizes.Add(childName, childLayoutElement.preferredWidth);

                                    childLayoutElement.preferredWidth = 250f;
                                }
                                else
                                    childLayoutElement.preferredWidth = originalSizes.GetValueSafe(childName);
                            }
                        }
                    }
                }
            }
        }

        static void SMElementActiveSetter(GameObject go)
        {
            if (go.name == "UserIconCameraButton")
                go.SetActive(!MelonPreferences.GetEntryValue<bool>(BuildInfo.Name, mlCfgNameHideIconCameraButton));
            else if (go.name == "UserIconButton")
                go.SetActive(!MelonPreferences.GetEntryValue<bool>(BuildInfo.Name, mlCfgNameHideUserIconsButton));
            else if (go.name == "VRCPlusThankYou" || go.name == "VRCPlusBanner" || go.name == "VRCPlusMiniBanner")
            {
                bool hideAds = MelonPreferences.GetEntryValue<bool>(BuildInfo.Name, mlCfgNameHideAds);

                if (hideAds)
                    if (go.name == "VRCPlusThankYou")
                        go.SetActive(hideAds);
                    else if (go.name == "VRCPlusBanner" || go.name == "VRCPlusMiniBanner")
                        go.SetActive(!hideAds);
            }
        }

        //from VRC-Minus (bottom of the README file)
        static void ShortcutMenuPatch(ShortcutMenu __instance)
        {
            SMElementActiveSetter(__instance.field_Public_GameObject_0);
            SMElementActiveSetter(__instance.field_Public_GameObject_1);
            SMElementActiveSetter(__instance.field_Public_GameObject_2);
            SMElementActiveSetter(__instance.field_Public_GameObject_3);
            SMElementActiveSetter(__instance.field_Public_GameObject_4);
            SMElementActiveSetter(__instance.field_Public_GameObject_5);
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
            WWW www = new WWW(filePath, null, new Il2CppSystem.Collections.Generic.Dictionary<string, string>());
            yield return www;

            AudioClip audioClip = www.GetAudioClip();
            audioClip.hideFlags |= HideFlags.DontUnloadUnusedAsset;

            audioClips.Add(audioClip);
        }

        static string SetupConfigFile(string fileName, ref Il2CppSystem.Collections.Generic.List<string> phrasesArray, bool isDirectory = false)
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

        static void SetupSprite(string fileName, string configName, ref Sprite sprite, bool specialBorder = false)
        {
            string texturePath = SetupConfigFile(fileName, ref emptyList);

            if (texturePath != null)
            {
                Texture2D newTexture = new Texture2D(2, 2);
                byte[] imageByteArray = File.ReadAllBytes(texturePath);

                //poka-yoke
                if (imageByteArray.Length < 67 || !ImageConversion.LoadImage(newTexture, imageByteArray))
                    MelonLogger.Error(string.Format("Option \"{0}\" | Image loading error", configName));
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
            cachedCfgHideAds = MelonPreferences.GetEntryValue<bool>(BuildInfo.Name, mlCfgNameHideAds);

            MelonPreferences.CreateEntry(BuildInfo.Name, mlCfgNameHideUserIconTab, false);
            cachedCfgHideUserIconTab = MelonPreferences.GetEntryValue<bool>(BuildInfo.Name, mlCfgNameHideUserIconTab);

            MelonPreferences.CreateEntry(BuildInfo.Name, mlCfgNameHideIconCameraButton, false);
            MelonPreferences.CreateEntry(BuildInfo.Name, mlCfgNameHideUserIconsButton, false);
            MelonPreferences.CreateEntry(BuildInfo.Name, mlCfgNameReplacePet, false);
            MelonPreferences.CreateEntry(BuildInfo.Name, mlCfgNameReplaceBubble, false);
            MelonPreferences.CreateEntry(BuildInfo.Name, mlCfgNameReplacePhrases, false);
            MelonPreferences.CreateEntry(BuildInfo.Name, mlCfgNameReplaceSounds, false);

            if (!MelonHandler.Mods.Any(mod => mod.Info.Name == "UI Expansion Kit"))
                MelonLogger.Warning("UIExpansionKit not found, visual preferences cannot be accessed");

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
                SetupConfigFile("normalPhrases.txt", ref petNormalPhrases);
                SetupConfigFile("pokePhrases.txt", ref petPokePhrases);
            }

            if (MelonPreferences.GetEntryValue<bool>(BuildInfo.Name, mlCfgNameReplaceSounds))
            {
                MelonLogger.Msg(string.Format("Option \"{0}\" | Pet sounds will be replaced", mlCfgNameReplaceSounds));

                foreach (string fileName in Directory.GetFiles(SetupConfigFile("audio", ref emptyList, true), "*.*", SearchOption.TopDirectoryOnly))
                    if (fileName.Contains(".ogg") || fileName.Contains(".wav"))
                        MelonCoroutines.Start(SetupAudioFile(Path.Combine("file://", fileName)));
                    else
                        MelonLogger.Warning("Option \"aud\" | File has wrong audio format (Only .ogg/.wav are supported), will be ignored");
            }

            if (cachedCfgHideUserIconTab)
                MelonLogger.Msg(string.Format("Option \"{0}\" | Menu Icon Tab will be hided", mlCfgNameHideUserIconTab));

            if (MelonPreferences.GetEntryValue<bool>(BuildInfo.Name, mlCfgNameHideIconCameraButton))
                MelonLogger.Msg(string.Format("Option \"{0}\" | Icon Camera Button will be hided", mlCfgNameHideIconCameraButton));

            if (MelonPreferences.GetEntryValue<bool>(BuildInfo.Name, mlCfgNameHideUserIconsButton))
                MelonLogger.Msg(string.Format("Option \"{0}\" | User Icons Button will be hided", mlCfgNameHideUserIconsButton));

            MelonLogger.Msg(spaces);

            Patch(typeof(ShortcutMenu).GetMethod("Method_Private_Void_1"), null, GetLocalPatchMethod("ShortcutMenuPatch"));
            Patch(typeof(VRCPlusThankYou).GetMethod("OnEnable"), GetLocalPatchMethod("SetupMenuPetPatch"), null);
        }

        public override void VRChat_OnUiManagerInit()
        {
            if (MelonPreferences.GetEntryValue<bool>(BuildInfo.Name, mlCfgNameHideAds))
                InitUI(true);
        }
    }
}
