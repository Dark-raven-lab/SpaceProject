using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.ObjectBuilders;
using Sandbox.Common.ObjectBuilders;

namespace ServerMod.Turret
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_LargeGatlingTurret), true)]
    public class FixGatlingTurrets : FixTurret  { }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_LargeMissileTurret), true)]
    public class FixMissleTurrets : FixTurret  { }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_InteriorTurret), true)]
    public class FixInteriorTurret : FixTurret  { }
    
    public class FixTurret : MyGameLogicComponent  {
        private IMyLargeTurretBase myBlock;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            myBlock = (Entity as IMyLargeTurretBase);
            myBlock.PropertiesChanged += MyBlock_PropertiesChanged;
            myBlock.OnMarkForClose += MyBlock_OnMarkForClose;
        }

        private void MyBlock_PropertiesChanged(IMyTerminalBlock obj) {
            myBlock.EnableIdleRotation = false;
        }

        private void MyBlock_OnMarkForClose(VRage.ModAPI.IMyEntity obj) {
             myBlock.OnMarkForClose -= MyBlock_OnMarkForClose;
             myBlock.PropertiesChanged -= MyBlock_OnMarkForClose;
        }
    }
}
