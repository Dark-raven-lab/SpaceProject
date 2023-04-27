using System;
using System.IO;
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
using Example;

namespace Nevcairiel.HeavyGas
{
    // This object gets attached to entities depending on their type and optionally subtype aswell.
    // The 2nd arg, "false", is for entity-attached update if set to true which is not recommended, see for more info: https://forum.keenswh.com/threads/modapi-changes-jan-26.7392280/
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_OxygenTank), false)]
    public class HeavyGas : MyGameLogicComponent
    {
        // A molecule of water weights 18 atomic mass units
        // .. 2 of which are hydrogen
        // .. and 16 are oxygen
        // in SE, turning 1kg of Ice into gas results in 10L of Hydrogen, and 5L of Oxygen, scale the values accordingly
        //public static double GAS_L_KG_CONVERSION_H2 = (2.0 / 18.0) / 10.0;
        //public static double GAS_L_KG_CONVERSION_O2 = (16.0 / 18.0) / 5.0;

        private IMyGasTank tank;

        bool SetupComplete = false;
        double massMultiplier = 0;

        bool NPCOwned = false;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            // this method is called async! always do stuff in the first update unless you're sure it must be in this one.
            // NOTE the objectBuilder arg is not the Entity's but the component's, and since the component wasn't loaded from an OB that means it's always null, which it is (AFAIK).
            base.Init(objectBuilder);
            tank = (IMyGasTank)Entity;

            MyGasTankDefinition tankDef = (MyGasTankDefinition)tank.SlimBlock.BlockDefinition;

            if (tankDef != null && tankDef.StoredGasId == MyResourceDistributorComponent.HydrogenId)
                massMultiplier = Example.Mod.HeavyGasSettings.Conversion_H2;
            else if (tankDef != null && tankDef.StoredGasId == MyResourceDistributorComponent.OxygenId)
                massMultiplier = Example.Mod.HeavyGasSettings.Conversion_O2;

            NeedsUpdate = massMultiplier > 0f ? MyEntityUpdateEnum.EACH_FRAME : MyEntityUpdateEnum.NONE;
        }

        private void CheckIfNPCOwned(IMyCubeGrid grid)
        {
            NPCOwned = true;
            foreach (var owner in grid.BigOwners)
            {
                if (owner == 0)
                    continue;

                if (MyAPIGateway.Players.TryGetSteamId(owner) > 0)
                    NPCOwned = false;
            }
        }

        private void OnGridSplit(IMyCubeGrid arg1, IMyCubeGrid arg2)
        {
            // stop listening for events on split grids
            arg1.OnBlockOwnershipChanged -= CheckIfNPCOwned;
            arg1.OnGridSplit -= OnGridSplit;
            arg2.OnBlockOwnershipChanged -= CheckIfNPCOwned;
            arg2.OnGridSplit -= OnGridSplit;

            // and continue listening on the tanks grid
            tank.CubeGrid.OnBlockOwnershipChanged += CheckIfNPCOwned;
            tank.CubeGrid.OnGridSplit += OnGridSplit;

            // .. and update ownership now
            CheckIfNPCOwned(tank.CubeGrid);
        }

        public override void UpdateBeforeSimulation()
        {
            base.UpdateBeforeSimulation();

            if (SetupComplete == false)
            {
                tank.CubeGrid.OnBlockOwnershipChanged += CheckIfNPCOwned;
                tank.CubeGrid.OnGridSplit += OnGridSplit;
                CheckIfNPCOwned(tank.CubeGrid);

                // Update every 100 frames only from now on
                NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME;
                SetupComplete = true;
            }
        }

        public override void UpdateAfterSimulation100()
        {
            base.UpdateAfterSimulation100();

            // disable for static grid
            if (tank.CubeGrid.IsStatic){
                return;
            }

            MyFixedPoint newExternalMass;

            // disable extra mass for NPC grids, if needed
            if (Example.Mod.HeavyGasSettings.EnableNPCs == false && NPCOwned == true)
            {
                newExternalMass = 0;
            }
            else
            {
                newExternalMass = (MyFixedPoint)((tank.FilledRatio * tank.Capacity) * massMultiplier);
            }

            MyInventory inv = (MyInventory)tank.GetInventory();
            // update external mass
            if (inv != null && newExternalMass != inv.ExternalMass)
            {
                inv.ExternalMass = newExternalMass;
                inv.Refresh();
            }
        }
    }
}
