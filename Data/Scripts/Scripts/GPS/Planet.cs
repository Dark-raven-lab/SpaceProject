    using System;
    using System.Text;
    using Sandbox.ModAPI;
    using VRage.Game.Components;
    using VRage.Game.ModAPI;


    using Sandbox.Common.ObjectBuilders;
    using Sandbox.Definitions;
    using Sandbox.Game;
    using Sandbox.Game.Entities;
    using Sandbox.Game.EntityComponents;
    using Sandbox.ModAPI.Interfaces.Terminal;
    using VRage;
    using VRage.ModAPI;
    using VRage.ObjectBuilders;
    using VRage.Utils;
    using VRageMath;
    using System.Collections.Generic;
    using VRage.Voxels;
    using VRage.Game;


namespace ServerMod.GPS {


    public static class GPSHelper {

        public static void AddLocalGpsColored(string name, string description, Vector3D position, Color color)
        {
            IMyGps gps = MyAPIGateway.Session.GPS.Create(name, description, position, true, true);
            gps.GPSColor = color;
            MyAPIGateway.Session.GPS.AddLocalGps(gps);
        }

        public static void CreateGPSForPlanet(){
            AddLocalGpsColored("Земля", "Есть руды", new Vector3D(0,0,0), Color.Blue);
            
            
        }


    }


}