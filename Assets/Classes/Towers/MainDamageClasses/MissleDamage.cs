using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissleDamage : MonoBehaviour, IDamageMethod
{
    public LayerMask EnemiesLayer;
    [SerializeField] private ParticleSystem MissleSystem;
    [SerializeField] private Transform TowerHead;

    private ParticleSystem.MainModule MissleSystemMain;
    public float Damage;
    private float Firerate;
    private float Delay;

    public void Init(float Damage, float Firerate)
    {
        MissleSystemMain = MissleSystem.main;
        this.Damage = Damage;
        this.Firerate = Firerate;

        Delay = 1f / Firerate;
    }

    public void DamageTick(Enemy Target)
    {
        if (Target != null)
        {
            if (Delay > 0f)
            {
                Delay -= Time.deltaTime;
                return;
            }

            MissleSystemMain.startRotationX = TowerHead.forward.x;
            MissleSystemMain.startRotationY = TowerHead.forward.y;
            MissleSystemMain.startRotationZ = TowerHead.forward.z;

            // if (!MissleSystem.isPlaying)
            // {
                MissleSystem.Play();
            // }
            // return;
            
            Delay = 1f / Firerate;
            return;
        }
        // if (MissleSystem.isPlaying) 
        // {
        //     MissleSystem.Stop();
        // }
    }
}
