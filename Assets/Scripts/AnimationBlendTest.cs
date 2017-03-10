using UnityEngine;
using System.Collections;

public class AnimationBlendTest : MonoBehaviour {

    public float lerpValue;

    Animator animator;

	// Use this for initialization
	void Start () {
        animator = GetComponent<Animator>();
        animator.speed = 0f;
	}
	
	// Update is called once per frame
	void Update () {
        animator.Play("Take 001", -1, Mathf.Clamp01(lerpValue));
	}
}
