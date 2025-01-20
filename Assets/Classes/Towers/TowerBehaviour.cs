using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TowerBehaviour : MonoBehaviour
{
    public LayerMask EnemiesLayer;

    public Enemy Target;
    public Transform TowerPivot;

    public int SummonCost = 100;
    public float Damage;
    public float Firerate;
    public float Range;

    private float Delay; // time before we apply damage

    private IDamageMethod CurrentDamageMethodClass;

    void Start()
    {
        CurrentDamageMethodClass = GetComponent<IDamageMethod>();
        if (CurrentDamageMethodClass == null)
        {
            Debug.LogError("TOWERS: no damage class attched to given tower!");
        } else {
            CurrentDamageMethodClass.Init(Damage, Firerate);
        }
        Delay = 1 / Firerate;
    }

    public void Tick()
    {
        CurrentDamageMethodClass.DamageTick(Target);
        if (Target != null)
        {
            TowerPivot.transform.rotation = Quaternion.LookRotation(Target.transform.position - transform.position);
        }
    }

    private void OnDrawGizmos()
    {
        if (Target != null)
        {
            Gizmos.DrawWireSphere(transform.position, Range);
            Gizmos.DrawLine(TowerPivot.position, Target.transform.position);
        }
    }

}
