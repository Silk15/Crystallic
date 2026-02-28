using ThunderRoad;
using ThunderRoad.Skill.Spell;
using UnityEngine;
using UnityEngine.Serialization;

namespace Crystallic.Skill.Spell.Attunement;

public class AttunementRuneBehaviour : ThunderBehaviour
{
    public ParticleSystemRenderer particleSystemRenderer;
    public ParticleSystem particleSystem;
    public Mesh originalMesh;
    public Mesh mesh;

    #if !SDK
    protected override void ManagedOnEnable()
    {
        base.ManagedOnEnable();
        particleSystemRenderer = GetComponent<ParticleSystemRenderer>();
        particleSystem = GetComponent<ParticleSystem>();
        originalMesh = particleSystemRenderer.mesh;
    }

    public void Init(Mesh mesh)
    {
        particleSystemRenderer.mesh = mesh;
        this.mesh = mesh;
        particleSystem.Clear();
        particleSystem.Play();
    }

    protected override void ManagedOnDisable()
    {
        base.ManagedOnDisable();
        particleSystemRenderer.mesh = originalMesh;
        particleSystemRenderer = null;
        originalMesh = null;
        mesh = null;
    }
    #endif
}