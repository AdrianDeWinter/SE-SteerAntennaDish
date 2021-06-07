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
		public class Rotor : Motor
		{
			public Rotor(IMyMotorStator rotor, int forwardAngle = 0, float traverseSpeed = 2F)
			{
				motor = rotor;
				this.forwardAngle = forwardAngle;
				speed = traverseSpeed;
			}

			public Rotor(BlockConfig config)
            {
				if(config.blockType != BlockType.Rotor)
					throw new Exception("Rotor was Initialized with a config a different block type");
				motor = (IMyMotorStator)config.block;
				speed = config.traverseSpeed;
				forwardAngle = config.normalAngle;
				groupId = config.groupId;
			}

			public override void UpdateCoords(Vector3D target)
			{
				this.target = target;

				motorCoords = motor.CubeGrid.GridIntegerToWorld(motor.Position);
				motorNormal = AbsoluteBlockUp(motor);

				if (forwardAngle == 90) {
					zeroDegree = AbsoluteBlockRight(motor);
					ninetyDegrees = AbsoluteBlockBackwards(motor); 
				}
				if (forwardAngle == 180) {
					zeroDegree = AbsoluteBlockForward(motor);
					ninetyDegrees = AbsoluteBlockRight(motor); 
				}
				if (forwardAngle == 270)
				{
					zeroDegree = AbsoluteBlockLeft(motor);
					ninetyDegrees = AbsoluteBlockForward(motor);
				}
				else
				{
					zeroDegree = AbsoluteBlockBackwards(motor);
					ninetyDegrees = AbsoluteBlockLeft(motor);
				}

				motorPlane = new PlaneD(motorCoords, motorNormal);
				projection = PathOnPlane(motorPlane, target, motorCoords);

				lowerLimit = PositiveAngle(motor.LowerLimitRad);
				upperLimit = PositiveAngle(motor.UpperLimitRad);

				currentPosition = motor.Angle;

				targetRotation = AngleBetweenVectors(projection, zeroDegree);

				if (AngleBetweenVectors(ninetyDegrees, projection) > (Math.PI / 2))
					targetRotation = 2 * Math.PI - targetRotation;

				//rotor at >180° and target between rotor position and 360°
				if (currentPosition >= Math.PI && targetRotation > currentPosition)
					clockwise = true;

				//rotor at >180° and target between rotor position and rotor - 180°
				else if (currentPosition >= Math.PI && targetRotation > currentPosition - Math.PI)
					clockwise = false;

				else if (currentPosition >= Math.PI)
					clockwise = true;

				//rotor at <180° and target between rotor position and 0°
				else if (currentPosition <= Math.PI && targetRotation < currentPosition)
					clockwise = false;

				//rotor at <180° and target between rotor position and rotor + 180°
				else if (currentPosition <= Math.PI && targetRotation < currentPosition + Math.PI)
					clockwise = true;

				else if (currentPosition <= Math.PI)
					clockwise = false;
			}
		}

		public class Hinge : Motor
		{
			public Hinge(IMyMotorStator hinge, float traverseSpeed = 2F, int forwardAngle = 0)
			{
				motor = hinge;
				speed = traverseSpeed;
				this.forwardAngle = forwardAngle;
			}

			public Hinge(BlockConfig config)
			{
				if (config.blockType != BlockType.Hinge)
					throw new Exception("Hinge was Initialized with a config for a different block type");

				motor = (IMyMotorStator)config.block;
				speed = config.traverseSpeed;
				minSpeed = config.minSpeed;
				forwardAngle = config.normalAngle;
				groupId = config.groupId;

				this.config = config;
			}

			public override void UpdateCoords(Vector3D target)
			{
				this.target = target;

				motorCoords = motor.CubeGrid.GridIntegerToWorld(motor.Position);
				motorNormal = AbsoluteBlockUp(motor);

				zeroDegree = AbsoluteBlockLeft(motor);
				ninetyDegrees = AbsoluteBlockForward(motor);

				motorPlane = new PlaneD(motorCoords, motorNormal);
				projection = PathOnPlane(motorPlane, target, motorCoords);

				lowerLimit = PositiveAngle(motor.LowerLimitRad);
				upperLimit = PositiveAngle(motor.UpperLimitRad);

				currentPosition = motor.Angle;

				targetRotation = AngleBetweenVectors(projection, zeroDegree);

				if (AngleBetweenVectors(ninetyDegrees, projection) > (Math.PI / 2))
					targetRotation *= -1;
				clockwise = targetRotation > currentPosition;
			}
		}

		public abstract class Motor
		{
			protected IMyMotorStator motor;

			public int groupId = 0;

			protected Vector3D motorCoords;
			protected Vector3D motorNormal;

			protected Vector3D zeroDegree;
			protected Vector3D ninetyDegrees;

			protected PlaneD motorPlane;
			protected Vector3D projection;

			protected double lowerLimit;
			protected double upperLimit;

			protected int forwardAngle;

			protected double currentPosition;
			protected double targetRotation;

			protected Vector3D target;

			protected float speed = 2F;
			protected float minSpeed = 0.1F;

			protected float allowedDeviation = 0.0005F;
			protected float speedReductionUpperBound = 0.1F;

			protected bool clockwise;

			public abstract void UpdateCoords(Vector3D target);

			public BlockConfig config;

			public void Steer()
			{
				double deviation = Math.Abs(currentPosition - targetRotation);

				float newSpeed;

				if (deviation < allowedDeviation)//required precision reached, stop
					newSpeed = 0F;
				else if (deviation < speedReductionUpperBound)//within speed reduction zone, proportional to deviation
					newSpeed = (float)deviation / speedReductionUpperBound * (speed-minSpeed) + minSpeed;
				else//full bore
					newSpeed = speed;

				if (!clockwise)//set direction
					newSpeed *= -1;

				motor.TargetVelocityRPM = newSpeed;
			}

			public void PrintMotor(MyGridProgram parent)
			{
				this.UpdateCoords(target);
				parent.Echo(motor.CustomName);
				//PrintBlockOrientation(motor, parent);
				parent.Echo("Projection: " + FormatVector(projection));
				parent.Echo("Current Speed (RPM): " + motor.TargetVelocityRPM);
				parent.Echo("Angle to rotate to: " + formatAngle(targetRotation));
				parent.Echo("Curent Rotation Angle: " + formatAngle(motor.Angle));
				parent.Echo("0°: " + FormatVector(zeroDegree));
				parent.Echo("90°: " + FormatVector(ninetyDegrees));
				parent.Echo("\n");
			}
		}

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
