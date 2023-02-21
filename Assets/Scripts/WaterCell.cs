using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class WaterCell : MonoBehaviour
{
		private SpriteRenderer spriteRenderer;
		public Vector3 position;

		const double g = 9.81;

		public double bed_elevation;
		public double depth;
		public double velocity_x;
		public double velocity_y;
		//bernoulli hydraulic head - can use to model how surface roughness affects water flow
		public double bernoulli_head;

		void Start(){
				position = transform.position;
		}

		public void SetCellColor(Color color){
			spriteRenderer = GetComponent<SpriteRenderer>();
			spriteRenderer.color = color;
		}

		public void CalculateBernoulli(){
				//https://www.sciencedirect.com/science/article/pii/S0022169422010198 - 2.2 (1)
				bernoulli_head = bed_elevation + depth + (Math.Pow(velocity_x,2) + Math.Pow(velocity_y,2)) / (2*g);
		}
}
