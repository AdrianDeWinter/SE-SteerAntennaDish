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
		public static List<MyTuple<string, Vector3D, int, long>> CleanStationsList(List<MyTuple<string, Vector3D, int, long>> list)
		{
			List<MyTuple<string, Vector3D, int, long>> toBeRemoved = new List<MyTuple<string, Vector3D, int, long>>();
			for (int i = 0; i < list.Count; i++)
				if (list[i].Item3 >= 5)
					toBeRemoved.Add(list[i]);
				else
				{
					var station = list[i];
					station.Item3++;
					list[i] = station;
				}
			return list.Except(toBeRemoved).ToList();
		}

		public static List<AntennaConstruct> FindAntennasAndDrives(MyGridProgram parent)
		{
			MyIni ini = new MyIni();
			List<Rotor> myRotors = new List<Rotor>();
			List<Hinge> myHinges = new List<Hinge>();
			List<AntennaConstruct> constructs = new List<AntennaConstruct>();
			//get all antennas configured for this script
			List<IMyRadioAntenna> antennas = new List<IMyRadioAntenna>();
			parent.GridTerminalSystem.GetBlocksOfType<IMyRadioAntenna>(antennas, antenna => MyIni.HasSection(antenna.CustomData, "AntennaSteer"));

			//get all motors configured for this script
			List<IMyMotorStator> motors = new List<IMyMotorStator>();
			parent.GridTerminalSystem.GetBlocksOfType<IMyMotorStator>(motors, motor => MyIni.HasSection(motor.CustomData, "AntennaSteer"));

			//iterate over all antennas configured for this script
			//get one, grab all antennas, rotors and hinges bearing the same antennaGroup id
			//build AntennaConstruct
			//repeat

			motors.ForEach(motor => {
				var conf = ParseBlockConfig(motor);
				if (conf.blockType == BlockType.Hinge)
					myHinges.Add(new Hinge(conf));
				else if (conf.blockType == BlockType.Rotor)
					myRotors.Add(new Rotor(conf));
				else
					parent.Echo(conf.ToString());
				});

			while (antennas.Count != 0)
			{
				//get an antenna, remove it from the pool
				IMyRadioAntenna antenna = antennas.First();
				antennas.Remove(antenna);

				//parse its custom data
				ini.TryParse(antenna.CustomData, "AntennaSteer");
				int id = ini.Get("AntennaSteer", "antennaGroup").ToInt32();

				//get other antennas with same id, remove them from the pool
				List<IMyRadioAntenna> antenna_group = antennas.FindAll(
					ant => ParseBlockConfig(ant).groupId == id
					);
				antennas = antennas.Except(antenna_group).ToList();
				antenna_group.Add(antenna);
				//construct final object
				constructs.Add(
					new AntennaConstruct(
						id,
						antenna_group,
						myRotors.FindAll(rotor => rotor.groupId == id),
						myHinges.FindAll(hinge => hinge.groupId == id)
						)
					);
			}
			return constructs;
		}

		public static BlockConfig ParseBlockConfig(IMyTerminalBlock block)
        {
            BlockConfig config = new BlockConfig();
            ParseBlockConfig(config, block);
			return config;
		}

		public static void ParseBlockConfig(BlockConfig config, IMyTerminalBlock block = null)
		{
			if (block == null)
				block = config.block;

			MyIni ini = new MyIni();
			ini.TryParse(block.CustomData);

			config.block = block;

			config.groupId = ini.Get(BlockConfig.configTag, BlockConfig.groupString).ToInt32();
			config.traverseSpeed = ini.Get(BlockConfig.configTag, BlockConfig.speedString).ToInt32();
			if (config.traverseSpeed == 0)
				config.traverseSpeed = 2F;
			config.normalAngle = ini.Get(BlockConfig.configTag, BlockConfig.angleString).ToInt32();
			config.enableBroadcast = ini.Get(BlockConfig.configTag, BlockConfig.broadcastString).ToBoolean();
			config.target = ini.Get(BlockConfig.configTag, BlockConfig.targetString).ToString();

			switch (ini.Get(BlockConfig.configTag, BlockConfig.blockTypeString).ToString())
			{
				case "Antenna":
				case "antenna":
					config.blockType = BlockType.Antenna;
					break;
				case "Hinge":
				case "hinge":
					config.blockType = BlockType.Hinge;
					break;
				case "Rotor":
				case "rotor":
					config.blockType = BlockType.Rotor;
					break;
				case "PB":
				case "pb":
					config.blockType = BlockType.PB;
					break;
			}
			return;
		}
	}
}
