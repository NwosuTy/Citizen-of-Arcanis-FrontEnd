using System.Collections;
using UnityEngine;

public class CharacterStatistic : MonoBehaviour
{
    private int currentHealth;
    private bool showHealthBar;
    private WaitForSeconds waitForSeconds;

    private Camera mainCamera;
    public int currentEndurance { get; private set; }

    private int deathAnimation;
    private int[] lightDamageAnimationArray;
    private int[] heavyDamageAnimationArray;

    private CharacterManager characterManager;

    [Header("Components")]
    [SerializeField] private UIBar healthBar;
    [SerializeField] private UIBar enduranceBar;
    [SerializeField] private UIBar hoveringHealthBarAI;

    [Header("Parameters")]
    [SerializeField] private float duration;
    [SerializeField] private int healthLevel;
    [SerializeField] private int enduranceLevel;
    [SerializeField] private Vector3 hoverOffset;

    private void Awake()
    {
        characterManager = GetComponent<CharacterManager>();
    }

    private void Start()
    {
        PrepareDamageAnimations();
        mainCamera = Camera.main;

        waitForSeconds = new(duration);
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
        if(hoveringHealthBarAI != null)
        {
            hoveringHealthBarAI.SetBillboard(mainCamera, transform);
            hoveringHealthBarAI.gameObject.SetActive(showHealthBar);
        }
    }

    public void ReduceEndurance(float value)
    {

    }

    public void TakeDamage(int damageValue, AttackType attackType)
    {
        currentHealth -= damageValue;
        UpdateHealthBar();
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

    private void UpdateHealthBar()
    {
        showHealthBar = true;
        if(characterManager.characterType != CharacterType.AI || characterManager.combatMode)
        {
            healthBar.SetCurrentValue(currentHealth);
            return;
        }
        hoveringHealthBarAI.SetCurrentValue(currentHealth);
        hoveringHealthBarAI.gameObject.SetActive(showHealthBar);
        StartCoroutine(DisableHealthBar(hoveringHealthBarAI.gameObject));
    }

    private IEnumerator DisableHealthBar(GameObject healthBar)
    {
        while (showHealthBar == true)
        {
            yield return waitForSeconds;
            showHealthBar = false;
            healthBar.SetActive(false);
        }
    }
}