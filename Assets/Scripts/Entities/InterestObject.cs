


using System.Collections.Generic;
using UnityEngine;

public class InterestObject : MonoBehaviour
{
    private static List<InterestObject> _allInterestObjects = new List<InterestObject>();

    [SerializeField] private float _maxHealth = 50f;
    [SerializeField] private float _currentHealth;

    public static IReadOnlyList<InterestObject> AllInterestObjects => _allInterestObjects;

    private void Awake()
    {
        if (!_allInterestObjects.Contains(this))
        {
            _allInterestObjects.Add(this);
        }
        _currentHealth = _maxHealth;
    }

    private void OnDestroy()
    {
        if (_allInterestObjects.Contains(this))
        {
            _allInterestObjects.Remove(this);
        }
    }

    public void TakeDamage(float amount)
    {
        _currentHealth -= amount;
        if (_currentHealth <= 0f)
        {
            Destroy(gameObject);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.8f);
    }
}