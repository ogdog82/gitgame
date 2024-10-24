using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    private Queue<IActable> turnQueue;
    private PlayerController player;
    private EnemyManager enemyManager;
    private bool isPlayerTurn;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        turnQueue = new Queue<IActable>();
    }

    public void Initialize(PlayerController playerController, EnemyManager enemyMgr)
    {
        player = playerController;
        enemyManager = enemyMgr;
        Debug.Log("TurnManager initialized successfully.");
    }

    public void StartCombat(List<IActable> combatants)
    {
        foreach (var combatant in combatants)
        {
            turnQueue.Enqueue(combatant);
        }

        StartCoroutine(HandleTurns());
    }

    private IEnumerator HandleTurns()
    {
        while (turnQueue.Count > 0)
        {
            IActable currentActable = turnQueue.Dequeue();
            yield return StartCoroutine(currentActable.TakeTurn());

            if (currentActable is PlayerController)
            {
                isPlayerTurn = false;
            }

            turnQueue.Enqueue(currentActable);
        }
    }

    public void EndPlayerTurn()
    {
        isPlayerTurn = false;
    }

    public void AddCombatant(IActable combatant)
    {
        turnQueue.Enqueue(combatant);
    }

    public void RemoveCombatant(IActable combatant)
    {
        Queue<IActable> newQueue = new Queue<IActable>();

        while (turnQueue.Count > 0)
        {
            IActable actable = turnQueue.Dequeue();
            if (actable != combatant)
            {
                newQueue.Enqueue(actable);
            }
        }

        turnQueue = newQueue;
    }

    public void ClearCombatants()
    {
        turnQueue.Clear();
    }

    public void PauseTurns()
    {
        StopAllCoroutines();
    }

    public void ResumeTurns()
    {
        StartCoroutine(HandleTurns());
    }

    public void ResetTurns()
    {
        ClearCombatants();
        StopAllCoroutines();
    }

    public bool IsPlayerTurn()
    {
        return isPlayerTurn;
    }

    public void SetPlayerTurn(bool isTurn)
    {
        isPlayerTurn = isTurn;
    }

    public void LogTurnQueue()
    {
        Debug.Log("Current Turn Queue:");
        foreach (var actable in turnQueue)
        {
            Debug.Log(actable.GetType().Name);
        }
    }

    public void ShuffleTurnQueue()
    {
        List<IActable> tempList = new List<IActable>(turnQueue);
        turnQueue.Clear();

        while (tempList.Count > 0)
        {
            int randomIndex = Random.Range(0, tempList.Count);
            turnQueue.Enqueue(tempList[randomIndex]);
            tempList.RemoveAt(randomIndex);
        }
    }

    public void SortTurnQueueBySpeed()
    {
        List<IActable> tempList = new List<IActable>(turnQueue);
        tempList.Sort((a, b) => a.GetSpeed().CompareTo(b.GetSpeed()));
        turnQueue = new Queue<IActable>(tempList);
    }

    public void PrintTurnQueue()
    {
        Debug.Log("Turn Queue:");
        foreach (var actable in turnQueue)
        {
            Debug.Log(actable.GetType().Name);
        }
    }

    public void SkipTurn(IActable combatant)
    {
        Queue<IActable> newQueue = new Queue<IActable>();

        while (turnQueue.Count > 0)
        {
            IActable actable = turnQueue.Dequeue();
            if (actable != combatant)
            {
                newQueue.Enqueue(actable);
            }
        }

        turnQueue = newQueue;
        turnQueue.Enqueue(combatant);
    }

    public void DelayTurn(IActable combatant, int delayTurns)
    {
        Queue<IActable> newQueue = new Queue<IActable>();

        while (turnQueue.Count > 0)
        {
            IActable actable = turnQueue.Dequeue();
            if (actable != combatant)
            {
                newQueue.Enqueue(actable);
            }
        }

        for (int i = 0; i < delayTurns; i++)
        {
            newQueue.Enqueue(combatant);
        }

        turnQueue = newQueue;
    }

    public void AdvanceTurn()
    {
        if (turnQueue.Count > 0)
        {
            IActable currentActable = turnQueue.Dequeue();
            turnQueue.Enqueue(currentActable);
        }
    }

    public void ReverseTurn()
    {
        if (turnQueue.Count > 0)
        {
            List<IActable> tempList = new List<IActable>(turnQueue);
            turnQueue.Clear();

            for (int i = tempList.Count - 1; i >= 0; i--)
            {
                turnQueue.Enqueue(tempList[i]);
            }
        }
    }

    public void SwapTurns(IActable combatant1, IActable combatant2)
    {
        List<IActable> tempList = new List<IActable>(turnQueue);
        int index1 = tempList.IndexOf(combatant1);
        int index2 = tempList.IndexOf(combatant2);

        if (index1 >= 0 && index2 >= 0)
        {
            tempList[index1] = combatant2;
            tempList[index2] = combatant1;
        }

        turnQueue = new Queue<IActable>(tempList);
    }

    public void InsertTurn(IActable combatant, int position)
    {
        List<IActable> tempList = new List<IActable>(turnQueue);

        if (position >= 0 && position < tempList.Count)
        {
            tempList.Insert(position, combatant);
        }
        else
        {
            tempList.Add(combatant);
        }

        turnQueue = new Queue<IActable>(tempList);
    }

    public void RemoveTurnAt(int position)
    {
        List<IActable> tempList = new List<IActable>(turnQueue);

        if (position >= 0 && position < tempList.Count)
        {
            tempList.RemoveAt(position);
        }

        turnQueue = new Queue<IActable>(tempList);
    }

    public void MoveTurn(IActable combatant, int newPosition)
    {
        List<IActable> tempList = new List<IActable>(turnQueue);
        tempList.Remove(combatant);

        if (newPosition >= 0 && newPosition < tempList.Count)
        {
            tempList.Insert(newPosition, combatant);
        }
        else
        {
            tempList.Add(combatant);
        }

        turnQueue = new Queue<IActable>(tempList);
    }

    public void RotateTurns(int steps)
    {
        List<IActable> tempList = new List<IActable>(turnQueue);

        for (int i = 0; i < steps; i++)
        {
            IActable first = tempList[0];
            tempList.RemoveAt(0);
            tempList.Add(first);
        }

        turnQueue = new Queue<IActable>(tempList);
    }

    public void ReverseTurns()
    {
        List<IActable> tempList = new List<IActable>(turnQueue);
        tempList.Reverse();
        turnQueue = new Queue<IActable>(tempList);
    }

    public void DuplicateTurn(IActable combatant)
    {
        List<IActable> tempList = new List<IActable>(turnQueue);
        int index = tempList.IndexOf(combatant);

        if (index >= 0)
        {
            tempList.Insert(index + 1, combatant);
        }

        turnQueue = new Queue<IActable>(tempList);
    }

    public void SplitTurn(IActable combatant)
    {
        List<IActable> tempList = new List<IActable>(turnQueue);
        int index = tempList.IndexOf(combatant);

        if (index >= 0)
        {
            tempList.Insert(index + 1, combatant);
            tempList.Insert(index + 2, combatant);
        }

        turnQueue = new Queue<IActable>(tempList);
    }

    public void MergeTurns(IActable combatant1, IActable combatant2)
    {
        List<IActable> tempList = new List<IActable>(turnQueue);
        int index1 = tempList.IndexOf(combatant1);
        int index2 = tempList.IndexOf(combatant2);

        if (index1 >= 0 && index2 >= 0)
        {
            tempList.RemoveAt(index2);
            tempList[index1] = combatant1;
        }

        turnQueue = new Queue<IActable>(tempList);
    }

    public void SplitQueue(int splitIndex)
    {
        List<IActable> tempList = new List<IActable>(turnQueue);
        List<IActable> firstHalf = tempList.GetRange(0, splitIndex);
        List<IActable> secondHalf = tempList.GetRange(splitIndex, tempList.Count - splitIndex);

        turnQueue = new Queue<IActable>(firstHalf);
        Queue<IActable> secondQueue = new Queue<IActable>(secondHalf);

        // Handle the second queue as needed
    }

    public void MergeQueues(Queue<IActable> otherQueue)
    {
        List<IActable> tempList = new List<IActable>(turnQueue);
        tempList.AddRange(otherQueue);

        turnQueue = new Queue<IActable>(tempList);
    }

    public void PrintQueue()
    {
        Debug.Log("Turn Queue:");
        foreach (var actable in turnQueue)
        {
            Debug.Log(actable.GetType().Name);
        }
    }

    public void ClearQueue()
    {
        turnQueue.Clear();
    }

    public void ResetQueue()
    {
        turnQueue.Clear();
        StopAllCoroutines();
    }

    public void PauseQueue()
    {
        StopAllCoroutines();
    }

    public void ResumeQueue()
    {
        StartCoroutine(HandleTurns());
    }

    public void ShuffleQueue()
    {
        List<IActable> tempList = new List<IActable>(turnQueue);
        turnQueue.Clear();

        while (tempList.Count > 0)
        {
            int randomIndex = Random.Range(0, tempList.Count);
            turnQueue.Enqueue(tempList[randomIndex]);
            tempList.RemoveAt(randomIndex);
        }
    }

    public void SortQueueBySpeed()
    {
        List<IActable> tempList = new List<IActable>(turnQueue);
        tempList.Sort((a, b) => a.GetSpeed().CompareTo(b.GetSpeed()));
        turnQueue = new Queue<IActable>(tempList);
    }

    public void SkipQueueTurn(IActable combatant)
    {
        Queue<IActable> newQueue = new Queue<IActable>();

        while (turnQueue.Count > 0)
        {
            IActable actable = turnQueue.Dequeue();
            if (actable != combatant)
            {
                newQueue.Enqueue(actable);
            }
        }

        turnQueue = newQueue;
        turnQueue.Enqueue(combatant);
    }

    public void DelayQueueTurn(IActable combatant, int delayTurns)
    {
        Queue<IActable> newQueue = new Queue<IActable>();

        while (turnQueue.Count > 0)
        {
            IActable actable = turnQueue.Dequeue();
            if (actable != combatant)
            {
                newQueue.Enqueue(actable);
            }
        }

        for (int i = 0; i < delayTurns; i++)
        {
            newQueue.Enqueue(combatant);
        }

        turnQueue = newQueue;
    }

    public void AdvanceQueueTurn()
    {
        if (turnQueue.Count > 0)
        {
            IActable currentActable = turnQueue.Dequeue();
            turnQueue.Enqueue(currentActable);
        }
    }

    public void ReverseQueueTurn()
    {
        if (turnQueue.Count > 0)
        {
            List<IActable> tempList = new List<IActable>(turnQueue);
            turnQueue.Clear();

            for (int i = tempList.Count - 1; i >= 0; i--)
            {
                turnQueue.Enqueue(tempList[i]);
            }
        }
    }

    public void SwapQueueTurns(IActable combatant1, IActable combatant2)
    {
        List<IActable> tempList = new List<IActable>(turnQueue);
        int index1 = tempList.IndexOf(combatant1);
        int index2 = tempList.IndexOf(combatant2);

        if (index1 >= 0 && index2 >= 0)
        {
            tempList[index1] = combatant2;
            tempList[index2] = combatant1;
        }

        turnQueue = new Queue<IActable>(tempList);
    }

    public void InsertQueueTurn(IActable combatant, int position)
    {
        List<IActable> tempList = new List<IActable>(turnQueue);

        if (position >= 0 && position < tempList.Count)
        {
            tempList.Insert(position, combatant);
        }
        else
        {
            tempList.Add(combatant);
        }

        turnQueue = new Queue<IActable>(tempList);
    }

    public void RemoveQueueTurnAt(int position)
    {
        List<IActable> tempList = new List<IActable>(turnQueue);

        if (position >= 0 && position < tempList.Count)
        {
            tempList.RemoveAt(position);
        }

        turnQueue = new Queue<IActable>(tempList);
    }

    public void MoveQueueTurn(IActable combatant, int newPosition)
    {
        List<IActable> tempList = new List<IActable>(turnQueue);
        tempList.Remove(combatant);

        if (newPosition >= 0 && newPosition < tempList.Count)
        {
            tempList.Insert(newPosition, combatant);
        }
        else
        {
            tempList.Add(combatant);
        }

        turnQueue = new Queue<IActable>(tempList);
    }

    public void RotateQueueTurns(int steps)
    {
        List<IActable> tempList = new List<IActable>(turnQueue);

        for (int i = 0; i < steps; i++)
        {
            IActable first = tempList[0];
            tempList.RemoveAt(0);
            tempList.Add(first);
        }

        turnQueue = new Queue<IActable>(tempList);
    }

    public void ReverseQueueTurns()
    {
        List<IActable> tempList = new List<IActable>(turnQueue);
        tempList.Reverse();
        turnQueue = new Queue<IActable>(tempList);
    }

    public void DuplicateQueueTurn(IActable combatant)
    {
        List<IActable> tempList = new List<IActable>(turnQueue);
        int index = tempList.IndexOf(combatant);

        if (index >= 0)
        {
            tempList.Insert(index + 1, combatant);
        }

        turnQueue = new Queue<IActable>(tempList);
    }

    public void SplitQueueTurn(IActable combatant)
    {
        List<IActable> tempList = new List<IActable>(turnQueue);
        int index = tempList.IndexOf(combatant);

        if (index >= 0)
        {
            tempList.Insert(index + 1, combatant);
            tempList.Insert(index + 2, combatant);
        }

        turnQueue = new Queue<IActable>(tempList);
    }

    public void MergeQueueTurns(IActable combatant1, IActable combatant2)
    {
        List<IActable> tempList = new List<IActable>(turnQueue);
        int index1 = tempList.IndexOf(combatant1);
        int index2 = tempList.IndexOf(combatant2);

        if (index1 >= 0 && index2 >= 0)
        {
            tempList.RemoveAt(index2);
            tempList[index1] = combatant1;
        }

        turnQueue = new Queue<IActable>(tempList);
    }
}