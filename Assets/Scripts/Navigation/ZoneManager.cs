using System.Collections.Generic;
using UnityEngine;

public class ZoneManager : MonoBehaviour
{
    public static ZoneManager Instance { get; private set; }

    [Header("Zone Transforms")]
    [SerializeField] private Transform _spawnPoint;
    [SerializeField] private Transform _huntingGroundPoint;
    [SerializeField] private List<Transform> _safeZonePoints = new List<Transform>();
    [SerializeField] private Transform _exitPoint;

    public Transform SpawnPoint => _spawnPoint;
    public Transform HuntingGroundPoint => _huntingGroundPoint;
    public IReadOnlyList<Transform> SafeZonePoints => _safeZonePoints;
    public Transform ExitPoint => _exitPoint;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
}
