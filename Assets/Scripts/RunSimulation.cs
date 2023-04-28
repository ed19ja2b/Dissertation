using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

// executes the simulation by calling several functions from the appropriate scripts
// attached to the object GridInstantiator - runs upon pressing play
public class RunSimulation : MonoBehaviour
{
		// adjusts camera so that we can view the whole simulation
		void AdjustCamera(int _gridSize){
			int orthographicSize = (_gridSize)/2;
			float offset = 0.5f;
			Camera.main.orthographicSize = orthographicSize;// (/1) - assuming always square aspect ratio (1/1 = 1)
			Camera.main.transform.position = new Vector3(orthographicSize - offset, orthographicSize - offset, -10);// subtracts offset to centre grid in view
		}

    void Start()// Start is called before the first frame update
    {
			// specifying simulation parameters
			int diffusion_steps = 100;
			float p = .6f;
			int invasion_steps;
			int gridSize = 256;// taking 'w' (gridSize) to be 256 as in Hawick's implementation - https://www.researchgate.net/publication/287274420_Modelling_Flood_Incursion_and_Coastal_Erosion_using_Cellular_Automata_Simulations
			// instantiate site cells
			GameObject[,] cells = GetComponent<InstantiateGridCells>().InstantiateGrid(gridSize);

			//adjust camera to view the whole grid
			AdjustCamera(gridSize);

			// once parameters are specified and all sites are initialised
			// get the state of cells after termination of invasion percolation algorithm, as well as how many invasion steps were performed
			(invasion_steps, cells) = GetComponent<InvasionPercolation>().RunInvasionPercolation(gridSize, cells, p);
			// simple formula for adjusting the number of diffusion steps compared to invasion steps - Math.Abs() accounts for negative parameter p
			diffusion_steps = Math.Abs((int)(Math.Round(Math.Sqrt(invasion_steps) / 2 * (p))));

			// get the state of cells after executing the kawasaki diffusion model
			cells = GetComponent<KawasakiDiffusion>().RunKawasakiDiffusion(gridSize, cells, diffusion_steps);
    }
}
