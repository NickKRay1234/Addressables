/*
 * Copyright (c) 2020 Razeware LLC
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * Notwithstanding the foregoing, you may not use, copy, modify, merge, publish,
 * distribute, sublicense, create a derivative work, and/or sell copies of the
 * Software in any work that is designed, intended, or marketed for pedagogical or
 * instructional purposes related to programming, coding, application development,
 * or information technology.  Permission for such use, copying, modification,
 * merger, publication, distribution, sublicensing, creation of derivative works,
 * or sale is expressly withheld.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    [Header("Player Options")]
    [SerializeField]
    public float playerSpeed = 10;
    public float maxVelocityChange;
    public int solverIterations = 2; //How many collision iterations it solves for
    public LayerMask discludePlayer; //Layer mask for player movement

    [Header("Weapon Configuration")]
    public Animator weaponAnimator;
    public Transform aimPoint;
    new public Camera camera;
    public FreeCamera cameraController;
    public float fireRate;
    private ObjectPooler bulletPooler; //Pooling System for the bullets
    public Image crosshair;
    public AudioSource source;

    private bool aiming = false;
    private float fireTimer = 0f;
    private Rigidbody rBody; //Rigidbody Reference

    void Start()
    {
        //Find the crosshair through a predefined tag
        crosshair = GameObject.FindGameObjectWithTag("Crosshair").GetComponent<Image>();
        rBody = GetComponent<Rigidbody>();

        //Create object pooler with max bullet count of 40
        bulletPooler = new ObjectPooler(40);
    }

    //The Update methods deals with the input handling
    private void Update()
    {
        //Right Click : Allows for Aiming Functionality (Modifies gameplay for better aim and control)
        if (Input.GetMouseButton(1))
        {
            crosshair.enabled = true;
            weaponAnimator.SetBool("aiming", true);
            aiming = true;
            camera.fieldOfView = Mathf.Lerp(camera.fieldOfView, 60, 7*Time.deltaTime);
            cameraController.m_LookSpeedMouse = Mathf.Lerp(cameraController.m_LookSpeedMouse, 5, 7 * Time.deltaTime);
        }
        else
        {
            crosshair.enabled = false;
            aiming = false;
            weaponAnimator.SetBool("aiming", false);
            camera.fieldOfView = Mathf.Lerp(camera.fieldOfView, 90, 7*Time.deltaTime);
            cameraController.m_LookSpeedMouse = Mathf.Lerp(cameraController.m_LookSpeedMouse, 10, 7 * Time.deltaTime);
        }

        //Left Click : Allows for Shooting
        if (Input.GetMouseButton(0))
        {
            Vector3 dir = aimPoint.forward;

            //The direction of the bullet is changed depending on the aim state
            if (aiming)
            {
                //If aiming, shoot the bullet towards the crosshair for fine aiming control
                dir = camera.ScreenPointToRay(crosshair.rectTransform.position).direction;
            }
            else
            {
                dir = aimPoint.forward;
            }

            //Ensures shooting is only possible when the fire rate timer has run out
            if (fireTimer <= 0)
            {
                fireTimer = fireRate;
                weaponAnimator.SetBool("shooting", true);

                source.Play();
                //Instantiate bullet at the aim point
                bulletPooler.Instantiate(aimPoint.position,dir);
            }
            else
            {
                weaponAnimator.SetBool("shooting", false);
            }
        }
        else
        {
            weaponAnimator.SetBool("shooting", false);
        }

        //Controls the fire rate of the gun
        if (fireTimer >= 0)
        {
            fireTimer -= Time.deltaTime;
        }
    }

    //The fixed update is for movement that will be consistent across all devices
    void FixedUpdate()
    {
        //Target velocity from WASD keys
        Vector3 targetVelocity = camera.transform.TransformDirection
            (
                new Vector3(Input.GetAxis("Horizontal"),
                0,
                Input.GetAxis("Vertical")) * playerSpeed * Time.deltaTime
            );

        //Clamping Velocity
        var velocityChange = targetVelocity;
        velocityChange.x = Mathf.Clamp(velocityChange.x, -maxVelocityChange, maxVelocityChange);
        velocityChange.z = Mathf.Clamp(velocityChange.z, -maxVelocityChange, maxVelocityChange);
        velocityChange.y = 0;

        //Check all nearby colliders (except self)
        Collider[] c = Physics.OverlapSphere(transform.position, 20, discludePlayer);

        //Custom Collision Implementation
        foreach (Collider col in c)
        {
            Vector3 penDir = new Vector3();
            float penDist = 0f;
            Vector3 newDir = velocityChange;

            for (int i = 0; i < solverIterations; i++)
            {
                bool d = Physics.ComputePenetration(col, col.transform.position, col.transform.rotation, this.GetComponent<CapsuleCollider>(), transform.position + newDir, transform.rotation, out penDir, out penDist);

                if (d == false) continue;

                transform.position += -penDir.normalized * penDist;
                newDir = -penDir.normalized * penDist;
            }
        }

        //Moves the player towards the desired velocity with the added gravity
        transform.position += (velocityChange + Physics.gravity*Time.deltaTime);
    }
}


class ObjectPooler
{
    public int maxInstances; //How many bullets can exist in the scene at one time

    //The queue is used to ensure bullets are reused in First In First Out Order
    public Queue<GameObject> instanceQueue = new Queue<GameObject>();

    public ObjectPooler(int maxInstances)
    {
        this.maxInstances = maxInstances;
        //this.instance = instance;
    }

    public void Instantiate(Vector3 position, Vector3 faceDirection)
    {
        if (instanceQueue.Count == maxInstances)
        {
            GameObject t = instanceQueue.Dequeue(); //Get the bullet from Queue
            t.transform.position = position;
            t.transform.forward = faceDirection;
            t.GetComponent<Bullet>().Begin(); //Add force
            t.GetComponent<MeshRenderer>().enabled = true;
            t.GetComponent<BoxCollider>().enabled = true;
            instanceQueue.Enqueue(t); //Readd to the Queue
        }
        else
        {
            //Instantiate Object using ReferenceLookupManager and "Bullet" tag
            ReferenceLookupManager.instance.Instantiate(
                "Bullet",
                position,
                faceDirection,
                null
            ).Completed += reference => {
                instanceQueue.Enqueue(reference.Result);   //Ensure bullet is added to the Queue
            };
        }
    }
}