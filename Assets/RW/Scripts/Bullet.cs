﻿/*
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

using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Physics Options")]
    public float speed;
    public Rigidbody rBody;
    public AudioClip impactSound;

    private void Start()
    {
        Begin();
    }

    public void Begin()
    {
        rBody.velocity = transform.forward * speed; //Set a forward velocity at given speed
    }

    private void OnCollisionEnter(Collision other)
    {
        rBody.velocity = Vector3.zero;

        AudioSource.PlayClipAtPoint(impactSound, other.GetContact(0).point);

        GetComponent<MeshRenderer>().enabled = false;
        GetComponent<BoxCollider>().enabled = false;

        //Spawn a Debris particle effect at this point using the Addressables System

        ReferenceLookupManager.instance.Instantiate("Debris", other.GetContact(0).point, other.GetContact(0).normal,
            this.transform).Completed += go =>
        {
            go.Result.GetComponent<ParticleSystem>().Play();
            GameObject.Destroy(go.Result,1);
        };
    }
}
