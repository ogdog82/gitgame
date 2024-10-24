using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    private Queue<IActable> turnOrder = new Queue<IActable>();
    private bool isProcessingTurn = false;

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
        }
    }

    public void StartCombat(List<IActable> combatants)
    {
        turnOrder.Clear();
        foreach (var combatant in combatants)
        {
            if (combatant != null && combatant.IsAlive())
            {
                turnOrder.Enqueue(combatant);
                Debug.Log($"Added {combatant} to turn order.");
            }
        }

        if (turnOrder.Count > 0)
        {
            StartNextTurn();
        }
        else
        {
            Debug.LogWarning("No combatants in turn order!");
        }
    }

    public void StartNextTurn()
    {
        if (isProcessingTurn || turnOrder.Count == 0)
        {
            Debug.LogWarning("No combatants in turn order!");
            return;
        }

        IActable currentActor = turnOrder.Dequeue();
        StartCoroutine(ProcessTurn(currentActor));
    }

    private IEnumerator ProcessTurn(IActable currentActor)
    {
        isProcessingTurn = true;

        if (currentActor != null && currentActor.IsAlive())
        {
            Debug.Log($"Processing turn for {currentActor}");
            yield return StartCoroutine(currentActor.TakeTurn());
            turnOrder.Enqueue(currentActor);
        }

        isProcessingTurn = false;

        if (turnOrder.Count > 0)
        {
            StartNextTurn();
        }
    }



    public void EndPlayerTurn()
    {
        isProcessingTurn = false;
        StartNextTurn();
    }

    public void RemoveCombatant(IActable combatant)
    {
        List<IActable> tempList = new List<IActable>(turnOrder);
        tempList.Remove(combatant);
        turnOrder = new Queue<IActable>(tempList);
    }
}