using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class WaterCell : MonoBehaviour
{
		private SpriteRenderer spriteRenderer;
		public Vector3 position;

		const double g = 9.81;
		const float maxDepth = 30.0f;

		public float depth;
		public double velocity_x;
		public double velocity_y;
		//bernoulli hydraulic head - can use to model how surface roughness affects water flow
		public double bernoulli_head;

		void Start(){
				position = transform.position;
		}

		public void SetCellColor(){
			// randomised
			// Color color = UnityEngine.Random.ColorHSV(241f / 360f, 260f / 360f, 1f, 1f, 0.6f, 1f);
			// Calculating colour based on the depth value
      Color color = Color.Lerp(new Color(0, 0.26f, 0.51f), new Color(0, 0.5f, 1), 1 - (depth/maxDepth));
			Debug.Log("Color value: " + color.ToString() + " depth/maxdepth: " + (depth/maxDepth).ToString());

			spriteRenderer = GetComponent<SpriteRenderer>();
			spriteRenderer.color = color;
		}

		public void SetDepth(float _depth){
			 depth = _depth;
			 SetCellColor();
		}

		public void CalculateBernoulli(){
				//https://www.sciencedirect.com/science/article/pii/S0022169422010198 - 2.2 (1)
				//taking bed elevation to be equal to water depth - not concerned about tidal heights, etc.
				bernoulli_head = (2 * depth) + (Math.Pow(velocity_x,2) + Math.Pow(velocity_y,2)) / (2*g);
		}
}
