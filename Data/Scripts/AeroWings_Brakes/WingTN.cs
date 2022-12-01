using System;
using System.Text;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
//using System.Collections.Generic;



namespace Digi2.AeroWings
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_TerminalBlock), false,
            "aero-wing_1x5x1_rounded_edge_Small",
            "aero-wing_2x5x1_rounded_edge_Small",
            "aero-wing_3x5x1_rounded_edge_Small",
            "aero-wing_4x5x1_rounded_edge_Small",
            "aero-wing_5x5x1_rounded_edge_Small",
            "aero-wing_6x5x1_rounded_edge_Small",
            "aero-wing_5x3x1_rounded_edge_Small",
            "aero-wing_5x2x1_rounded_edge_Small",
            "aero-wing_5x1x1_rounded_edge_Small",

            "aero-wing_1x5x1_rounded_edge_Large",
            "aero-wing_2x5x1_rounded_edge_Large",
            "aero-wing_3x5x1_rounded_edge_Large",
            "aero-wing_4x5x1_rounded_edge_Large",
            "aero-wing_5x5x1_rounded_edge_Large",
            "aero-wing_6x5x1_rounded_edge_Large",
            "aero-wing_5x3x1_rounded_edge_Large",
            "aero-wing_5x2x1_rounded_edge_Large",
            "aero-wing_5x1x1_rounded_edge_Large",

            "aero-wing_2x5x1_pointed_edge_Small",
            "aero-wing_3x5x1_pointed_edge_Small",
            "aero-wing_4x5x1_pointed_edge_Small",
            "aero-wing_5x5x1_pointed_edge_Small",
            "aero-wing_6x5x1_pointed_edge_Small",
            "aero-wing_7x5x1_pointed_edge_Small",
            "aero-wing_2x1x1_pointed_edge_Small",

            "aero-wing_2x5x1_pointed_edge_Large",
            "aero-wing_3x5x1_pointed_edge_Large",
            "aero-wing_4x5x1_pointed_edge_Large",
            "aero-wing_5x5x1_pointed_edge_Large",
            "aero-wing_6x5x1_pointed_edge_Large",
            "aero-wing_7x5x1_pointed_edge_Large",
            "aero-wing_2x1x1_pointed_edge_Large"
        )]
    public class WingTN : MyGameLogicComponent
    {
        //test
        private Vector3D COM_offset;
        int tempcount;
        int tempcountmax;
        private MyCubeGrid biggestgrid;
        // test ende

        private const string USE_GROUP_COM_TAG = "UseGridCOM";

        private IMyTerminalBlock block;
        private float atmosphere = 0;
        private int atmospheres = 0;

        private bool debugPrevColorSaved = false;
        private Vector3 debugPrevColor;

        private bool _useGridCOM = false;
        public bool UseGridCOM
        {
            get { return _useGridCOM; }
            set
            {
                _useGridCOM = value;

                var data = block.CustomData;
                var tagIndex = data.IndexOf(USE_GROUP_COM_TAG, 0, StringComparison.InvariantCultureIgnoreCase);

                if (tagIndex != -1)
                    data = data.Substring(0, tagIndex) + data.Substring(tagIndex + USE_GROUP_COM_TAG.Length); // strip the tag from customdata

                if (_useGridCOM)
                    data = (string.IsNullOrWhiteSpace(block.CustomData) ? USE_GROUP_COM_TAG : $"{block.CustomData}\n{USE_GROUP_COM_TAG}");

                block.CustomData = data.Trim();
            }
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            block = (IMyTerminalBlock)Entity;
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }


        public override void UpdateOnceBeforeFrame()
        {
            try
            {
                if (AerodynamicsModTN.instance == null)
                    throw new NullReferenceException("AerodynamicsModTN.instance is null");

                AerodynamicsModTN.instance.AddTerminalControls();

                if (block.CubeGrid?.Physics == null)
                    return;

                NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;

                if (MyAPIGateway.Session.IsServer)
                {
                    block.ShowInTerminal = false;
                    block.ShowInToolbarConfig = false;
                }

                block.AppendingCustomInfo += CustomInfo;
                block.CustomDataChanged += CustomDataChanged;
                CustomDataChanged(block);
            }
            catch (Exception e)
            {
                LogTN.Error($"UpdateOnceBeforeFrame error {e.Message}, blockId={Entity?.EntityId}");
                LogTN.Error(e);
            }
        }

        public override void Close()
        {
            block = (IMyTerminalBlock)Entity;

            if (block != null)
            {
                block.AppendingCustomInfo -= CustomInfo;
                block.CustomDataChanged -= CustomDataChanged;
            }
        }

        private void CustomDataChanged(IMyTerminalBlock _unused)
        {
            _useGridCOM = (block.CustomData.IndexOf(USE_GROUP_COM_TAG, 0, StringComparison.InvariantCultureIgnoreCase) != -1);
        }

        //Digi: public override void UpdateAfterSimulation()
        public override void UpdateBeforeSimulation()
        {
            //new test 30.09.2019 to fix the wheel bug, in tests the problem only occurs afetr pasting ships, no longer on ships they are in world with load
            base.UpdateBeforeSimulation();

            bool noLiftWarning = true;

            try
            {
                if (!AerodynamicsModTN.instance.enabled)
                    return;

                var grid = block.CubeGrid;

                if (grid.Physics == null || grid.Physics.IsStatic || block.MarkedForClose || block.Closed || !block.IsWorking)
                    return;

                bool debug = (MyAPIGateway.Session.CreativeMode && block.ShowInToolbarConfig);
                bool debugText = true;

                if (debug)
                {
                    var controlled = MyAPIGateway.Session.ControlledObject as IMyEntity;

                    if (controlled != null)
                    {
                        debugText = (controlled.EntityId == grid.EntityId || Vector3D.DistanceSquared(block.GetPosition(), controlled.GetPosition()) <= (30 * 30));
                    }

                    if (!debugPrevColorSaved)
                    {
                        debugPrevColorSaved = true;
                        debugPrevColor = block.SlimBlock.GetColorMask();
                    }
                }
                else if (debugPrevColorSaved)
                {
                    debugPrevColorSaved = false;
                    block.Render.ColorMaskHsv = debugPrevColor;
                }

                if (atmospheres == 0 || atmosphere <= float.Epsilon)
                {
                    if (debug)
                    {
                        if (debugText)
                            MyAPIGateway.Utilities.ShowNotification(block.CustomName + ": not in atmosphere", 16, MyFontEnum.Red);

                        DebugSetColor(ref AerodynamicsModTN.instance.DEBUG_COLOR_INACTIVE);
                    }

                    return;
                }

                var blockMatrix = block.WorldMatrix;
                var vel = grid.Physics.GetVelocityAtPoint(blockMatrix.Translation);
                double speedSq = MathHelper.Clamp(vel.LengthSquared() * 2, 0, 10000);

                //if(debug)
                //{
                //    MyTransparentGeometry.AddLineBillboard(MyStringId.GetOrCompute("Square"), (Color.YellowGreen * 0.5f), blockMatrix.Translation, Vector3D.Normalize(grid.Physics.LinearVelocity), 12, 0.1f);
                //}

                if (speedSq >= 50)
                {
                    Vector3D fw = blockMatrix.Left;
                    double forceMul = 0.75;

                    switch (block.BlockDefinition.SubtypeId)
                    {
                        case "aero-wing_2x5x1_rounded_edge_Small":
                        case "aero-wing_5x2x1_rounded_edge_Small":
                        case "aero-wing_3x5x1_pointed_edge_Small":
                            forceMul = 1.0;
                            fw = Vector3D.Normalize(blockMatrix.Left + blockMatrix.Forward * 0.15);
                            break;
                        case "aero-wing_3x5x1_rounded_edge_Small":
                        case "aero-wing_5x3x1_rounded_edge_Small":
                        case "aero-wing_4x5x1_pointed_edge_Small":
                            forceMul = 1.25;
                            fw = Vector3D.Normalize(blockMatrix.Left + blockMatrix.Forward * 0.25);
                            break;
                        case "aero-wing_4x5x1_rounded_edge_Small":
                        case "aero-wing_5x5x1_pointed_edge_Small":
                            forceMul = 1.5;
                            fw = Vector3D.Normalize(blockMatrix.Left + blockMatrix.Forward * 0.35);
                            break;
                        case "aero-wing_5x5x1_rounded_edge_Small":
                        case "aero-wing_6x5x1_pointed_edge_Small":
                            forceMul = 1.75;
                            fw = Vector3D.Normalize(blockMatrix.Left + blockMatrix.Forward * 0.45);
                            break;
                        case "aero-wing_6x5x1_rounded_edge_Small":
                        case "aero-wing_7x5x1_pointed_edge_Small":
                            forceMul = 2.0;
                            fw = Vector3D.Normalize(blockMatrix.Left + blockMatrix.Forward * 0.55);
                            break;

                        case "aero-wing_1x5x1_rounded_edge_Large":
                        case "aero-wing_5x1x1_rounded_edge_Large":
                        case "aero-wing_2x5x1_pointed_edge_Large":
                            forceMul = 0.75 * 10;
                            fw = Vector3D.Normalize(blockMatrix.Left);
                            break;
                        case "aero-wing_2x5x1_rounded_edge_Large":
                        case "aero-wing_5x2x1_rounded_edge_Large":
                        case "aero-wing_3x5x1_pointed_edge_Large":
                            forceMul = 1.0 * 10;
                            fw = Vector3D.Normalize(blockMatrix.Left + blockMatrix.Forward * 0.15);
                            break;
                        case "aero-wing_3x5x1_rounded_edge_Large":
                        case "aero-wing_5x3x1_rounded_edge_Large":
                        case "aero-wing_4x5x1_pointed_edge_Large":
                            forceMul = 1.25 * 10;
                            fw = Vector3D.Normalize(blockMatrix.Left + blockMatrix.Forward * 0.25);
                            break;
                        case "aero-wing_4x5x1_rounded_edge_Large":
                        case "aero-wing_5x5x1_pointed_edge_Large":
                            forceMul = 1.5 * 10;
                            fw = Vector3D.Normalize(blockMatrix.Left + blockMatrix.Forward * 0.35);
                            break;
                        case "aero-wing_5x5x1_rounded_edge_Large":
                        case "aero-wing_6x5x1_pointed_edge_Large":
                            forceMul = 1.75 * 10;
                            fw = Vector3D.Normalize(blockMatrix.Left + blockMatrix.Forward * 0.45);
                            break;
                        case "aero-wing_6x5x1_rounded_edge_Large":
                        case "aero-wing_7x5x1_pointed_edge_Large":
                            forceMul = 2.0 * 10;
                            fw = Vector3D.Normalize(blockMatrix.Left + blockMatrix.Forward * 0.55);
                            break;
                    }

                    double speedDir = fw.Dot(vel);

                    if (debug)
                    {
                        //changed, because both directions are alowed                        MyTransparentGeometry.AddLineBillboard(MyStringId.GetOrCompute("Square"), ((speedDir > 0 ? Color.Blue : Color.Red) * 0.5f), blockMatrix.Translation, Vector3D.Normalize(vel), 10, 0.05f);
                        MyTransparentGeometry.AddLineBillboard(MyStringId.GetOrCompute("Square"), (Color.Blue * 0.5f), blockMatrix.Translation, Vector3D.Normalize(vel), 10, 0.05f);
                    }

                    // changed to bidirectional, works now in both directions, was "if(speedDir > 0)"
                    if (speedDir > 0 || speedDir < 0)
                    {
                        var upDir = blockMatrix.Up;
                        var forceVector = -upDir * upDir.Dot(vel) * forceMul * speedSq * atmosphere;
                        var applyForceAt = Vector3D.Zero;

                        if (_useGridCOM)
                        {
                            applyForceAt = grid.Physics.CenterOfMassWorld;

                            //moved to here because acting to biggest grid in other case
                            grid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, forceVector, applyForceAt, null);
                        }
                        else
                        {
                            //org from Digi, have problems with rotating wheels
                            //var gridSharedProperties = MyGridPhysicalGroupData.GetGroupSharedProperties((MyCubeGrid)grid);
                            //applyForceAt = gridSharedProperties.CoMWorld;
                            //----------------------------

                            //new version: Ship COM
                            if (++tempcount > tempcountmax)
                            {
                                tempcount = 0;
                                tempcountmax = 60;

                                var COM_ship = grid.Physics.CenterOfMassWorld;
                                float grid_mass = grid.Physics.Mass;
                                var biggestgrid = grid;
                                COM_offset = Vector3D.Zero;

                                var subgrids = MyAPIGateway.GridGroups.GetGroup(grid, VRage.Game.ModAPI.GridLinkTypeEnum.Logical);
                                if (subgrids.Count > 1)
                                {
                                    foreach (MyCubeGrid subgrid in subgrids)
                                    {
                                        if (subgrid != grid && subgrid.Physics != null)
                                        {
                                            if (subgrid.Physics.Mass > biggestgrid.Physics.Mass)
                                            {
                                                biggestgrid = subgrid;
                                            }

                                            COM_ship = COM_ship + (subgrid.Physics.CenterOfMassWorld - COM_ship) * (subgrid.Physics.Mass / (grid_mass + subgrid.Physics.Mass));
                                            grid_mass = grid_mass + subgrid.Physics.Mass;
                                        }
                                    }
                                    COM_offset = Vector3D.TransformNormal(COM_ship - grid.Physics.CenterOfMassWorld, MatrixD.Transpose(blockMatrix));
                                }
                            }

                            //show COM_offset
                            //MyTransparentGeometry.AddLineBillboard(MyStringId.GetOrCompute("Square"), Color.Red, grid.Physics.CenterOfMassWorld, Vector3D.TransformNormal(COM_offset, blockMatrix), 1, 1.15f);

                            applyForceAt = grid.Physics.CenterOfMassWorld + Vector3D.TransformNormal(COM_offset, blockMatrix);


                            //acting to biggest grid to prevent from having impact to weak rotors
                            if (biggestgrid != null)
                                biggestgrid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, forceVector, applyForceAt, null);
                            else
                                grid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, forceVector, applyForceAt, null);
                            //test ende
                        }

                        //test only, center of mass bug
                        //MyTransparentGeometry.AddPointBillboard(MyStringId.GetOrCompute("Square"), Color.Green, applyForceAt, 2.5f, 0);

                        //moved up
                        //grid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, forceVector, applyForceAt, null);

                        noLiftWarning = false;

                        if (debug)
                        {
                            var totalForce = forceVector.Normalize();

                            var height = (float)totalForce / 100000f;
                            var pos = blockMatrix.Translation + forceVector * height;

                            MyTransparentGeometry.AddBillboardOriented(MyStringId.GetOrCompute("Square"), Color.Green * 0.5f, pos, blockMatrix.Forward, forceVector, 1.25f, height);

                            if (debugText)
                                MyAPIGateway.Utilities.ShowNotification(block.CustomName + ": forceMul=" + Math.Round(forceMul, 2) + "; atmosphere=" + Math.Round(atmosphere * 100, 0) + "%; totalforce=" + Math.Round(totalForce / 1000, 2) + " MN", 16, MyFontEnum.Green);

                            DebugSetColor(ref AerodynamicsModTN.instance.DEBUG_COLOR_ACTIVE);
                        }
                    }
                    else if (debug)
                    {
                        if (debugText)
                            MyAPIGateway.Utilities.ShowNotification(block.CustomName + ": wrong direction", 16, MyFontEnum.Red);

                        DebugSetColor(ref AerodynamicsModTN.instance.DEBUG_COLOR_INACTIVE);
                    }
                }
                else if (debug)
                {
                    if (debugText)
                        MyAPIGateway.Utilities.ShowNotification(block.CustomName + ": not enough speed", 16, MyFontEnum.Red);

                    DebugSetColor(ref AerodynamicsModTN.instance.DEBUG_COLOR_INACTIVE);
                }
            }
            catch (Exception e)
            {
                if (noLiftWarning)
                    LogTN.Error(e, "Error in wing update - WARNING NO WING LIFT!");
                else
                    LogTN.Error(e);
            }
        }

        public override void UpdateBeforeSimulation10()
        {
            try
            {
                if (!AerodynamicsModTN.instance.enabled)
                    return;

                var block = (IMyTerminalBlock)Entity;
                var grid = (MyCubeGrid)block.CubeGrid;

                if (grid.Physics == null || grid.Physics.IsStatic || block.MarkedForClose || block.Closed || !block.IsWorking)
                    return;

                var gridCenter = grid.Physics.CenterOfMassWorld;
                var planets = AerodynamicsModTN.instance.planets;

                atmosphere = 0;
                atmospheres = 0;

                for (int i = planets.Count - 1; i >= 0; --i)
                {
                    var planet = planets[i];

                    if (planet.Closed || planet.MarkedForClose)
                    {
                        planets.RemoveAt(i);
                        continue;
                    }

                    if (Vector3D.DistanceSquared(gridCenter, planet.WorldMatrix.Translation) < (planet.AtmosphereRadius * planet.AtmosphereRadius))
                    {
                        atmosphere += planet.GetAirDensity(gridCenter);
                        atmospheres++;
                    }
                }

                if (atmospheres > 0)
                {
                    atmosphere /= atmospheres;
                    atmosphere = MathHelper.Clamp((atmosphere - AerodynamicsModTN.MIN_ATMOSPHERE) / (AerodynamicsModTN.MAX_ATMOSPHERE - AerodynamicsModTN.MIN_ATMOSPHERE), 0f, 1f);
                }
            }
            catch (Exception e)
            {
                LogTN.Error(e);
            }
        }

        public override void UpdateBeforeSimulation100()
        {
            try
            {
                var block = (IMyTerminalBlock)Entity;
                var on = AerodynamicsModTN.instance.enabled;

                block.RefreshCustomInfo();

                if (on)
                    NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME;
                else
                    NeedsUpdate &= ~(MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME);
            }
            catch (Exception e)
            {
                LogTN.Error(e);
            }
        }

        private void CustomInfo(IMyTerminalBlock block, StringBuilder info)
        {
            try
            {
                info.Append('\n');

                if (AerodynamicsModTN.instance.enabled)
                    info.Append("Wings are enabled and working.");
                else
                    info.Append("Wings disabled by another mod: ").Append(AerodynamicsModTN.instance.disabledBy ?? "(unknown mod)");
            }
            catch (Exception e)
            {
                LogTN.Error(e);
            }
        }

        private void DebugSetColor(ref Vector3 color)
        {
            if (!block.Render.ColorMaskHsv.Equals(color, 0.0001f))
            {
                block.Render.ColorMaskHsv = color; // this does not get saved and also faster than changing color normally
            }
        }
    }
}
