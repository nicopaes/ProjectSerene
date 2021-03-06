﻿using UnityEngine;
using System.Collections;
using System;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

[RequireComponent (typeof (Controller2D))]
public class PlayerComponent : MonoBehaviour {

    public delegate void playerAction();
    public static event playerAction ActionButton;

    [Header("Debug?")]
	public bool debug;

	[Header("Movement Variables")]
	public float maxJumpHeight = 4;
	public float minJumpHeight = 1;
	public float timeToJumpApex = .4f;
	public float fallMultipler = 2.5f;
	[Space]
	public float accelerationTimeAirborne = .2f;	
	public float accelerationTimeGrounded = .1f;
	[Range(0.1f,50f)]
	public float moveSpeed = 4;
    public float runSpeed = 8;
	[Header("MELEE ATTACK")]
	public MeleeAttack meleeAtt;
	public float activeAttackTime = 0.5f;


    [Space]
    [Header("PRIVATE DEBUG SHIT")]
	[SerializeField]
	float gravity;
	[SerializeField]
	float maxJumpVelocity;
	[SerializeField]
	float minJumpVelocity;
	[SerializeField]
	[Space]
	public float velocityXSmoothing;

    [SerializeField]
	Vector3 velocity;

	public Controller2D controller {get; private set;}

    public Animator anim;

	Vector2 directionalInput;
    [SerializeField]
    [Space]

    [HideInInspector]
    public GameObject nearBox = null;

	private Transform spawnPoint;

    [HideInInspector]
    public bool isOnDialogTrigger = false;
    private bool holdingBox = false;

    private GameObject originalBoxParent;

    //diz se o player está respawning ou não. 
    private bool _respawning;

    //diz se o movemento do player está bloqueado ou não
    private bool _movementBlocked;
    private float _runMultiplier = 1;

    private bool _alreadyKilledThisScene;

    //diz se o player esta escondido em alguma caixa
    public bool IsHidden;


    void Start() {
		controller = GetComponent<Controller2D> ();
        anim = GetComponentInChildren<Animator> ();

        spawnPoint = GameObject.FindWithTag("Respawn").transform;

		gravity = -(2 * maxJumpHeight) / Mathf.Pow (timeToJumpApex, 2);
		maxJumpVelocity = Mathf.Abs(gravity) * timeToJumpApex;
		minJumpVelocity = Mathf.Sqrt (2 * Mathf.Abs (gravity) * minJumpHeight);

        //_movementBlocked = false;
        BlockPlayerMovement(false);

        //garante que só o meu audio listener esta ligado no começo de uma cena
        GameObject.FindObjectOfType<ChangeScene>().audioListener.enabled = false;
        this.GetComponent<AudioListener>().enabled = true;

        _alreadyKilledThisScene = false;
        IsHidden = false;
	}

	void OnEnable()
	{
        //transform.parent.position = spawnPoint.position;
        
        //desnecessário com reload da cena para respawnar o player
        //transform.localPosition = Vector3.zero;
	}

    private void FixedUpdate()
    {
        anim.SetBool("onPush", false);
    }

    void Update() {
       
        RecalculatePhysics(debug);
		CalculateVelocity ();

        if (_movementBlocked && !gameObject.isStatic)
        {
            //se movemento está bloqueado, 
            //zera velocity.x
            velocity.x = 0.0f;

            //mantem velocidade x só pra baixo. É uma maneira simplista de garantir que o player não vai ficar flutuando mas também não vai poder pular
            velocity.y = velocity.y <= 0.0f ? velocity.y : 0.0f;
            //o problema com isso: animação de início do pulo ainda é feita... bloquear depois
        }

        controller.Move (velocity * Time.deltaTime);
                
        if (controller.collisionsInf.above || controller.collisionsInf.below) {
            velocity.y = 0;
            anim.SetBool("onFloor", true);
        }

        if (!controller.collisionsInf.below)
        {
            anim.SetBool("onFloor", false);
        }


        //Entregando valores ao animator
        anim.SetFloat("velH", velocity.x);
    }

	public void SetDirectionalInput (Vector2 input) {
		directionalInput = input;
	}

    public Vector2 GetDirectionalInput()
    {
        return directionalInput;
    }

	public void OnJumpInputDown() 
	{		
		if (controller.collisionsInf.below) {
			velocity.y = maxJumpVelocity;
		}
	}

	public void OnJumpInputUp() {
		if (velocity.y > minJumpVelocity) {
			velocity.y = minJumpVelocity;
		}
	}

	void CalculateVelocity() {
         
        float targetVelocityX = directionalInput.x * moveSpeed * _runMultiplier;
        velocity.x = Mathf.SmoothDamp (velocity.x, targetVelocityX, ref velocityXSmoothing, (controller.collisionsInf.below)?accelerationTimeGrounded:accelerationTimeAirborne);
        if (velocity.y < 0f)
		{
			velocity.y += gravity * fallMultipler * Time.deltaTime;
		}
		else
		{			
			velocity.y += gravity * Time.deltaTime;
		}
	}
    void RecalculatePhysics(bool debug)
    {
        if (debug)
        {
            gravity = -(2 * maxJumpHeight) / Mathf.Pow(timeToJumpApex, 2);
            maxJumpVelocity = Mathf.Abs(gravity) * timeToJumpApex;
            minJumpVelocity = Mathf.Sqrt(2 * Mathf.Abs(gravity) * minJumpHeight);
        }
    }
    public void OnActionDown()
    {
        isOnDialogTrigger = true; 

        ActionButton();
		//StartCoroutine(DoMeleeAttack(activeAttackTime));
		
    }

    public void OnActionUp()
    {
        isOnDialogTrigger = false;
        //StartCoroutine(DoMeleeAttack(activeAttackTime));
    }

    public void KillPlayer()
    {
        if(_alreadyKilledThisScene) return;

        //toca som
        //preciso desligar o meu audio listener antes
        this.GetComponent<AudioListener>().enabled = false;
        GameObject.FindObjectOfType<ChangeScene>().PlayCaughtSound();
        //reloada scene atual
        GameObject.FindObjectOfType<ChangeScene>().ChangeSingleScene(SceneManager.GetActiveScene().name, false);
        
        _alreadyKilledThisScene = true;
    }

    

    public void BlockPlayerMovement(bool block)
    {
        _movementBlocked = block;
        anim.SetBool("isFrozen", block);
    }

    public bool MovementIsBlocked()
    {
        return _movementBlocked;
    }

    public void OnRunInputDown()
    {
        _runMultiplier = runSpeed / moveSpeed;
        Debug.Log(_runMultiplier);
        anim.SetBool("running", true);
    }

    public void OnRunInputUp()
    {
        _runMultiplier = 1;
        anim.SetBool("running", false);
        
    }

    //// Realiza o respawn do player com um delay
    //public IEnumerator RespawnPlayerWithDelay(float delay)
    //{
    //    _respawning = true;
    //    yield return new WaitForSeconds(delay);
    //    Player.SetActive(true);
    //    _respawning = false;
    //}

    //REMOÇÃO DA MECANICA DE GRAB
    //// Gruda o player colado a caixa e desfreeza a posição x da caixa
    //public void GrabBox(){

    //    if(nearBox){
    //        nearBox.GetComponent<PushObject>().checkBox(this.gameObject);
    //    }
    //}

    /*IEnumerator DoMeleeAttack(float activeTime)
	{
		meleeAtt.startCheckingCollision();
		Debug.Log("ATTACK");
		yield return new WaitForSeconds(activeTime);
		meleeAtt.stopCheckingCollision();
		Debug.Log("STOP ATTACK");
	}*/
}