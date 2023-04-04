using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InvasionPercolation : MonoBehaviour
{
		private System.Random random;
		public float p;
		private float[,] randomField;
		int gridSize;
		int [,] neighbour_positions = {{0, 1}, {1, 0}, {0, -1}, {-1, 0}};
		public GameObject waterCellPrefab;
		public GameObject[,] cells;
		//sorted lists have two lists for key-value pairs, and we sort by the key, which will be the site growth probability
		SortedList<float, GameObject> rankedGrowthSites = new SortedList<float, GameObject>();
		private int numGrowthSites;

		bool IsValidPosition(int x, int y){
				//if within boundaries of grid
				if (x < gridSize && x >= 0 && y < gridSize && y >= 0){
						return true;
				}
				return false;
		}

		bool IsDefender(GameObject cell){
				if (cell.CompareTag("LandCell")){
						return true;
				}
				return false;
		}

		// Hawick's equation 10 - random site field
    void InitGrowthProbabilities(){
				random = new System.Random();
				for (int x = 0; x < gridSize; x++){
					for (int y = 0; y < gridSize; y++){
							float u = (float)random.NextDouble();//0 <= u < 1
							//x + 1 to avoid dividing by zero
							float Px_y = (u * p) + ((1 - p) * ((gridSize - x + 1)/(x + 1)));
							randomField[x, y] = Px_y;
					}
				}
		}

		//returns true if reached RHS of grid
		bool ErodeCell(GameObject cell){
				if (cell != null){
					int x = (int)cell.transform.position.x;
					int y = (int)cell.transform.position.y;
					GameObject.Destroy(cell);
					float key = randomField[x, y];
					rankedGrowthSites.Remove(key);
					numGrowthSites -= 1;

					GameObject _cell = Instantiate(waterCellPrefab, transform);
					_cell.GetComponent<WaterCell>().SetCellColor(0.75f);
					_cell.transform.position = new Vector3(x, y, 0);
					cells[x, y] = _cell;
					if (x == gridSize - 1){
						return true;
					}
				}
				return false;
		}

		GameObject[] GetNeighboursAtPos(int x, int y){
			GameObject[] neighbours = new GameObject[4];
			for (int i = 0; i < 4; i++){
					// neighbour coordinates
					int n_x = x + neighbour_positions[i,0];
					int n_y = y + neighbour_positions[i,1];
					if (IsValidPosition(n_x, n_y) == true){
						neighbours[i] = cells[n_x, n_y];
					} else{
						neighbours[i] = null;
					}
			}
			return neighbours;
		}

		//should only take land cells as growth sites
		//https://learn.microsoft.com/en-us/dotnet/api/system.collections.sortedlist?view=net-7.0
		void UpdateRankedGrowthSites(GameObject[] growthSites){
			foreach(GameObject site in growthSites){
				if (site != null){
					int x = (int)site.transform.position.x;
					int y = (int)site.transform.position.y;
					float growthProbability = randomField[x, y];
					//if it isn't already in the list
					if(!rankedGrowthSites.ContainsKey(growthProbability)){
							rankedGrowthSites.Add(growthProbability, site);
							numGrowthSites += 1;
					}
				}
			}
		}

		void ConstructRankedGrowthSites(){
				numGrowthSites = 0;
				for (int x = 0; x < gridSize; x++){
					for (int y = 0; y < gridSize; y++){
						//find neighbouring defender sites to water sites
						if (IsDefender(cells[x, y]) == false){
								GameObject[] neighbours = GetNeighboursAtPos(x, y);
								GameObject[] growthSites = IdentifyGrowthSites(neighbours);
								UpdateRankedGrowthSites(growthSites);
						}
					}
				}
		}

		GameObject FindLargestGrowthSite(){
				if (numGrowthSites == 0){
					return null;
				}
				GameObject growthSite = rankedGrowthSites.Values[numGrowthSites - 1];
				return growthSite;
		}

		//identifies growth sites from array of neighbours
	GameObject[] IdentifyGrowthSites(GameObject[] neighbours){
				GameObject[] growthSites = new GameObject[4];
				// for all 4 neighbours
				for (int i = 0; i < 4; i++){
						GameObject cell = neighbours[i];
						//if the neighbour exists
						if (cell != null){
								// and if its a defender (land)
								if (IsDefender(cell) == true){
										// add it to list of potential growth sites
										growthSites[i] = cell;
								} else{
										growthSites[i] = null;
								}
						} else {
								growthSites[i] = null;
						}
				}
				return growthSites;
		}

		public GameObject[,] RunInvasionPercolation(int _gridSize, GameObject[,] _cells){
				gridSize = _gridSize;
				randomField = new float[gridSize, gridSize];
				cells = _cells;

				random = new System.Random();
				p = -1f;

				InitGrowthProbabilities();
				ConstructRankedGrowthSites();

				bool reachedRHS = false;
				while(reachedRHS == false){
						GameObject cell = FindLargestGrowthSite();
						reachedRHS = ErodeCell(cell);
						int x = (int)cell.transform.position.x;
						int y = (int)cell.transform.position.y;
						GameObject[] neighbours = GetNeighboursAtPos(x, y);
						GameObject[] growthSites = IdentifyGrowthSites(neighbours);
						UpdateRankedGrowthSites(growthSites);
				}
				Debug.Log("Exited while loop");
				return cells;
		}
}
