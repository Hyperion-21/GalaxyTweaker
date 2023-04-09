using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using SpaceWarp.API.Mods;
using UnityEngine;
using KSP.Game.Load;
using KSP.Game;
using SpaceWarp.API.UI;
using System.Diagnostics;
using Newtonsoft.Json;
using KSP.IO;
using KSP.Sim;
using KSP.Sim.Definitions;
using KSP.Sim.impl;
using KSP;

namespace GalaxyTweaker
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    //[BepInDependency(SpaceWarpPlugin.ModGuid, SpaceWarpPlugin.ModVer)]
    public class GalaxyTweakerPlugin : BaseSpaceWarpPlugin
    {
        // These are useful in case some other mod wants to add a dependency to this one
        public const string ModGuid = MyPluginInfo.PLUGIN_GUID;
        public const string ModName = MyPluginInfo.PLUGIN_NAME;
        public const string ModVer = MyPluginInfo.PLUGIN_VERSION;

        public static string Path { get; private set; }

        private static string _selectedTarget;

        private static string DefaultDirectory => $"{Path}/GalaxyDefinitions";
        private static string DefaultPath => $"{Path}/GalaxyDefinitions/GalaxyDefinition_Default.json";
        private static string ConfigPath => $"{Path}/GalaxyDefinitions/{_selectedTarget}";
        private static string CampaignDirectory => $"{Path}/saves/{Game.SessionManager.ActiveCampaignName}";
        private static string CampaignPath => $"{CampaignDirectory}/CampaignGalaxyDefinition.json";
        private static string FlagIgnorePath => $"{Path}/flagreplace.json";

        public static GalaxyTweakerPlugin Instance { get; set; }
        private static ManualLogSource _logger;

        private static CampaignMenu _campaignMenuInstance = null;

        private static bool _isWindowOpen;
        private Rect _windowRect = new((Screen.width - 600) / 2, (Screen.height - 400) / 2, 600, 400);

        private string galaxyDefinition = "GalaxyDefinition_Default.json";
        private const string galaxyDefFileType = ".json";
        public List<string> galaxyDefsList = new();
        public List<string> celestialDataList = new();

        private bool useDefaultDirectory = true;
        private static bool useDefaultGalaxyDefinition = false;
        private static bool useDefaultCelestialData = false;
        private string currentDirectory = "GalaxyDefinitions";
        private string newFolderDirectory;

        private string loadedDirectory = "unspecified";
        private Vector2 scrollbarPos;

        string newPath;

        // static string startingPlanet = "Karbin";
        static readonly string[] stockCelestialBodies = { "Kerbol", "Moho", "Eve", "Gilly", "Kerbin", "Mun", "Minmus", "Duna", "Ike", "Dres", "Jool", "Laythe", "Vall", "Tylo", "Bop", "Pol", "Eeloo" };

        private static string _activePlanetPack = "Default Pack";

        public override void OnPreInitialized()
        {
            base.OnPreInitialized();
            Path = PluginFolderPath;
            newFolderDirectory = DefaultDirectory;
            Instance = this;
            _logger = Logger;
        }

        public override void OnInitialized()
        {
            // Register all Harmony patches in the project
            Harmony.CreateAndPatchAll(typeof(GalaxyTweakerPlugin));

            // Generate GalaxyDefinition_Default.json from address
            Directory.CreateDirectory($"{Path}/GalaxyDefinitions");
            GameManager.Instance.Game.Assets.Load<TextAsset>("GalaxyDefinition_Default", asset =>
                File.WriteAllText(DefaultPath, asset.text)
            );
            _logger.LogInfo($"Copying the original asset into: {DefaultPath}");

            Directory.CreateDirectory($"{Path}/CelestialBodyData/Default Pack");
            foreach (String celestialBody in stockCelestialBodies)
            {
                GameManager.Instance.Game.Assets.Load<TextAsset>($"{celestialBody}.json", asset =>
                    File.WriteAllText($"{Path}/CelestialBodyData/Default Pack/{celestialBody}.json", asset.text)
                );
            }

            galaxyDefsList.Clear();
            celestialDataList.Clear();
            GetGalaxyDefinitions();
            GetCelestialData();
        }

        [HarmonyPatch(typeof(LoadCelestialBodyDataFilesFlowAction), nameof(LoadCelestialBodyDataFilesFlowAction.DoAction))]
        [HarmonyPrefix]
        public static bool LoadCelestialBodyDataFilesFlowAction_DoAction(Action resolve, LoadCelestialBodyDataFilesFlowAction __instance)
        {
            __instance._game.UI.SetLoadingBarText(__instance.Description);
            __instance._resolve = resolve;

            LoadDefinitions(__instance.OnGalaxyDefinitionLoaded);

            return false;
        }

        [HarmonyPatch(typeof(CreateCelestialBodiesFlowAction), nameof(CreateCelestialBodiesFlowAction.DoAction))]
        [HarmonyPrefix]
        public static bool CreateCelestialBodiesFlowAction_DoAction(Action resolve, CreateCelestialBodiesFlowAction __instance)
        {
            __instance._game.UI.SetLoadingBarText(__instance.Description);
            __instance._resolve = resolve;

            LoadDefinitions(__instance.OnGalaxyDefinitionLoaded);

            return false;
        }

        private static void LoadDefinitions(Action<TextAsset> onGalaxyDefinitionLoaded)
        {
            //GameManager.Instance.Game.Assets.Load<TextAsset>("GalaxyDefinition_Default", asset =>
            //    File.WriteAllText(DefaultPath, asset.text)
            //);
            //_logger.LogInfo($"Copying the original asset into: {DefaultPath}");
            // _logger.LogInfo("File Exists: " + File.Exists(CampaignPath).ToString());
            // _logger.LogInfo("Campaign Exists: " + Game.SaveLoadManager.CampaignExists(Game.SessionManager.ActiveCampaignType, Game.SessionManager.ActiveCampaignName).ToString());

            if (Game.SaveLoadManager.CampaignExists(Game.SessionManager.ActiveCampaignType, Game.SessionManager.ActiveCampaignName) && !File.Exists(CampaignPath))
            {
                _logger.LogInfo("Using default galaxy definition.");
                GameManager.Instance.Game.Assets.Load<TextAsset>("GalaxyDefinition_Default", asset =>
                    onGalaxyDefinitionLoaded(new TextAsset(asset.text))
                );
                _logger.LogInfo($"Loaded default campaign definition.");
                return;
            }

            _logger.LogInfo("Did not return out of default exception. Performing normally.");
            if (!File.Exists(CampaignPath))
            {
                Directory.CreateDirectory(CampaignDirectory);
                File.WriteAllText(CampaignPath, File.ReadAllText(ConfigPath));
                _logger.LogInfo($"Campaign definition not found, creating file: {CampaignPath}");
            }

            var jsonFeed = File.ReadAllText(CampaignPath);
            _logger.LogInfo($"Loaded campaign definition: {CampaignPath}");
            onGalaxyDefinitionLoaded(new TextAsset(jsonFeed));
        }

        //Some code below this line has been contributed by JohnsterSpaceProgram.
        /// <summary>
        /// Opens and closes window based on if the "Create New Campaign" menu is open.
        /// </summary>
        private void LateUpdate()
        {
            _selectedTarget = galaxyDefinition;

            // Opens and closes window based on if the "Create New Campaign" menu is open.
            if (_campaignMenuInstance != null)
            {
                _isWindowOpen = _campaignMenuInstance._createCampaignMenu.activeInHierarchy;
            }
        }
        
        /// <summary>
        /// Draws a simple UI window when <code>this._isWindowOpen</code> is set to <code>true</code>.
        /// </summary>
        private void OnGUI()
        {
            // Set the apperance of the UI window
            GUI.skin = Skins.ConsoleSkin;

            //If the window open boolean is set to true, show the UI window
            if (_isWindowOpen)
            {
                _windowRect = GUILayout.Window(
                    GUIUtility.GetControlID(FocusType.Passive),
                    _windowRect,
                    FillWindow,
                    "<size=40><color=#696DFF>// GALAXY TWEAKER</color></size>",
                    GUILayout.Height(100),
                    GUILayout.Width(600)
                );
            }
        }
        /// <summary>
        /// Defines the content of the UI window drawn in the <code>OnGui</code> method.
        /// </summary>
        /// <param name="windowID"></param>
        private void FillWindow(int windowID)
        {
            GUILayout.Space(20);
            useDefaultGalaxyDefinition = GUILayout.Toggle(useDefaultGalaxyDefinition, "Use Default Galaxy Definition?");
            if (!useDefaultGalaxyDefinition)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Selected Galaxy Definition: ");
                galaxyDefinition = GUILayout.TextField(galaxyDefinition, 45);
                GUILayout.EndHorizontal();
                GUILayout.Space(10);

                if (GUILayout.Button("Reload Galaxy Definitions"))
                {
                    newPath = $"{Path}/" + currentDirectory;
                    if (Directory.Exists(newPath))
                    {
                        newFolderDirectory = newPath;
                    }

                    GetGalaxyDefinitions();
                }

                GUILayout.Space(15);

                if (galaxyDefsList.Count == 1)
                {
                    GUILayout.Label("<size=20>Found " + galaxyDefsList.Count + " Galaxy Definition!</size>");
                }
                else
                {
                    GUILayout.Label("<size=20>Found " + galaxyDefsList.Count + " Galaxy Definitions!</size>");
                }
                GUILayout.BeginVertical();
                scrollbarPos = GUILayout.BeginScrollView(scrollbarPos, false, true, GUILayout.Height(215));
                foreach (string galaxyDef in galaxyDefsList)
                {
                    if (GUILayout.Button(galaxyDef))
                    {
                        galaxyDefinition = galaxyDef;
                    }
                }
                GUILayout.EndScrollView();
                GUILayout.EndVertical();

                //Allows for the use of a different folder inside of galaxy_tweaker for loading Galaxy Definitions from
                GUILayout.Space(5);
                useDefaultDirectory = GUILayout.Toggle(useDefaultDirectory, "Use Default Folder?");

                if (!useDefaultDirectory)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Selected Folder: ");
                    currentDirectory = GUILayout.TextField(currentDirectory, 25);
                    GUILayout.EndHorizontal();
                    GUILayout.Space(5);
                    GUILayout.Label("Note: The specified folder MUST be located inside of the galaxy_tweaker folder.");
                }

                GUILayout.Space(5);

                if (GUILayout.Button("Open Folder Location") && Directory.Exists(newFolderDirectory))
                {
                    Process.Start(newFolderDirectory);
                }
                GUILayout.Space(15);
            }
            else
            {
                galaxyDefinition = "GalaxyDefinition_Default.json";
            }

            GUILayout.Space(5);
            useDefaultCelestialData = GUILayout.Toggle(useDefaultCelestialData, "Use Default Celestial Body Data?");
            if (!useDefaultCelestialData)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Selected Celestial Body Data Pack: ");
                _activePlanetPack = GUILayout.TextField(_activePlanetPack, 45);
                GUILayout.EndHorizontal();
                GUILayout.Space(10);

                if (GUILayout.Button("Reload Celestial Body Data"))
                {
                    newPath = $"{Path}/CelestialBodyData/" + _activePlanetPack;
                    if (Directory.Exists(newPath))
                    {
                        newFolderDirectory = newPath;
                    }

                    GetCelestialData();
                }

                GUILayout.Space(15);

                if (celestialDataList.Count == 1)
                {
                    GUILayout.Label("<size=20>Found " + celestialDataList.Count + " Celestial Body Data Pack!</size>");
                }
                else
                {
                    GUILayout.Label("<size=20>Found " + celestialDataList.Count + " Celestial Body Data Packs!</size>");
                }
                GUILayout.BeginVertical();
                scrollbarPos = GUILayout.BeginScrollView(scrollbarPos, false, true, GUILayout.Height(215));
                foreach (string celestialData in celestialDataList)
                {
                    if (GUILayout.Button(celestialData))
                    {
                        _activePlanetPack = celestialData;
                    }
                }
                GUILayout.EndScrollView();
                GUILayout.EndVertical();

                /*
                GUILayout.Space(5);
                useDefaultDirectory = GUILayout.Toggle(useDefaultDirectory, "Use Default Folder?");

                if (!useDefaultDirectory)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Selected Folder: ");
                    currentDirectory = GUILayout.TextField(currentDirectory, 25);
                    GUILayout.EndHorizontal();
                    GUILayout.Space(5);
                    GUILayout.Label("Note: The specified folder MUST be located inside of the galaxy_tweaker folder.");
                }
                */

                GUILayout.Space(5);

                if (GUILayout.Button("Open Folder Location") && Directory.Exists($"{Path}/CelestialBodyData"))
                {
                    Process.Start($"{Path}/CelestialBodyData");
                }
                // GUILayout.Space(15);
            }

            GUI.DragWindow(new Rect(0, 0, 10000, 500));
        }

        /// <summary>
        /// Attempts to get all of the galaxy definition json files that are currently in the galaxy definitions folder
        /// and put them into a list.
        /// </summary>
        private void GetGalaxyDefinitions()
        {
            if (galaxyDefsList.Count > 0) //If the current list of galaxy definitions is not empty, clear it
            {
                galaxyDefsList.Clear(); //This is done to refresh the list everytime this function is called
            }

            if (useDefaultDirectory)
            {
                loadedDirectory = DefaultDirectory;
            }
            else
            {
                loadedDirectory = newFolderDirectory;
            }

            if (Directory.Exists(loadedDirectory)) //A check to only get files from the loaded directory (folder) if it exists
            {
                DirectoryInfo galaxyDefFolder = new(loadedDirectory);

                FileInfo[] galaxyDefInfo = galaxyDefFolder.GetFiles("*" + galaxyDefFileType + "*");

                foreach (FileInfo galaxyDef in galaxyDefInfo)
                {
                    if (!galaxyDefsList.Contains(galaxyDef.Name))
                    {
                        galaxyDefsList.Add(galaxyDef.Name);
                    }
                }
            }
        }

        private void GetCelestialData()
        {
            if (celestialDataList.Count > 0) //If the current list of galaxy definitions is not empty, clear it
            {
                celestialDataList.Clear(); //This is done to refresh the list everytime this function is called
            }

            /*if (useDefaultDirectory)
            {
                loadedDirectory = DefaultDirectory;
            }
            else
            {
                loadedDirectory = newFolderDirectory;
            }*/

            if (Directory.Exists($"{Path}/CelestialBodyData")) //A check to only get files from the loaded directory (folder) if it exists
            {
                DirectoryInfo celestialDataFolder = new($"{Path}/CelestialBodyData");

                DirectoryInfo[] celestialDataInfo = celestialDataFolder.GetDirectories("*");

                foreach (DirectoryInfo celestialData in celestialDataInfo)
                {
                    if (!celestialDataList.Contains(celestialData.Name))
                    {
                        celestialDataList.Add(celestialData.Name);
                    }
                }
            }
        }

        // This "catches" the CampaignMenu instance (because I couldn't figure out how else to do it, will probably be replaced later)
        [HarmonyPatch(typeof(CampaignMenu), nameof(CampaignMenu.StartNewCampaignMenu))]
        [HarmonyPrefix]
        public static bool CatchCampaignMenuInstance(CampaignMenu __instance)
        {
            _campaignMenuInstance = __instance;
            return true;
        }

        //private void PlanetReplacer()
        //{
        //    if (!File.Exists(FlagIgnorePath))
        //    {
        //        _logger.LogInfo("Did not find flagingnore.json, skipping file readout.");
        //        return;
        //    }
        //    _logger.LogInfo("Reading out flagignore.json");
        //    JsonTextReader reader = new(new StringReader(File.ReadAllText(FlagIgnorePath)));
        //    // _logger.LogInfo(File.ReadAllText(FlagIgnorePath));
        //    while (reader.Read())
        //    {
        //        if (reader.Value == null) continue;
        //        string key = reader.Value.ToString();
        //        reader.Read();
        //        string value = reader.Value.ToString();

        //        _logger.LogInfo(key + ": " + value);
        //    }
        //}

        [HarmonyPatch(typeof(LoadCelestialBodyDataFilesFlowAction), nameof(LoadCelestialBodyDataFilesFlowAction.OnGalaxyDefinitionLoaded))]
        [HarmonyPrefix]
        public static bool OverrideLoadCelestialBodyDataFilesFlowAction(TextAsset asset, LoadCelestialBodyDataFilesFlowAction __instance)
        {
            if (useDefaultCelestialData) return true;
            __instance._galaxy = IOProvider.FromJson<SerializedGalaxyDefinition>(asset.text);
            __instance._game.Assets.LoadByLabel<TextAsset>("celestial_bodies", null, delegate (IList<TextAsset> allBodiesTextAssets)
            {
                __instance._data.CelestialBodyProperties = new CelestialBodyProperties[__instance._galaxy.CelestialBodies.Count];
                __instance._game.CelestialBodies.Initialize();
                int num = 0;
                for (int i = 0; i < allBodiesTextAssets.Count; i++)
                {
                    allBodiesTextAssets[i] = PlanetReplacer(allBodiesTextAssets[i]);
                    CelestialBodyCore celestialBodyCore = IOProvider.FromBuffer<CelestialBodyCore>(allBodiesTextAssets[i].bytes);
                    if (__instance.GalaxyHasBody(celestialBodyCore.data.bodyName))
                    {
                        __instance._game.CelestialBodies.RegisterBodyFromData(celestialBodyCore);
                        __instance._data.CelestialBodyProperties[num] = celestialBodyCore.data.ToOldBodyProperties();
                        num++;
                    }
                }
                GameManager.Instance.Game.Assets.ReleaseAsset<IList<TextAsset>>(allBodiesTextAssets);
                __instance._resolve();
            });
            return false;
        }

        public static TextAsset PlanetReplacer(TextAsset celes)
        {
            JsonTextReader reader = new(new StringReader(celes.text));
            while (reader.Read())
            {
                if (reader.Value == null) continue;
                string key = reader.Value.ToString();
                if (key != "bodyName") continue;
                reader.Read();
                string value = reader.Value.ToString();
                if (File.Exists($"{Path}/CelestialBodyData/{_activePlanetPack}/{value}.json"))
                {
                    _logger.LogInfo($"Found replacement file for {value}, replacing.");
                    celes = new TextAsset(File.ReadAllText($"{Path}/CelestialBodyData/{_activePlanetPack}/{value}.json"));
                }
                else
                {
                    _logger.LogInfo($"Did not find replacement file for {value}, using stock values.");
                }
                break;
            }
            // reader.CloseInput = false;
            reader.Close();
            return celes;
        }

        //[HarmonyPatch(typeof(AssetProvider), nameof(AssetProvider.LoadByLabel))]
        //[HarmonyPrefix]
        //public static bool LoadByLabelReplacer<T>(string label, Action<T> assetLoadCallback, Action<IList<T>> resultCallback = null) where T : UnityEngine.Object
        //{
        //    if (label != "celestial_bodies") return true;
        //    if (!File.Exists(FlagIgnorePath))
        //    {
        //        _logger.LogInfo("Did not find flagingnore.json, assuming no stock planets are being replaced.");
        //        return true;
        //    }
        //    JsonTextReader reader = new(new StringReader(File.ReadAllText(FlagIgnorePath)));
        //    while (reader.Read())
        //    {
        //        if (reader.Value == null) continue;
        //        string key = reader.Value.ToString();
        //        reader.Read();
        //        string value = reader.Value.ToString();
        //    }


        //        return false;
        //}

        //[HarmonyPatch(typeof(LoadStartingCelestialBodyFlowAction), nameof(LoadStartingCelestialBodyFlowAction.GetStartingSoiCelestialBodyName))]
        //[HarmonyPostfix]
        //public static void ChangeDefaultStartingPlanet1(ref string __result)
        //{
        //    _logger.LogInfo("LoadStartingCelestialBodyFlowAction Postfix was run. Would have ran " + __result + " otherwise.");
        //    __result = startingPlanet;
        //}

        [HarmonyPatch(typeof(CelestialBodyBehavior), nameof(CelestialBodyBehavior.OnScaledSpaceViewInstantiated))]
        [HarmonyPrefix]
        public static bool PQSOverride(GameObject instance, CelestialBodyBehavior __instance)
        {
            if (instance == null || __instance == null || useDefaultCelestialData == true) return true;

            instance.TryGetComponent<CoreCelestialBodyData>(out __instance._coreCelestialBodyData);
            //_logger.LogInfo("Here's some celestial body data, let's hope it works:");
            //_logger.LogInfo(__instance._coreCelestialBodyData.Data.radius.ToString());

            //if (__instance._coreCelestialBodyData.Data.bodyName == "Kerbin")
            //{
            //    _logger.LogInfo("Hey, we found Kerbin! Let's change it :)");
            //    _logger.LogInfo("Currently, radius is " + __instance._coreCelestialBodyData.Data.radius.ToString());
            //    __instance._coreCelestialBodyData.Data.radius = 6000000;
            //    _logger.LogInfo("Now, it is " + __instance._coreCelestialBodyData.Data.radius.ToString());
            //}
            __instance._coreCelestialBodyData = PlanetReplacer2(__instance._coreCelestialBodyData);

            return true;
        }

        public static CoreCelestialBodyData PlanetReplacer2(CoreCelestialBodyData data)
        {
            if (!File.Exists($"{Path}/CelestialBodyData/{_activePlanetPack}/{data.Data.bodyName}.json")) return data;
            JsonTextReader reader = new(new StringReader(File.ReadAllText($"{Path}/CelestialBodyData/{_activePlanetPack}/{data.Data.bodyName}.json")));
            while (reader.Read())
            {
                if (reader.Value == null) continue;
                string key = reader.Value.ToString();
                if (key != "bodyName") continue;
                reader.Read();
                string value = reader.Value.ToString();
                if (value != data.Data.bodyName) return data;
                break;
            }
            while (reader.Read())
            {
                string key = reader.Value.ToString();
                if (key != "radius") continue;
                reader.Read();
                data.Data.radius = (double)reader.Value;
                break;
            }
            return data;
        }
    }
}