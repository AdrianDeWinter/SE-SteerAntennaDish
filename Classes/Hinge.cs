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
	}
}
