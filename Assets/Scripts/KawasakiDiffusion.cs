using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class KawasakiDiffusion : MonoBehaviour
{
		private System.Random random;
		public GameObject[,] cells;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
		//https://www.researchgate.net/profile/Ken-Hawick/publication/287274420_Modelling_Flood_Incursion_and_Coastal_Erosion_using_Cellular_Automata_Simulations/links/5ed7d6e845851529452b0e83/Modelling-Flood-Incursion-and-Coastal-Erosion-using-Cellular-Automata-Simulations.pdf
		public void RunKawasakiDiffusion(int gridSize, GameObject[,] cells){
			random = new System.Random();
			// random iteration in for loop - https://stackoverflow.com/questions/13457917/random-iteration-in-for-loop
			// enumerate over two variables - https://stackoverflow.com/questions/14516537/can-we-use-multiple-variables-in-foreach
			// loop over all cells in random order - cells inputted from InstantiateGridCells.cs InstantiateGrid()
			foreach (var pos in Enumerable.Range(0, gridSize).OrderBy(x => random.Next()).Zip(Enumerable.Range(0, gridSize).OrderBy(x => random.Next()), (x, y) => new { x, y })){
				int x = pos.x;
				int y = pos.y;
				int randNeighbourIndex = random.Next(0, 4);//choose random neighbour (Moore neighbourhood NESW)
				int randNeighbourX, randNeighbourY = 0;
				GameObject waterCell = cells[x,y];
				WaterCell _waterCell = waterCell.GetComponent<WaterCell>();

				//select the random neighbour
				if (randNeighbourIndex % 2 == 0){
					randNeighbourX = (int)_waterCell.position.x;//if neighbour is above or below, x stays the same
					//if randNeighbourIndex == 0 (north neighbour), y decreases, otherwise randNeighbourIndex == 2 (south neighbour) and we increase y
					//if randNeighbourIndex == 0: (-1 + 0 = -1 (decreases)) | if randNeighbourIndex == 2 (-1 + 2 = 1 (increases))
					randNeighbourY = (int)_waterCell.position.y - 1 + randNeighbourIndex;//therefore y either decreases by 1 or increases by 1
				} else{
					randNeighbourY = (int)_waterCell.position.y;//if neighbour is left or right, y stays the same
					//if randNeighbourIndex == 1 (east neighbour), x decreases, otherwise randNeighbourIndex == 3 (west neighbour) and we increase x
					//if randNeighbourIndex == 1: (-2 + 1 = -1 (decreases)) | if randNeighbourIndex == 3 (-2 + 3 = 1 (increases))
					randNeighbourX = (int)_waterCell.position.x - 2 + randNeighbourIndex;//therefore y either decreases by 1 or increases by 1
				}

				Vector3 randomNeighbourPosition = new Vector3 (randNeighbourX, randNeighbourY, 0);
				GameObject neighbourCell = cells[randNeighbourX,randNeighbourY];
				print("Successful");
			}
		}
}
