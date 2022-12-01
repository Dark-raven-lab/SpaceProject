using System;
using System.Collections.Generic;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

// This mod can also be disabled by other mods if the actions are overlapping and you want people to still be able to use the wings as props.
// ----
// A quick way to disable it is by calling this after a second after the session is not null:
//
// MyAPIGateway.Utilities.SendModMessage(473571246, MyTuple.Create(false, "YourModName"));
//
// -----
// Alternatively you can register the same message ID to get a getter and a setter which you can call whenever you want.
//
// private const long AERODYNAMICS_WORKSHOP_ID = 473571246;
// private Func<bool> aerodynamicsGetter = null;
// private Action<bool, string> aerodynamicsSetter = null;
//
// // in your init method
//     MyAPIGateway.Utilities.RegisterMessageHandler(AERODYNAMICS_WORKSHOP_ID, AerodynamicsMethods);
//
// // in UnloadData()
//     MyAPIGateway.Utilities.UnregisterMessageHandler(AERODYNAMICS_WORKSHOP_ID, AerodynamicsMethods);
//
// void AerodynamicsMethods(object obj)
// {
//     if(obj is MyTuple<Func<bool>, Action<bool, string>>)
//     {
//         var methods = (MyTuple<Func<bool>, Action<bool, string>>)obj;
//         aerodynamicsGetter = methods.Item1;
//         aerodynamicsSetter = methods.Item2;
//
//         // avoiding a collection changed exception by calling the unregister after it's done iterating handlers.
//         MyAPIGateway.Utilities.InvokeOnGameThread(() => MyAPIGateway.Utilities.UnregisterMessageHandler(AERODYNAMICS_WORKSHOP_ID, AerodynamicsMethods));
//     }
// }
//
// // anywhere you want to toggle, example
// if(aerodynamicsGetter != null)
// {
//     var enabled = aerodynamicsGetter.Invoke();
//     aerodynamicsSetter.Invoke(!enabled, "MyMod");
// }

namespace Digi2.AeroWings
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class AerodynamicsModTN : MySessionComponentBase
    {
        public override void LoadData()
        {
            VRage.Utils.MyLog.Default.WriteLineAndConsole($"###### LoadData() called");
            instance = this;
            LogTN.SetUp("Plane Parts", WORKSHOP_ID);
        }

        public static AerodynamicsModTN instance = null;

        private bool init = false;
        private short planetRefreshTick = 0;
        private byte delaySendMethods = 60;

        public bool enabled = true;
        public string disabledBy = null;
        public readonly List<MyPlanet> planets = new List<MyPlanet>();

        private const long WORKSHOP_ID = 837058476;
        private const short PLANET_REFRESH_TICKS = 60 * 5; // refreshing planet cache time
        public const float MIN_ATMOSPHERE = 0.4f;
        public const float MAX_ATMOSPHERE = 0.7f;
        public Vector3 DEBUG_COLOR_ACTIVE = new Vector3(120f / 360f, 1, 1);
        public Vector3 DEBUG_COLOR_INACTIVE = new Vector3(0, 1, 1);

        public void Init()
        {
            init = true;
            LogTN.Init();

            // API enable toggle message handler
            MyAPIGateway.Utilities.RegisterMessageHandler(WORKSHOP_ID, ModMessageHandler);

            // find planets ASAP
            MyAPIGateway.Entities.GetEntities(null, IterateEntity);
        }

        protected override void UnloadData()
        {
            instance = null;

            try
            {
                if (init)
                {
                    init = false;
                    MyAPIGateway.Utilities.UnregisterMessageHandler(WORKSHOP_ID, ModMessageHandler);
                    planets.Clear();
                }
            }
            catch (Exception e)
            {
                LogTN.Error(e);
            }

            LogTN.Close();
        }

        public override void UpdateAfterSimulation()
        {
            try
            {
                if (!init)
                {
                    if (MyAPIGateway.Session == null)
                        return;

                    Init();
                }

                if (++planetRefreshTick >= PLANET_REFRESH_TICKS)
                {
                    planetRefreshTick = 0;
                    planets.Clear();
                    MyAPIGateway.Entities.GetEntities(null, IterateEntity);
                }

                // send the API methods after a second
                if (delaySendMethods > 0 && --delaySendMethods == 0)
                {
                    MyAPIGateway.Utilities.SendModMessage(WORKSHOP_ID, MyTuple.Create<Func<bool>, Action<bool, string>>(ModEnabledGetter, ModEnabledSetter));
                }
            }
            catch (Exception e)
            {
                LogTN.Error(e);
            }
        }

        private bool IterateEntity(IMyEntity e)
        {
            var p = e as MyPlanet;

            if (p != null && p.HasAtmosphere)
                planets.Add(p);

            return false; // never return true or you'll get a NRE because of the null list given to the GetEntities() method.
        }

        private void ModMessageHandler(object obj)
        {
            try
            {
                if (obj is MyTuple<bool, string>)
                {
                    var data = (MyTuple<bool, string>)obj;
                    enabled = data.Item1;

                    if (enabled)
                    {
                        LogTN.Info("Wing logic re-enabled by mod \"" + data.Item2 + "\".");
                        disabledBy = null;
                    }
                    else
                    {
                        LogTN.Info("Wing logic turned off by mod \"" + data.Item2 + "\".");
                        disabledBy = data.Item2;
                    }
                }
            }
            catch (Exception e)
            {
                LogTN.Error(e);
            }
        }

        private bool ModEnabledGetter()
        {
            return enabled;
        }

        private void ModEnabledSetter(bool set, string name)
        {
            try
            {
                enabled = set;

                if (enabled)
                {
                    LogTN.Info("Wing logic re-enabled by mod \"" + name ?? "(unknown mod)" + "\".");
                    disabledBy = null;
                }
                else
                {
                    LogTN.Info("Wing logic turned off by mod \"" + name ?? "(unknown mod)" + "\".");
                    disabledBy = name;
                }
            }
            catch (Exception e)
            {
                LogTN.Error(e);
            }
        }

        private bool controlsAdded = false;

        public void AddTerminalControls()
        {
            if (controlsAdded)
                return;

            controlsAdded = true;

            var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlOnOffSwitch, IMyTerminalBlock>("Wings.UseGridCOM");
            c.Title = MyStringId.GetOrCompute("Use center of mass of");
            c.OnText = MyStringId.GetOrCompute("Grid");
            c.OffText = MyStringId.GetOrCompute("Ship");
            c.SupportsMultipleBlocks = true;
            c.Visible = TC_GridCOM_Visible;
            c.Getter = TC_GridCOM_Getter;
            c.Setter = TC_GridCOM_Setter;
            MyAPIGateway.TerminalControls.AddControl<IMyTerminalBlock>(c);
        }

        private WingTN GetGameLogic(IMyTerminalBlock block) => block?.GameLogic?.GetAs<WingTN>();

        private bool TC_GridCOM_Visible(IMyTerminalBlock block)
        {
            return GetGameLogic(block) != null;
        }

        private bool TC_GridCOM_Getter(IMyTerminalBlock block)
        {
            var logic = GetGameLogic(block);
            return (logic != null ? logic.UseGridCOM : false);
        }

        private void TC_GridCOM_Setter(IMyTerminalBlock block, bool value)
        {
            var logic = GetGameLogic(block);
            if (logic != null)
                logic.UseGridCOM = value;
        }
    }
}