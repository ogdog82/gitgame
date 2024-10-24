using System.Collections;

public interface IActable
{
    IEnumerator TakeTurn();
    bool IsAlive();
}