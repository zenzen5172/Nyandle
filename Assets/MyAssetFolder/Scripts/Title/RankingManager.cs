using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.LowLevelPhysics2D.PhysicsLayers;



[System.Serializable]
public class ClearRecord
{
    public string playerName; // ★追加
    public float time;
    public int activeTorches;
    public int totalTorches;
}

[System.Serializable]
public class RankingData
{
    public List<ClearRecord> records = new List<ClearRecord>();
}

public enum RankingSortMode
{
    ByTime,
    ByTorches
}

public static class RankingManager
{
    private const string TIME_RANKING_KEY = "GameRankingData_Time";
    private const string TORCH_RANKING_KEY = "GameRankingData_Torch";

    // ★ MAX_RANKING_COUNT（上限）を削除

    public static void AddRecord(string playerName,float clearTime, int activeTorches, int totalTorches)
    {
        ClearRecord newRecord = new ClearRecord
        {
            playerName = string.IsNullOrEmpty(playerName) ? "名無し" : playerName,
            time = clearTime,
            activeTorches = activeTorches,
            totalTorches = totalTorches
        };

        RankingData timeData = LoadRanking(RankingSortMode.ByTime);
        timeData.records.Add(newRecord);
        timeData.records.Sort((a, b) =>
        {
            int timeCompare = a.time.CompareTo(b.time);
            if (timeCompare != 0) return timeCompare;
            return b.activeTorches.CompareTo(a.activeTorches);
        });
        SaveData(timeData, TIME_RANKING_KEY);

        RankingData torchData = LoadRanking(RankingSortMode.ByTorches);
        torchData.records.Add(newRecord);
        torchData.records.Sort((a, b) =>
        {
            int torchCompare = b.activeTorches.CompareTo(a.activeTorches);
            if (torchCompare != 0) return torchCompare;
            return a.time.CompareTo(b.time);
        });
        SaveData(torchData, TORCH_RANKING_KEY);
    }

    // ★ 5件で切り捨てる処理を撤廃し、そのまま全件保存する
    private static void SaveData(RankingData data, string key)
    {
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(key, json);
        PlayerPrefs.Save();
    }

    public static RankingData LoadRanking(RankingSortMode mode)
    {
        string key = (mode == RankingSortMode.ByTime) ? TIME_RANKING_KEY : TORCH_RANKING_KEY;

        if (PlayerPrefs.HasKey(key))
        {
            string json = PlayerPrefs.GetString(key);
            RankingData data = JsonUtility.FromJson<RankingData>(json);
            if (data != null && data.records != null)
            {
                return data;
            }
        }
        return new RankingData();
    }

    public static void ClearAllRankings()
    {
        PlayerPrefs.DeleteKey(TIME_RANKING_KEY);
        PlayerPrefs.DeleteKey(TORCH_RANKING_KEY);
        PlayerPrefs.Save();
    }
}