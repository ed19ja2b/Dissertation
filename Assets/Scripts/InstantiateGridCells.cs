using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

public class InstantiateGridCells : MonoBehaviour
{
	const float maxDepth = 30.0f;//in meters (45ft/13.7m average ish)- find solid source for this

	private int _gridSize;
	private int _cellSize;
	public int gridSize;
	public int cellSize;//units

	public GameObject waterCellPrefab;
	public GameObject rockCellPrefab;

	void AdjustCamera(){
		//adjusting camera to view entire grid
		int orthographicSize = (gridSize * cellSize)/2;
		float offset = 0.5f;

		Camera.main.orthographicSize = orthographicSize;//(/1) - assuming always square aspect ratio (1/1 = 1)
		Camera.main.transform.position = new Vector3(orthographicSize - offset, orthographicSize - offset, -10);//subtracts offset to centre grid
	}

	int IsRockCell(int x, int y){
			float distanceFromCenter = Vector2.Distance(new Vector2(x, y), new Vector2(gridSize/2,gridSize/2));
			if (distanceFromCenter < gridSize/4){//if within a central circle, instantiate as a rock cell
					return 0;
			}
			return -1;
	}

	void InstantiateGrid(){
			AdjustCamera();

			//instantiating cells
			for (int x = 0; x < gridSize; x++){
					for (int y = 0; y < gridSize; y++){
							//instantiating rock cells based on distance from center of grid
							if (IsRockCell(x,y) == 0){//if within a central circle, instantiate as a rock cell
								GameObject cell = Instantiate(rockCellPrefab, transform);
								cell.transform.position = new Vector3(x * cellSize, y * cellSize, 0);
							}
							else{//otherwise, instantiate water cell
								GameObject cell = Instantiate(waterCellPrefab, transform);
								cell.transform.position = new Vector3(x * cellSize, y * cellSize, 0);

								//set water cell colour
								// ************ later - assign based on water cell depth
								WaterCell waterCell = cell.GetComponent<WaterCell>();
								if (waterCell != null){
									float distanceFromCenter = Vector2.Distance(new Vector2(x, y), new Vector2(gridSize/2,gridSize/2));
									float maxDistance = (float)Math.Sqrt(2 * (Math.Pow((gridSize/2), 2)));
									float waterDepth = (distanceFromCenter/maxDistance) * maxDepth;
									waterCell.SetDepth(waterDepth);
								}
							}
					}
			}
	}

	void Start(){
			gridSize = 20;
			cellSize = 1;
			_gridSize = gridSize;
			_cellSize = cellSize;
			InstantiateGrid();
	}

	void Update()
	{
			if (gridSize != _gridSize || cellSize != _cellSize){
					GameObject[] cells = GameObject.FindGameObjectsWithTag("WaterCell");
					cells = cells.Concat(GameObject.FindGameObjectsWithTag("RockCell")).ToArray();
					foreach(GameObject cell in cells)
					GameObject.Destroy(cell);
					InstantiateGrid();
					_gridSize = gridSize;
					_cellSize = cellSize;
			}
	}
}
