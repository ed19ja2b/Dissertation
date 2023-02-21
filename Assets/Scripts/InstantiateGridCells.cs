using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InstantiateGridCells : MonoBehaviour
{
	public int gridSize;
	public int cellSize;//units

	public GameObject waterCellPrefab;
	public GameObject rockCellPrefab;


	void Start()
	{
			cellSize = 1;
			gridSize = 50;

			//adjusting camera to view entire grid
			Camera.main.orthographicSize = (gridSize * cellSize) / 2;//assuming always square aspect ratio (1/1 = 1)
			Camera.main.transform.position = new Vector3((gridSize * cellSize)/2 - 0.5f,(gridSize * cellSize)/2 - 0.5f, -10);//subtracts offset to centre grid

			//instantiating cells
			for (int x = 0; x < gridSize; x++){
					for (int y = 0; y < gridSize; y++){

							//instantiating rock cells based on distance from center of grid
							float distanceFromCenter = Vector2.Distance(new Vector2(x, y), new Vector2(gridSize/2,gridSize/2));
							if (distanceFromCenter < gridSize/4){//if within a central circle, instantiate as a rock cell
								GameObject cell = Instantiate(rockCellPrefab, transform);
								cell.transform.position = new Vector3(x * cellSize, y * cellSize, 0);
							}
							else{//otherwise, instantiate water cell
								GameObject cell = Instantiate(waterCellPrefab, transform);
								cell.transform.position = new Vector3(x * cellSize, y * cellSize, 0);
								//set water cell colour
								// ************ - assign based on water cell depth
								WaterCell waterCell = cell.GetComponent<WaterCell>();
								if (waterCell != null){
									Color randomBlue = Random.ColorHSV(241f / 360f, 260f / 360f, 1f, 1f, 0.6f, 1f);
									waterCell.SetCellColor(randomBlue);
								}
							}

					}
			}
	}

    // Update is called once per frame
    void Update()
    {

    }
}
