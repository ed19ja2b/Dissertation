using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;

// attached to the object RunSimulation
public class SimulationStatistics : MonoBehaviour
{
		private int gridSize;

		// writing the statistics to a csv file
		public void WriteResultsToFile((float, int, float) invasionPercolationResults, float fraction_green, (int, float) kawasakiDiffusionResults, float total_elapsed_time, int num_parameters){
        	string fileName = "simulation_statistics.csv";
			if(!File.Exists(fileName)){
				// write header to a new csv file 'true' means append to existing file
				using (StreamWriter writer = new StreamWriter(fileName, true))
				{
					writer.WriteLine("p,invasion_steps,IP_elapsed_time,fraction_green,diffusion_steps,KD_elapsed_time,total_elapsed_time");
					
				}
			}
        using (StreamWriter writer = new StreamWriter(fileName, true))
        {

			(float p, int invasion_steps, float IP_elapsed_time) = invasionPercolationResults;
			p = (float)Math.Round(p, 2);
			(int diffusion_steps, float KD_elapsed_time) = kawasakiDiffusionResults;
			fraction_green = (float)Math.Round(fraction_green, 4);
			writer.WriteLine($"{p},{invasion_steps},{IP_elapsed_time},{fraction_green},{diffusion_steps},{KD_elapsed_time},{total_elapsed_time}");
        }
        UnityEngine.Debug.Log($"Results appended to {fileName}");
    }


		// initialising p values to test in invasion percolation algorithm
		private float[] InitialisePValues(float p_init, float p_end, int num_parameters){
			float[] p_values = new float[num_parameters];
			float p_increment = (p_end - p_init) / (float)(num_parameters - 1);
			float p = p_init;
			for (int i = 0; i < num_parameters; i++){
				p_values[i] = p;
				p += p_increment;
			}
			return p_values;
		}

		// calculating fraction of effectively invaded growth sites left behind be invading water in Invasion Percolation algorithm
		private float CalculateFractionGreen(GameObject[,] cells){
			GameObject cell;
			int num_green = 0;
			int num_sites_total = 0;
			for(int x = 0; x < gridSize; x++){
				for(int y = 0; y < gridSize; y++){
					cell = cells[x,y];
					if (cell.GetComponent<SpriteRenderer>().color == Color.green){
						num_green++;
					}
					num_sites_total += 1;
				}
			}
			// calculate the fraction of green sites (effectively invaded) out of all land sites
			float fraction_green = (float)num_green / ((float)num_sites_total/2f);
			return fraction_green;
		}

		private (GameObject[,], float, (float, int, float)) GatherInvasionPercolationStatistics(float p){
			Stopwatch IP_timer = new Stopwatch();// create and start a timer to measure IP algorithm execution time for this value of parameter p
			IP_timer.Start();

			(int invasion_steps, GameObject[,] invasion_cells) = GetComponent<RunSimulation>().ExecuteInvasionPercolation(gridSize, p);

			IP_timer.Stop();// stop the timer for the invasion percolation model
			float IP_elapsed_time = IP_timer.ElapsedMilliseconds / 1000f;// convert time to seconds
			
			// calculating and storing statistical data about fraction of effectively invaded sites
			float fraction_green = CalculateFractionGreen(invasion_cells);

			return (invasion_cells, fraction_green, (p, invasion_steps, IP_elapsed_time));
		}

		private (GameObject[,], (int, float)) GatherKawasakiDiffusionStatistics(GameObject[,] invasion_cells, int invasion_steps){
			Stopwatch KD_timer = new Stopwatch();// create timer for measuring kawasaki diffusion model execution time
			GameObject[,] kawasaki_cells = new GameObject[gridSize, gridSize];

			KD_timer.Start();// start the timer to measure kawasaki diffusion model execution time
			kawasaki_cells = GetComponent<RunSimulation>().ExecuteKawasakiDiffusion(invasion_cells, gridSize, invasion_steps);
			KD_timer.Stop();// stop the timer for the kawasaki model
			float KD_elapsed_time = KD_timer.ElapsedMilliseconds / 1000f;// convert time to seconds

			// get the number of diffusion steps performed by the algorithm
			int diffusion_steps = GetComponent<RunSimulation>().CalculateDiffusionSteps(invasion_steps);
			return (kawasaki_cells, (diffusion_steps, KD_elapsed_time));
		}

		
		public GameObject[,] GatherStatistics(int _gridSize, float _p_init, float _p_end, int _num_parameters, int num_to_average){
			gridSize = _gridSize;

			// ****************************************** INITIALISING VARIABLES ******************************************
			// create array of all p values to iterate over
			float[] pValues = InitialisePValues(_p_init, _p_end, _num_parameters);
			Stopwatch stop_timer = new Stopwatch();
			stop_timer.Start();
			// run numerous time to average the results
			for (int run = 0; run < num_to_average; run++){
				// ****************************************** INITIALISING VARIABLES ******************************************
				stop_timer.Stop();
				if(stop_timer.ElapsedMilliseconds / 1000f > 90){
					UnityEngine.Debug.Log("Exited GatherStatistics() early - ran for too long");
					return null;
				}
				stop_timer.Start();
				// run the invasion percolation model for all values of p to be tested
				foreach(float p in pValues){
					// ****************************************** INITIALISING VARIABLES ******************************************
					
					Stopwatch total_timer = new Stopwatch();// create and start a timer to measure entire model's elapsed time
					total_timer.Start();
					
					
					int counter = 0;// initialising a counter for counting all runs					

					// ******************************************* INVASION PERCOLATION *******************************************
					// run the invasion percolation algorithm for this value of p and store results
					// tuple storing statistics about each run: (p, invasion_steps, elapsed_time)
					(GameObject[,] invasion_cells, float fraction_green, (float, int, float) invasionPercolationResults) = GatherInvasionPercolationStatistics(p);
					int invasion_steps = invasionPercolationResults.Item2;
					// ******************************************** KAWASAKI DIFFUSION ********************************************
					// run the kawasaki model with the new state of cells after the IP model has terminated
					// tuple storing statistical data about the KD model
					(GameObject[,] kawasaki_cells, (int, float) kawasakiDiffusionResults) = GatherKawasakiDiffusionStatistics(invasion_cells, invasion_steps);


					// ************************************************ FINALIZING ************************************************
					// stop the total elapsed time counter
					total_timer.Stop();
					float total_elapsed_time = total_timer.ElapsedMilliseconds / 1000f;// convert time to seconds

					

					// append results to file
					WriteResultsToFile(invasionPercolationResults, fraction_green, kawasakiDiffusionResults, total_elapsed_time, _num_parameters);
					GetComponent<RunSimulation>().DestroyCells(kawasaki_cells);
					GetComponent<RunSimulation>().DestroyCells(invasion_cells);

					// if this is the final iteration, output this state of cells
					if (run == (num_to_average - 1) && counter == (_num_parameters - 1)){
						return kawasaki_cells;
					}
					// increment counter
					counter++;
				}// end of inner loop (iterating over different p)
				// ************************************************ FINALIZING ************************************************		
			}// end of outer loop (averaging results)
			return null;
		}// end of GatherStatistics()
}
