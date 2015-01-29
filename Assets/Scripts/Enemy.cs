using UnityEngine;
using System.Collections;

public class Enemy : MonoBehaviour {

    private const int ENEMIES_LAYER_MASK = 1 << 8;

    public enum Direction {
        Left, Right
    }

    private const float OFFSCREEN_SPAWN_MULTIPLIER = 2f;

    public float enemySpeed = 200f;
    [Range(0f,1f)]
    public float speedRandomness = 0.1f;
    public Direction movementDirection;

    private float destroyX;
    private float speedMultiplier;

	// Use this for initialization
	void SetVelocity () {
        // the speed multiplier is a base multiplier which is used to
        // offset the effects of different aspect ratios
        speedMultiplier = (9f / 16f) / (Screen.height / (float) Screen.width);
        float speed = Mathf.Lerp(enemySpeed * (1 - speedRandomness), enemySpeed * (1 + speedRandomness), Random.value);
        float mag = speed * speedMultiplier;
        rigidbody.velocity = (movementDirection == Direction.Left ? Vector3.right * mag : Vector3.left * mag);
	}

    void Update()
    {
        if(movementDirection == Direction.Left) {
            if (transform.position.x >= destroyX)
            {
                Destroy(gameObject);
            }
        }
        else
        {
            if (transform.position.x <= destroyX)
            {
                Destroy(gameObject);
            }
        }
    }

    public bool Spawn(Direction dir, float z)
    {
        float halfObjectWidth = renderer.bounds.size.x / 2;
        float halfObjectHeight = renderer.bounds.size.y / 2; 
        
        float leftSpawnX = Camera.main.ScreenToWorldPoint(new Vector3(0, 0, z)).x + halfObjectWidth;
        float rightSpawnX = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, 0, z)).x - halfObjectWidth;
        
        float spawnX;
        float destroyX;
        if (dir == Direction.Right)
        {
            spawnX = leftSpawnX;
            destroyX = rightSpawnX;
        }
        else
        {
            spawnX = rightSpawnX;
            destroyX = leftSpawnX;
        }

        float randomY = Screen.height * OFFSCREEN_SPAWN_MULTIPLIER * Random.value;
        Vector3 pos = new Vector3(spawnX, Camera.main.ScreenToWorldPoint(new Vector3(0, randomY, z)).y);

        float rayDist = Mathf.Abs(leftSpawnX - rightSpawnX);

        if (TestPos(pos, dir, halfObjectHeight, rayDist))
        {
            Enemy e = (Enemy)Instantiate(this, pos, Quaternion.identity);

            e.movementDirection = dir;
            e.destroyX = destroyX;
            e.SetVelocity();

            return true;
        }

        return false;
    }

    private bool TestPos(Vector3 pos, Direction dir, float halfObjectHeight, float rayDist)
    {
        if (pos.y < 3)
        {
            // don't spawn close to start position
            return false;
        }

        // don't spawn enemies on a collision course with each other
        halfObjectHeight *= 1.2f;
        Vector3 top = pos + (transform.up * halfObjectHeight);
        Vector3 bottom = pos + (transform.up * -halfObjectHeight);
        Vector3 centre = pos;

        Vector3 direction = dir == Direction.Left ? transform.right : transform.right * -1;
        
        Ray topRay = new Ray(top, direction);
        Ray bottomRay = new Ray(bottom, direction);
        Ray centreRay = new Ray(centre, direction);

        Debug.DrawRay(top, direction, Color.blue, 4);
        Debug.DrawRay(bottom, direction, Color.green, 4);
        Debug.DrawRay(centre, direction, Color.red, 4);

        if (Physics.Raycast(topRay, rayDist, ENEMIES_LAYER_MASK) ||
            Physics.Raycast(bottomRay, rayDist, ENEMIES_LAYER_MASK) ||
            Physics.Raycast(centreRay, rayDist, ENEMIES_LAYER_MASK))
        {
            Debug.Log("HIT ANOTHER ENEMY, TRYING TO SPAWN AGAIN");
            return false;
        }


        return true;
    }
	
}
