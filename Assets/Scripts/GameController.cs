using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GameController : MonoBehaviour {

    private const string KEY_HIGH_SCORE = "key_high_score";
    private const int PLAYER_LAYER_MASK = 1 << 9;
    private const float TIME_TO_FADE = 0.2f;
    private const float TIME_FOR_DUMMIES = 3f;
    private const float END_MUSIC_DELAY = 0.8f;

    public AudioSource introMusic;
    public AudioSource endMusic;

    public Color startColour;
    public Color endColour;

    public int dayEnd, nightStart;

    public GUIStyle guiStyle;
    public GUIStyle deadGuiStyle;

    public GameObject titleCard;
    public GameObject scoreCard;
    public Text touchToStartText;

    private Text[] titleTexts;
    private Text[] scoreTexts;
    private Color[] startTitleColours;
    private Color[] startScoreColours;
    private Color[] targetTitleColours;
    private Color[] targetScoreColours;

    private bool fading = false;
    private float timeFaded = 0f;

    public Text scoreText;
    public Text highScoreText;
    public Text tryAgainText;
    public Text newHighScoreText;

    public Ship playerShip;
    public Enemy[] enemies;
    public Camera camera;

    private bool started = false;
    private float dummyTimer = 0f;

    [Range(0,1)]
    public float spawnRandomness = 0.2f;

    [Range(0.1f, 0.8f)]
    public float minSpawnRateSeconds = 1.2f;

    [Range(0.001f,0.8f)]
    public float maxSpawnRateSeconds = 0.1f;


    public AnimationCurve difficultyCurve;

    [Range(100,10000)]
    public int maxDifficultyAtScore = 1000;

    [Range(0.05f, 0.4f)]
    public float panBounds = 0.2f;

    [SerializeField]
    private float spawnRate = 0f;

    public bool IgnoreCurrentTouch
    {
        get;
        set;
    }

    private Vector3 panUpPos;
    private Vector3 panDownPos;

    private int score = 0;
    private int highScore = 0;

    private Vector3 startingCameraPos;

    private bool desktopTouchDown;
    private float timeToNextEnemySpawn;

    public Image vignette;
    private float vignetteMinAlpha = (30f / 255f);
    private float vignetteMaxAlpha = (40f / 255f);

	// Use this for initialization
	void Start () {
        desktopTouchDown = false;
        IgnoreCurrentTouch = true;

        float halfScreenWidth = Screen.width / 2;
        float boundDown = Screen.height * panBounds;
        float boundUp = Screen.height - boundDown;

        panUpPos = new Vector3(halfScreenWidth, boundUp, 0);
        panDownPos = new Vector3(halfScreenWidth, boundDown, 0);

        startingCameraPos = camera.transform.position;

        // load high score
        highScore = PlayerPrefs.GetInt(KEY_HIGH_SCORE, 0);
        highScoreText.text = highScore.ToString();

        // initialize targetColours
        InitializeTextGroupTargetColours(titleCard, out titleTexts, out startTitleColours, out targetTitleColours);
        InitializeTextGroupTargetColours(scoreCard, out scoreTexts, out targetScoreColours, out startScoreColours);

        touchToStartText.gameObject.SetActive(false);

        GenerateTimeToNextEnemySpawn();
	}

    private void InitializeTextGroupTargetColours(GameObject textGroup, out Text[] texts, out Color[] startColours, out Color[] targetColours)
    {
        texts = textGroup.GetComponentsInChildren<Text>();
        
        startColours = new Color[texts.Length];
        for (int i = 0; i < texts.Length; i++)
        {
            Color c = texts[i].color;
            c.a = 1f;
            startColours[i] = c;
        }

        targetColours = new Color[texts.Length];
        for (int i = 0; i < texts.Length; i++)
        {
            Color c = texts[i].color;
            c.a = 0f;
            targetColours[i] = c;
        }
    }

	// Update is called once per frame
	void Update () 
    {
        // check for back button on Android
        if (Application.isMobilePlatform && Input.GetKeyDown(KeyCode.Escape)) {
            Application.Quit(); 
        }

        // check for touch and apply thrust to ship
        bool touch = CheckInput();

        if (!started && touch)
        {
            started = true;
            introMusic.Play();
        }
        else if (!started && dummyTimer < TIME_FOR_DUMMIES)
        {
            dummyTimer += Time.deltaTime;
        } 
        else if (!started && !touchToStartText.gameObject.activeSelf) 
        {
            touchToStartText.gameObject.SetActive(true);
        }

        if (touch && timeFaded < TIME_TO_FADE)
        {
            fading = true;
        }

        if (fading)
        {
            float dC = timeFaded / TIME_TO_FADE;
            
            for (int i = 0; i < titleTexts.Length; i++)
            {
                titleTexts[i].color = Color.Lerp(startTitleColours[i], targetTitleColours[i], dC);
            }

            for (int i = 0; i < scoreTexts.Length; i++)
            {
                scoreTexts[i].color = Color.Lerp(startScoreColours[i], targetScoreColours[i], dC);
            }

            timeFaded += Time.deltaTime;

            if (timeFaded >= TIME_TO_FADE)
            {
                fading = false;
                for (int i = 0; i < titleTexts.Length; i++)
                {
                    titleTexts[i].color = targetTitleColours[i];
                }

                for (int i = 0; i < scoreTexts.Length; i++)
                {
                    scoreTexts[i].color = targetScoreColours[i];
                }
            }
        }

        // set background colour
        float t = (camera.transform.position.y - dayEnd) / (float)(nightStart - dayEnd);
        camera.backgroundColor = Color.Lerp(startColour, endColour, t);

        // flicker vignette
        Color c = vignette.color;
        c.a = Mathf.Lerp(vignetteMinAlpha, vignetteMaxAlpha, Random.value);
        vignette.color = c;

        if (playerShip.Dead)
        {
            if (touch)
            {
                // restart game
                Application.LoadLevel(Application.loadedLevel);
            }
        }

        playerShip.ThrustOn = touch && !playerShip.Dead;

        // check if ship is within "pan zone"
        bool shouldCameraPan = CheckCameraPan() && !playerShip.Dead;
        if (shouldCameraPan)
        {
            camera.transform.parent = playerShip.transform;
        }
        else
        {
            camera.transform.parent = null;
        }

        // spawn enemies
        SpawnEnemies();

        if (!playerShip.Dead)
        {
            // calculate score
            score = (int)(playerShip.transform.position.y);
            scoreText.text = score.ToString();
            if (score > highScore)
            {
                highScoreText.text = score.ToString();
            }

            int target = (dayEnd + nightStart) / 3;
            playerShip.SetLightEnabled(score >= target);
        }
	}

    private void SpawnEnemies()
    {
        timeToNextEnemySpawn -= Time.deltaTime;

        if (timeToNextEnemySpawn < 0)
        {
            Enemy.Direction dir = (Random.value < 0.5 ? Enemy.Direction.Left : Enemy.Direction.Right);
            bool success = enemies[0].Spawn(dir, camera.transform.position.z + playerShip.transform.position.z);

            if (success)
            {
                // only reset the timer if an enemy was spawned successfully; otherwise, let's try again
                GenerateTimeToNextEnemySpawn();
            }
        }
    }

    private void GenerateTimeToNextEnemySpawn()
    {
        spawnRate = Mathf.Lerp(minSpawnRateSeconds, maxSpawnRateSeconds, difficultyCurve.Evaluate(score / (float)maxDifficultyAtScore));

        float minTime = spawnRate * (1 - spawnRandomness);
        float maxTime = spawnRate * (1 + spawnRandomness);

        timeToNextEnemySpawn += Mathf.Lerp(minTime, maxTime, Random.value);
    }

    private bool CheckCameraPan()
    {
        // we don't want to let the camera drop below its starting position
        if (camera.transform.position.y < startingCameraPos.y)
        {
            camera.transform.position = startingCameraPos;
        }


        Ray upRay = camera.ScreenPointToRay(panUpPos);
        Ray downRay = camera.ScreenPointToRay(panDownPos);

        RaycastHit upHit;
        RaycastHit downHit;

        // we assume that a velocity of 0 doesn't require panning, otherwise we get some strange
        // behaviour when we first play the game!

        if (playerShip.rigidbody.velocity.y > 0)
        {
            return Physics.Raycast(upRay, out upHit, Mathf.Infinity, PLAYER_LAYER_MASK);
        }
        else if (playerShip.rigidbody.velocity.y < 0 && camera.transform.position.y > startingCameraPos.y)
        {
            // only check for downwards pan if we are not past the starting camera position
            return Physics.Raycast(downRay, out downHit, Mathf.Infinity, PLAYER_LAYER_MASK);
        }

        return false;
    }

    private bool CheckInput()
    {
        if(Application.isMobilePlatform)
        {
            if(Input.touches.Length > 0 && !IgnoreCurrentTouch)
            {
                return true;
            }

            if (Input.touches.Length == 0)
            {
                IgnoreCurrentTouch = false;
            }

            return false;
        } 
        else 
        {
            if (Input.GetMouseButtonDown(0))
            {
                desktopTouchDown = true;
            }

            if(Input.GetMouseButtonUp(0)) 
            {
                desktopTouchDown = false;
            }

            if (!desktopTouchDown)
            {
                IgnoreCurrentTouch = false;
            } 
            else if (IgnoreCurrentTouch) {
                desktopTouchDown = false;
            }

            return desktopTouchDown;
        }

    }

    public void GameOver()
    {
        IgnoreCurrentTouch = true;
        if (score > highScore)
        {
            PlayerPrefs.SetInt(KEY_HIGH_SCORE, score);
        }

        if (introMusic.isPlaying)
        {
            introMusic.Stop();
        }

        endMusic.PlayDelayed(END_MUSIC_DELAY);

        
        if (score > highScore)
        {
            newHighScoreText.gameObject.SetActive(true);
        }
        else
        {
            tryAgainText.gameObject.SetActive(true);
        }
    }
}
