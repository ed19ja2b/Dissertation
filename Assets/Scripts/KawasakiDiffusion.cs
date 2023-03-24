using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class KawasakiDiffusion : MonoBehaviour
{
		private System.Random random;
		public GameObject[,] cells;
		private int gridSize;
		public float temperature = 2.0f;//used in metropolis probability
		public int num_runs = 5;

		// adding these positions to a coordinate as a vector will give position of north, east, south and west neighbours respectively
		Vector2[] neighbour_directions = {new Vector2(0, 1), new Vector2(1, 0), new Vector2(0, -1), new Vector2(-1, 0)};

		// check if the cell is a water cell - https://docs.unity3d.com/ScriptReference/GameObject.TryGetComponent.html
		int IsWaterCell(GameObject cell){
			if (cell == null){
				return -1;//failure (shouldn't occur)
			}
			else if (cell.TryGetComponent<WaterCell>(out _)){//out isn't needed, only returning -1, 0 or 1
				return 1;//is water cell
			}
			return 0;//is rock cell
		}

		int IsValidPosition(Vector2 pos){
			if (pos.x < gridSize - 1 && pos.x >= 0 && pos.y < gridSize && pos.y >= 0){
				return 1;//valid position - does not fall outside boundary of grid
			}
			return 0;
		}

		//returns neighbour as a GameObject
		GameObject PickRandomNeighbour(GameObject cell){

			// loop over neighbours in random order - random iteration in for loop - https://stackoverflow.com/questions/13457917/random-iteration-in-for-loop
			foreach(int rand_neighbour in Enumerable.Range(0,4).OrderBy(x => random.Next())){	//random int 0,1,2 or 3 for N,E,S and W
				Vector2 neighbour_position = new Vector2(neighbour_directions[rand_neighbour].x, neighbour_directions[rand_neighbour].y);

				if(IsValidPosition(neighbour_position) == 1){//if the chosen neighbour is in a valid position
					return cells[(int)neighbour_position.x, (int)neighbour_position.y];//return the neighbouring cell
				}
				// otherwise, re-iterates with different random neighbour index
			}
			return null;// should never reach here
		}


		//https://www.researchgate.net/profile/Ken-Hawick/publication/287274420_Modelling_Flood_Incursion_and_Coastal_Erosion_using_Cellular_Automata_Simulations/links/5ed7d6e845851529452b0e83/Modelling-Flood-Incursion-and-Coastal-Erosion-using-Cellular-Automata-Simulations.pdf
		public float CalculateHamiltonian(GameObject cell_i){
				//cell's position
				Vector2 pos = new Vector2(cell_i.transform.position.x, cell_i.transform.position.y);
				float energy = 0f;
				float J_water_water = 1f;//water-water interactions give positive V coupling constant as in Hawick's Kawasaki Model
				float J_water_land = -1f;//water-land interactions give negative V coupling constant

				//determine first neighbour's position (north)
				Vector2 neighbour_position = new Vector2(neighbour_directions[0].x + pos.x, neighbour_directions[0].y + pos.y);
				int c_i = IsWaterCell(cell_i);
				int c_j = 0;
				//iterate through all the moore neighbours of the cell at position 'pos'
				//starting from i == 1, considering we set neighbour_position to the north neighbour already (i == 0)
				for (int i = 1; i < 4; i++){
						if (IsValidPosition(neighbour_position) == 1){
							c_j = IsWaterCell(cells[(int)neighbour_position.x,(int)neighbour_position.y]);
							//Debug.Log("c_i: " + c_i + ", c_j: " + c_j);
							if (c_i != 0 && c_j != 0){//if both are land cells, there is nothing to do
								if (c_i == 1 && c_j == 1){//if both the current cell and its neighbour are water cells
									// modify energy of system based on water-water interactions -
									//Debug.Log("energy: " + energy + " + " + J_water_water + " (water-water interaction)");
									energy += J_water_water;

								} else if ((c_i == 1 && c_j == 0) || (c_i == 0 && c_j == 1)){//if one is land and one is water
									//Debug.Log("energy: " + energy + " + " + J_water_land + " (water-land interaction)");
									energy += J_water_land;
								}
							}
						}
						neighbour_position = new Vector2(neighbour_directions[i].x + pos.x, neighbour_directions[i].y + pos.y);
				}
				//Debug.Log("hamiltonian energy: " + energy);
				return energy;
		}

		private void SwapCells(GameObject cell1, GameObject cell2){
			// swap two cells
			GameObject tempCell = cell1;
			Vector2 cell1_pos = new Vector2(cell1.transform.position.x, cell1.transform.position.y);
			Vector2 cell2_pos = new Vector2(cell2.transform.position.x, cell2.transform.position.y);
			cells[(int)cell1_pos.x, (int)cell1_pos.y] = cell2;
			cells[(int)cell2_pos.x, (int)cell2_pos.y] = tempCell;
		}

		//calculate energy change if we swap the cell out for its neighbouring cell, and make any changes
		public float ComputeEnergyChange(GameObject cell, GameObject neighbourCell){
			//calculate initial energy
			float initial_energy = CalculateHamiltonian(cell) + CalculateHamiltonian(neighbourCell);
			// store cell positions (more intuitive to understand code)
			Vector2 cell_pos = new Vector2(cell.transform.position.x, cell.transform.position.y);
			Vector2 neighbour_pos = new Vector2(neighbourCell.transform.position.x, neighbourCell.transform.position.y);

			//temporarily swap the cells
			SwapCells(cell, neighbourCell);

			//calculate the energy once the cells are swapped
			float new_energy = CalculateHamiltonian(cells[(int)cell_pos.x, (int)cell_pos.y]) + CalculateHamiltonian(cells[(int)neighbour_pos.x, (int)neighbour_pos.y]);
			float energy_change = new_energy - initial_energy;
			//Debug.Log("Energy change: " + energy_change);
			if (energy_change <= 0){// 'if energy falls, accept change and do exchange' - Hawick's Algorithm 1
					//Debug.Log("Energy change: " + energy_change + ", swap made");
					return energy_change;
			}

			// otherwise, compute Metropolis probability, accept change if less than random probability
			float r = (float)random.NextDouble();
			//not using Boltzmann constant as mentioned in Hawick's paper - temperature serves as control variable instead
			float metropolis_probability = Mathf.Exp(-energy_change / temperature);

			if (r < metropolis_probability){
				//Debug.Log("Energy change: " + energy_change + ", swap made");
				return energy_change;
			} else{
				// otherwise, we reject the change and swap cells back to original order
				SwapCells(cell, neighbourCell);
				//Debug.Log("Energy change: " + energy_change + ", no swap");
				return energy_change;
			}
		}

		//one single pass of the Kawasaki diffusion model algorithm
		private float RunSimulation(){
			float total_energy_change = 0f;

			// random iteration in for loop - https://stackoverflow.com/questions/13457917/random-iteration-in-for-loop
			// enumerate over two variables - https://stackoverflow.com/questions/14516537/can-we-use-multiple-variables-in-foreach
			// loop over all cells in random order - cells inputted from InstantiateGridCells.cs InstantiateGrid()
			foreach (var pos in Enumerable.Range(0, gridSize).OrderBy(x => random.Next()).Zip(Enumerable.Range(0, gridSize).OrderBy(x => random.Next()), (x, y) => new { x, y })){
				GameObject cell = cells[pos.x,pos.y];
				//pick random neighbouring ('site j') - derived from Hawick's algorithm 1 'Kawasaki Diffusion Model Algorithm' pseudocode
				GameObject neighbourCell = PickRandomNeighbour(cell);
				float energy_change = ComputeEnergyChange(cell, neighbourCell);//compute energy exchange
				total_energy_change += energy_change;
			}
			return total_energy_change;
		}

		//https://www.researchgate.net/profile/Ken-Hawick/publication/287274420_Modelling_Flood_Incursion_and_Coastal_Erosion_using_Cellular_Automata_Simulations/links/5ed7d6e845851529452b0e83/Modelling-Flood-Incursion-and-Coastal-Erosion-using-Cellular-Automata-Simulations.pdf
		public GameObject[,] RunKawasakiDiffusion(int _gridSize, GameObject[,] _cells){
			float total_energy_change = 0f;

			gridSize = _gridSize;
			random = new System.Random();
			cells = _cells;

			for (int r = 0; r < num_runs; r++){
					float energy_change = RunSimulation();
					total_energy_change += energy_change;
			}

			float avg_energy_change = total_energy_change / num_runs;
			Debug.Log("Average energy change over " + num_runs + " runs: " + avg_energy_change);
			return cells;
		}
}
