using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

// Site Instantiation Process described in report (algorithm 2.1)
public class InstantiateGridCells : MonoBehaviour
{
	public int gridSize;// width 'w' as discussed in report

	public GameObject waterCellPrefab;// prefabs attached to script from inside Unity editor
	public GameObject landCellPrefab;

	bool IsLandPos(Vector2 pos){
		if (pos.x >= gridSize / 2){//if position is in RHS of grid, site should be land
			return true;
		}
		return false;
	}
	// instantiate water cell from water cell prefab (tagged 'WaterCell'), positioning tile (prefab is already coloured brown)
	GameObject InstantiateLandCell(Vector2 pos){
		GameObject cell = Instantiate(landCellPrefab, transform);//instantiate cell from prefab
		cell.transform.position = new Vector3(pos.x, pos.y, 0);//transforming to correct position
		// colouring blue for testing kawasaki diffusion on 50/50 bar
		// cell.GetComponent<SpriteRenderer>().color = Color.blue;
		return cell;
	}

	// instantiate water cell from water cell prefab (tagged 'WaterCell'), positioning and colouring the tile
	GameObject InstantiateWaterCell(Vector2 pos){
		GameObject cell = Instantiate(waterCellPrefab, transform);//instantiate cell from prefab
		cell.transform.position = new Vector3(pos.x, pos.y, 0);//transforming to correct position
		cell.GetComponent<SpriteRenderer>().color = new Color(0, 255, 255);//colouring cyan for water
		// colouring white for testing kawasaki diffusion on 50/50 bar
		// cell.GetComponent<SpriteRenderer>().color = Color.blue;
		return cell;
	}

	// instantiate all sites in the simulation - called from RunSimulation.cs
	public GameObject[,] InstantiateGrid(int _gridSize){
			gridSize = _gridSize;// width 'w' - inputted from RunSimulation.cs
			GameObject[,] _cells = new GameObject[gridSize, gridSize]; //initialise 2D array for all sites in the simulation
			// for all site positions
			for (int x = 0; x < gridSize; x++){
				for (int y = 0; y < gridSize; y++){
					Vector2 vector_pos = new Vector2(x, y);// create vector for coordinates
					if (IsLandPos(vector_pos)){// if the site should be land
						_cells[x,y] = InstantiateLandCell(vector_pos);// instantiate a land cell
					} else{
						_cells[x,y] = InstantiateWaterCell(vector_pos);// otherwise instantiate water
					}
				}
			}
			// returns to RunSimulation.cs
			return _cells;
	}
}
