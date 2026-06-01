using UnityEngine;

public class TowerAttack : MonoBehaviour
{
    public float attackRange = 5f;
    public float attackDamage = 10f;
    public float attackFireRate = 1f;

    public GameObject enemy;

    private float timer;

    void Attack(float timer)
    {
        Debug.Log("Attacking enemy for " + attackDamage + " damage!");
        timer = 0f;
    }


    // Update is called once per frame
    void Update()
    {
        if (enemy == null) return;

        float distanceToEnemy = Vector3.Distance(transform.position, enemy.transform.position);
        if (distanceToEnemy <= attackRange)
        {
            timer += Time.deltaTime;
            if (timer >= attackFireRate)
            {
                Attack(timer);
            }
        }

    }
}
