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

	private float _current_direction;
	private float _current_strength;
	public float current_direction;//clockwise between 0 and 1 (1 wraps back around)
	public float current_strength;//m/s - between 1.0 and 2.5 - https://hypertextbook.com/facts/2002/EugeneStatnikov.shtml

	public GameObject waterCellPrefab;
	public GameObject rockCellPrefab;
	public GameObject[,] cells;

	void AdjustCamera(){
		//adjusting camera to view entire grid
		int orthographicSize = (gridSize * cellSize)/2;
		float offset = 0.5f;

		Camera.main.orthographicSize = orthographicSize;//(/1) - assuming always square aspect ratio (1/1 = 1)
		Camera.main.transform.position = new Vector3(orthographicSize - offset, orthographicSize - offset, -10);//subtracts offset to centre grid
	}

	int IsRockCell(int x, int y){
			// float distanceFromCenter = Vector2.Distance(new Vector2(x, y), new Vector2(gridSize/2,gridSize/2));
			// if (distanceFromCenter < gridSize/4){//if within a central circle, instantiate as a rock cell
			// 		return 0;
			// }
			if (x >= gridSize / 2){//right half 'rock'
				return 0;
			}
			return -1;
	}

	Vector2 InitVelocity(GameObject cell){
			Vector2 cellPosition = cell.transform.position;
			float turbulence = UnityEngine.Random.Range(0.9f, 1.1f); //will multiply velocity by this (+-10%)
			float velocity_x = current_strength * Mathf.Cos(current_direction * Mathf.PI * 2) * turbulence;
			float velocity_y = current_strength * Mathf.Sin(current_direction * Mathf.PI * 2) * turbulence;
			return new Vector2(velocity_x, velocity_y);
	}

	void InstantiateGrid(){
			AdjustCamera();
			cells = new GameObject[gridSize, gridSize];
			//instantiating cells
			for (int x = 0; x < gridSize; x++){
					for (int y = 0; y < gridSize; y++){
							//instantiating rock cells based on distance from center of grid
							if (IsRockCell(x,y) == 0){//if within a central circle, instantiate as a rock cell
								GameObject cell = Instantiate(rockCellPrefab, transform);
								cell.transform.position = new Vector3(x * cellSize, y * cellSize, 0);
								cells[x,y] = cell;
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

									Vector2 velocities = InitVelocity(cell);
									waterCell.SetVelocity(velocities);
									waterCell.SetDepth(waterDepth);
									cells[x,y] = cell;
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
			current_direction = 0.25f;
			current_strength = 1.5f;
			InstantiateGrid();
	}

	void Update()
	{
			if (gridSize != _gridSize || cellSize != _cellSize || current_direction != _current_direction || current_strength != _current_strength){
					GameObject[] cells = GameObject.FindGameObjectsWithTag("WaterCell");
					cells = cells.Concat(GameObject.FindGameObjectsWithTag("RockCell")).ToArray();
					foreach(GameObject cell in cells)
					GameObject.Destroy(cell);
					InstantiateGrid();
					_gridSize = gridSize;
					_cellSize = cellSize;
					_current_direction = current_direction;
					_current_strength = current_strength;
			}
	}
}
