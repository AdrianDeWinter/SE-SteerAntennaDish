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

namespace IngameScript
{
	partial class Program
	{
		public enum BlockType { Antenna, Rotor, Hinge, PB };

		public class BlockConfig
		{
			private static readonly MyIni ini = new MyIni();

			public int groupId = 0;
			public BlockType blockType = BlockType.Antenna;
			public IMyTerminalBlock block = null;
			public float traverseSpeed = 2F;
			public float minSpeed = 0.1F;
			public int normalAngle = 0;
			public bool enableBroadcast = true;
			public string target="";
				
			public static string configTag = "AntennaSteer";
			public static string blockTypeString = "blockType";
			public static string groupString = "antennaGroup";
			public static string speedString = "traverseSpeed";
			public static string minSpeedString = "minSpeed";
			public static string angleString = "normalAngle";
			public static string targetString = "trackingTarget";
			public static string broadcastString = "enableBroadcast";
			public void Update()
			{
				if (block == null)
					throw new Exception("Attempted to call Update() on a BlockConfig that does not have an IMyTerminalBlock assigned to it");
				ParseBlockConfig(this);
			}

			public void WriteConfigTemplateAntenna()
			{
				ini.TryParse(block.CustomData);

				string templateString = "[" + configTag + "]\n";
				templateString += groupString + "=" + groupId + "\n";
				templateString += blockTypeString + "=" + BlockType.Antenna.ToString() + "\n";

				block.CustomData = templateString;
			}

			public void WriteConfigTemplateHinge()
			{
				ini.TryParse(block.CustomData);

				string templateString = "[" + configTag + "]\n";
				templateString += groupString + "=" + groupId + "\n";
				templateString += blockTypeString + "=" + BlockType.Hinge.ToString() + "\n";
				templateString += angleString + "=" + normalAngle + "\n";
				templateString += speedString + "=" + traverseSpeed + "\n";
				templateString += minSpeedString + "=" + minSpeed + "\n";

				block.CustomData = templateString;
			}
			public void WriteConfigTemplateRotor()
			{
				ini.TryParse(block.CustomData);

				string templateString = "[" + configTag + "]\n";
				templateString += groupString + "=" + groupId + "\n";
				templateString += blockTypeString + "=" + BlockType.Rotor.ToString() + "\n";
				templateString += angleString + "=" + normalAngle + "\n";
				templateString += speedString + "=" + traverseSpeed + "\n";
				templateString += minSpeedString + "=" + minSpeed + "\n";

				block.CustomData = templateString;
			}
			public void WriteConfigTemplatePB()
			{
				ini.TryParse(block.CustomData);

				string templateString = "[" + configTag + "]\n";
				templateString += groupString + "=" + groupId + "\n";
				templateString += blockTypeString + "=" + BlockType.PB.ToString() + "\n";
				templateString += targetString + "=" + target + "\n";
				templateString += broadcastString + "=" + enableBroadcast.ToString() + "\n";

				block.CustomData = templateString;
			}

			public override string ToString()
			{
				string id = "   Group ID = " + groupId;
				string type = "   Block Type = " + blockType.ToString();
				string blockName = "   Block Name : " + block.CustomName;
				string speed = "   Set Speed = " + traverseSpeed;
				string angle = "   Configured Normal Angle : " + normalAngle + "°";
				return "BlockConfig:" + "\n" + id + "\n" + type + "\n" + blockName + "\n" + speed + "\n" + angle + "\n";
			}
		}
	}
}
