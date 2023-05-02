using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

// Kawasaki site exchange diffusion model described in report (section 3.1.3)
// attached to the object RunSimulation
public class KawasakiDiffusion : MonoBehaviour
{
    private System.Random random;// used in generating random probability r in metropolis probability calculation
    public GameObject[,] cells;// initially inputted from RunSimulation.cs - returned once model terminates
    private int gridSize;// width 'w' - inputted from RunSimulation.cs

	// specifying constants
    // Boltzmann Constant as defined here - https://www.nist.gov/si-redefinition/meet-constants
    // taken from https://www.codeproject.com/Articles/11647/Special-Function-s-for-C
	const double BOLTZMANN = 1.380649e-23;
	// temperature of system - used in metropolis probability
	// mostly arbitrary choice, although was adjusted to modify diffusion rates to match Hawick's results
	// (figure 2 https://www.researchgate.net/publication/287274420_Modelling_Flood_Incursion_and_Coastal_Erosion_using_Cellular_Automata_Simulations)
    private const float temperature = 14.0f;
	private const float tau = 0.574f;// scaling parameter for metropolis probability calculation
	// relative positions of north, east, south, west neighbours given some coordinate
    private int[,] neighbour_positions = { { 0, 1 }, { 1, 0 }, { 0, -1 }, { -1, 0 } };

		// returns true if the position is within the boundaries of the grid
    private bool IsValidPosition(int x, int y){
        if (x < gridSize && x >= 0 && y < gridSize && y >= 0)
        {
            return true;
        }
        return false;
    }

		// returns 1 if the inputted cell is water, 0 otherwise
		private int IsWater(GameObject cell){
			if(cell.CompareTag("WaterCell")){
				return 1;
			}
			return 0;
		}

		// chooses a random neighbour of the inputted cell
		GameObject ChooseRandomNeighbour(GameObject cell)
    {
        int x = (int)cell.transform.position.x;
        int y = (int)cell.transform.position.y;
        // pick a random neighbour from the 4 Moore neighbours by choosing randomly from 0, 1, 2, 3 and addressing neighbour_positions with this index
        foreach (int i in Enumerable.Range(0, 4).OrderBy(x => random.Next()))
        {
            int neighbour_x = x + neighbour_positions[i, 0];
            int neighbour_y = y + neighbour_positions[i, 1];
            if (IsValidPosition(neighbour_x, neighbour_y))
            {//if the selected random neighbour is within the boundaries of the grid
                return cells[neighbour_x, neighbour_y];// return the neighbouring cell
            }// if its not a valid choice, re-iterate and re-choose with different random neighbour
        }
        return null; // should never reach here
    }

		// returns all neighbours of the inputted cell
		GameObject[] GetNeighbours(GameObject cell)
		{
			GameObject[] neighbours = new GameObject[4];
			int x = (int)Math.Round(cell.transform.position.x);
			int y = (int)Math.Round(cell.transform.position.y);
			// for all 4 possible neighbours
			for (int i = 0; i < 4; i++)
			{
				int n_x = x + neighbour_positions[i, 0];
				int n_y = y + neighbour_positions[i, 1];
				// if the neighbour is within the boundaries of the grid
				if (IsValidPosition(n_x, n_y) == true)
				{
						neighbours[i] = cells[n_x, n_y];
				}	else// otherwise, there is no neighbour here and so we set this to null
				{
						neighbours[i] = null;
				}
			}
			return neighbours;
		}

		// hamiltonian energy calculation determined by site interactions at a given cell - based on Hawick's equation 1 https://www.researchgate.net/publication/287274420_Modelling_Flood_Incursion_and_Coastal_Erosion_using_Cellular_Automata_Simulations
		// algorithm 2.3 in the report
		private float CalculateHamiltonian(GameObject cell_i){
			int c_i = IsWater(cell_i);
			GameObject[] neighbours = GetNeighbours(cell_i);// get all neighbours of the cell

			float H = 0;
			float V_WW = 1.0f;
			foreach(GameObject cell_j in neighbours){// for each potential neighbour of cell_i
				if(cell_j!=null){// if the neighbour exists
					int c_j = IsWater(cell_j);
					if(c_i == 1 && c_j == 1){// if both cell_i and this neighbour are water sites
						H += V_WW;// increase the energy of the system
					} // otherwise we disregard the site interaction
				}
			}
			return H;// return the hamiltonian energy of this cell
		}

		// swaps two inputted cells
		private GameObject[] SwapCells(GameObject cell_i, GameObject cell_j){
			if(cell_i == null || cell_j == null){
				Debug.Log("Swap attempted with null cell");
				return null;
			}
			GameObject temp_cell = cell_i;// temporarily holds cell_i while performing swap
			// getting cell coordinates
			int x_i = (int)Math.Round(cell_i.transform.position.x);
			int y_i = (int)Math.Round(cell_i.transform.position.y);
			int x_j = (int)Math.Round(cell_j.transform.position.x);
			int y_j = (int)Math.Round(cell_j.transform.position.y);

			// transforming the cells to their new position
			cell_j.transform.position = new Vector3(x_i, y_i, 0);
			temp_cell.transform.position = new Vector3(x_j, y_j, 0);
			// placing the cells in their new positions in the array
			cells[x_i, y_i] = cell_j;
			cells[x_j, y_j] = temp_cell;
			// returning the swapped cells as an array of size 2
			GameObject[] swappedCells = {cells[x_i, y_i], cells[x_j, y_j]};
			return swappedCells;
		}

