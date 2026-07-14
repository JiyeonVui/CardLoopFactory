using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LevelManager
{
    public List<TrayModel> TrayModels;

    // Vị trí vật lý các slot match-color trên sân (đọc từ level JSON).
    public List<Vector3> MatchSlotPositions;

    // Góc xoay (euler độ) các slot match-color, song song theo index với
    // MatchSlotPositions. Dùng để play anim card khi xếp vào slot.
    public List<Vector3> MatchSlotRotations;

    // Số slot match-color được phép active cùng lúc (có thể nhỏ hơn số vị trí ở trên).
    public int MaxActiveMatchColor;

    // Waypoint quỹ đạo belt (chữ U): các đỉnh gấp khúc, BeltPath sẽ bo tròn góc.
    public List<Vector3> BeltPathPoints;

    // Bán kính bo góc tại các đỉnh belt và tốc độ card chạy trên belt.
    public float BeltCornerRadius;
    public float BeltVelocity;

    // Số card tối đa được phép có mặt trên belt cùng lúc. <= 0 hoặc thiếu trong JSON
    // thì BeltController dùng mặc định (24).
    public int MaxCardsInBelt;

    // Loads a level JSON placed under any Resources folder (path without extension),
    // e.g. LoadFromResources("level_test") for Assets/Game/Resources/level_test.json.
    public static LevelManager LoadFromResources(string resourcePath)
    {
        TextAsset jsonAsset = Resources.Load<TextAsset>(resourcePath);
        if (jsonAsset == null)
        {
            Debug.LogError($"[LevelManager] Level file not found at Resources/{resourcePath}.");
            return null;
        }

        LevelManager level = JsonUtility.FromJson<LevelManager>(jsonAsset.text);

        // JSON has no TotalCardCount, so seed it from each tray's composition.
        foreach (TrayModel tray in level.TrayModels)
        {
            tray.RecalculateTotalCardCount();
        }

        return level;
    }
}
