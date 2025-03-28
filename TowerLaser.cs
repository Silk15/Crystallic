using System.Collections;
using System.Linq;
using ThunderRoad;
using UnityEngine;

namespace Crystallic;

public class TowerLaser : MonoBehaviour
{
    public Animator animator;
    public MeshRenderer crystal;
    public AudioSource turnAudio;
    public Color currentColor;
    public TowerLaserType towerLaserType;
    public EffectData beamMechanicsEffectData;
    public EffectData chargeEffectData;
    private Settings crystalData;
    public EffectData fireEffectData;

    public void Init(TowerLaserType towerLaserType, RuntimeAnimatorController controller, Settings crystalData)
    {
        beamMechanicsEffectData = Catalog.GetData<EffectData>(crystalData.laserMechanicsEffectId);
        fireEffectData = Catalog.GetData<EffectData>(crystalData.laserFireEffectId);
        chargeEffectData = Catalog.GetData<EffectData>(crystalData.laserLoadEffectId);
        animator = gameObject.GetComponentInChildren<Animator>();
        this.crystalData = crystalData;
        this.towerLaserType = towerLaserType;
        turnAudio = gameObject.transform.GetChildByNameRecursive($"AudioLaser{towerLaserType}").GetComponent<AudioSource>();
        crystal = gameObject.transform.GetChildByNameRecursive($"Dalg_Crystal{towerLaserType}").GetComponent<MeshRenderer>();
        animator.runtimeAnimatorController = controller;
        Debug.Log($"{towerLaserType} clips:\n - " + string.Join("\n - ", animator.runtimeAnimatorController.animationClips.ToList()));
        currentColor = crystal.material.GetColor("_EmissionColor");
        PlayAndLerp();
    }

    public void PlayAndLerp()
    {
        animator.Play("Laser_Khemenet_Animation");
        StartCoroutine(BeamRoutine());
        StartCoroutine(Emissive(new Color(3.232298f, 2.1f, 7.1f), crystal.material));
    }

    public IEnumerator BeamRoutine()
    {
        beamMechanicsEffectData.Spawn(transform).Play();
        yield return new WaitForSeconds(4.5f);
        var instance = chargeEffectData.Spawn(transform);
        instance.Play();
        yield return new WaitForSeconds(17);
        StartCoroutine(Emissive(new Color(3.232298f, 2.1f, 7.1f), crystal.material));
        yield return new WaitForSeconds(6);
        instance.End();
        fireEffectData.Spawn(transform).Play();
    }

    public IEnumerator Emissive(Color newColor, Material material)
    {
        yield return new WaitForSeconds(0.1f);
        float tts = 10;
        float timeElapsed = 0;
        while (timeElapsed <= tts)
        {
            material.SetColor("_EmissionColor", Color.Lerp(currentColor, newColor, timeElapsed / tts));
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        currentColor = newColor;
    }
}