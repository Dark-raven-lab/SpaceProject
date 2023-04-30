using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.ObjectBuilders;
using System;
using System.Collections.Generic;
using Sandbox.Game.Entities;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;
using ObjectBuilders.SafeZone;
using ServerMod;
using Sandbox.ModAPI.Interfaces.Terminal;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using System.Collections.Generic;
using Sandbox.Game;


namespace ServerMod.SafeZone
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_SafeZoneBlock), true)]
    public class FixSafeZone : MyGameLogicComponent  {
        private SpaceEngineers.Game.ModAPI.IMySafeZoneBlock myBlock;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            myBlock = Entity as SpaceEngineers.Game.ModAPI.IMySafeZoneBlock;
            //myBlock.PropertiesChanged += MyBlock_PropertiesChanged;
            //myBlock.OnMarkForClose += MyBlock_OnMarkForClose;
        }

        private void MyBlock_PropertiesChanged(Sandbox.ModAPI.Ingame.IMyTerminalBlock obj) {
            
            bool enabled = myBlock.IsSafeZoneEnabled();
            if (!enabled) {
                return;
            }
            
        }

        private void MyBlock_OnMarkForClose(VRage.ModAPI.IMyEntity obj) {
            myBlock.OnMarkForClose -= MyBlock_OnMarkForClose;
            myBlock.PropertiesChanged -= MyBlock_OnMarkForClose;
        }
    }
}
