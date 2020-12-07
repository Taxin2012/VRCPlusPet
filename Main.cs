
using System;
using System.IO;
using System.Reflection;

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
        public const string Description = "Removes VRC+ advertising, can replace default pet, his phrases and poke sounds (if needed).";
        public const string Author = "Taxin2012";
        public const string Company = null;
        public const string Version = "1.0.1";
        public const string DownloadLink = "https://github.com/Taxin2012/VRC-Minus-Pet";
    }

    public class VRCPlusPet : MelonMod
    {
        static string configPath = Path.Combine(MelonLoaderBase.UserDataPath, "VRCPlusPet_Config");
        static bool removeAdverts = true;
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
            GameObject.DestroyImmediate(GameObject.Find("UserInterface/MenuContent/Backdrop/Header/Tabs/ViewPort/Content/UserIconTab"));
            GameObject.DestroyImmediate(GameObject.Find("UserInterface/MenuContent/Backdrop/Header/Tabs/ViewPort/Content/VRC+PageTab"));
            GameObject.DestroyImmediate(GameObject.Find("UserInterface/MenuContent/Screens/UserInfo/User Panel/Supporter"));
            GameObject.DestroyImmediate(GameObject.Find("UserInterface/MenuContent/Screens/Avatar/Vertical Scroll View/Viewport/Content/Favorite Avatar List/GetMoreFavorites"));
        }

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

        public override void OnApplicationStart()
        {
            string spaces = new string('-', 40);
            MelonLogger.Log(spaces);
            MelonLogger.Log(string.Format("Initializing {0}...", BuildInfo.Name));

            bool optionFound = false;

            foreach (string arg in Environment.GetCommandLineArgs())
            {
                if (arg.StartsWith("-mp."))
                {
                    optionFound = true;

                    if (arg.Contains(".ads"))
                    {
                        MelonLogger.Log("Found \"ads\" | Advertising will stay");
                        removeAdverts = false;
                    }

                    if (arg.Contains(".pet"))
                    {
                        MelonLogger.Log("Found \"pet\" | Pet will be replaced");

                        string texturePath = SetupConfigFiles("pet.png", ref emptyList);

                        if (texturePath == null)
                        {
                            MelonLogger.LogWarning(string.Format("Option \"pet\" | Image not found ({0}/pet.png)", configPath));
                            continue;
                        }

                        Texture2D newTexture = new Texture2D(2, 2);
                        byte[] imageByteArray = File.ReadAllBytes(texturePath);

                        //poka-yoke
                        if (imageByteArray.Length < 67 ||  !ImageConversion.LoadImage(newTexture, imageByteArray))
                        {
                            MelonLogger.LogError("Option \"pet\" | Image loading error");
                            continue;
                        }

                        petSprite = Sprite.CreateSprite(newTexture, new Rect(.0f, .0f, newTexture.width, newTexture.height), new Vector2(.5f, .5f), 100f, 0, 0, new Vector4(), false);
                        petSprite.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                    }

                    if (arg.Contains(".phs"))
                    {
                        MelonLogger.Log("Found \"phs\" | Pet phrases will be replaced");

                        SetupConfigFiles("normalPhrases.txt", ref petNormalPhrases);
                        SetupConfigFiles("pokePhrases.txt", ref petPokePhrases);
                    }

                    if (arg.Contains(".aud"))
                    {
                        MelonLogger.Log("Found \"aud\" | Pet sounds will be replaced");
                        MelonCoroutines.Start(SetupAudioFiles());
                    }

                    break;
                }
            }

            if (!optionFound)
                MelonLogger.Log("No options found (example: -mp.ads.pet.phs)");

            MelonLogger.Log(spaces);

            if (removeAdverts)
                Patch(typeof(ShortcutMenu).GetMethod("Method_Private_Void_0"), GetLocalPatch("ShortcutMenuPatch"), null);

            Patch(typeof(VRCPlusThankYou).GetMethod("OnEnable"), GetLocalPatch("SetupMenuPetPatch"), null);
        }

        static System.Collections.IEnumerator SetupAudioFiles()
        {
            foreach (string fileName in Directory.GetFiles(SetupConfigFiles("audio", ref emptyList, true)))
            {
                if (fileName.Contains(".ogg") || fileName.Contains(".wav"))
                {
                    WWW www = new WWW(Path.Combine("file://", fileName));
                    yield return www;

                    AudioClip audioClip = www.GetAudioClip();
                    audioClip.hideFlags |= HideFlags.DontUnloadUnusedAsset;

                    audioClips.Add(audioClip);
                }
                else
                    MelonLogger.LogWarning("Option \"aud\" | File has wrong audio format (Only .ogg/.wav are supported), will be ignored");
            }

            if (audioClips.Count == 0)
                MelonLogger.LogWarning(string.Format("Option \"aud\" | Audio files not found ({0}/audio/)", configPath));
        }

        public override void VRChat_OnUiManagerInit()
        {
            if (removeAdverts)
                InitUI();
        }

        static string SetupConfigFiles(string fileName, ref Il2CppSystem.Collections.Generic.List<string> phrasesArray, bool isDirectory = false)
        {
            if (!Directory.Exists(configPath))
                Directory.CreateDirectory(configPath);

            string filePath = Path.Combine(configPath, fileName);

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
    }
}
