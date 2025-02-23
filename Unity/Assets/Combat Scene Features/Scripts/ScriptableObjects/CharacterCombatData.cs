using UnityEngine;

[CreateAssetMenu(fileName = "CharacterData", menuName = "ScriptableObject/CharacterCombatData")]
public class CharacterCombatData : ScriptableObject
{
    [field: SerializeField] public CharacterManager characterManager { get; private set; }
}
