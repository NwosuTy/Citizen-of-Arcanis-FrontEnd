using UnityEngine;
using Cinemachine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class CombatManager : MonoBehaviour
{
    private WaitForSeconds waitForSeconds;

    public Image CrossHairImg { get; private set; }
    public Transform CameraObject { get; protected set; }
    public CinemachineFreeLook FreeLookCamera { get; private set; }

    public static CombatManager Instance { get; private set; }
    [field: SerializeField] public CharacterManager PlayerCombatPrefab { get; private set; }
    [field: SerializeField] public CharacterManager OppositionCombatPrefab { get; private set; }

    [Header("Parameters")]
    [SerializeField] private float transitionDelay;
    [SerializeField] private Animator sceneTransitionAnimator;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("A duplicate CombatManager was found and destroyed.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        if (CrossHairImg == null)
        {
            CrossHairImg = GameObject.Find("Crosshair").GetComponent<Image>();
        }   
        if (FreeLookCamera == null)
        {
            FreeLookCamera = GameObject.Find("Gun Camera").GetComponent<CinemachineFreeLook>();
        }  
        if(CameraObject == null)
        {
            CameraObject = Camera.main.transform;
        }
    }

    private void Start()
    {
        waitForSeconds = new WaitForSeconds(transitionDelay);
    }

    public void AssignPlayer(CharacterManager player)
    {
        PlayerCombatPrefab = player;
    }

    public void StartDuel(CharacterManager npc)
    {
        OppositionCombatPrefab = npc;
        StartCoroutine(LoadCombatScene());
    }

    private IEnumerator LoadCombatScene()
    {
        sceneTransitionAnimator.SetTrigger("Fade Out");

        yield return waitForSeconds;
        SceneManager.LoadScene("Combat Scene");

        yield return waitForSeconds;
        sceneTransitionAnimator.CrossFade("RectangleGridOut", 0.0f);
    }
}
