using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class WaterCell : MonoBehaviour
{
		private SpriteRenderer spriteRenderer;
		public Vector3 position;

		const float maxDepth = 30.0f;//difficult to find information on average water depth of coastal waters
		const float drag_coef = 0.03;//mostly arbitrary, hard to find a concrete answer for what's reasonable
		const int unitToMeters = 100;//arbitrary - each unity unit is 100m
		const float viscosity = 0.0013;//can change later - find and reference source for this

		public float depth;
		public float velocity_x;
		public float velocity_y;
		//bernoulli hydraulic head - can use to model how surface roughness affects water flow
		public double bernoulli_head;

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

		public void SetDepth(float _depth){
			 depth = _depth;
			 // float weight = 1 - (depth/maxDepth);
			 // SetCellColor(weight);
		}

		public void SetVelocity(float current_strength, float current_direction, int cellSize){
				cellSize = cellSize * unitToMeters;//converting into meters

				float turbulence = UnityEngine.Random.Range(0.95f, 1.05f); //will multiply velocity by this (+-5%)
				velocity_x = current_strength * Mathf.Cos(current_direction * Mathf.PI * 2) * turbulence;
				velocity_y = current_strength * Mathf.Sin(current_direction * Mathf.PI * 2) * turbulence;

				velocity_magnitude = Math.Sqrt(Math.Pow(velocity_x, 2) + Math.Pow(velocity_y, 2));

				//drag force
				float dragForceMagnitude = drag_coef * Math.Pow(velocity_magnitude, 2);
				Vector2 dragForce = -dragForceMagnitude * velocity_magnitude.normalized;

				Vector2 dragAcceleration = dragForce / (depth * viscosity * cellSize);//drag force divided by the area of the cell and the viscosity of water

				velocity_x += dragAcceleration.x;
				velocity_y += dragAcceleration.y;

				float weight = 1 - (float)(velocity_magnitude / 2.5f);
				SetCellColor(weight);
		}

		// public void CalculateBernoulli(){
		// 		//https://www.sciencedirect.com/science/article/pii/S0022169422010198 - 2.2 (1)
		// 		//taking bed elevation to be equal to water depth - not concerned about tidal heights, etc.
		// 		bernoulli_head = (2 * depth) + (Math.Pow(velocity_x,2) + Math.Pow(velocity_y,2)) / (2*g);
		// }
}
