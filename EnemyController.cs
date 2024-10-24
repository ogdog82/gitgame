using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyController : MonoBehaviour
{
    public float moveSpeed = 1f;
    private Vector2Int targetPosition;
    private DungeonGenerator dungeonGenerator;

    private void Start()
    {
        // Initialize target position to current position
        targetPosition = Vector2Int.RoundToInt(transform.position);
        dungeonGenerator = FindObjectOfType<DungeonGenerator>();

        // Start the random movement coroutine
        StartCoroutine(RandomMovement());
    }

    private IEnumerator RandomMovement()
    {
        while (true)
        {
            MoveRandomly();
            yield return new WaitForSeconds(1f / moveSpeed); // Adjust speed as needed
        }
    }

    private void MoveRandomly()
    {
        Vector2Int currentPos = Vector2Int.RoundToInt(transform.position);
        List<Vector2Int> validMoves = GetValidMoves(currentPos);

        if (validMoves.Count > 0)
        {
            int moveIndex = UnityEngine.Random.Range(0, validMoves.Count);
            Vector2Int nextPos = validMoves[moveIndex];
            StartCoroutine(MoveToPosition(nextPos));
        }
    }

    private IEnumerator MoveToPosition(Vector2Int newPosition)
    {
        Vector3 startPosition = transform.position;
        Vector3 endPosition = new Vector3(newPosition.x, newPosition.y, transform.position.z);
        float elapsedTime = 0f;
        float moveDuration = 1f / moveSpeed; // Adjust move speed as needed

        while (elapsedTime < moveDuration)
        {
            transform.position = Vector3.Lerp(startPosition, endPosition, (elapsedTime / moveDuration));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = endPosition;
    }

    private List<Vector2Int> GetValidMoves(Vector2Int position)
    {
        List<Vector2Int> validMoves = new List<Vector2Int>();
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        foreach (var dir in directions)
        {
            Vector2Int newPos = position + dir;
            if (dungeonGenerator.IsWalkableTile(newPos.x, newPos.y))
            {
                validMoves.Add(newPos);
            }
        }

        return validMoves;
    }
}