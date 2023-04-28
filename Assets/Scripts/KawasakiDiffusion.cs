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
		const double BOLTZMANN = 1.380649e-23;
    private const float temperature = 14.0f; //used in metropolis probability
		private const float tau = 0.574f;

    int[,] neighbour_positions = { { 0, 1 }, { 1, 0 }, { 0, -1 }, { -1, 0 } };

    private bool IsValidPosition(int x, int y){
        //if within boundaries of grid
        if (x < gridSize && x >= 0 && y < gridSize && y >= 0)
        {
            return true;
        }
        return false;
    }

		private int IsWater(GameObject cell){
			if(cell.CompareTag("WaterCell")){
				return 1;
			}
			return 0;
		}

		GameObject ChooseRandomNeighbour(GameObject cell)
    {
        int x = (int)cell.transform.position.x;
        int y = (int)cell.transform.position.y;
        //pick a random neighbour from the 4 Moore neighbours
        //if its not a valid choice, re-choose with different random neighbour
        foreach (int i in Enumerable.Range(0, 4).OrderBy(x => random.Next()))
        {
            int neighbour_x = x + neighbour_positions[i, 0];
            int neighbour_y = y + neighbour_positions[i, 1];
            if (IsValidPosition(neighbour_x, neighbour_y))
            {
                return cells[neighbour_x, neighbour_y];
            }
        }
        return null; //should never reach here
    }

		GameObject[] GetNeighbours(GameObject cell)
		{
			GameObject[] neighbours = new GameObject[4];
			int x = (int)Math.Round(cell.transform.position.x);
			int y = (int)Math.Round(cell.transform.position.y);
			for (int i = 0; i < 4; i++)
			{
					// neighbour coordinates
					int n_x = x + neighbour_positions[i, 0];
					int n_y = y + neighbour_positions[i, 1];
					if (IsValidPosition(n_x, n_y) == true)
					{
							neighbours[i] = cells[n_x, n_y];
					}
					else
					{
							neighbours[i] = null;
					}
			}
			return neighbours;
		}

		private float CalculateHamiltonian(GameObject cell_i){
			int c_i = IsWater(cell_i);
			GameObject[] neighbours = GetNeighbours(cell_i);

			float H = 0;
			float V_WW = 1.0f;
			foreach(GameObject cell_j in neighbours){
				if(cell_j!=null){
					int c_j = IsWater(cell_j);
					if(c_i == 1 && c_j == 1){
						H += V_WW;
					}
				}
			}
			return H;
		}

		private GameObject[] SwapCells(GameObject cell_i, GameObject cell_j){
			if(cell_i == null || cell_j == null){
				Debug.Log("Swap attempted with null cell");
				return null;
			}
			GameObject temp_cell = cell_i;
			int x_i = (int)Math.Round(cell_i.transform.position.x);
			int y_i = (int)Math.Round(cell_i.transform.position.y);
			int x_j = (int)Math.Round(cell_j.transform.position.x);
			int y_j = (int)Math.Round(cell_j.transform.position.y);

			cell_j.transform.position = new Vector3(x_i, y_i, 0);
			temp_cell.transform.position = new Vector3(x_j, y_j, 0);

			cells[x_i, y_i] = cell_j;
			cells[x_j, y_j] = temp_cell;

			GameObject[] swappedCells = {cells[x_i, y_i], cells[x_j, y_j]};
			return swappedCells;
		}

		private Dictionary<double, double> boltzmannFactors;
		private void PreComputeBoltzmannFactors(int maxEnergyChange)
		{
				boltzmannFactors = new Dictionary<double, double>();
				for (int i = 0; i <= maxEnergyChange; i++)
				{
						boltzmannFactors[i] = Math.Exp(-i / (BOLTZMANN * temperature));
				}
		}

		private float ComputeMetropolisProbability(float energy_change)
		{
		    if (energy_change <= 0)
		    {
		        return 1.0f;
		    }
		    double logFactor;
		    logFactor = -energy_change / (temperature * BOLTZMANN * 1e+23);
		    double factor = tau * Math.Exp(logFactor);
				//Debug.Log("Factor: " + factor + "given energy_change: " + energy_change);
		    return (float)factor;
		}


    private void PerformSingleIteration(){

			//iterate over cells in random order
			//works by ordering all potential (x, y) coordinates in random order, placing these inside 'pos' and retrieving x and y from 'pos'
			//never iterates over the same position twice
			//https://stackoverflow.com/questions/29601965/random-numbers-in-specific-range-c-sharp
			foreach (var pos in Enumerable.Range(0, gridSize).OrderBy(x => random.Next()).Zip(Enumerable.Range(0, gridSize).OrderBy(x => random.Next()), (x, y) => new { x, y }))
			{
				GameObject cell = cells[(int)pos.x, (int)pos.y];
				GameObject neighbourCell = ChooseRandomNeighbour(cell);
				//we don't swap cells of the same type, since there is no change in energy if we swap them
				//if the cells are of different types
				if(IsWater(cell) != IsWater(neighbourCell)){
					//finding energy change
					float init_energy = CalculateHamiltonian(cell) + CalculateHamiltonian(neighbourCell);
					GameObject[] swappedCells = SwapCells(cell, neighbourCell);
					float new_energy = CalculateHamiltonian(swappedCells[0]) + CalculateHamiltonian(swappedCells[1]);
					float energy_change = new_energy - init_energy;

					//we accept the change if the energy change is negative
					//otherwise, if energy change is greater than or equal to zero
					if(energy_change >= 0){
						float p = ComputeMetropolisProbability(energy_change);
						float r = (float)random.NextDouble();
						//we accept if r < p, so we reject if r >= p (and swap the cells back)
						if(r >= p){
							swappedCells = SwapCells(swappedCells[0], swappedCells[1]);
						}
						// else{
						// 	Debug.Log("Accepted swap with r (" + r + ") less than p (" + p + ")");
						// }
					}
				}
			}
		}

    public GameObject[,] RunKawasakiDiffusion(int _gridSize, GameObject[,] _cells, int _num_runs)
    {
      gridSize = _gridSize;
			cells = _cells;
			random = new System.Random();
			int time_steps = 50;
			int num_runs = _num_runs;

			PreComputeBoltzmannFactors(8);

			for(int r = 0; r < num_runs; r++){
				//Debug.Log("Run " + r);
				for(int t = 0; t < time_steps; t++){
					PerformSingleIteration();
				}
			}
			return cells;
    }
	}
