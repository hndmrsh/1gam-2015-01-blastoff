using UnityEngine;
using System.Collections;

public class Ship : MonoBehaviour {

    public int thrustAmount = 1000;

    public AudioClip fire, stop;
    private AudioSource engine;

    public Mesh damagedMesh;
    public Material damagedMaterial;

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
            AudioSource.PlayClipAtPoint(fire, this.transform.position);
            engine.Play();
            GetComponentInChildren<ParticleSystem>().Play();
        }
        else
        {
            engine.Stop();
            AudioSource.PlayClipAtPoint(stop, this.transform.position);
            GetComponentInChildren<ParticleSystem>().Stop();
        }
    }

    void Start()
    {
        Dead = false;
        engine = GetComponent<AudioSource>();
    }

    public void Update()
    {
        if(ThrustOn)
        {
            Vector3 thrust = transform.up * (thrustAmount * Time.deltaTime);
            rigidbody.AddForce(thrust);
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

                GameController gameController = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameController>();
                gameController.GameOver();

                GetComponent<MeshFilter>().mesh = damagedMesh;
                GetComponent<MeshRenderer>().material = damagedMaterial;
            }

            e.BlowUp();
        }
    }

}
