using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class TowerTargeting
{
    public enum TargetType 
    {
        First,
        Last,
        Close
    }

    public static Enemy GetTarget(TowerBehaviour CurrentTower, TargetType TargetMethod)
    {
        Collider[] EnemiesInRange = Physics.OverlapSphere(CurrentTower.transform.position, CurrentTower.Range, CurrentTower.EnemiesLayer);
        if (EnemiesInRange.Length == 0)
        {
            return null;
        }

        NativeArray<EnemyData> EmeniesToCalculate = new NativeArray<EnemyData>(EnemiesInRange.Length, Allocator.TempJob);
        NativeArray<Vector3> NodePositions = new NativeArray<Vector3>(GameLoopManager.NodePositions, Allocator.TempJob); // no Len
        NativeArray<float> NodeDistances = new NativeArray<float>(GameLoopManager.NodeDistances, Allocator.TempJob); // no Len
        NativeArray<int> EnemyToIndex = new NativeArray<int>(new int[] { -1 }, Allocator.TempJob);
        int EnemyIndexToReturn = -1;

        for (int i = 0; i < EmeniesToCalculate.Length; i++)
        {
            // Debug.Log("FFFFF >>>> i = " + i + "; EnemiesInRange[i] = " + EnemiesInRange[i] + "; GetComponent = " + EnemiesInRange[i].GetComponent<Enemy>());
            // // Enemy CurrentEnemy = EnemiesInRange[i].transform.parent.GetComponent<Enemy>();
            // Enemy CurrentEnemy;
            // if (EnemiesInRange[i].transform.parent != null)
            // {
            //     CurrentEnemy = EnemiesInRange[i].transform.parent.GetComponent<Enemy>();
            // } else {
            //     CurrentEnemy = EnemiesInRange[i].GetComponent<Enemy>();
            // }
            Enemy CurrentEnemy = EnemiesInRange[i].GetComponent<Enemy>();
            int EnemyIndexInList = EntitySummoner.EnemiesInGame.FindIndex(x => x == CurrentEnemy);
            EmeniesToCalculate[i] = new EnemyData(CurrentEnemy.transform.position, CurrentEnemy.NodeIndex, CurrentEnemy.Health, EnemyIndexInList);
            // EntitySummoner.EnemiesInGame.Find(x => x == CurrentEnemy);
        }

        SearchForEnemy EnemysearchJob = new SearchForEnemy
        {
            _EmeniesToCalculate = EmeniesToCalculate,
            _NodePositions = NodePositions,
            _NodeDistances = NodeDistances,
            _EnemyToIndex = EnemyToIndex,
            // CompareValue = Mathf.Infinity,
            TargetingType = (int)TargetMethod,
            TowerPosition = CurrentTower.transform.position
        };

        switch((int)TargetMethod)
        {
            case 0: // first
                EnemysearchJob.CompareValue = Mathf.Infinity;
                break;
            case 1: // last
                EnemysearchJob.CompareValue = Mathf.NegativeInfinity;
                break;
            case 2: // close
                goto case 0;
            case 3: // strong
                goto case 1;
            case 4: // weak
                goto case 0;
        }

        JobHandle dependency = new JobHandle();
        JobHandle SearchJobHandle = EnemysearchJob.Schedule(EmeniesToCalculate.Length, dependency);
        SearchJobHandle.Complete();

        // Debug.Log("FFFFF>>> " + EnemyToIndex[0]);
        EnemyIndexToReturn = EmeniesToCalculate[EnemyToIndex[0]].EnemyIndex;

        EmeniesToCalculate.Dispose();
        NodePositions.Dispose();
        NodeDistances.Dispose();
        EnemyToIndex.Dispose();

        if (EnemyIndexToReturn == -1)
        {
            return null;
        }

        return EntitySummoner.EnemiesInGame[EnemyIndexToReturn];
    }

    struct EnemyData
    {
        public EnemyData(Vector3 pos, int nodeIdx, float hp, int enemyIdx)
        {
            EnemyPosition = pos;
            EnemyIndex = enemyIdx;
            NodeIndex = nodeIdx;
            Health = hp;
        }

        public Vector3 EnemyPosition;
        public int EnemyIndex;
        public int NodeIndex;
        public float Health;
    }

    struct SearchForEnemy : IJobFor
    {
        // [ReadOnly] public NativeArray<EnemyData> _EmeniesToCalculate;
        // [ReadOnly] public NativeArray<Vector3> _NodePositions;
        // [ReadOnly] public NativeArray<float> _NodeDistances;
        // [ReadOnly] public NativeArray<int> _EnemyToIndex;
        [ReadOnly] public NativeArray<EnemyData> _EmeniesToCalculate;
        [ReadOnly] public NativeArray<Vector3> _NodePositions;
        [ReadOnly] public NativeArray<float> _NodeDistances;
        public NativeArray<int> _EnemyToIndex;

        public Vector3 TowerPosition;
        public float CompareValue;
        public int TargetingType;

        public void Execute(int index)
        {  
            // Debug.Log("FFFFF>>> TargetingType = " + TargetingType + "; index = " + index);

            float CurrentEnemyDistanceToEnd = 0f;
            switch (TargetingType)
            {
                case 0: // First
                    CurrentEnemyDistanceToEnd = GetDistanceToEnd(_EmeniesToCalculate[index]);
                    if (CurrentEnemyDistanceToEnd < CompareValue)
                    {
                        _EnemyToIndex[0] = index;
                        CompareValue = CurrentEnemyDistanceToEnd;
                    }
                    break;
                case 1: // Last
                    CurrentEnemyDistanceToEnd = GetDistanceToEnd(_EmeniesToCalculate[index]);
                    if (CurrentEnemyDistanceToEnd > CompareValue)
                    {
                        _EnemyToIndex[0] = index;
                        CompareValue = CurrentEnemyDistanceToEnd;
                    }
                    break;
                case 2: // Close
                    float DistanceToEnemy = Vector3.Distance(TowerPosition, _EmeniesToCalculate[index].EnemyPosition);
                    if (DistanceToEnemy < CompareValue)
                    {
                        _EnemyToIndex[0] = index;
                        CompareValue = DistanceToEnemy;
                    }
                    break;
                case 3: // Strong
                    if (_EmeniesToCalculate[index].Health > CompareValue)
                    {
                        _EnemyToIndex[0] = index;
                        CompareValue = _EmeniesToCalculate[index].Health;
                    }
                    break;
                case 4: // Weak
                    if (_EmeniesToCalculate[index].Health < CompareValue)
                    {
                        _EnemyToIndex[0] = index;
                        CompareValue = _EmeniesToCalculate[index].Health;
                    }
                    break;
            }

            // TODO: remove
            // _EnemyToIndex[0] = 0;
        }  

        private float GetDistanceToEnd(EnemyData EnemyToEvaluate)
        {
            float FinalDistance = Vector3.Distance(EnemyToEvaluate.EnemyPosition, _NodePositions[ EnemyToEvaluate.NodeIndex ]);
            for (int i = EnemyToEvaluate.NodeIndex; i < _NodeDistances.Length; i++)
            {
                FinalDistance += _NodeDistances[i];
            }
            return FinalDistance;
        } 
    }

}
