using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grid2<TGridObject>
{
    private int width;
    private int height;
    private float cellSize;
    private Vector3 originPosition;
    private int[,] gridArray;
    public Grid2(int width, int height, float cellSize, Vector3 originPosition)
    {
        this.width = width;
        this.height = height;
        this.cellSize = cellSize;
        this.originPosition = originPosition;

        gridArray = new int[width, height];
    }

    private Vector3 GetWorldPos(int x, int y)
    {
        return new Vector3(x, y) * cellSize + originPosition;
    }
    private void GetXY(Vector3 worldPosition, out int x, out int y)
    {
        x = Mathf.FloorToInt((worldPosition - originPosition).x / cellSize);
        y = Mathf.FloorToInt((worldPosition - originPosition).y / cellSize);
    }

    public void SetValue(int x, int y, int value)
    {
        if(x >= 0 && y >= 0 && x < width && y < height)
            gridArray[x, y] = value;
    }

    public void SetValue(Vector3 worldPosition, int value)
    {
        int x, y;
        GetXY(worldPosition, out x, out y);
        SetValue(x, y, value);
    }

    public int GetValue(int x, int y) {
        if (x >= 0 && y >= 0 && x < width && y < height)
            return gridArray[x, y];
        else
        {
            return 0;
        }
    }

    public int GetValue(Vector3 worldPosition)
    {
        int x, y;
        GetXY(worldPosition, out x, out y);
        return GetValue(x, y);
    }
}
