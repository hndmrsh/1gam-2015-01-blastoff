using UnityEngine;
using System.Collections;

public class Ship : MonoBehaviour {

    private const float MAX_FLICKER_TIME = 0.2f;
    private const int MAX_FLICKERS = 5;

    private const float MAX_CAMERA_SHAKE = 0.08f;
    private const float MAX_VELOCITY = 20f;

    public int thrustAmount = 1000;

    public AudioClip fire, stop;
    private AudioSource engine;

    public Mesh damagedMesh;
    public Material damagedMaterial;

    private Light cockpitLight;
    private bool cockpitLightOn;
    private float currentFlickerTime;
    private int flickersLeft;

    public float engineOnVolume = 0.4f;
    public float engineOffVolume = 0.08f;

    private float startCameraRotX;
    private float startCameraRotY;

    public bool Dead
    {
        get;
        set;
    }

    private bool thrustOn;
    public bool ThrustOn
    {
        get
        {
            return thrustOn;
        }
        set
        {
            if (thrustOn != value)
            {
                UpdateAudio(value);
            }

            thrustOn = value;
        }
    }

    private void UpdateAudio(bool play)
    {
        if (play)
        {
            engine.volume = engineOnVolume;
            AudioSource.PlayClipAtPoint(fire, this.transform.position);
            GetComponentInChildren<ParticleSystem>().Play();
        }
        else
        {
            engine.volume = engineOffVolume;
            AudioSource.PlayClipAtPoint(stop, this.transform.position);
            GetComponentInChildren<ParticleSystem>().Stop();
        }
    }

    void Start()
    {
        Dead = false;
        engine = GetComponent<AudioSource>();
        engine.volume = engineOffVolume;

        cockpitLight = GetComponentInChildren<Light>();

        Vector3 cameraRot = Camera.main.transform.rotation.eulerAngles;
        startCameraRotX = cameraRot.x;
        startCameraRotY = cameraRot.y;
    }

    public void Update()
    {
        if(ThrustOn)
        {
            Vector3 thrust = transform.up * (thrustAmount * Time.deltaTime);
            rigidbody.AddForce(thrust);
        }


        if (flickersLeft > 0 && cockpitLightOn) 
        {
            if (currentFlickerTime <= 0f)
            {
                flickersLeft--;

                currentFlickerTime = (float) (Random.value * MAX_FLICKER_TIME);

                cockpitLight.enabled = !cockpitLight.enabled;
            }
            else
            {
                currentFlickerTime -= Time.deltaTime;

            }
        }
        else
        {
            cockpitLight.enabled = cockpitLightOn;
        }

        if (!Dead)
        {
            float maxCameraShakeWeighted = Mathf.Abs((rigidbody.velocity.y / MAX_VELOCITY) * MAX_CAMERA_SHAKE);
            float xRot = Random.Range(startCameraRotX - maxCameraShakeWeighted, startCameraRotX + maxCameraShakeWeighted);
            float yRot = Random.Range(startCameraRotY - maxCameraShakeWeighted, startCameraRotY + maxCameraShakeWeighted);
            Camera.main.transform.rotation = Quaternion.Euler(new Vector3(xRot, yRot));
        }
    }

    // OnCollisionEnter is called when this collider/rigidbody has begun touching another rigidbody/collider (Since v1.0)
    public void OnCollisionEnter(Collision collision)
    {
        Enemy e = collision.gameObject.GetComponent<Enemy>();
        if (e)
        {
            if (!Dead)
            {
                Dead = true;
                engine.Stop();

                cockpitLightOn = false;
                cockpitLight.enabled = false;

                GameController gameController = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameController>();
                gameController.GameOver();

                GetComponent<MeshFilter>().mesh = damagedMesh;
                GetComponent<MeshRenderer>().material = damagedMaterial;
            }

            e.BlowUp();
        }
    }

    public void SetLightEnabled(bool enabled)
    {
        if (!cockpitLightOn && enabled)
        {
            flickersLeft = MAX_FLICKERS;
        }

        cockpitLightOn = enabled;
    }

}
