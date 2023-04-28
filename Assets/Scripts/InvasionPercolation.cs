using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Invasion Percolation algorithm described in report (section 3.1.4)
// attached to the object GridInstantiator
public class InvasionPercolation : MonoBehaviour
{
		private System.Random random;// used in generating random probability r
		private int gridSize;// width 'w' - inputted from RunSimulation.cs
		public float p;// parameter p adjusting erosion - inputted from RunSimulation.cs
		public GameObject waterCellPrefab;
		public GameObject[,] cells;// initially inputted from RunSimulation.cs - returned once algorithm terminates

		private float[,] randomField; // random site field of pore size incursion probabilities
		// sorted lists have two lists for key-value pairs, and we sort by the key, which will are the site growth probabilities
		private SortedList<float, GameObject> rankedGrowthSites = new SortedList<float, GameObject>();

		private int numGrowthSites;// used in finding largest growth site
		// relative positions of north, east, south, west neighbours given some coordinate
		private int [,] neighbour_positions = {{0, 1}, {1, 0}, {0, -1}, {-1, 0}};

		// returns true if the position is within the boundaries of the grid
    private bool IsValidPosition(int x, int y){
        if (x < gridSize && x >= 0 && y < gridSize && y >= 0)
        {
            return true;
        }
        return false;
    }

		// returns true if the inputted cell is a defender (is a land cell)
		bool IsDefender(GameObject cell){
				if (cell.CompareTag("LandCell")){
						return true;
				}
				return false;
		}

		// algorithm 2.6 in the report
		// based on Hawick's equation 10 (random site field) - https://www.researchgate.net/publication/287274420_Modelling_Flood_Incursion_and_Coastal_Erosion_using_Cellular_Automata_Simulations
		// initialises all pore site incursion probabilities
    void InitGrowthProbabilities(){
				random = new System.Random();
				// for all rows in the grid
				for (int x = 0; x < gridSize; x++){
					// distinctly different from lowercase 'p'
					float P = 0;// holds part of the final calculation (discussed in 3.1.4.3)
					if (p < 0){// adjusted formula for negative parameter p
							P = (1 + p) * (gridSize + x)/gridSize;
					} else {// for positive parameter p values
							P = (1 - p) * (gridSize - x + 1)/(x + 1);//x + 1 to avoid dividing by zero
					}
					// for all columns in each row
					for (int y = 0; y < gridSize; y++){
							float u = (float)random.NextDouble();// random probability on [0,1)
							// named Px_y since it is dependent on 'P' but is unique to each site at position (x, y)
							float Px_y = P + (u * p);// final calculation - takes P from earlier and finds the pore size incursion probability

							// used for figure 2.2 (visualizing pore size incursion probabilities)
							// GameObject cell = cells[x,y];
							// Color start_colour = new Color(113/255f,73/255f,198/255f);
							// Color end_colour = new Color(252/255f,41/255f,71/255f);
							// Color growth_colour = Color.Lerp(start_colour, end_colour, Px_y);
							// cell.GetComponent<SpriteRenderer>().color = growth_colour;

							randomField[x, y] = Px_y;// assign growth probability to the site by placing inside random site field
					}
				}
		}

		// erodes the inputted cell by changing the cell to water
		// returns true if water reached RHS of grid - used to terminate simulation
		bool ErodeCell(GameObject cell){
				if (cell != null){
					int x = (int)cell.transform.position.x;
					int y = (int)cell.transform.position.y;
					GameObject.Destroy(cell);// destroy the land cell to re-instantiate as WaterCell
					// removing the eroded cell from the ranked list of growth sites
					float key = randomField[x, y];// key for the ranked list rankedGrowthSites
					rankedGrowthSites.Remove(key);
					numGrowthSites -= 1;// decrement the number of growth sites identified after eroding the cell

					// instantiate a new water cell, colour and transform the cell
					GameObject _cell = Instantiate(waterCellPrefab, transform);
					_cell.GetComponent<SpriteRenderer>().color = Color.cyan;
					_cell.transform.position = new Vector3(x, y, 0);
					cells[x, y] = _cell;// place the newly instantiated cell in the correct position
					if (x == gridSize - 1){// if reached RHS, return true
						return true;
					}
				}
				return false;
		}

		// returns all neighbours of a cell
		GameObject[] GetNeighboursAtPos(int x, int y){
			GameObject[] neighbours = new GameObject[4];
			for (int i = 0; i < 4; i++){
					// neighbour coordinates
					int n_x = x + neighbour_positions[i,0];
					int n_y = y + neighbour_positions[i,1];
					// if the position of the neighbour is within the boundaries of the grid
					if (IsValidPosition(n_x, n_y) == true){
						neighbours[i] = cells[n_x, n_y];
					} else{// otherwise, the current cell is on the border of the grid and therefore there is no neighbour in this direction
						neighbours[i] = null;
					}
			}
			return neighbours;
		}

