using UnityEngine;

/// <summary>
/// Box volume (no Collider required). Set the GameObject tag to <c>RangerArea</c> (Ranger prefab only) or
/// <c>Unplaceable</c> (Obstacle-tagged prefabs blocked). Rules are enforced in <see cref="BuildMode"/>.
/// </summary>
public class PlacementRestrictionZone : MonoBehaviour
{
    [SerializeField] private Vector3 localCenter = Vector3.zero;
    [SerializeField] private Vector3 localHalfExtents = new Vector3(5f, 2f, 5f);

    public Bounds GetWorldBounds()
    {
        Vector3 c = localCenter;
        Vector3 h = new Vector3(
            Mathf.Max(0.01f, localHalfExtents.x),
            Mathf.Max(0.01f, localHalfExtents.y),
            Mathf.Max(0.01f, localHalfExtents.z));

        Vector3[] corners = new Vector3[8];
        int idx = 0;
        for (int x = -1; x <= 1; x += 2)
        for (int y = -1; y <= 1; y += 2)
        for (int z = -1; z <= 1; z += 2)
            corners[idx++] = transform.TransformPoint(c + new Vector3(x * h.x, y * h.y, z * h.z));

        Bounds b = new Bounds(corners[0], Vector3.zero);
        for (int i = 1; i < 8; i++)
            b.Encapsulate(corners[i]);
        return b;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Color fill;
        if (CompareTag("RangerArea"))
            fill = new Color(0.25f, 0.55f, 1f, 0.25f);
        else if (CompareTag("Unplaceable"))
            fill = new Color(1f, 0.25f, 0.25f, 0.25f);
        else
            fill = new Color(0.7f, 0.7f, 0.7f, 0.2f);
        Gizmos.color = fill;
        Gizmos.DrawCube(localCenter, localHalfExtents * 2f);
        Gizmos.color = new Color(fill.r, fill.g, fill.b, 0.9f);
        Gizmos.DrawWireCube(localCenter, localHalfExtents * 2f);
    }
#endif
}
