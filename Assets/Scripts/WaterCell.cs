using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class WaterCell : MonoBehaviour
{
		private SpriteRenderer spriteRenderer;
		public Vector3 position;

		// const float maxDepth = 30.0f;

		// public float depth;
		// public double velocity_x;
		// public double velocity_y;

		void Start(){
				position = transform.position;
		}

		public void SetCellColor(float weight){
			// randomised
			// Color color = UnityEngine.Random.ColorHSV(241f / 360f, 260f / 360f, 1f, 1f, 0.6f, 1f);
			// Calculating colour based on the depth value
      Color color = Color.Lerp(new Color(0, 0.26f, 0.51f), new Color(0, 0.5f, 1), weight);

			spriteRenderer = GetComponent<SpriteRenderer>();
			spriteRenderer.color = color;
		}

		// public void SetDepth(float _depth){
		// 	 depth = _depth;
		// 	 // float weight = 1 - (depth/maxDepth);
		// 	 // SetCellColor(weight);
		// }
		//
		// public void SetVelocity(Vector2 velocity){
		// 		velocity_x = velocity[0];
		// 		velocity_y = velocity[1];
		// 		float weight = 1 - (float)(Math.Sqrt(Math.Pow(velocity_x, 2) + Math.Pow(velocity_y, 2)) / 2.5f);
		// 		Console.WriteLine("Velocities: {0}, {1}", velocity_x, velocity_y);
		// 		SetCellColor(weight);
		// }
}
