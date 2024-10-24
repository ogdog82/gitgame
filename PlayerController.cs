using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour, IActable
{
    public int maxHealth = 100;
    public int currentHealth;
    public int attackPower = 10;
    public float moveSpeed = 1f;
    public float visibilityRadius = 20f;
    public float attackRange = 1.5f;
    public float attackSpeed = 1f;
    private float lastAttackTime = 0f;
    private Vector2 targetPosition;
    private bool isMoving = false;

    private EnemyManager enemyManager;

    public void Initialize(EnemyManager manager)
    {
        enemyManager = manager;
    }

    public void SetEnemyManager(EnemyManager manager)
    {
        enemyManager = manager;
    }

    private IEnumerator Attack()
    {
        if (Time.time - lastAttackTime >= 1f / attackSpeed)
        {
            EnemyController nearestEnemy = FindNearestEnemy();
            if (nearestEnemy != null && Vector2.Distance(transform.position, nearestEnemy.transform.position) <= 1.5f)
            {
                int damage = CalculateDamage();
                GameManager.Instance.ShowDamageNumber(nearestEnemy.transform.position, damage);
                lastAttackTime = Time.time;

                Vector2 attackDirection = (nearestEnemy.transform.position - transform.position).normalized;
                yield return StartCoroutine(ShakeAnimation(attackDirection));
            }
        }
        yield return null;
    }

    private int CalculateDamage()
    {
        // Simple damage calculation, can be expanded later
        return Random.Range(attackPower - 2, attackPower + 3);
    }

    public delegate void HealthChangedDelegate(int currentHealth, int maxHealth);
    public event HealthChangedDelegate OnHealthChanged;

    private IEnumerator AutoAttackCoroutine()
    {
        while (true)
        {
            if (Time.time - lastAttackTime >= 1f / attackSpeed)
            {
                EnemyController nearestEnemy = FindNearestEnemy();
                if (nearestEnemy != null && Vector2.Distance(transform.position, nearestEnemy.transform.position) <= attackRange)
                {
                    Attack(nearestEnemy);
                }
            }
            yield return null;
        }
    }

    private void Attack(EnemyController enemy)
    {
        int damage = CalculateDamage();
        lastAttackTime = Time.time;

        Vector2 attackDirection = (enemy.transform.position - transform.position).normalized;
        StartCoroutine(ShakeAnimation(attackDirection));
    }

    private void AlignToGrid()
    {
        Vector3 alignedPosition = new Vector3(
            Mathf.RoundToInt(transform.position.x),
            Mathf.RoundToInt(transform.position.y),
            transform.position.z + 1
        );
        transform.position = alignedPosition;
        targetPosition = alignedPosition;
    }

    public IEnumerator TakeTurn()
    {
        isMoving = false;
        yield return StartCoroutine(WaitForInput());
        TurnManager.Instance.EndPlayerTurn();
    }
    private IEnumerator WaitForInput()
    {
        bool actionTaken = false;
        float moveInterval = 1f / moveSpeed; // Time between moves
        float lastMoveTime = 0f;

        while (!actionTaken)
        {
            Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            input = Vector2.ClampMagnitude(input, 1f); // Normalize diagonal movement

            if (input != Vector2.zero && Time.time - lastMoveTime >= moveInterval)
            {
                Vector2 moveDirection = input.normalized;
                actionTaken = TryMove(moveDirection);

                if (actionTaken)
                {
                    lastMoveTime = Time.time;
                    yield return StartCoroutine(MoveToTargetPosition());
                }
            }

            yield return null;
        }
    }

    private bool TryMove(Vector2 direction)
    {
        Vector2 newPosition = (Vector2)transform.position + direction;
        int newX = Mathf.RoundToInt(newPosition.x);
        int newY = Mathf.RoundToInt(newPosition.y);

        if (GameManager.Instance.DungeonGenerator.IsWalkableTile(newX, newY))
        {
            targetPosition = new Vector2(newX, newY);
            isMoving = true;
            return true;
        }
        return false;
    }

    private bool IsEnemyAtPosition(Vector2 position)
    {
        EnemyController[] enemies = FindObjectsOfType<EnemyController>();
        foreach (EnemyController enemy in enemies)
        {
            if (Vector2.Distance(position, enemy.transform.position) < 0.5f)
            {
                return true;
            }
        }
        return false;
    }

    private IEnumerator MoveToTargetPosition()
    {
        Vector3 startPosition = transform.position;
        float journeyLength = Vector3.Distance(startPosition, targetPosition);
        float moveDuration = journeyLength / moveSpeed;
        float elapsedTime = 0f;

        while (elapsedTime < moveDuration)
        {
            float t = elapsedTime / moveDuration;
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPosition;
        isMoving = false;
    }
    private EnemyController FindNearestEnemy()
    {
        if (GameManager.Instance == null || GameManager.Instance.enemyManager == null)
        {
            Debug.LogError("GameManager or EnemyManager is null in PlayerController");
            return null;
        }

        return GameManager.Instance.enemyManager.GetNearestEnemy(transform.position);
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        Vector3 damagePosition = transform.position + Vector3.up; // Adjust this offset as needed

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        GameManager.Instance.ChangeGameState(GameManager.GameState.GameOver);
    }

    public bool IsAlive()
    {
        return currentHealth > 0;
    }

    public void ResetPosition()
    {
        Vector2 startPosition = GameManager.Instance.DungeonGenerator.GetPlayerStartPosition();
        transform.position = new Vector3(Mathf.RoundToInt(startPosition.x), Mathf.RoundToInt(startPosition.y), transform.position.z);
        targetPosition = transform.position;
    }

    private void UpdateVisibility()
    {
        if (GameManager.Instance != null && GameManager.Instance.DungeonGenerator != null)
        {
            GameManager.Instance.DungeonGenerator.UpdateVisibility(transform.position, visibilityRadius);
        }
    }
    private IEnumerator ShakeAnimation(Vector2 direction)
    {
        Vector3 originalPosition = transform.position;
        float shakeDuration = .25f / attackSpeed; // Duration based on attack speed
        float elapsedTime = 0f;

        while (elapsedTime < shakeDuration)
        {
            float strength = (1 - (elapsedTime / shakeDuration)) * 0.1f;
            transform.position = originalPosition + (Vector3)(direction * strength * Mathf.Sin(elapsedTime * 30));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = originalPosition;
    }
}