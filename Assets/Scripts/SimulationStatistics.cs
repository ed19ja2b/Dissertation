using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;


public class SimulationStatistics : MonoBehaviour
{
		private int gridSize;

		// writing the statistics to a csv file
		public void WriteResultsToFile((float, int, float)[][] allInvasionPercolationResults, float[][] allFractionGreenResults, (int, float)[][] allKawasakiDiffusionResults, float[][] allElapsedTimeResults, int num_to_average, int num_parameters){
			string fileName = "simulation_statistics.csv";
      using (StreamWriter writer = new StreamWriter(fileName, false))
      {
        writer.WriteLine("p,invasion_steps,IP_elapsed_time,fraction_green,diffusion_steps,KD_elapsed_time,total_elapsed_time");

				for(int i = 0; i < num_to_average; i++){
					(float, int, float)[] invasionPercolationResults = allInvasionPercolationResults[i];
					(int, float)[] kawasakiDiffusionResults = allKawasakiDiffusionResults[i];
					float[] elapsed_times = allElapsedTimeResults[i];
					float[] fraction_green_values = allFractionGreenResults[i];

	        for(int n = 0; n < num_parameters; n++){
	          (float p, int invasion_steps, float IP_elapsed_time) = invasionPercolationResults[n];
						p = (float)Math.Round(p, 2);
	          (int diffusion_steps, float KD_elapsed_time) = kawasakiDiffusionResults[n];
						float total_elapsed_time = elapsed_times[n];
						float fraction_green = (float)Math.Round(fraction_green_values[n], 4);
	          writer.WriteLine($"{p},{invasion_steps},{IP_elapsed_time},{fraction_green},{diffusion_steps},{KD_elapsed_time},{total_elapsed_time}");
	        }
				}
      }
      UnityEngine.Debug.Log($"Results written to {fileName}");
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
			// calculate the fraction of green sites (effectively invaded) out of all sites
			float fraction_green = (float)num_green / (float)num_sites_total;
			return fraction_green;
		}

		public GameObject[,] GatherStatistics(int _gridSize, float _p_init, float _p_end, int _num_parameters, int num_to_average){
			gridSize = _gridSize;

			// ****************************************** INITIALISING VARIABLES ******************************************
			// create array of all p values to iterate over
			float[] pValues = InitialisePValues(_p_init, _p_end, _num_parameters);
			// initialise cells array for final state (to output back to RunSimulation.cs)
			GameObject[,] final_cells_state = new GameObject[gridSize, gridSize];
			// create arrays of tuple arrays for all measurements taken
			(float, int, float)[][] allInvasionPercolationResults = new (float, int, float)[num_to_average][];
			(int, float)[][] allKawasakiDiffusionResults = new (int, float)[num_to_average][];
			float[][] allElapsedTimeResults = new float[num_to_average][];
			float[][] allFractionGreenResults = new float[num_to_average][];

			// run numerous time to average the results
			for (int run = 0; run < num_to_average; run++){
				// ****************************************** INITIALISING VARIABLES ******************************************
				int counter = 0;// initialising a counter for counting all runs
				// tuple array storing statistics about each run: (p, invasion_steps, elapsed_time)
				(float, int, float)[] invasionPercolationResults = new (float, int, float)[_num_parameters];
				// tuple array storing statistics about kawasaki model for this p: (diffusion_steps, elapsed_time)
				(int, float)[] kawasakiDiffusionResults = new (int, float)[_num_parameters];
				// float array storing total elapsed time for each p
				float[] total_elapsed_times = new float[_num_parameters];
				// float array storing the fraction of green (effectively invaded) sites left behind by the invading water of the IP algorithm
				float[] fraction_green_results = new float[_num_parameters];


				// run the invasion percolation model for all values of p to be tested
				foreach(float p in pValues){
					// ****************************************** INITIALISING VARIABLES ******************************************
					Stopwatch IP_timer = new Stopwatch();// create and start a timer to measure IP algorithm execution time for this value of parameter p
					IP_timer.Start();
					Stopwatch total_timer = new Stopwatch();// create and start a timer to measure entire model's elapsed time
					total_timer.Start();
					Stopwatch KD_timer = new Stopwatch();// create timer for measuring kawasaki diffusion model execution time


					// ******************************************* INVASION PERCOLATION *******************************************
					// run the invasion percolation algorithm for this value of p
					(int invasion_steps, GameObject[,] invasion_cells) = GetComponent<RunSimulation>().ExecuteInvasionPercolation(gridSize, p);
					IP_timer.Stop();// stop the timer for the invasion percolation model
					float IP_elapsed_time = IP_timer.ElapsedMilliseconds / 1000f;// convert time to seconds

					// storing statistical data about the IP algorithm for this p
					invasionPercolationResults[counter] = (p, invasion_steps, IP_elapsed_time);
					// calculating and storing statistical data about fraction of effectively invaded sites
					float fraction_green = CalculateFractionGreen(invasion_cells);
					fraction_green_results[counter] = fraction_green;

					// ******************************************** KAWASAKI DIFFUSION ********************************************
					// run the kawasaki model with the new state of cells after the IP model has terminated
					GameObject[,] kawasaki_cells = new GameObject[gridSize, gridSize];

					KD_timer.Start();// start the timer to measyre kawasaki diffusion model execution time
					kawasaki_cells = GetComponent<RunSimulation>().ExecuteKawasakiDiffusion(invasion_cells, gridSize, invasion_steps);
					KD_timer.Stop();// stop the timer for the kawasaki model
					float KD_elapsed_time = KD_timer.ElapsedMilliseconds / 1000f;// convert time to seconds

					// get the number of diffusion steps performed by the algorithm
					int diffusion_steps = GetComponent<RunSimulation>().CalculateDiffusionSteps(invasion_steps);


					// storing statistical data about the KD model
					kawasakiDiffusionResults[counter] = (diffusion_steps, KD_elapsed_time);


					// ************************************************ FINALIZING ************************************************
					// stop the total elapsed time counter
					total_timer.Stop();
					float total_elapsed_time = total_timer.ElapsedMilliseconds / 1000f;// convert time to seconds

					// storing statistical data about total elapsed execution time for this p
					total_elapsed_times[counter] = total_elapsed_time;

					// increment counter
					counter++;
					// if this is the final iteration, output this state of cells
					if (run == (num_to_average - 1) && counter == (_num_parameters - 1)){
						final_cells_state = kawasaki_cells;
					}
				}// end of inner loop (iterating over different p)
				// ************************************************ FINALIZING ************************************************
				allInvasionPercolationResults[run] = invasionPercolationResults;
				allKawasakiDiffusionResults[run] = kawasakiDiffusionResults;
				allElapsedTimeResults[run] = total_elapsed_times;
				allFractionGreenResults[run] = fraction_green_results;
			}// end of outer loop (averaging results)

			WriteResultsToFile(allInvasionPercolationResults, allFractionGreenResults, allKawasakiDiffusionResults, allElapsedTimeResults, num_to_average, _num_parameters);
			return final_cells_state;
		}// end of GatherStatistics()
}
