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
		public class AntennaConstruct
		{
			public readonly int id;

			private readonly List<IMyRadioAntenna> antennas;
			private readonly List<Rotor> rotors;
			private readonly List<Hinge> hinges;

			private Vector3D coordinates;

			public AntennaConstruct(int id, List<IMyRadioAntenna> antennas, List<Rotor> rotors, List<Hinge> hinges)
			{
				this.id = id;
				this.antennas = antennas;
				this.rotors = rotors;
				this.hinges = hinges;
			}

			public void UpdateCoords(Vector3D coordinates)
			{
				this.coordinates = coordinates;

				foreach (var r in rotors)
					r.UpdateCoords(coordinates);
				foreach (var h in hinges)
					h.UpdateCoords(coordinates);
			}

			public void Steer()
			{
				foreach (var r in rotors)
					r.Steer();
				foreach (var h in hinges)
					h.Steer();
			}

			public void PrintGroup(MyGridProgram parent)
			{
				parent.Echo("\nAntenna Group: " + id + "\n");
				parent.Echo("Pointing at: " + FormatVector(coordinates) + "\n");
				parent.Echo("Contains: " + "\n");
				parent.Echo("   Antennas: " + antennas.Count + "\n");
				parent.Echo("   Rotors: " + rotors.Count + "\n");
				parent.Echo("   Hinges: " + hinges.Count + "\n");
				foreach (var item in rotors)
					item.PrintMotor(parent);
				foreach (var item in hinges)
					item.PrintMotor(parent);
			}
		}
	}
}
