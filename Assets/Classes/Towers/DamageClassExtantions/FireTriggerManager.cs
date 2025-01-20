using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireTriggerManager : MonoBehaviour
{
    [SerializeField] private FlamethrowerDamage BaseClass;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Enemy"))
        {
            Effect Flameeffect = new Effect("Fire", BaseClass.Firerate, BaseClass.Damage, 5f);
            // ApplyEffectData EffectData = new ApplyEffectData( EntitySummoner.EnemyTransformPairs[other.transform.parent], Flameeffect );
            ApplyEffectData EffectData = new ApplyEffectData( EntitySummoner.EnemyTransformPairs[other.transform], Flameeffect );
            GameLoopManager.EnqueueEffectToApply(EffectData);
        }
    }
}
