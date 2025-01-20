using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlamethrowerDamage : MonoBehaviour, IDamageMethod
{
    [SerializeField] private ParticleSystem FireEffect;
    [SerializeField] private Collider FireTrigger;

    [HideInInspector] public float Damage;
    [HideInInspector]public float Firerate;

    public void Init(float Damage, float Firerate)
    {
        this.Damage = Damage;
        this.Firerate = Firerate;

    }

    public void DamageTick(Enemy Target)
    {
        FireTrigger.enabled = Target != null;
        if (Target != null)
        {
            if (!FireEffect.isPlaying)
                FireEffect.Play();
            return;
        }

        FireEffect.Stop();
    }
}
