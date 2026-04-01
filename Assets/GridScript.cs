using UnityEngine;
using System.Collections.Generic;

public class GridScript : MonoBehaviour
{
    [Header("Grid Settings")]
    public int gridSize = 40; 

    public float cellSize = 1f;
    public Color gridColor = Color.cyan;
    private HashSet<Vector2Int> occupiedCells = new HashSet<Vector2Int>();
    public bool IsCellEmpty(int x, int z)
    {
        Vector2Int targetPos = new Vector2Int(x, z);

        if (x < 0 || x >= gridSize || z < 0 || z >= gridSize)
        {
            Debug.Log("Click is outside grid bounds.");
            return false; 
        }
        return !occupiedCells.Contains(targetPos);
    }
    public void OccupyCell(int x, int z)
    {
        occupiedCells.Add(new Vector2Int(x, z));
    }
    public void VacateCell(int x, int z)
    {
        occupiedCells.Remove(new Vector2Int(x, z));
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = gridColor;


        float halfSize = (gridSize * cellSize) / 2f;
        Vector3 origin = transform.position;


        for (int i = 0; i <= gridSize; i++)
        {
            float offset = i * cellSize - halfSize;
            
           
            Vector3 startX = origin + new Vector3(offset, 0, -halfSize);
            Vector3 endX = origin + new Vector3(offset, 0, halfSize);
            Gizmos.DrawLine(startX, endX);

            
            Vector3 startZ = origin + new Vector3(-halfSize, 0, offset);
            Vector3 endZ = origin + new Vector3(halfSize, 0, offset);
            Gizmos.DrawLine(startZ, endZ);
        }
    }
}
