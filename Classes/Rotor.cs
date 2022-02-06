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
					throw new Exception("Rotor was Initialized with a config for a different block type");
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
	}
}
