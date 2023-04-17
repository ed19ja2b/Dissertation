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
	//if position is in RHS of grid
	bool IsRockPos(Vector2 pos){
		if (pos.x >= gridSize / 2){
			return true;
		}
		return false;
	}

	GameObject InstantiateLandCell(Vector2 pos){
		GameObject cell = Instantiate(landCellPrefab, transform);
		cell.transform.position = new Vector3(pos.x, pos.y, 0);
		//cell.GetComponent<SpriteRenderer>().color = new Color(.30f,.10f,.10f);
		return cell;
	}

	GameObject InstantiateWaterCell(Vector2 pos){
		GameObject cell = Instantiate(waterCellPrefab, transform);
		cell.transform.position = new Vector3(pos.x, pos.y, 0);
		cell.GetComponent<SpriteRenderer>().color = new Color(0, 255, 255);
		return cell;
	}

	GameObject[,] InstantiateGrid(){
			AdjustCamera();
			GameObject[,] _cells = new GameObject[gridSize, gridSize];
			for (int x = 0; x < gridSize; x++){
				for (int y = 0; y < gridSize; y++){
					Vector2 vector_pos = new Vector2(x, y);
					if (IsRockPos(vector_pos)){
						_cells[x,y] = InstantiateLandCell(vector_pos);
					} else{
						_cells[x,y] = InstantiateWaterCell(vector_pos);
					}
				}
			}
			return _cells;
	}

	void DestroyCells(){
		foreach(GameObject cell in cells){
			if (cell != null){
				GameObject.Destroy(cell);
			}
		}
	}

	void Start(){
		gridSize = 256;
		cells = InstantiateGrid();
		cells = GetComponent<InvasionPercolation>().RunInvasionPercolation(gridSize, cells, .25f);
		//cells = GetComponent<KawasakiDiffusion>().RunKawasakiDiffusion(gridSize, cells, 100);

	}

	void Update()
	{
	}
}
