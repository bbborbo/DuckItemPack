using RoR2;
using System;
using System.Collections.Generic;
using System.Text;

namespace DuckItemPack.Equipment.Zapinator
{
    public class ZapinatorRollData
    {
        public bool hasRolledBonusMultipliers = false;

        public float damageCoefficient = 5;
        public float procCoefficient = 0.5f;
        public float aoeSizeMultiplier = 1;

        public float forceMultiplier = 1000;
        public float selfForceMultiplier = 0.5f;

        public float velocityMultiplier = 1;
        public float accuracy = 1;
        public int totalProjectiles = 1;

        public DamageType damageTypes;
    }
}
