using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using ServerMod.GPS;

namespace Example {
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class Mod : MySessionComponentBase {

        public MyLanguagesEnum? Language { get; private set; }

        public static HeavyGasSettings GasSettigs = new HeavyGasSettings();

        public override void LoadData() {
            LoadLocalization("Localization");
            MyAPIGateway.Gui.GuiControlRemoved += OnGuiControlRemoved;

            GasSettigs.Load();
        }

        protected override void UnloadData() {
            MyAPIGateway.Gui.GuiControlRemoved -= OnGuiControlRemoved;
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();
            GPSHelper.CreateGPSForPlanet();
        }

        private void LoadLocalization(string folder) {
            var path = Path.Combine(ModContext.ModPathData, folder);
            var supportedLanguages = new HashSet<MyLanguagesEnum>();
            MyTexts.LoadSupportedLanguages(path, supportedLanguages);

            var currentLanguage = supportedLanguages.Contains(MyAPIGateway.Session.Config.Language) ? MyAPIGateway.Session.Config.Language : MyLanguagesEnum.English;
            if (Language != null && Language == currentLanguage) {
                return;
            }

            Language = currentLanguage;
            var languageDescription = MyTexts.Languages.Where(x => x.Key == currentLanguage).Select(x => x.Value).FirstOrDefault();
            if (languageDescription != null) {
                var cultureName = string.IsNullOrWhiteSpace(languageDescription.CultureName) ? null : languageDescription.CultureName;
                var subcultureName = string.IsNullOrWhiteSpace(languageDescription.SubcultureName) ? null : languageDescription.SubcultureName;
                MyTexts.LoadTexts(path, cultureName, subcultureName);
            }
        }

        private void OnGuiControlRemoved(object obj) {
            if (obj.ToString().EndsWith("ScreenOptionsSpace")) {
                LoadLocalization("Localization");
            }
        }

        public class HeavyGasSettings
        {
            const string VariableId = nameof(HeavyGasSettings); // IMPORTANT: must be unique as it gets written in a shared space (sandbox.sbc)
            const string FileName = "Config.ini"; // the file that gets saved to world storage under your mod's folder
            const string IniSection = "HeavyGas";

            public static bool EnableNPCs = false; // Default
            public static double Conversion_H2 = (2.0 / 18.0) / 10.0; // Default
            public static double Conversion_O2 = (16.0 / 18.0) / 5.0; // Default

            public HeavyGasSettings()
            {

            }

            public void Load()
            {
                if (MyAPIGateway.Session.IsServer)
                    LoadOnHost();
                else
                    LoadOnClient();
            }

            void LoadOnHost()
            {
                MyIni iniParser = new MyIni();

                // load file if exists then save it regardless so that it can be sanitized and updated
                if (MyAPIGateway.Utilities.FileExistsInWorldStorage(FileName, typeof(HeavyGasSettings)))
                {
                    using (TextReader file = MyAPIGateway.Utilities.ReadFileInWorldStorage(FileName, typeof(HeavyGasSettings)))
                    {
                        string text = file.ReadToEnd();

                        MyIniParseResult result;
                        if (!iniParser.TryParse(text, out result))
                            throw new Exception($"Config error: {result.ToString()}");

                        LoadConfig(iniParser);
                    }
                }

                iniParser.Clear(); // remove any existing settings that might no longer exist
                SaveConfig(iniParser);

                string saveText = iniParser.ToString();
                MyAPIGateway.Utilities.SetVariable<string>(VariableId, saveText);

                using (TextWriter file = MyAPIGateway.Utilities.WriteFileInWorldStorage(FileName, typeof(HeavyGasSettings)))
                {
                    file.Write(saveText);
                }
            }

            void LoadOnClient()
            {
                string text;
                if (!MyAPIGateway.Utilities.GetVariable<string>(VariableId, out text))
                    throw new Exception("No config found in sandbox.sbc!");

                MyIni iniParser = new MyIni();
                MyIniParseResult result;
                if (!iniParser.TryParse(text, out result))
                    throw new Exception($"Config error: {result.ToString()}");

                LoadConfig(iniParser);
            }

            void LoadConfig(MyIni iniParser)
            {
                EnableNPCs = iniParser.Get(IniSection, nameof(EnableNPCs)).ToBoolean(EnableNPCs);
                Conversion_H2 = iniParser.Get(IniSection, nameof(Conversion_H2)).ToDouble(Conversion_H2); // NEW
                Conversion_O2 = iniParser.Get(IniSection, nameof(Conversion_O2)).ToDouble(Conversion_O2); // NEW
            }

            void SaveConfig(MyIni iniParser)
            {
                iniParser.Set(IniSection, nameof(EnableNPCs), EnableNPCs);
                iniParser.Set(IniSection, nameof(Conversion_H2), Conversion_H2); // NEW
                iniParser.Set(IniSection, nameof(Conversion_O2), Conversion_O2); // NEW
            }
        }
    }
}