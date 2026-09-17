using UnityEngine;

public class InterestObject : MonoBehaviour
{
    [SerializeField] private float _maxHealth = 50f;
    private float _currentHealth;

    private void Awake()
    {
        _currentHealth = _maxHealth;
    }

    public void TakeDamage(float amount)
    {
        _currentHealth -= amount;
        if (_currentHealth <= 0f)
        {
            Destroy(gameObject);
        }
    }
}