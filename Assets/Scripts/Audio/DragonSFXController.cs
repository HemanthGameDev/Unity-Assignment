using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class DragonSFXController : MonoBehaviour
{
    [Header("Clips")]
    [SerializeField] private AudioClip fire;
    [SerializeField] private AudioClip tailAttack;
    [SerializeField] private AudioClip fly;
    [SerializeField] private AudioClip fireball;
    [SerializeField] private AudioClip hit;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    public void PlayFire() => Play(fire);
    public void PlayTailAttack() => Play(tailAttack);
    public void PlayFly() => Play(fly);
    public void PlayFireball() => Play(fireball);
    public void PlayHit() => Play(hit);

    private void Play(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }
}