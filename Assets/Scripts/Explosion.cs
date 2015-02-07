using UnityEngine;
using System.Collections;

public class Explosion : MonoBehaviour {

    private Light light;

    private float timeToFade = 0.7f;
    private float timeElapsed = 0f;

    private float startIntensity;

	// Use this for initialization
	void Start () {
        light = GetComponentInChildren<Light>();
        startIntensity = light.intensity;
	}
	
	// Update is called once per frame
	void Update () {
        timeElapsed += Time.deltaTime;
        light.intensity = Mathf.Lerp(startIntensity, 0f, timeElapsed / timeToFade);

        Debug.Log("intensity = " + light.intensity);
	}
}
