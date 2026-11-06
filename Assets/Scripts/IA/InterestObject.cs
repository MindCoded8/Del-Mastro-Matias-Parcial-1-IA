using UnityEngine;

public class InterestObject : MonoBehaviour
{
    [Header("Interest Object Settings")]
    [SerializeField] private float _maxHealth = 100f;
    [SerializeField] private float _currentHealth = 100f;
    [SerializeField] private float _duration = 30f; 
    private float _timer = 0f;

    public float CurrentHealth => _currentHealth;

    private void Awake()
    {
        _currentHealth = _maxHealth;
        _timer = 0f;
    }

    private void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= _duration)
        {
            DestroySelf();
        }
    }

    public void TakeDamage(float amount)
    {
        _currentHealth -= amount;
        if (_currentHealth <= 0f)
        {
            DestroySelf();
        }
    }

    private void DestroySelf()
    {
        gameObject.SetActive(false);
        Destroy(gameObject, 0.1f);
    }
}
