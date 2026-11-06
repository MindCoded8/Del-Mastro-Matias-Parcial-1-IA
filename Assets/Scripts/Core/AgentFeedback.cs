using UnityEngine;

public class AgentFeedback : MonoBehaviour
{
    [Header("Mesh Reference (Acepta MeshRenderer y SkinnedMeshRenderer)")]
    [SerializeField] private Renderer _meshRenderer;

    [Header("Optional Text / Label Reference")]
    [SerializeField] private TextMesh _textMesh;

    [Header("Text Height Offset")]
    [SerializeField] private float _heightOffset = 0.8f;

    private MaterialPropertyBlock _propBlock;

    private void Awake()
    {
        _propBlock = new MaterialPropertyBlock();

        if (_meshRenderer == null)
        {
            _meshRenderer = GetComponentInChildren<Renderer>();
        }

        EnsureTextMesh();
    }

    private void EnsureTextMesh()
    {
        if (_textMesh != null) return;
        _textMesh = GetComponentInChildren<TextMesh>();
        if (_textMesh == null)
        {
            GameObject textObj = new GameObject("HeadText");
            textObj.transform.SetParent(transform.root != null ? transform.root : transform);
            _textMesh = textObj.AddComponent<TextMesh>();
            _textMesh.characterSize = 0.12f;
            _textMesh.fontSize = 24;
            _textMesh.alignment = TextAlignment.Center;
            _textMesh.anchor = TextAnchor.MiddleCenter;
            _textMesh.color = Color.yellow;
            _textMesh.fontStyle = FontStyle.Bold;
        }
    }

    private void LateUpdate()
    {
        if (_textMesh != null)
        {
            Vector3 rootPos = (transform.root != null) ? transform.root.position : transform.position;
            _textMesh.transform.position = rootPos + Vector3.up * _heightOffset;

            if (Camera.main != null)
            {
                _textMesh.transform.rotation = Camera.main.transform.rotation;
            }
        }
    }

    public void UpdateBoidStateVisual(bool isDead, bool isEvading, bool isFeeding = false)
    {
        if (_meshRenderer == null)
        {
            _meshRenderer = GetComponentInChildren<Renderer>();
        }

        Color targetColor = Color.cyan;
        string labelText = "BOID: FLOCKING";

        if (isDead)
        {
            targetColor = Color.gray;
            labelText = "BOID: DEAD";
        }
        else if (isEvading)
        {
            targetColor = Color.red;
            labelText = "BOID: EVADING!";
        }
        else if (isFeeding)
        {
            targetColor = Color.yellow;
            labelText = "BOID: FEEDING";
        }

        SetColor(targetColor);
        SetStateText(labelText, targetColor);
    }

    public void SetColor(Color color)
    {
        if (_meshRenderer == null)
        {
            _meshRenderer = GetComponentInChildren<Renderer>();
            if (_meshRenderer == null) return;
        }

        if (_propBlock == null) _propBlock = new MaterialPropertyBlock();
        _meshRenderer.GetPropertyBlock(_propBlock);
        
        _propBlock.SetColor("_Color", color);
        _propBlock.SetColor("_BaseColor", color);
        _meshRenderer.SetPropertyBlock(_propBlock);

        if (_meshRenderer.material != null)
        {
            if (_meshRenderer.material.HasProperty("_Color"))
            {
                _meshRenderer.material.color = color;
            }
            else if (_meshRenderer.material.HasProperty("_BaseColor"))
            {
                _meshRenderer.material.SetColor("_BaseColor", color);
            }
        }
    }

    public void SetStateText(string text, Color color)
    {
        EnsureTextMesh();
        if (_textMesh != null)
        {
            _textMesh.text = text;
            _textMesh.color = color;
        }
    }

    public void ShowShotBeam(Vector3 origin, Vector3 targetPosition)
    {
    }
}
