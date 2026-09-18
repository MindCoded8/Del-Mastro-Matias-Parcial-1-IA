using UnityEngine;

public class AgentFeedback : MonoBehaviour
{
    [Header("Mesh Renderer Reference")]
    [SerializeField] private MeshRenderer _meshRenderer;

    [Header("Color Settings")]
    [SerializeField] private Color _normalColor = Color.white;
    [SerializeField] private Color _evadeColor = Color.yellow;
    [SerializeField] private Color _deadColor = Color.gray;

    [Header("World Space Text Settings")]
    [SerializeField] private Vector3 _textOffset = new Vector3(0f, 2.2f, 0f);
    [SerializeField] private int _fontSize = 24;
    [SerializeField] private float _characterSize = 0.15f;

    private TextMesh _textMesh;

    private void Awake()
    {
        if (_meshRenderer == null)
        {
            _meshRenderer = GetComponent<MeshRenderer>();
        }

        CreateFloatingText();
    }

    private void CreateFloatingText()
    {
        if (_textMesh != null) return;

        GameObject textObj = new GameObject("StateLabel");
        textObj.transform.SetParent(transform);
        textObj.transform.localPosition = _textOffset;
        textObj.transform.localRotation = Quaternion.identity;

        _textMesh = textObj.AddComponent<TextMesh>();
        _textMesh.alignment = TextAlignment.Center;
        _textMesh.anchor = TextAnchor.MiddleCenter;
        _textMesh.fontSize = _fontSize;
        _textMesh.characterSize = _characterSize;
        _textMesh.fontStyle = FontStyle.Bold;
        _textMesh.text = "";
    }

    private void LateUpdate()
    {
        if (_textMesh != null && Camera.main != null)
        {
            _textMesh.transform.rotation = Camera.main.transform.rotation;
        }
    }

    public void SetStateText(string text, Color color)
    {
        if (_textMesh == null)
        {
            CreateFloatingText();
        }

        if (_textMesh != null)
        {
            _textMesh.text = text;
            _textMesh.color = color;
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
            SetStateText("DEAD", _deadColor);
        }
        else if (isEvading)
        {
            SetColor(_evadeColor);
            SetStateText("EVADE!", _evadeColor);
        }
        else
        {
            SetColor(_normalColor);
            SetStateText("", Color.clear);
        }
    }
}