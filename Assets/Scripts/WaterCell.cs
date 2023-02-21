using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterCell : MonoBehaviour
{
    // Start is called before the first frame update
		private SpriteRenderer spriteRenderer;
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

		public void SetCellColor(Color color){
			spriteRenderer = GetComponent<SpriteRenderer>();
			spriteRenderer.color = color;
		}
}
