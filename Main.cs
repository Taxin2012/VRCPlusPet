
using System;
using System.IO;
using System.Reflection;
using System.Collections;

using Harmony;
using MelonLoader;
using UnhollowerRuntimeLib;
using MethodBase = System.Reflection.MethodBase;

using UnityEngine;
using UnityEngine.UI;

namespace VRC_Minus_Pet
{
    public static class BuildInfo
    {
        public const string Name = "VRC_Minus_Pet";
        public const string Description = "Removes VRC+ advertising and can replacese default pet and his phrases (if needed).";
        public const string Author = "Taxin2012";
        public const string Company = null;
        public const string Version = "1.0.0";
        public const string DownloadLink = "https://github.com/Taxin2012/VRC-Minus-Pet";
    }

    public class VRC_Minus_Pet : MelonMod
    {
        static bool
            removeAdverts = true,
            useCustomPet = false,
            useCustomPhrases = false;

        static Il2CppSystem.Collections.Generic.List<string> petNormalPhrases = new Il2CppSystem.Collections.Generic.List<string>();
        static Il2CppSystem.Collections.Generic.List<string> petPokePhrases = new Il2CppSystem.Collections.Generic.List<string>();

        static HarmonyInstance datHarmonyInstance = HarmonyInstance.Create("VRC_Minus_Pet");
        static AssetBundle modAssetBundle;
        static Sprite petSprite;

        static void Patch(MethodBase TargetMethod, HarmonyMethod Prefix, HarmonyMethod Postfix)
        {
            datHarmonyInstance.Patch(TargetMethod, Prefix, Postfix);
        }

        static HarmonyMethod GetLocalPatch(string name) {
            return new HarmonyMethod(typeof(VRC_Minus_Pet).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic));
        }

        static void InitUI()
        {
            //VRC-Minus
            GameObject.DestroyImmediate(GameObject.Find("UserInterface/MenuContent/Backdrop/Header/Tabs/ViewPort/Content/UserIconTab")); // VRC+ User Icons New Label
            GameObject.DestroyImmediate(GameObject.Find("UserInterface/MenuContent/Backdrop/Header/Tabs/ViewPort/Content/VRC+PageTab")); // Main Menu VRC+ Tab
            GameObject.DestroyImmediate(GameObject.Find("UserInterface/MenuContent/Screens/UserInfo/User Panel/Supporter")); // User Info VRC+ Supporter Button
            GameObject.DestroyImmediate(GameObject.Find("UserInterface/MenuContent/Screens/Avatar/Vertical Scroll View/Viewport/Content/Favorite Avatar List/GetMoreFavorites")); // Avatars Get More Favorites Button
        }

        static IEnumerator PostInitUI()
        {
            while (VRCUiManager.prop_VRCUiManager_0 == null)
            {
                yield return new WaitForSeconds(2f);
            }

            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("VRC_Minus_Pet.Resources.vrcmp"))
            using (var tempStream = new MemoryStream((int)stream.Length))
            {
                stream.CopyTo(tempStream);

                modAssetBundle = AssetBundle.LoadFromMemory_Internal(tempStream.ToArray(), 0);
                modAssetBundle.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            }

            petSprite = modAssetBundle.LoadAsset_Internal("Assets/VRCMP/pet.png", Il2CppType.Of<Sprite>()).Cast<Sprite>();
            petSprite.hideFlags |= HideFlags.DontUnloadUnusedAsset;

            yield break;
        }

        static IEnumerator CheckAndSetSprite(Sprite sprite, Image imageComp)
        {
            while (sprite == null)
                yield return new WaitForSeconds(2f);

            imageComp.sprite = sprite;
        }

        //VRC-Minus
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

            if (useCustomPhrases)
            {
                if (petNormalPhrases.Count > 0)
                    __instance.normalPhrases = petNormalPhrases;

                if (petPokePhrases.Count > 0)
                    __instance.pokePhrases = petPokePhrases;
            }

            __instance.gameObject.SetActive(true);
            __instance.transform.parent.gameObject.SetActive(true);

            if (useCustomPet)
            {
                Transform character = __instance.transform.FindChild("Character");

                if (character != null)
                {
                    Image imageComp = character.GetComponent<Image>();

                    if (imageComp != null)
                        MelonCoroutines.Start(CheckAndSetSprite(petSprite, imageComp));
                }
            }
        }

        public override void OnApplicationStart()
        {
            string spaces = new string('-', 40);
            MelonLogger.Log(spaces);
            MelonLogger.Log(string.Format("Initializing {0}...", BuildInfo.Name));

            foreach (string arg in Environment.GetCommandLineArgs())
            {
                if (arg == "-mp.ads")
                {
                    MelonLogger.Log("-mp.ads | Advertising will stay");
                    removeAdverts = false;
                }
                else if (arg == "-mp.pet")
                {
                    MelonLogger.Log("-mp.pet | Pet will be replaced");
                    useCustomPet = true;
                }
                else if (arg == "-mp.phs")
                {
                    MelonLogger.Log("-mp.phs | Pet phrases will be replaced");
                    useCustomPhrases = true;
                }
            }

            MelonLogger.Log(spaces);

            if (removeAdverts)
                Patch(typeof(ShortcutMenu).GetMethod("Method_Private_Void_0"), GetLocalPatch("ShortcutMenuPatch"), null);

            Patch(typeof(VRCPlusThankYou).GetMethod("OnEnable"), GetLocalPatch("SetupMenuPetPatch"), null);

            if (useCustomPet)
                MelonCoroutines.Start(PostInitUI());
        }

        public override void VRChat_OnUiManagerInit()
        {
            if (useCustomPhrases)
                SetupMenuPetPhrases();

            if (removeAdverts)
                InitUI();
        }

        //VRC-Minus
        private static void SetupMenuPetPhrases()
        {
            string petpath = Path.Combine(MelonLoaderBase.UserDataPath, "Custom_Menu_Pet");

            if (!Directory.Exists(petpath))
                Directory.CreateDirectory(petpath);

            string normal_phrases_path = Path.Combine(petpath, "normal_phrases.txt");

            if (File.Exists(normal_phrases_path))
            {
                foreach (string line in File.ReadAllLines(normal_phrases_path))
                    if (!string.IsNullOrEmpty(line))
                        petNormalPhrases.Add(line);
            }
            else
                File.Create(normal_phrases_path);

            string poke_phrases_path = Path.Combine(petpath, "poke_phrases.txt");

            if (File.Exists(poke_phrases_path))
            {
                foreach (string line in File.ReadAllLines(poke_phrases_path))
                    if (!string.IsNullOrEmpty(line))
                        petPokePhrases.Add(line);
            }
            else
                File.Create(poke_phrases_path);
        }
    }
}
