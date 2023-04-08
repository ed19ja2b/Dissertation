using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

public class KawasakiDiffusion : MonoBehaviour
{
		private System.Random random;
		public GameObject[,] cells;
		private int gridSize;

		// Boltzmann Constant as defined here - https://www.nist.gov/si-redefinition/meet-constants
		// taken from https://www.codeproject.com/Articles/11647/Special-Function-s-for-C
		public const double BOLTZMAN = 1.3807e-16;
		private const float temperature = 32.0f;//used in metropolis probability

		int [,] neighbour_positions = {{0, 1}, {1, 0}, {0, -1}, {-1, 0}};

		bool IsValidPosition(int x, int y){
				//if within boundaries of grid
				if (x < gridSize && x >= 0 && y < gridSize && y >= 0){
						return true;
				}
				return false;
		}

		bool IsWater(GameObject cell){
				if (cell.CompareTag("WaterCell")){
						return true;
				}
				return false;
		}

		GameObject ChooseRandomNeighbour(GameObject cell){
				int x = (int)cell.transform.position.x;
				int y = (int)cell.transform.position.y;
				//pick a random neighbour from the 4 Moore neighbours
				//if its not a valid choice, re-choose with different random neighbour
				foreach(int i in Enumerable.Range(0,4).OrderBy(x => random.Next())){
						int neighbour_x = x + neighbour_positions[i, 0];
						int neighbour_y = y + neighbour_positions[i, 1];
						if (IsValidPosition(neighbour_x, neighbour_y)){
								return cells[neighbour_x, neighbour_y];
						}
				}
				return null;//should never reach here
		}

		GameObject[] GetNeighboursAtPos(int x, int y){
			GameObject[] neighbours = new GameObject[4];
			for (int i = 0; i < 4; i++){
					// neighbour coordinates
					int n_x = x + neighbour_positions[i,0];
					int n_y = y + neighbour_positions[i,1];
					if (IsValidPosition(n_x, n_y) == true){
						neighbours[i] = cells[n_x, n_y];
					} else{
						neighbours[i] = null;
					}
			}
			return neighbours;
		}

		private GameObject[] SwapCells(GameObject cell1, GameObject cell2){
				if (cell1 == null || cell2 == null){
						Debug.Log("Swap attempted with a null cell");
						return null;
				}

				GameObject _cell1 = cell1;
				int cell1_x = (int)cell1.transform.position.x;
				int cell1_y = (int)cell1.transform.position.y;
				int cell2_x = (int)cell2.transform.position.x;
				int cell2_y = (int)cell2.transform.position.y;

				cell2.transform.position = new Vector3(cell1_x, cell1_y, 0);
				cell1.transform.position = new Vector3(cell2_x, cell2_y, 0);

				cells[cell1_x, cell1_y] = cell2;
				cells[cell2_x, cell2_y] = _cell1;
				GameObject[] swappedCells = {cells[cell1_x, cell1_y], 	cells[cell2_x, cell2_y]};
				return swappedCells;
		}

		//limiting to nearest neighbours
		private float CalculateHamiltonian(GameObject cell){
				float energy = 0f;
				float J_water_water = -1f;
				float J_water_land = 1f;

				int x = (int)cell.transform.position.x;
				int y = (int)cell.transform.position.y;
				GameObject[] neighbours = GetNeighboursAtPos(x, y);
				int c_i = 0;
				if(IsWater(cell))c_i = 1;
				foreach(GameObject _cell in neighbours){
						if (_cell != null){
								int c_j = 0;
								if(IsWater(_cell))c_j = 1;
								if(c_i != 0 || c_j != 0){
										//if water and land
										if(c_i == 1 && c_j == 1){
												energy += J_water_water;
										}
										else {
												energy += J_water_land;
										}
								}
						}
				}

				return energy;
		}

		private float ComputeMetropolisProbability(float energy_change){
				float p = 1.0f/temperature;
				if (energy_change <= 0){
						//Debug.Log("metropolis (1/t): " + p + " given energy_change of: " + energy_change);
						return p;
				}
				// p *= (float)(Math.Exp(-1 * (energy_change / (BOLTZMAN * temperature))));
				p = (float)random.NextDouble();
				//Debug.Log("metropolis: " + energy_change / (BOLTZMAN * temperature) + " given energy_change of: " + energy_change);
				return p;
		}

		private void DetermineEnergyChange(GameObject cell1, GameObject cell2){
				float initial_energy = CalculateHamiltonian(cell1) + CalculateHamiltonian(cell2);
				GameObject[] swappedCells = SwapCells(cell1, cell2);
				float new_energy = CalculateHamiltonian(swappedCells[0]) + CalculateHamiltonian(swappedCells[1]);
				float energy_change = (initial_energy - new_energy);

				//we accept change if energy falls, so if it rises instead, we enter this if statement
				if (energy_change >= 0){
						float p = ComputeMetropolisProbability(energy_change);
						float r = (float)random.NextDouble();
						if (r >= p){//we accept change if r < p, so we reject (swap back) if r >= p
								SwapCells(swappedCells[0], swappedCells[1]);
						}
				}
		}

		public GameObject[,] RunKawasakiDiffusion(int _gridSize, GameObject[,] _cells, int _num_runs){
				random = new System.Random();
				gridSize = _gridSize;
				cells = _cells;
				int num_runs = _num_runs;

				for (int i = 0; i < num_runs; i++){
					foreach(var pos in Enumerable.Range(0, gridSize).OrderBy(x => random.Next()).Zip(Enumerable.Range(0, gridSize).OrderBy(x => random.Next()), (x, y) => new { x, y})){
							GameObject cell = cells[(int)pos.x, (int)pos.y];
							GameObject neighbourCell = ChooseRandomNeighbour(cell);
							DetermineEnergyChange(cell, neighbourCell);
					}
				}

				return cells;
		}
}
