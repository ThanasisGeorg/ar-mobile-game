using UnityEngine;

public class TowerAttack : MonoBehaviour
{
    public float attackRange = 8f;
    public float attackDamage = 10f;
    public float attackFireRate = 2f;
    
    [Header("Range Display")]
    public bool showRangeCircle = true;
    public Color rangeCircleColor = new Color(0, 1, 0, 0.3f);
    public int circleSegments = 64;
    public float circleHeightOffset = -0.5f;

    public GameObject enemy;

    private float timer;
    private int attackCounter = 0;
    private LineRenderer rangeIndicator;

    void Attack(float counter)
    {
        Debug.Log("Attack " + counter + ": " + attackDamage + " damage!");
        timer = 0f;
    }

    void Start()
    {
        if (showRangeCircle)
        {
            InitializeRangeIndicator();
        }
    }

    void InitializeRangeIndicator()
    {
        // Create a new GameObject for the line renderer
        GameObject rangeIndicatorGO = new GameObject("RangeIndicator");
        rangeIndicatorGO.transform.SetParent(transform);
        rangeIndicatorGO.transform.localPosition = new Vector3(0, circleHeightOffset, 0);

        rangeIndicator = rangeIndicatorGO.AddComponent<LineRenderer>();
        rangeIndicator.useWorldSpace = false;
        rangeIndicator.positionCount = circleSegments + 1;
        rangeIndicator.startColor = rangeCircleColor;
        rangeIndicator.endColor = rangeCircleColor;
        rangeIndicator.startWidth = 0.1f;
        rangeIndicator.endWidth = 0.1f;

        // Generate circle points
        Vector3[] positions = new Vector3[circleSegments + 1];
        for (int i = 0; i <= circleSegments; i++)
        {
            float angle = (i / (float)circleSegments) * 2f * Mathf.PI;
            positions[i] = new Vector3(
                Mathf.Cos(angle) * attackRange,
                0f,
                Mathf.Sin(angle) * attackRange
            );
        }
        rangeIndicator.SetPositions(positions);
    }


    // Update is called once per frame
    void Update()
    {
        if (enemy == null) return;

        float distanceToEnemy = Vector3.Distance(transform.position, enemy.transform.position);
        if (distanceToEnemy <= attackRange)
        {
            Debug.Log("Enemy in range!");
            timer += Time.deltaTime;
            if (timer >= attackFireRate)
            {
                attackCounter++;
                Attack(attackCounter);
            }
        }

    }

    void OnDrawGizmos()
    {
        // Draw range circle in the editor
        Gizmos.color = new Color(0, 1, 0, 0.5f);
        Vector3 gizmoCenter = transform.position + new Vector3(0, circleHeightOffset, 0);
        DrawCircleGizmo(gizmoCenter, attackRange, circleSegments);
    }

    void DrawCircleGizmo(Vector3 center, float radius, int segments)
    {
        float angle = 0f;
        float angleStep = 360f / segments;
        Vector3 prevPos = center + new Vector3(radius, 0, 0);

        for (int i = 0; i <= segments; i++)
        {
            float rad = angle * Mathf.Deg2Rad;
            Vector3 newPos = center + new Vector3(Mathf.Cos(rad) * radius, 0, Mathf.Sin(rad) * radius);
            Gizmos.DrawLine(prevPos, newPos);
            prevPos = newPos;
            angle += angleStep;
        }
    }
}
