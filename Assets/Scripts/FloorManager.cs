using UnityEngine;

public class FloorGridManager : MonoBehaviour
{
    // 行數與列數
    public int rows = 5;
    public int columns = 5;

    // 地板的單位大小
    public float tileSize = 350f;

    void Start()
    {
        // 查找場景中所有帶有 "Ground" 標籤的 Plane
        GameObject[] groundPlanes = GameObject.FindGameObjectsWithTag("Ground");

        Debug.Log($"Number of Ground objects found: {groundPlanes.Length}");

        foreach (GameObject ground in groundPlanes)
        {
            Debug.Log($"Found ground object: {ground.name}");

            // 鋪設地板
            PlaceFloorTiles(ground);
        }
    }

    void PlaceFloorTiles(GameObject ground)
    {
        Renderer renderer = ground.GetComponent<Renderer>();
        if (renderer == null)
        {
            Debug.LogError($"Ground object {ground.name} does not have a Renderer component!");
            return;
        }

        // 獲取 ground 的邊界
        Bounds bounds = renderer.bounds;

        // 起始位置為 ground 的左下角
        Vector3 startPosition = bounds.min;

        // 循環鋪設地板
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                // 計算每塊地板的位置
                Vector3 tilePosition = startPosition + new Vector3(row * tileSize, 0, col * tileSize);

                Debug.Log($"Placing tile at position {tilePosition} for row {row}, column {col}");

                // 在該位置生成地板
                GameObject floorTile = GameObject.CreatePrimitive(PrimitiveType.Plane);

                // 縮放地板以匹配單塊地板的大小
                floorTile.transform.localScale = new Vector3(tileSize / 10f, 1, tileSize / 10f);

                // 設置地板位置
                floorTile.transform.position = tilePosition;

                // 設置地板為 ground 的子物件
                floorTile.transform.parent = ground.transform;

                // 命名地板
                floorTile.name = $"Tile_Row{row}_Col{col}";
            }
        }

        Debug.Log($"Finished placing tiles for {ground.name}");
    }
}