		// pre-calculating possible metropolis probabilities (coined boltzmannFactors) for all possible energy changes
		private double[] boltzmannFactors;// creating an array for the factors
		private void PreComputeBoltzmannFactors(int maxEnergyChange)
		{
			boltzmannFactors = new double[maxEnergyChange + 1];
			for (int i = 0; i <= maxEnergyChange; i++)
			{
				// doubles used for precision in the calculation - unnecessary otherwise (floats suffice)
				double logFactor;// exponent inside the probability calculation
				double scaling_factor = 1e+23;// normalise denominator to not get incredibly large exponents when dividing by the boltzmann constant
				// calculate exponent to be raised to e - based on Hawick's equation 9 - https://www.researchgate.net/publication/287274420_Modelling_Flood_Incursion_and_Coastal_Erosion_using_Cellular_Automata_Simulations
				logFactor = -i / (temperature * BOLTZMANN * scaling_factor);
				// scale the probability by tau and store this inside the dictionary at the index of the energy change
				boltzmannFactors[i] = tau * Math.Exp(logFactor);
			}
		}

		// compute the Metropolis probability given an energy_change
		// algorithm 2.4 in the report
		private float ComputeMetropolisProbability(float energy_change)
		{
		    if (energy_change <= 0)// should only ever have positive inputs for energy_change - shouldn't satisfy this condition
		    {
		        return 1.0f;
		    }
			float p = (float)boltzmannFactors[(int)Math.Round(energy_change)];// get the boltzmann factor for this energy change
		    return p;
		}


		// one single iteration of the kawasaki site exchange model
    private void PerformSingleIteration(){

		// works by ordering all potential (x, y) coordinates in random order, placing these inside 'pos' and retrieving x and y from 'pos'
		// https://stackoverflow.com/questions/29601965/random-numbers-in-specific-range-c-sharp
		// iterate over cells in random order - never iterates over the same position twice
		foreach (var pos in Enumerable.Range(0, gridSize).OrderBy(x => random.Next()).Zip(Enumerable.Range(0, gridSize).OrderBy(x => random.Next()), (x, y) => new { x, y }))
		{
			// get the cell at this random position
			GameObject cell = cells[(int)pos.x, (int)pos.y];
			GameObject neighbourCell = ChooseRandomNeighbour(cell);// choose a random neighbour of the cell (stochastic mechanism)
			// we don't swap cells of the same type, since there is no change in energy if we swap them
			if(IsWater(cell) != IsWater(neighbourCell)){// if the cells are of different types
				// finding energy change
				float init_energy = CalculateHamiltonian(cell) + CalculateHamiltonian(neighbourCell);// initial energy in current configuration
				GameObject[] swappedCells = SwapCells(cell, neighbourCell);// swap cells
				float new_energy = CalculateHamiltonian(swappedCells[0]) + CalculateHamiltonian(swappedCells[1]);// energy in new configuration
				float energy_change = new_energy - init_energy;// difference in energy between proposed and original configurations

				// we accept the change if the energy change is negative, i.e., we don't satisfy this condition (and therefore don't continue)
				if(energy_change >= 0){// if energy change is greater than or equal to zero
					float p = ComputeMetropolisProbability(energy_change);
					float r = (float)random.NextDouble();
					// we accept if r < p, so we reject if r >= p (and swap the cells back)
					if(r >= p){
						swappedCells = SwapCells(swappedCells[0], swappedCells[1]);
					}
				}
			}
		}
		}

		// runs the kawasaki diffusion model - called from RunSimulation.cs
		// algorithm 2.2 in the report
    public GameObject[,] RunKawasakiDiffusion(int _gridSize, GameObject[,] _cells, int _diffusion_steps)
    {
      	gridSize = _gridSize;// store gridSize locally - inputted from RunSimulation.cs
		cells = _cells;// store cells locally - inputted from RunSimulation.cs
		// used in metropolis probability calculation
		random = new System.Random();

		int time_steps = 10;// specifying time steps per run
		int diffusion_steps = _diffusion_steps;// storing diffusion_steps locally
		//Debug.Log("diffusion_steps: " + diffusion_steps);
		// pre computing potential metropolis probabilities for all possible positive energy_change
		PreComputeBoltzmannFactors(8);
		// for each diffusion step
		for(int ds = 0; ds < diffusion_steps; ds++){
			// for each time step
			for(int t = 0; t < time_steps; t++){
				// perform one iteration of the model
				PerformSingleIteration();
			}
		}
		// returns to RunSimulation.cs
		return cells;
    }

	public void Reset(){
		random = null;
		gridSize = 0;
		cells = null;
	}
}
