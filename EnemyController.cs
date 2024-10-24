using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyController : MonoBehaviour, IActable
{
    public float moveSpeed = 1f;
    private Vector2Int targetPosition;
    private DungeonGenerator dungeonGenerator;

    public Transform player;
    public float speed = 2f;
    public float attackRange = 1.5f;
    public int attackDamage = 10;
    public int health = 50;

    private float lastAttackTime = 0f;
    public float attackSpeed = 1f;

    private void Start()
    {
        // Find the player in the scene
        player = GameObject.FindGameObjectWithTag("Player").transform;
        if (player == null)
        {
            Debug.LogError("Player not found in the scene!");
        }

        // Find the DungeonGenerator in the scene
        dungeonGenerator = FindObjectOfType<DungeonGenerator>();
        if (dungeonGenerator == null)
        {
            Debug.LogError("DungeonGenerator not found in the scene!");
        }

        StartCoroutine(AutoAttackCoroutine());
    }

    private IEnumerator AutoAttackCoroutine()
    {
        while (true)
        {
            if (Time.time - lastAttackTime >= 1f / attackSpeed)
            {
                PlayerController nearestPlayer = FindNearestPlayer();
                if (nearestPlayer != null && Vector2.Distance(transform.position, nearestPlayer.transform.position) <= attackRange)
                {
                    Attack(nearestPlayer);
                }
            }
            yield return null;
        }
    }

    private PlayerController FindNearestPlayer()
    {
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null && Vector2.Distance(transform.position, player.transform.position) <= attackRange)
        {
            return player;
        }
        return null;
    }

    private void Attack(PlayerController player)
    {
        int damage = attackDamage;
        lastAttackTime = Time.time;

        Vector2 attackDirection = (player.transform.position - transform.position).normalized;
        StartCoroutine(ShakeAnimation(attackDirection));
        player.TakeDamage(damage);
    }

    private IEnumerator ShakeAnimation(Vector2 direction)
    {
        Vector3 originalPosition = transform.position;
        float shakeDuration = 0.1f;
        float shakeMagnitude = 0.1f;
        float elapsed = 0.0f;

        while (elapsed < shakeDuration)
        {
            float offsetX = Random.Range(-1f, 1f) * shakeMagnitude;
            float offsetY = Random.Range(-1f, 1f) * shakeMagnitude;
            transform.position = new Vector3(originalPosition.x + offsetX, originalPosition.y + offsetY, originalPosition.z);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = originalPosition;
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
        if (health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        // Notify the GameManager to remove this enemy
        GameManager.Instance.RemoveEnemy(this);
        Destroy(gameObject);
    }
}