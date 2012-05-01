using UnityEngine;
using System.Collections;

public class EnemySpawnShotProjectile : MonoBehaviour {

    public GameObject Star1;
    public GameObject Star2;

    GameObject Enemy;

    float angle1 = 0f;
    float angle2 = 0f;

    float speed = 15f;
    float splittime = 1f;

    bool splitting = false;
    bool dying = false;
    
    float dietime;

    public Vector3 direction;

    Object spawnshot;

	// Use this for initialization
	void Start () 
    {
        spawnshot = Resources.Load("Bullets/EnemySmallShot");

        NetworkManager Netman = NetworkManager.Instance;//GetInstance();
        Enemy = Netman.Enemy;
        if (Netman.difficulty == 0)
        {
            speed = 20f;
            splittime = 3f;
        }
	}

    IEnumerator FadeAway(float time)
    {
        dying = true;
        dietime = time;

        while (dietime > 0)
        {
            dietime -= Time.deltaTime;
            gameObject.transform.localScale = new Vector3(dietime / time * 2, dietime / time * 2, dietime / time * 2);
            yield return new WaitForEndOfFrame();
        }

        Destroy(gameObject);
    }

    IEnumerator Split()
    {
        splitting = true;

        while (speed > 0)
        {
            speed -= Time.deltaTime * 10f;
            yield return new WaitForEndOfFrame();
        }

        int bulletstomake = NetworkManager.Instance.difficulty +1;//NetworkManager.GetInstance().difficulty +1;
        bulletstomake *= bulletstomake;
        float angle = 360 / (float)bulletstomake;

        for (int i = 0; i < bulletstomake; i++)
        {
            GameObject bullet = GameObject.Instantiate(spawnshot, gameObject.transform.position, Quaternion.identity) as GameObject;
            EnemySmallShotProjectile bulletctrl = bullet.GetComponent<EnemySmallShotProjectile>();

            Quaternion rot = Quaternion.Euler(new Vector3(0f, i * angle, 0f));
            bulletctrl.direction = rot * Vector3.forward;
        }

        GameObject.Instantiate(Resources.Load("Sounds/SpawnSmallShotFire"), Vector3.zero, Quaternion.identity);

        Destroy(gameObject);
    }
	
	// Update is called once per frame
	void Update () {
        Star1.transform.rotation = Quaternion.Euler(0f, angle1, 0f);
        angle1 += Time.deltaTime * 100f;

        Star2.transform.rotation = Quaternion.Euler(0f, angle2, 0f);
        angle2 -= Time.deltaTime * 80f;

        transform.position += direction * speed * Time.deltaTime;

        splittime -= Time.deltaTime;

        if (splittime < 0 && !splitting && !dying)
        {
            if (NetworkManager.Instance.difficulty == 0)//GetInstance().difficulty == 0)
                StartCoroutine(FadeAway(2f));
            else
                StartCoroutine(Split());
        }

        if (!dying && Vector3.Distance(gameObject.transform.position, Enemy.transform.position) > 125)
            StartCoroutine(FadeAway(0.5f));
	}

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Shield")
            StartCoroutine(FadeAway(0.2f));

        if (other.tag == "Player")
        {
            PlayerController player = other.gameObject.GetComponent<PlayerController>();
            player.TakeHit();
            Destroy(gameObject);
        }
    }
}