		// only takes land cells as growth sites
		// https://learn.microsoft.com/en-us/dotnet/api/system.collections.sortedlist?view=net-7.0
		void UpdateRankedGrowthSites(GameObject[] growthSites){
			foreach(GameObject site in growthSites){
				if (site != null){// if the neighbour exists
					int x = (int)site.transform.position.x;
					int y = (int)site.transform.position.y;
					// get the pore size incursion probability for this neighbour
					float growthProbability = randomField[x, y];
					// if the cell isn't already in the list (address the list by the key (growth probability))
					if(!rankedGrowthSites.ContainsKey(growthProbability)){
							// colour the growth site green (marking as effectively invaded)
							site.GetComponent<SpriteRenderer>().color = Color.green;
							// add the site to the ordered list of potential new growth sites
							rankedGrowthSites.Add(growthProbability, site);
							numGrowthSites += 1;// increment the number of identified growth sites
					}
				}
			}
		}

		// construct (initialise) the list of ranked growth sites
		void ConstructRankedGrowthSites(){
				numGrowthSites = 0;// initialising list, hence there are currently none identified
				// for all sites in the simulation
				for (int x = 0; x < gridSize; x++){
					for (int y = 0; y < gridSize; y++){
						// if the site at (x, y) is water
						if (!IsDefender(cells[x, y])){
								// find neighbouring defender sites
								GameObject[] neighbours = GetNeighboursAtPos(x, y);
								// identify potential new growth sites from the neighbours of this cell
								GameObject[] growthSites = IdentifyGrowthSites(neighbours);
								UpdateRankedGrowthSites(growthSites);// update the ranked list with these growth sites (if any)
						}
					}
				}
		}

		GameObject FindLargestGrowthSite(){
				if (numGrowthSites == 0){// if there are currently no identified growth sites, there is no largest
					return null;
				}
				GameObject growthSite = rankedGrowthSites.Values[numGrowthSites - 1];// take the last element of the list (largest probability)
				return growthSite;
		}

		// identifies growth sites from array of neighbours
		GameObject[] IdentifyGrowthSites(GameObject[] neighbours){
				GameObject[] growthSites = new GameObject[4];
				// for all 4 neighbours
				for (int i = 0; i < 4; i++){
						GameObject cell = neighbours[i];
						// if the neighbour exists
						if (cell != null){
								// and if its a defender (land)
								if (IsDefender(cell) == true){
										// add it to list of potential growth sites
										growthSites[i] = cell;
								} else{// otherwise, there is no potential growth site here
										growthSites[i] = null;
								}
						} else {// if the neighbour doesn't exist, there is no potential growth site here
								growthSites[i] = null;
						}
				}
				return growthSites;
		}

		// runs the Invasion Percolation algorithm - called from RunSimulation.cs
		// algorithm 2.5 in the report
		public (int, GameObject[,]) RunInvasionPercolation(int _gridSize, GameObject[,] _cells, float _p){
				gridSize = _gridSize;// store gridSize locally - inputted from RunSimulation.cs
				cells = _cells;// store cells locally - inputted from RunSimulation.cs
				p = _p;// store parameter p locally - inputted from RunSimulation.cs
				randomField = new float[gridSize, gridSize];// initialise empty random site field of correct size
				// used in pore size incursion probability calculation
				random = new System.Random();

				// initialise the random site field of pore size incursion probabilities
				InitGrowthProbabilities();
				// initialise the ranked list of potential growth sites
				ConstructRankedGrowthSites();

				bool reachedRHS = false;
				int invasion_steps = 0;
				while(reachedRHS == false){
						// attempt to find and erode the largest identified pore size potential growth site
						GameObject cell = FindLargestGrowthSite();
						if(cell!=null){
							int x = (int)cell.transform.position.x;
							int y = (int)cell.transform.position.y;
							reachedRHS = ErodeCell(cell);// erode the defender with highest growth p
							// identify new growth sites from the neighbours of the cell at this position
							GameObject[] neighbours = GetNeighboursAtPos(x, y);
							GameObject[] growthSites = IdentifyGrowthSites(neighbours);
							// update the ranked list of potential growth sites with these newly identified growth sites (if any)
							UpdateRankedGrowthSites(growthSites);
						}
						invasion_steps++;
				}
				// returns to RunSimulation.cs
				return (invasion_steps, cells);
		}
}
