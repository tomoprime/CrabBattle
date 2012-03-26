using UnityEngine;
using System.Collections;

public class EnemySmallShotProjectile : MonoBehaviour {

    public GameObject Flare;

    GameObject Enemy;

    public float speed = 40f;
    float life = 15f;

    bool dying = false;
        
    public Vector3 direction;

	// Use this for initialization
	void Start () 
    {
        Enemy = NetworkManager.GetInstance().Enemy;
	}

    IEnumerator FadeAway(float time)
    {
        dying = true;
        float dietime = time;

        while (dietime > 0)
        {
            dietime -= Time.deltaTime;
            gameObject.transform.localScale = new Vector3(dietime / time, dietime / time, dietime / time);
            yield return new WaitForEndOfFrame();
        }

        Destroy(gameObject);
    }

	// Update is called once per frame
	void Update () {
        transform.position += direction * speed * Time.deltaTime;

        life -= Time.deltaTime;

        if (life < 0 && !dying)
            StartCoroutine(FadeAway(0.5f));

        if(!dying && Vector3.Distance(gameObject.transform.position, Enemy.transform.position) > 125)
            StartCoroutine(FadeAway(0.5f));
	}

    void OnTriggerEnter(Collider other)
    {
        //Debug.Log("Collision with " + other.tag);

        if(other.tag == "Shield")
            StartCoroutine(FadeAway(0.2f));

        if (other.tag == "Player")
        {
            PlayerController player = other.gameObject.GetComponent<PlayerController>();
            player.TakeHit();
            Destroy(gameObject);
        }
    }
}
