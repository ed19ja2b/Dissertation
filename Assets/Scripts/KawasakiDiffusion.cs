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
		// Boltzmann Constant as defined here - https://www.nist.gov/si-redefinition/meet-constants
		// taken from https://www.codeproject.com/Articles/11647/Special-Function-s-for-C
		public const double BOLTZMAN = 1.3807e-16;
		int num_runs = 50;


		// adding these positions to a coordinate as a vector will give position of north, east, south and west neighbours respectively
		Vector2[] neighbour_positions = {new Vector2(0, 1), new Vector2(1, 0), new Vector2(0, -1), new Vector2(-1, 0)};

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
				int pos_x = (int)pos.x;
				int pos_y = (int)pos.y;
				//if within boundaries of grid
				if (pos_x < gridSize && pos_x >= 0 && pos_y < gridSize && pos_y >= 0){
						return 1;
				}
				return 0;
		}

		//returns a random moore neighbour of 'cell' as a GameObject
		GameObject PickRandomNeighbour(GameObject cell){
				int cell_x = (int)cell.transform.position.x;
				int cell_y = (int)cell.transform.position.y;
				int neighbour_x, neighbour_y;
				random = new System.Random();
				//iterate through neighbours in random order - https://stackoverflow.com/questions/254844/random-array-using-linq-and-c-sharp/254861#254861
				foreach(int i in Enumerable.Range(0,4).OrderBy(x => random.Next())){
						//neighbour_pos = cell_pos + neighbour_positions[i];
						neighbour_x = cell_x + (int)neighbour_positions[i].x;
						neighbour_y = cell_y + (int)neighbour_positions[i].y;
						Vector2 neighbour_pos = new Vector2(neighbour_x, neighbour_y);

						if (IsValidPosition(neighbour_pos) == 1){
							if (neighbour_x != cell_x && neighbour_y != cell_y){//if its not part of the moore neighbourhood
									Debug.Log("PickRandomNeighbour() error: Neighbour not part of moore neighbourhood");
							} else {
								GameObject neighbourCell = cells[neighbour_x, neighbour_y];
								Debug.Log("PickRandomNeighbour(): iteration " + i + " cell pos: " + cell_x + ", " + cell_y + " neighbour: " + neighbour_pos.x + ", " + neighbour_pos.y);
								Debug.Log("PickRandomNeighbour() test: neighbourCell position: " + neighbourCell.transform.position.x + ", " + neighbourCell.transform.position.y);
								return neighbourCell;
							}

						}
				}
				Debug.Log("Error in PickRandomNeighbour() - unexpectedly found no valid neighbour position");
				return null;
		}


		//https://www.researchgate.net/profile/Ken-Hawick/publication/287274420_Modelling_Flood_Incursion_and_Coastal_Erosion_using_Cellular_Automata_Simulations/links/5ed7d6e845851529452b0e83/Modelling-Flood-Incursion-and-Coastal-Erosion-using-Cellular-Automata-Simulations.pdf
		public float CalculateHamiltonian(GameObject cell_i){
				Vector2 pos = new Vector2(cell_i.transform.position.x, cell_i.transform.position.y);
				Vector2 neighbour_pos = neighbour_positions[0] + pos;
				float energy = 0f;
				float J_water_water = 3f;//water-water interactions give positive V coupling constant as in Hawick's Kawasaki Model
				float J_water_land = -1f;//water-land interactions give negative V coupling constant

				int c_i = IsWaterCell(cell_i);
				int c_j = 0;

				//iterate through all moore neighbours of cell
				for (int i = 0; i < 4; i++){
						neighbour_pos = neighbour_positions[i] + pos;
						if (IsValidPosition(neighbour_pos) == 1){
								GameObject cell_j = cells[(int)neighbour_pos.x, (int)neighbour_pos.y];
								c_j = IsWaterCell(cell_j);
								if (c_i != 0 && c_j != 0){//if neither are land cells
										if(c_i == 1 && c_j == 1){//if both water cells
												energy += J_water_water;
										} else {//else if one is water and one is land (only other case here)
												energy += J_water_land;
										}
								}
						}
				}
				return energy;
		}

		private GameObject[] SwapCells(GameObject cell1, GameObject cell2){

				GameObject tempCell = cell1;
				Vector2 cell1_pos = new Vector2(cell1.transform.position.x, cell1.transform.position.y);
				Vector2 cell2_pos = new Vector2(cell2.transform.position.x, cell2.transform.position.y);
				if (cell1_pos == cell2_pos){
						Debug.Log("Swap attempted with the same cell, position: " + cell1_pos.x + ", " + cell1_pos.y);
						GameObject[] original_cells = {cell1, cell2};
						return original_cells;
				}

				if ((IsWaterCell(cell1) + IsWaterCell(cell2)) == 1){
					Color color = new Color(1, 0.26f, 0.51f);
					cell2.GetComponent<SpriteRenderer>().color = color;
					tempCell.GetComponent<SpriteRenderer>().color = color;
					Debug.Log("Swap made at pos: " + (int)cell1_pos.x + ", " + (int)cell1_pos.y + " with pos: " + (int)cell2_pos.x + ", " + (int)cell2_pos.y);
				}
				cells[(int)cell1_pos.x, (int)cell1_pos.y] = cell2;
				cells[(int)cell2_pos.x, (int)cell2_pos.y] = tempCell;

				GameObject[] swappedCells = {cells[(int)cell1_pos.x, (int)cell1_pos.y], cells[(int)cell2_pos.x, (int)cell2_pos.y]};
				return swappedCells;
		}

		//calculate energy change if we swap the cell out for its neighbouring cell, and make any changes
		public float ComputeEnergyChange(GameObject cell, GameObject neighbourCell){
				float initial_energy = CalculateHamiltonian(cell) + CalculateHamiltonian(neighbourCell);

				Vector2 cell_pos = new Vector2(cell.transform.position.x, cell.transform.position.y);
				Vector2 neighbour_pos = new Vector2(neighbourCell.transform.position.x, neighbourCell.transform.position.y);

				if ((int)cell_pos.x == (int)neighbour_pos.x && (int)cell_pos.y == (int)neighbour_pos.y){
						Debug.Log("Energy change calculation attempted between same cell, position: " + cell_pos.x + ", " + cell_pos.y);
						return 0;
				}


				//temporarily swap the cells
				GameObject[] swappedCells = SwapCells(cell, neighbourCell);
				GameObject _cell = swappedCells[0];
				GameObject _neighbourCell = swappedCells[1];

				float new_energy = CalculateHamiltonian(_cell) + CalculateHamiltonian(_neighbourCell);
				float energy_change = new_energy - initial_energy;

				if (energy_change < 0){
					Debug.Log("Accepted cell swap for " + cell_pos.x + ", " + cell_pos.y + " & " + neighbour_pos.x + ", " + neighbour_pos.y);
					return energy_change;
				} else {
					float r = (float)random.NextDouble();
					float p = (float)random.NextDouble();
					//implement metropolis_probability here
					if (r < p){
						Debug.Log("Accepted cell swap for " + cell_pos.x + ", " + cell_pos.y + " & " + neighbour_pos.x + ", " + neighbour_pos.y);
						return energy_change;
					} else {
						Debug.Log("r < p: cell swap rejected, swapping back");
						swappedCells = SwapCells(_cell, _neighbourCell);
						return 0;
					}
				}
		}

		//one single pass of the Kawasaki diffusion model algorithm
		private float RunSimulation(){
				random = new System.Random();
				float total_energy_change = 0f;
				// random iteration in for loop - https://stackoverflow.com/questions/13457917/random-iteration-in-for-loop
				// enumerate over two variables - https://stackoverflow.com/questions/14516537/can-we-use-multiple-variables-in-foreach
				// loop over all cells in random order - cells inputted from InstantiateGridCells.cs InstantiateGrid()
				foreach(var pos in Enumerable.Range(0, gridSize).OrderBy(x => random.Next()).Zip(Enumerable.Range(0, gridSize).OrderBy(x => random.Next()), (x, y) => new { x, y})){
						GameObject cell = cells[(int)pos.x, (int)pos.y];
						GameObject neighbourCell = PickRandomNeighbour(cell);

						float energy_change = ComputeEnergyChange(cell, neighbourCell);
						total_energy_change += energy_change;
				}
				return total_energy_change;
		}

		//https://www.researchgate.net/profile/Ken-Hawick/publication/287274420_Modelling_Flood_Incursion_and_Coastal_Erosion_using_Cellular_Automata_Simulations/links/5ed7d6e845851529452b0e83/Modelling-Flood-Incursion-and-Coastal-Erosion-using-Cellular-Automata-Simulations.pdf
		public GameObject[,] RunKawasakiDiffusion(int _gridSize, GameObject[,] _cells){
				random = new System.Random();

				float total_energy_change = 0f;
				cells = _cells;
				gridSize = _gridSize;

				for (int r = 0; r < num_runs; r++){
					float energy_change = RunSimulation();
					total_energy_change += energy_change;
				}

				float avg_energy_change = total_energy_change / num_runs;
				Debug.Log("Average energy change over " + num_runs + " runs: " + avg_energy_change);
				return cells;
		}
}
