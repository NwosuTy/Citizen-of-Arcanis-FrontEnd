using UnityEngine;

public class AttackTrigger : MonoBehaviour
{
    private BeastAI beast;

    void Start()
    {
        beast = GetComponentInParent<BeastAI>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            beast.OnAttackRangeEnter();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            beast.OnAttackRangeExit();
        }
    }
}
