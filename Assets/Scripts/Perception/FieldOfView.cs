using UnityEngine;

public class FieldOfView : MonoBehaviour
{
    [SerializeField] private float _viewRadius = 8f;
    [Range(0f, 360f)]
    [SerializeField] private float _viewAngle = 90f;
    [SerializeField] private LayerMask _obstacleMask;
    [SerializeField] private float _eyeHeightOffset = 0.5f;

    public float ViewRadius => _viewRadius;
    public float ViewAngle => _viewAngle;

    public bool CanSeeTarget(Transform target)
    {
        if (target == null || !target.gameObject.activeInHierarchy) return false;

        HunterAgent hunter = target.GetComponent<HunterAgent>();
        if (hunter != null && (hunter.IsInSafeZone || hunter.IsDead)) return false;

        Vector3 eyePos = transform.position + Vector3.up * _eyeHeightOffset;
        Vector3 targetPos = target.position + Vector3.up * _eyeHeightOffset;
        Vector3 dirToTarget = targetPos - eyePos;

        float sqrDist = dirToTarget.sqrMagnitude;
        if (sqrDist > _viewRadius * _viewRadius) return false;

        float angleToTarget = Vector3.Angle(transform.forward, dirToTarget.normalized);
        if (angleToTarget > _viewAngle * 0.5f) return false;

        float distance = Mathf.Sqrt(sqrDist);
        if (Physics.Raycast(eyePos, dirToTarget.normalized, out RaycastHit hit, distance, _obstacleMask))
        {
            if (hit.transform != target && !hit.transform.IsChildOf(target))
            {
                return false;
            }
        }

        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _viewRadius);

        Vector3 leftDir = Quaternion.Euler(0, -_viewAngle * 0.5f, 0) * transform.forward;
        Vector3 rightDir = Quaternion.Euler(0, _viewAngle * 0.5f, 0) * transform.forward;

        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position + Vector3.up * _eyeHeightOffset, leftDir * _viewRadius);
        Gizmos.DrawRay(transform.position + Vector3.up * _eyeHeightOffset, rightDir * _viewRadius);
    }
}
