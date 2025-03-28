using UnityEngine;

public class CharacterStatistic : MonoBehaviour
{
    private int currentHealth;
    public int currentEndurance { get; private set; }

    private int deathAnimation;
    private int[] lightDamageAnimationArray;
    private int[] heavyDamageAnimationArray;

    private CharacterManager characterManager;

    [Header("Components")]
    [SerializeField] private UIBar healthBar;
    [SerializeField] private UIBar enduranceBar;

    [Header("Parameters")]
    [SerializeField] private int healthLevel;
    [SerializeField] private int enduranceLevel;

    private void Awake()
    {
        characterManager = GetComponent<CharacterManager>();
    }

    private void Start()
    {
        PrepareDamageAnimations();
        deathAnimation = Animator.StringToHash("Death");
    }

    public void SetBarUIs(UIBar hB, UIBar eB)
    {
        healthBar = hB;
        enduranceBar = eB;
    }

    private void PrepareDamageAnimations()
    {
        int lightDamage1 = Animator.StringToHash("Light Damage 1");
        int lightDamage2 = Animator.StringToHash("Light Damage 1");

        int heavyDamage1 = Animator.StringToHash("Heavy Damage 1");
        int heavyDamage2 = Animator.StringToHash("Heavy Damage 2");

        lightDamageAnimationArray = new int[] { lightDamage1, lightDamage2 };
        heavyDamageAnimationArray = new int[] { heavyDamage1, heavyDamage2 };
    }

    public void ResetStats()
    {
        characterManager.isDead = false;
        int multiplier = (characterManager.characterType == CharacterType.Player) ? 10 : 3;

        currentHealth = healthLevel * multiplier;
        currentEndurance = enduranceLevel * multiplier;

        if(healthBar != null)
        {
            healthBar.SetMaxValue(currentHealth);
            healthBar.SetCurrentValue(currentHealth);
        }
    }

    public void ReduceEndurance(float value)
    {

    }

    public void TakeDamage(int damageValue, AttackType attackType)
    {
        currentHealth -= damageValue;
        healthBar.SetCurrentValue(currentHealth);
        CharacterAnim anim = characterManager.AnimatorManagaer;

        if(currentHealth <= 0)
        {
            currentHealth = 0;
            characterManager.isDead = true;
            anim.PlayTargetAnimation(deathAnimation, true);
            return;
        }
        PlayDamageAnimation(anim, attackType);
    }

    public void PlayDamageAnimation(CharacterAnim anim, AttackType attackType)
    {
        if (attackType == AttackType.Light)
        {
            int lightRandom = Random.Range(0, lightDamageAnimationArray.Length);
            anim.PlayTargetAnimation(lightDamageAnimationArray[lightRandom], true);
            return;
        }
        int heavyRandom = Random.Range(0, heavyDamageAnimationArray.Length);
        anim.PlayTargetAnimation(heavyDamageAnimationArray[heavyRandom], true);
    }
}