using UnityEngine;
using System.Collections;

public class Ship : MonoBehaviour {

    public int thrustAmount = 1000;

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
            thrustOn = value;     
        }
    }

    void Start()
    {
        Dead = false;
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
        if (collision.gameObject.GetComponent<Enemy>())
        {
            Dead = true;
            GameController gameController = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameController>();
            gameController.GameOver();
        }
    }

}
