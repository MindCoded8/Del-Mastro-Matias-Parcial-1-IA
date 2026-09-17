using UnityEngine;

public class AgentFeedback : MonoBehaviour
{
    [SerializeField] private MeshRenderer _meshRenderer;
    [SerializeField] private Color _normalColor = Color.white;
    [SerializeField] private Color _evadeColor = Color.yellow;
    [SerializeField] private Color _deadColor = Color.gray;

    private void Awake()
    {
        if (_meshRenderer == null)
        {
            _meshRenderer = GetComponent<MeshRenderer>();
        }
    }

    public void SetColor(Color color)
    {
        if (_meshRenderer != null && _meshRenderer.material != null)
        {
            _meshRenderer.material.color = color;
        }
    }

    public void UpdateBoidStateVisual(bool isDead, bool isEvading)
    {
        if (isDead)
        {
            SetColor(_deadColor);
        }
        else if (isEvading)
        {
            SetColor(_evadeColor);
        }
        else
        {
            SetColor(_normalColor);
        }
    }
}