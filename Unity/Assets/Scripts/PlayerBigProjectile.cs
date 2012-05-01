using UnityEngine;
using System.Collections;

public class PlayerBigProjectile : MonoBehaviour {

    public float speed = 700;
    public float life = 3f;

    LensFlare flare;

    private bool moving;

	// Use this for initialization
	void Start () {
        moving = true;
        flare = gameObject.GetComponent<LensFlare>();
	}
	
	// Update is called once per frame
	void Update () {
        if(moving)
            transform.Translate(0f, 0f, speed * Time.deltaTime);
	}

    IEnumerator Autodestruct()
    {
        while (life > 0)
        {
            life -= Time.deltaTime;
            flare.brightness = life;
            yield return new WaitForEndOfFrame();
        }

        Destroy(this.gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!moving)
            return; //This bullet hit another collider already.

        if (other.tag == "Enemy" || other.tag == "WeakSpot" || other.tag == "EnemyProtected")
        {
            Debug.Log("Enemy Hit!");
            moving = false;

            StartCoroutine(Autodestruct());

            if (other.tag == "Enemy")
                NetworkManager.Instance.DealEnemyDamage(5, false);//GetInstance().DealEnemyDamage(5, false);

            if (other.tag == "WeakSpot")
            {
                NetworkManager netman = NetworkManager.Instance;//GetInstance();

                netman.DealEnemyDamage(20, true);
                netman.EnemyManager.HitWeakpoint();
                netman.EnemyManager.WeakPointFeedback(1f);
            }

            if (other.tag == "EnemyProtected")
            {
                int rnd = Random.Range(1, 5);
                GameObject.Instantiate(Resources.Load("Sounds/Ricochet/Rico" + rnd), Vector3.zero, Quaternion.identity);
            }

            GameObject.Instantiate(Resources.Load("Detonator-CrazySparks"),gameObject.transform.position,Quaternion.identity);
            GameObject.Instantiate(Resources.Load("SmallExplosion"), gameObject.transform.position, Quaternion.identity);
        }
    }
}