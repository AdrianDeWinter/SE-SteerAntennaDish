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
	}
}
