using ThunderRoad;
using UnityEngine;

namespace Crystallic;

public class HeadPart : GolemPart
{
    public GameObject faceplate;
    public GameObject vfxTarget;
    public CrystalSocket crystalSocket;

    public override void Awake()
    {
        faceplate = transform.GetMatchingChild("Faceplate").gameObject;
        vfxTarget = transform.GetMatchingChild("vfx_golem_roar_scan").gameObject;
        crystalSocket = transform.GetMatchingChild("Crystal").gameObject.AddComponent<CrystalSocket>();
    }

    public class CrystalSocket : ThunderBehaviour
    {
        public ConfigurableJoint heldJoint;
        public Item crystal;
        public ParticleSystem deathParticleSystem;

        public void Awake()
        {
            heldJoint = GetComponentInChildren<ConfigurableJoint>();
            crystal = GetComponentInChildren<Item>();
            deathParticleSystem = transform.GetMatchingChild("vfx_golem_CrystalTearingExplosion").gameObject.GetComponent<ParticleSystem>();
        }
    }
}