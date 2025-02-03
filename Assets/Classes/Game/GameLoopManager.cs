using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Jobs;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameLoopManager : MonoBehaviour
{
    public static Vector3[] NodePositions;

    public static Queue<ApplyEffectData> EffectsQueue;
    public static Queue<EnemyDamageData> DamageData;
    public static Queue<Enemy> EnemiesToRemove;
    public static Queue<int> EnemyIDsToSummon;

    public static float[] NodeDistances;
    public static List<TowerBehaviour> TowersInGame; // was NodeBehaviour

    private PlayerStats PlayerStatistics;

    public Transform NodeParent;
    public bool LoopShouldEnd;

    public static void ClearAllQueues()
    {
        EnemiesToRemove.Clear();
        DamageData.Clear();
        EnemyIDsToSummon.Clear();
        EffectsQueue.Clear();
    }

    void Start()
    {
        PlayerStatistics = FindObjectOfType<PlayerStats>();
        EffectsQueue = new Queue<ApplyEffectData>();
        DamageData = new Queue<EnemyDamageData>();
        TowersInGame = new List<TowerBehaviour>();  // was NodeBehaviour

        EnemiesToRemove = new Queue<Enemy>();
        EnemyIDsToSummon = new Queue<int>();
        EntitySummoner.Init();

        NodePositions = new Vector3[NodeParent.childCount];
        for (int i = 0; i < NodePositions.Length; i++)
        {
            NodePositions[i] = NodeParent.GetChild(i).position;
        }

        NodeDistances = new float[NodePositions.Length - 1];
        for (int i = 0; i < NodeDistances.Length; i++)
        {
            NodeDistances[i] = Vector3.Distance(NodePositions[i], NodePositions[i + 1]);
        }

        StartCoroutine(GameLoop());

        // Задержка перед началом создания врагов
        Invoke("StartSummoning", 4f);
    }

    void StartSummoning()
    {
        // Создание врагов в два раза реже (каждые 1 секунды)
        InvokeRepeating("SummonTest", 0f, 0.5f);
    }

    void SummonTest()
    {
        // Случайный выбор ID врага
        int randomEnemyID = Random.Range(1, 4); // 1, 2, 3
        EnqueueEnemyIDToSummon(randomEnemyID);
    }

    IEnumerator GameLoop() 
    {
        while (!LoopShouldEnd) {

            // Spawn Enemies
            if (EnemyIDsToSummon.Count > 0)
            {
                for (int i = 0; i < EnemyIDsToSummon.Count; i++)
                {
                    EntitySummoner.SummonEnemy(EnemyIDsToSummon.Dequeue());
                }
            }

            // Spawn Towers

            // Move enemies
            NativeArray<Vector3> NodesToUse = new NativeArray<Vector3>(NodePositions, Allocator.TempJob);
            NativeArray<float> EnemySpeeds = new NativeArray<float>(EntitySummoner.EnemiesInGame.Count, Allocator.TempJob);
            NativeArray<int> NodeIndices = new NativeArray<int>(EntitySummoner.EnemiesInGame.Count, Allocator.TempJob);
            TransformAccessArray EnemyAccess = new TransformAccessArray(EntitySummoner.EnemiesInGameTransform.ToArray(), 2);

            for (int i = 0; i < EntitySummoner.EnemiesInGame.Count; i++)
            {
                EnemySpeeds[i] = EntitySummoner.EnemiesInGame[i].Speed;
                NodeIndices[i] = EntitySummoner.EnemiesInGame[i].NodeIndex;
            }

            MoveEnemiesJob MoveJob = new MoveEnemiesJob
            {
                NodePositions = NodesToUse,
                EnemySpeed = EnemySpeeds,
                NodeIndex = NodeIndices,
                deltaTime = Time.deltaTime
            };
            JobHandle MoveJobHandle = MoveJob.Schedule(EnemyAccess);
            MoveJobHandle.Complete();

            for (int i = 0; i < EntitySummoner.EnemiesInGame.Count; i++)
            {
                EntitySummoner.EnemiesInGame[i].NodeIndex = NodeIndices[i];
                if (EntitySummoner.EnemiesInGame[i].NodeIndex >= NodePositions.Length)
                {
                    EnqueueEnemyToRemove(EntitySummoner.EnemiesInGame[i]);

                    ClearAllQueues();
                    
                    SceneManager.LoadScene("defeat");
                }
            }

            NodesToUse.Dispose();
            EnemySpeeds.Dispose();
            NodeIndices.Dispose();
            EnemyAccess.Dispose();

            // Apply Effects


            // Remove Towers


            // Tick Towers
            foreach (TowerBehaviour tower in TowersInGame)
            {
                tower.Target = TowerTargeting.GetTarget(tower, TowerTargeting.TargetType.Close);
                tower.Tick();
            }

            // Apply Effects
            if (EffectsQueue.Count > 0)
            {
                for (int i = 0; i < EffectsQueue.Count; i++)
                {
                    ApplyEffectData CurrentDamageData = EffectsQueue.Dequeue();

                    Effect EffectDuplicate = CurrentDamageData.EnemyToAffect.ActiveEffects.Find( x => x.EffectName == CurrentDamageData.EffectToApply.EffectName );
                    if (EffectDuplicate == null) 
                    {
                        CurrentDamageData.EnemyToAffect.ActiveEffects.Add(CurrentDamageData.EffectToApply);
                    }
                    else
                    {
                        EffectDuplicate.ExpireTime = CurrentDamageData.EffectToApply.ExpireTime;
                    }
                }
            }

            // Tick Enemies
            foreach (Enemy CurrentEnemy in EntitySummoner.EnemiesInGame)
            {
                CurrentEnemy.Tick();
            }

            // Damage Enemies
            if (DamageData.Count > 0)
            {
                for (int i = 0; i < DamageData.Count; i++)
                {
                    EnemyDamageData CurrentDamageData = DamageData.Dequeue();
                    CurrentDamageData.TargetedEnemy.Health -= CurrentDamageData.TotalDamage / CurrentDamageData.Resistance;

                    PlayerStatistics.AddMoney( (int)CurrentDamageData.TotalDamage );

                    if (CurrentDamageData.TargetedEnemy.Health <= 0)
                    {
                        EnqueueEnemyToRemove(CurrentDamageData.TargetedEnemy);
                    }
                }
            }

            // Remove Enemies DOUBLE
            if (EnemiesToRemove.Count > 0)
            {
                for (int i = 0; i <EnemiesToRemove.Count; i++)
                {
                    EntitySummoner.RemoveEnemy(EnemiesToRemove.Dequeue());
                }
            }

            yield return null;

        }
    }

    public static void EnqueueEffectToApply(ApplyEffectData effectData)
    {
        EffectsQueue.Enqueue(effectData);
    }

    public static void EnqueueDamageData(EnemyDamageData damageData)
    {
        DamageData.Enqueue(damageData);
    }

    public static void EnqueueEnemyIDToSummon(int ID)
    {
        EnemyIDsToSummon.Enqueue(ID);
    }

    public static void EnqueueEnemyToRemove(Enemy EnemyToRemove)
    {
        EnemiesToRemove.Enqueue(EnemyToRemove);
    }
}

