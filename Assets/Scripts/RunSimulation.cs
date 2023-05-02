using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

// executes the simulation by calling several functions from the appropriate scripts
public class RunSimulation : MonoBehaviour
{
		[Range(-1f, 0.90f)]
		public float p = 0.5f;// global - parameter p - adjust to modify erosion patterns
		// adjusts camera so that we can view the whole simulation
		public bool run_statistics = false;// set to true if measuring statistics of the model
		public int num_to_average = 1;// how many times we gather statistics for all the p we are testing so that we can average the results
		public int num_to_test = 9;// number of p parameters to test
		public float p_init = 0.1f;// initial p parameter to test
		public float p_final = 0.9f;// final p parameter to test
		void AdjustCamera(int _gridSize){
			int orthographicSize = (_gridSize)/2;
			float offset = 0.5f;
			Camera.main.orthographicSize = orthographicSize;// assuming always square aspect ratio (1/1 = 1)
			Camera.main.transform.position = new Vector3(orthographicSize - offset, orthographicSize - offset, -10);// subtracts offset to centre grid in view
		}

		// executes invasion percolation algorithm and returns the state of cells after termination of algorithm
		public (int, GameObject[,]) ExecuteInvasionPercolation(int gridSize, float p){
			// instantiate site cells
			GameObject[,] cells = GetComponent<InstantiateGridCells>().InstantiateGrid(gridSize);
			// specifying simulation parameters
			int invasion_steps = 0;
			// once parameters are specified and all sites are initialised
			// get the state of cells after termination of invasion percolation algorithm, as well as how many invasion steps were performed
			(invasion_steps, cells) = GetComponent<InvasionPercolation>().RunInvasionPercolation(gridSize, cells, p);
			GetComponent<InvasionPercolation>().Reset();
			return (invasion_steps, cells);
		}

		// calculates number of diffusion steps for kawasaki model based on number of invasion steps from invasion percolation
		public int CalculateDiffusionSteps(int invasion_steps)
		{
		    if (invasion_steps == 0) return 10; // arbitrary default number of diffusion steps for testing
		    // parameters for inverse relationship between invasion_steps and diffusion_steps
			// i.e., so that high invasion_steps produce low diffusion_steps and vice versa
		    float max_invasion_steps = 32000f;// rough estimated maximum
		    float max_diffusion_steps = 100f;// specified maximum diffusion_steps
		    // calculate diffusion_steps as inverse relationship between invasion_steps and diffusion_steps
		    int diffusion_steps = (int)Math.Round(max_diffusion_steps * (1 - (invasion_steps / max_invasion_steps)));
		    return diffusion_steps;
		}

		public GameObject[,] ExecuteKawasakiDiffusion(GameObject[,] _cells, int gridSize, int invasion_steps){
			GameObject[,] cells = _cells;
			// if invasion percolation hasn't executed, instantiate site cells
			if (invasion_steps == 0)cells = GetComponent<InstantiateGridCells>().InstantiateGrid(gridSize);
			// calculate number of diffusion steps for kawasaki model
			int diffusion_steps = CalculateDiffusionSteps(invasion_steps);
			// get the state of cells after executing the kawasaki diffusion model
			cells = GetComponent<KawasakiDiffusion>().RunKawasakiDiffusion(gridSize, cells, diffusion_steps);
			GetComponent<KawasakiDiffusion>().Reset();// reset all values in KawasakiDiffusion script
			return cells;
		}

    void Start()// Start is called before the first frame update
    {
			// p = .6f;// parameter p - adjust to modify erosion patterns
			int gridSize = 256;// taking 'w' (gridSize) to be 256 as in Hawick's implementation - https://www.researchgate.net/publication/287274420_Modelling_Flood_Incursion_and_Coastal_Erosion_using_Cellular_Automata_Simulations
			GameObject[,] cells = new GameObject[gridSize, gridSize];// initialising cells as empty array

			//adjust camera to view the whole grid
			AdjustCamera(gridSize);

			// if we want to gather statistics
			if(run_statistics){
				// measure statistics of IP algorithm for different p with: width, initial p, final p, num parameters to test, and how many times we test them
				cells = GetComponent<SimulationStatistics>().GatherStatistics(gridSize, p_init, p_final, num_to_test, num_to_average);
			} else{// otherwise we run the model normally
				int invasion_steps = 0;// initialise invasion_steps to 0
				// get invasion_steps and cells after executing invasion percolation
				(invasion_steps, cells) = ExecuteInvasionPercolation(gridSize, p);
				// get cells after executing kawasaki diffusion model, inputting cells and invasion_steps
				cells = ExecuteKawasakiDiffusion(cells, gridSize, invasion_steps);
			}
    }

	// used to ensure each consecutive execution of the model is independent of the previous when gathering statistics
	public void DestroyCells(GameObject[,] cells){
		foreach(GameObject cell in cells){
			Destroy(cell);// destroy all cells in the array (deleting from scene)
		}
	}
}
