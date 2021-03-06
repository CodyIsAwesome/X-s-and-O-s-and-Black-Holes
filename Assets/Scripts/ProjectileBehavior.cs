﻿using UnityEngine;
using System.Collections;

public class ProjectileBehavior : MonoBehaviour
{
    public float speed = 20f;
    public float followIntensity = 10f;
    public float blackHoleGravity = 8f;
    public float maxLifetime = 20f;

    public float distanceNeededForKill = 5f;

    public enum WeaponPower { zero, over9000 };
    public WeaponPower damage = WeaponPower.over9000;

    public GameObject tailParticleEffect;
    public GameObject deathParticleEffect;

    private GameObject target;
    private GameObject blackHole;
    private Vector3 velocity;

    private bool alive = true; //this gets turned to false once the object is technically dead, but alive so the animations can finish
    private bool playedCloseToTargetAlarm = false;

    private InputManager im;
    private AudioManager audio;

    public void Init(GameObject t, GameObject bh, Vector3 forward, AudioManager am)
    {
        //Don't hold an internal timer, let Unity handle it when the time has elapsed
        Invoke("DestroyThis", maxLifetime);
        target = t;
        blackHole = bh;
        audio = am;
        forward.Normalize();
        velocity = forward * speed;
        im = GameObject.Find("Controller Scripts").GetComponent<InputManager>();
    }

    void Update()
    {
        UpdateVelocity();
        MoveThis();
        CheckForKill();
    }

    private void MoveThis()
    {
        if (alive)
            this.transform.Translate(velocity);
    }

    private void UpdateVelocity()
    {
        if (!alive)
            return;
        Vector3 normalizedVectorToBH = GetDirection(this.transform.position, blackHole.transform.position);
        Vector3 normalizedVectorToTarget = GetDirection(this.transform.position, target.transform.position);
        Vector3 bhPull = normalizedVectorToBH * blackHoleGravity;
        Vector3 targetPull = normalizedVectorToTarget * followIntensity;
        velocity += bhPull + targetPull;
        velocity.Normalize();
        velocity *= speed;
    }

    private Vector3 GetDirection(Vector3 a, Vector3 b)
    {
        Vector3 direction = b - a;
        direction.Normalize();
        return direction;
    }

    //This method gets invoked when its lifetime is up
    private void DestroyThis() 
    {
        ParticleSystem trail = tailParticleEffect.GetComponent<ParticleSystem>();
        trail.enableEmission = false;
        trail.Clear();
        ParticleSystem death = deathParticleEffect.GetComponent<ParticleSystem>();
        float deathDelay = death.duration;
        deathDelay *= 3f; //Make it longer so fade effects have a chance to finish, instead of abruptly vanishing
        deathParticleEffect.SetActive(true);
        Invoke("ActuallyDestroyThis", deathDelay);
        alive = false;
        audio.SWProjectileDenotateHarmlessly();
    }

    //This one gets invoked when the particle effects are done and it is time to clean up this memory
    private void ActuallyDestroyThis()
    {
        Destroy(this.gameObject);
    }

    private void CheckForKill()
    {
        if ( (damage == WeaponPower.zero && Debug.isDebugBuild) || !alive)
            return;
        Vector3 here = this.transform.position;
        Vector3 there = target.transform.position;
        float distance = Vector3.Distance(here, there);
        if (distance < distanceNeededForKill * 50 && !playedCloseToTargetAlarm)
        {
            playedCloseToTargetAlarm = true;
            audio.SWIncomingProjectileAlarm();
        }
        if (distance < distanceNeededForKill)
        {
            im.ResolveContestedBoard(target);
        }
    }
}
