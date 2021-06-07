using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;
using Sandbox.Game.GameSystems;
using System.Xml;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        public bool interrupt = false;

        class Implementation : ScriptBase
        {
            public bool interrupt = false;

            private int counter = 0;
            private static readonly int versionInfoDisplayTime = 2;
            private static readonly bool printMotors = false;

            private List<MyTuple<string, Vector3D, int, long>> stations;

            private readonly IMyBroadcastListener bcListener;
            private static readonly string broadcastTag = "antennaScriptBroadcast";

            private readonly IMyUnicastListener ucListener;
            private static readonly string unicastPing = "queryPosition";
            private static readonly string unicastPong = "responsePosition";

            private long targetAddress = 0;
            Vector3D coordinates;
            bool targetVectorSet = false;
            List<AntennaConstruct> antennaGroups;

            BlockConfig pbConfig;

            public Implementation(MyGridProgram parent, MyIni custom, MyIni storage, MyIniParseResult customSectionResult) : base(custom, storage, customSectionResult)
            {
                antennaGroups = new List<AntennaConstruct>();
                
                parent.Runtime.UpdateFrequency |= UpdateFrequency.Update100;
                parent.Runtime.UpdateFrequency |= UpdateFrequency.Update10;
                parent.Runtime.UpdateFrequency |= UpdateFrequency.Update1;

                bcListener = parent.IGC.RegisterBroadcastListener(broadcastTag);
                bcListener.SetMessageCallback(broadcastTag);

                ucListener = parent.IGC.UnicastListener;

                stations = new List<MyTuple<string, Vector3D, int, long>>();

                pbConfig = ParseBlockConfig(parent.Me);
                pbConfig.WriteConfigTemplatePB();
            }

            public override void OnTerminal(MyGridProgram parent, string arguments)
            {
                parent.Runtime.UpdateFrequency ^= UpdateFrequency.Update100;
                parent.Runtime.UpdateFrequency ^= UpdateFrequency.Update10;
                parent.Runtime.UpdateFrequency ^= UpdateFrequency.Update1;
                parent.Runtime.UpdateFrequency |= UpdateFrequency.Once;//use CallOnce to ensure no other updates that were triggered in the same tick as OnTermial overwrite version info
            }

            public override void OnCallOnce(MyGridProgram parent)
            {
                parent.Runtime.UpdateFrequency |= UpdateFrequency.Update100;
                counter = versionInfoDisplayTime;
                parent.Echo("Steer Antenna Script Running, Version: 0.0.6");
            }

            public override void OnUpdate100(MyGridProgram parent)
            {
                if (counter != 0)
                    counter--;
                else
                {
                    parent.Runtime.UpdateFrequency |= UpdateFrequency.Update10;
                    parent.Runtime.UpdateFrequency |= UpdateFrequency.Update1;
                }

                targetAddress = 0;
                targetVectorSet = false;
                pbConfig = ParseBlockConfig(parent.Me);

                if (TryParseGPS(pbConfig.target, out coordinates) || TryParseVector3D(pbConfig.target, out coordinates))
                    targetVectorSet = true;
                else
                    foreach (var station in stations)
                        if (pbConfig.target.Equals(station.Item1))
                        {
                            coordinates = station.Item2;
                            targetVectorSet = true;
                            targetAddress = station.Item4;
                            break;
                        }

                //update other grids about this grids existance
                if (pbConfig.enableBroadcast)
                    parent.IGC.SendBroadcastMessage(broadcastTag, new MyTuple<string, Vector3D>(parent.Me.CubeGrid.CustomName, parent.Me.GetPosition()));

                //reparse grids to check for new or changed blocks
                if (targetVectorSet)
                    antennaGroups = FindAntennasAndDrives(parent);

                stations = CleanStationsList(stations);
            }

            public override void OnUpdate10(MyGridProgram parent)
            {
                if(targetVectorSet)//target configured an position known?
                    parent.Echo("Target Coords: " + FormatVector(coordinates));
                else
                    parent.Echo("No coordinates found");

                if (targetAddress != 0)
                    parent.Echo("Target Address: " + targetAddress);

                if (antennaGroups.Count == 0)//steerable antenna group defined?
                    parent.Echo("No Antenna Groups are configured");
                else if(printMotors)
                    foreach (var ag in antennaGroups)
                        ag.PrintGroup(parent);

                //print station info
                parent.Echo("Available Stations:\n");
                foreach (var station in stations)
                    parent.Echo(
                        "Station: " + station.Item1
                        + "\nLocation: " + FormatVector(station.Item2)
                        + "\nAddress: " + station.Item4
                        + "\nTime since last contact: " + station.Item3 + "\n");
            }

            public override void OnUpdate1(MyGridProgram parent)
            {
                if (interrupt)
                {
                    parent.Runtime.UpdateFrequency ^= UpdateFrequency.Update100;
                    parent.Runtime.UpdateFrequency ^= UpdateFrequency.Update10;
                    parent.Runtime.UpdateFrequency ^= UpdateFrequency.Update1;
                    interrupt = false;
                    return;
                }
                if (!targetVectorSet)
                    return;

                //update coordiantes and recalculate target rotations
                foreach (var a in antennaGroups)
                    if (a.id == pbConfig.groupId) {
                        a.UpdateCoords(coordinates);
                        a.Steer();
                    }

                //request position update
                if (targetAddress != 0)
                {
                    parent.IGC.SendUnicastMessage(targetAddress, unicastPing, "");
                }
            }
            public override void OnIGC(MyGridProgram parent, string callback)
            {
                while (bcListener.HasPendingMessage)
                {
                    var message = bcListener.AcceptMessage();
                    if (message.Tag.Equals(broadcastTag))
                    {
                        bool foundInList = false;
                        MyTuple<string, Vector3D> data = (MyTuple<string,Vector3D>)message.Data;
                        var source = message.Source;
                        for(int i = 0; i<stations.Count; i++)
                            if (stations[i].Item4.Equals(source))
                            {
                                var station = stations[i];
                                station.Item3 = 0;
                                station.Item2 = data.Item2;
                                station.Item1 = data.Item1;
                                stations[i] = station;
                                foundInList = true;
                                break;
                            }
                        if (!foundInList)
                            stations.Add(new MyTuple<string, Vector3D, int, long>(data.Item1, data.Item2, 0, source));
                    }
                }
                while (ucListener.HasPendingMessage)
                {
                    var message = ucListener.AcceptMessage();
                    parent.Echo(message.Tag);

                    if (message.Tag.Equals(unicastPing))
                        parent.IGC.SendUnicastMessage(message.Source, unicastPong, parent.Me.GetPosition());

                    else if (message.Tag.Equals(unicastPong))
                        coordinates = (Vector3D)message.Data;
                }
            }
        }
    }
}
