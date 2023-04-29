using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

// executes the simulation by calling several functions from the appropriate scripts
// attached to the object GridInstantiator - runs upon pressing play
public class RunSimulation : MonoBehaviour
{
		private float p;
		// adjusts camera so that we can view the whole simulation
		void AdjustCamera(int _gridSize){
			int orthographicSize = (_gridSize)/2;
			float offset = 0.5f;
			Camera.main.orthographicSize = orthographicSize;// (/1) - assuming always square aspect ratio (1/1 = 1)
			Camera.main.transform.position = new Vector3(orthographicSize - offset, orthographicSize - offset, -10);// subtracts offset to centre grid in view
		}

		public (int, GameObject[,]) ExecuteInvasionPercolation(int gridSize, float p){
			// instantiate site cells
			GameObject[,] cells = GetComponent<InstantiateGridCells>().InstantiateGrid(gridSize);
			// specifying simulation parameters
			int invasion_steps = 0;
			// once parameters are specified and all sites are initialised
			// get the state of cells after termination of invasion percolation algorithm, as well as how many invasion steps were performed
			(invasion_steps, cells) = GetComponent<InvasionPercolation>().RunInvasionPercolation(gridSize, cells, p);
			return (invasion_steps, cells);
		}

		public int CalculateDiffusionSteps(int invasion_steps)
		{
		    if (invasion_steps == 0)
		    {
		        return 50; // arbitrary default number of diffusion steps for testing
		    }

		    // parameters for inverse relationship between invasion_steps and diffusion_steps
				// i.e., high invasion_steps produce low diffusion_steps and vice versa
		    float max_invasion_steps = 30000f;// rough estimated maximum
		    float min_diffusion_steps = 0f;// specified minimum diffusion_steps
		    float max_diffusion_steps = 100f;// specified maximum diffusion_steps

		    // calculate diffusion_steps as inverse relationship between invasion_steps and diffusion_steps
		    int diffusion_steps = (int)Math.Round(min_diffusion_steps + (max_diffusion_steps - min_diffusion_steps) * (1 - (invasion_steps / max_invasion_steps)));
		    return diffusion_steps;
		}

		public GameObject[,] ExecuteKawasakiDiffusion(GameObject[,] _cells, int gridSize, int invasion_steps){
			GameObject[,] cells = _cells;
			if (invasion_steps == 0){// if invasion percolation hasn't executed
				// instantiate site cells
				cells = GetComponent<InstantiateGridCells>().InstantiateGrid(gridSize);
			}

			// calculate number of diffusion steps for kawasaki model
			int diffusion_steps = CalculateDiffusionSteps(invasion_steps);

			// get the state of cells after executing the kawasaki diffusion model
			cells = GetComponent<KawasakiDiffusion>().RunKawasakiDiffusion(gridSize, cells, diffusion_steps);
			return cells;
		}

    void Start()// Start is called before the first frame update
    {
			p = .6f;
			int gridSize = 256;// taking 'w' (gridSize) to be 256 as in Hawick's implementation - https://www.researchgate.net/publication/287274420_Modelling_Flood_Incursion_and_Coastal_Erosion_using_Cellular_Automata_Simulations
			GameObject[,] cells = new GameObject[gridSize, gridSize];

			//adjust camera to view the whole grid
			AdjustCamera(gridSize);

			bool run_statistics = true;
			if(run_statistics){
				int num_to_average = 3;
				// measure statistics of IP algorithm for different p with: width, initial p, final p, num parameters to test, and how many times we test them
				cells = GetComponent<SimulationStatistics>().GatherStatistics(gridSize, 0.05f, 0.9f, 18, num_to_average);
			} else{
				int invasion_steps = 0;
				(invasion_steps, cells) = ExecuteInvasionPercolation(gridSize, p);
				cells = ExecuteKawasakiDiffusion(cells, gridSize, invasion_steps);
			}
    }
}