public struct EnemyDamageData
{
    public EnemyDamageData(Enemy target, float damage, float resistance)
    {
        TargetedEnemy = target;
        TotalDamage = damage;
        Resistance = resistance;
    }

    public Enemy TargetedEnemy;
    public float TotalDamage;
    public float Resistance;
}

public class Effect
{
    public Effect(string effectName, float damageRate, float damage, float expireTime)
    {
        EffectName = effectName;
        DamageRate = damageRate;
        Damage = damage;
        ExpireTime = expireTime;
    }

    public string EffectName;
    public float Damage;
    public float ExpireTime;
    public float DamageRate;
    public float DamageDelay;
}

public struct ApplyEffectData 
{
    public ApplyEffectData(Enemy enemyToAffect, Effect effectToApply)
    {
        EnemyToAffect = enemyToAffect;
        EffectToApply = effectToApply;
    }

    public Enemy EnemyToAffect;
    public Effect EffectToApply;
}

public struct MoveEnemiesJob : IJobParallelForTransform
{
    [NativeDisableParallelForRestriction]
    public NativeArray<Vector3> NodePositions;

    [NativeDisableParallelForRestriction]
    public NativeArray<int> NodeIndex;
    
    [NativeDisableParallelForRestriction]
    public NativeArray<float> EnemySpeed;

    public float deltaTime;
        
    public void Execute(int idx, TransformAccess transform)
    {
        if (NodeIndex[idx] < NodePositions.Length)
        {
            Vector3 PositionToMoveTo = NodePositions[ NodeIndex[idx] ];
            transform.position = Vector3.MoveTowards(transform.position, PositionToMoveTo, EnemySpeed[idx] * deltaTime);

            if (transform.position == PositionToMoveTo)
            {
                NodeIndex[idx]++;
            }
        }

    }
}