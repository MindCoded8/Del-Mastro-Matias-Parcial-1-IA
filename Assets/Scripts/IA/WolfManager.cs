using System;
using UnityEngine;

public class WolfManager : MonoBehaviour
{
    public static WolfManager Instance { get; private set; }

    public event Action<Vector3> OnWolfHowlAlert;

    [SerializeField] private LayerMask _obstacleMask;
    [SerializeField] private AudioClip _howlAudioClip;
    [SerializeField] private AudioSource _audioSource;

    public HunterAgent TargetHunter { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (_audioSource == null) _audioSource = GetComponent<AudioSource>();
    }

    public void RegisterHunter(HunterAgent hunter)
    {
        TargetHunter = hunter;
    }

    public void BroadcastHowlAlert(Vector3 hunterPos)
    {
        if (_audioSource != null && _howlAudioClip != null)
        {
            _audioSource.PlayOneShot(_howlAudioClip);
        }

        OnWolfHowlAlert?.Invoke(hunterPos);
    }
}
