using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New AI Profile", menuName = "AI/AI Profile")]
public class AIProfile : ScriptableObject
{
    //[Header("Behavior")]
    //[Tooltip("Количество действий, которые юнит может выполнить за один ход.")]
    //public int maxActionPoints = 1;

    [Tooltip("Список всех действий, которые этот юнит в принципе может выполнять.")]
    public List<AIAction> availableActions;

    [Tooltip("Логика принятия решений для этого юнита.")]
    public AIBehaviorPattern behaviorPattern;
}