using UnityEngine;

[CreateAssetMenu(fileName = "New Player Class", menuName = "Game/Player Class")]
public class PlayerClass : ScriptableObject
{
    public string className;
    public GameObject prefab;
}
