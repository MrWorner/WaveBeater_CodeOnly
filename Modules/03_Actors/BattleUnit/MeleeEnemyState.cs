using UnityEngine;

public class MeleeEnemyState : MonoBehaviour
{
    public bool hasThrownHammer { get; private set; } = false;

    public void DisableUnit()
    {
        hasThrownHammer = true;
        Debug.Log($"{name} отключен после броска молота.");
    }
}