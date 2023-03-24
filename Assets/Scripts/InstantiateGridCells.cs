using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

public class InstantiateGridCells : MonoBehaviour
{
	public int gridSize;

	public GameObject waterCellPrefab;
	public GameObject landCellPrefab;
	public GameObject[,] cells;

	void AdjustCamera(){
		//adjusting camera to view entire grid
		int orthographicSize = (gridSize)/2;
		float offset = 0.5f;

		Camera.main.orthographicSize = orthographicSize;//(/1) - assuming always square aspect ratio (1/1 = 1)
		Camera.main.transform.position = new Vector3(orthographicSize - offset, orthographicSize - offset, -10);//subtracts offset to centre grid
	}

	//may change rule later
	bool IsRockCell(Vector2 pos){
		if (pos.x >= gridSize / 2){
			return true;
		}
		return false;
	}

	void InstantiateLandCell(Vector2 pos){
		GameObject cell = Instantiate(landCellPrefab, transform);
		cell.transform.position = new Vector3(pos.x, pos.y, 0);
		cells[(int)pos.x, (int)pos.y] = cell;
	}

	void InstantiateWaterCell(Vector2 pos){
		GameObject cell = Instantiate(waterCellPrefab, transform);
		cell.transform.position = new Vector3(pos.x, pos.y, 0);
		WaterCell waterCell = cell.GetComponent<WaterCell>();
		waterCell.SetCellColor(0.75f);
		cells[(int)pos.x, (int)pos.y] = cell;
	}

	void InstantiateGrid(){
			AdjustCamera();
			cells = new GameObject[gridSize, gridSize];
			for (int x = 0; x < gridSize; x++){
				for (int y = 0; y < gridSize; y++){
					Vector2 vector_pos = new Vector2(x, y);
					//if position is in RHS of grid
					if (IsRockCell(vector_pos)){
						InstantiateLandCell(vector_pos);
					} else{
						InstantiateWaterCell(vector_pos);
					}
				}
			}
	}

	void DestroyCells(){
		foreach(GameObject cell in cells){
			if (cell != null){
				GameObject.Destroy(cell);
			}
		}
	}

	void Start(){
		gridSize = 64;
		InstantiateGrid();
		GameObject[,] newCells = GetComponent<KawasakiDiffusion>().RunKawasakiDiffusion(gridSize, cells);
		DestroyCells();
		cells = newCells;
	}

	void Update()
	{
	}
}
