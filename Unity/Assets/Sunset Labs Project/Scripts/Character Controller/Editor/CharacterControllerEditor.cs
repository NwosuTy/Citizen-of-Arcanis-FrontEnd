using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CharacterCombat))]
public class CharacterCombatEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        CharacterCombat combat = (CharacterCombat)target;

        if(GUILayout.Button("Set Damage Colliders"))
        {
            combat.SetDamageColliders();
        }
    }
}


