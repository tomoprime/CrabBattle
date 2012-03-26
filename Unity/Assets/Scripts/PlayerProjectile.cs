using UnityEngine;
using System.Collections;

public class PlayerProjectile : MonoBehaviour {

    public float speed = 550;
    public float life = 3f;

    private bool moving;
    
	// Use this for initialization
	void Start () {
        moving = true;
	}
	
	// Update is called once per frame
	void Update () {
        if(moving)
            transform.Translate(0f, 0f, speed * Time.deltaTime);

        if(!IsInvoking("Autodestruct"))
            Invoke("Autodestruct",life);
	}

    void Autodestruct()
    {
        Destroy(this.gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        if(!moving)
            return; //This bullet hit another collider already.

        if (other.tag == "Enemy" || other.tag == "WeakSpot" || other.tag == "EnemyProtected")
        {
            //Debug.Log("Enemy Hit!");
            moving = false;

            if (other.tag == "Enemy")
                NetworkManager.GetInstance().DealEnemyDamage(1, false);

            if (other.tag == "WeakSpot")
            {
                NetworkManager netman = NetworkManager.GetInstance();

                netman.DealEnemyDamage(5, true);
                netman.EnemyManager.HitWeakpoint();
                netman.EnemyManager.WeakPointFeedback(0.75f);
            }

            
            if (other.tag == "EnemyProtected")
            {
                int rnd = Random.Range(1, 5);
                GameObject.Instantiate(Resources.Load("Sounds/Ricochet/Rico" + rnd), Vector3.zero, Quaternion.identity);
            }

            GameObject.Instantiate(Resources.Load("Detonator-Simple"),gameObject.transform.position,Quaternion.identity);
        }
    }
}