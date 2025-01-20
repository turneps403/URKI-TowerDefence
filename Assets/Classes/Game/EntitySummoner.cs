using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntitySummoner : MonoBehaviour
{
    public static List<Enemy> EnemiesInGame;
    public static List<Transform> EnemiesInGameTransform;
    public static Dictionary<int, GameObject> EnemyPrefabs;
    public static Dictionary<int, Queue<Enemy>> EnemyObjectPools;

    public static Dictionary<Transform, Enemy> EnemyTransformPairs;

    private static bool IsInitialized;

    public static void Init()
    {
        if (!IsInitialized)
        {
            EnemyTransformPairs = new Dictionary<Transform, Enemy>();
            EnemyPrefabs = new Dictionary<int, GameObject>();
            EnemyObjectPools = new Dictionary<int, Queue<Enemy>>();
            EnemiesInGame = new List<Enemy>();
            EnemiesInGameTransform = new List<Transform>();

            EnemySummonData[] Enemies = Resources.LoadAll<EnemySummonData>("Enemies");
            // Debug.Log(Enemies[0].name);

            foreach (EnemySummonData enemy in Enemies)
            {
                EnemyPrefabs.Add(enemy.EnemyID, enemy.EnemyPrefab);
                EnemyObjectPools.Add(enemy.EnemyID, new Queue<Enemy>());
            }

            IsInitialized = true;
        }
        else
        {
            Debug.Log("EntitySummoner class is already initialized");
        }
    }

    public static Enemy SummonEnemy(int EnemyID)
    {
        Enemy SummonedEnemmy = null;
        if (EnemyPrefabs.ContainsKey(EnemyID))
        {
            Queue<Enemy> ReferencedQueue = EnemyObjectPools[EnemyID];
            if (ReferencedQueue.Count > 0)
            {
                // Deque enemy and initialize
                SummonedEnemmy = ReferencedQueue.Dequeue();
                SummonedEnemmy.Init();
                SummonedEnemmy.gameObject.SetActive(true);
            }
            else
            {
                // Instantiate new enemy and initialize
                GameObject NewEnemy = Instantiate( EnemyPrefabs[EnemyID], GameLoopManager.NodePositions[0], Quaternion.identity );
                SummonedEnemmy = NewEnemy.GetComponent<Enemy>();
                SummonedEnemmy.Init();

            }
            SummonedEnemmy.ID = EnemyID;
            if (!EnemiesInGameTransform.Contains(SummonedEnemmy.transform))
                EnemiesInGameTransform.Add(SummonedEnemmy.transform);
            if (!EnemiesInGame.Contains(SummonedEnemmy))
                EnemiesInGame.Add(SummonedEnemmy);
            if (!EnemyTransformPairs.ContainsKey(SummonedEnemmy.transform))
                EnemyTransformPairs.Add(SummonedEnemmy.transform, SummonedEnemmy);
        }
        else
        {
            Debug.Log("EntitySummoner hasnt enemy with EnemyID = {EnemyID}");
        }
        return SummonedEnemmy;
    }

    public static void RemoveEnemy(Enemy EnemyToRemove)
    {
        EnemyObjectPools[EnemyToRemove.ID].Enqueue(EnemyToRemove);
        EnemyToRemove.gameObject.SetActive(false);

        EnemyTransformPairs.Remove(EnemyToRemove.transform);
        EnemiesInGameTransform.Remove(EnemyToRemove.transform);
        EnemiesInGame.Remove(EnemyToRemove);
    }

}
