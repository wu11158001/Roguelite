using System;
using UnityEngine;
public enum FORMATION_TYPE
{
    RENDOMS ,      //隨機
    WEDGE,       //楔形陣
    SQUARE,      //方形陣
    CIRCULAR,    //環形陣
}
public static class FormationUtils 
{
    /// <summary>
    /// 根據陣型種類，動態計算所有怪物的初始相對坐標
    /// </summary>
    /// <param name="type">陣型種類</param>
    /// <param name="count">怪物總數（可為任意正整數）</param>
    /// <param name="spacing">間距（X代表橫向/半徑間距，Y代表縱向/層級間距）</param>
    /// <returns>相對位置的 Vector3 陣列（Y 軸預設為 0，適用於平地地圖）</returns>
    public static Vector3[] GeneratePositions(FORMATION_TYPE type, int count, Vector2 spacing)
    {
        if (count <= 0) return Array.Empty<Vector3>();

        switch (type)
        {
            case FORMATION_TYPE.WEDGE:
                return GetWedgePositions(count, spacing);
            case FORMATION_TYPE.SQUARE:
                return GetSquarePositions(count, spacing);
            case FORMATION_TYPE.CIRCULAR:
                return GetCircularPositions(count, spacing);
            default:
                return Array.Empty<Vector3>();
        }
    }
    // ==================== 1. 楔形陣（Wedge） ====================
    private static Vector3[] GetWedgePositions(int count, Vector2 spacing)
    {
        Vector3[] positions = new Vector3[count];
        int spawned = 0;
        int row = 0;

        // 一排一排往後排，第 0 排 1 隻，第 1 排 2 隻... 就算數量不夠完美，也會依序填滿
        while (spawned < count)
        {
            int rowCount = row + 1; // 當前這一排「理想上」有幾隻怪

            // 計算這一排的寬度中點，用來靠中對齊
            float rowLeftOffset = (rowCount - 1) * spacing.x * 0.5f;

            for (int i = 0; i < rowCount; i++)
            {
                if (spawned >= count) break; // 數量不夠了，直接中斷，滿足不完美排列

                // 計算每隻怪物的 X 與 Z (或Y)
                float x = (i * spacing.x) - rowLeftOffset;
                float z = -row * spacing.y; // 往前方/後方延伸，這裡 -row 代表向後方排

                positions[spawned] = new Vector3(x, 0f, z);
                spawned++;
            }
            row++;
        }

        return positions;
    }

    // ==================== 2. 方形陣（Square） ====================
    private static Vector3[] GetSquarePositions(int count, Vector2 spacing)
    {
        Vector3[] positions = new Vector3[count];

        // 自動根據總數開根號來決定寬度（例：6隻怪會排成 3x2 的陣型）
        int columns = Mathf.CeilToInt(Mathf.Sqrt(count));

        for (int i = 0; i < count; i++)
        {
            int col = i % columns; // 哪一列 (X)
            int row = i / columns; // 哪一行 (Z)

            // 靠中對齊計算
            float xOffset = (columns - 1) * spacing.x * 0.5f;

            // 計算當前行（最後一排）可能沒有填滿時的靠中靠齊（自由選擇是否開啟）
            int currentRowCount = Mathf.Min(columns, count - (row * columns));
            float currentRowOffset = (currentRowCount - 1) * spacing.x * 0.5f;

            float x = (col * spacing.x) - currentRowOffset;
            float z = -row * spacing.y;

            positions[i] = new Vector3(x, 0f, z);
        }

        return positions;
    }

    // ==================== 3. 環形陣（Circular） ====================
    private static Vector3[] GetCircularPositions(int count, Vector2 spacing)
    {
        Vector3[] positions = new Vector3[count];

        // 在環形陣中，spacing.x 代表半徑（或者每多一圈增加的半徑），這裡簡單視作基本半徑
        // 若怪太多，一圈擠不下，此處採用的邏輯是均勻分布在一圈（可自由改成多層環形）
        float radius = spacing.x;

        // 如果數量太多，自動放大半徑，確保怪物之間不會重疊（間距防擠壓機制）
        if (count > 8)
        {
            radius = (count * spacing.x) / (2 * Mathf.PI);
        }

        for (int i = 0; i < count; i++)
        {
            // 將一圈 360 度 (2 * PI 弧度) 均分給所有怪物
            float angle = i * (2 * Mathf.PI) / count;

            float x = Mathf.Sin(angle) * radius;
            float z = Mathf.Cos(angle) * radius;

            positions[i] = new Vector3(x, 0f, z);
        }

        return positions;
    }
}
