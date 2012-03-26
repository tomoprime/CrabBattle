using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour {

    NetworkManager Netman;
    BulletManager BM;
    
    float VelocityX;
    float VelocityY;
    bool IsShooting;
    int MyID = -1;
    bool IsLocal;
    bool ShieldOn;
    bool HasRigidBody = false;

    GameObject Shield;

    GameObject powermeter;
    GameObject powerbar;
    GUITexture pb;

    internal GameObject playername;

    float barlerp = 0f;

    Vector3 RealPosition;

    bool initialized = false;

    float PlayerSpeed = 40f;
    float ShotDelay = 0.2f;
    float LastShotTime = 0f;

    float ShipPower = 25f;

    float TimeSinceHit = 0f;

    enum SpecialType
    {
        BigProjectile,
        ShieldOn,
        ShieldOff
    }

    GameObject[] guns = new GameObject[3];
    int lastGunFired = 0;

    public void Test(int i)
    {
        Debug.Log("Hello from " + i);
    }

    public void SetupPlayer(int id, bool local)
    {
        MyID = id;
        IsLocal = local;

        gameObject.name = "Player " + MyID;

        //I doubt any player will actually notice that shots cycle between
        //3 locations on the ship, but oh well.

        guns[0] = GameObject.Find("/Player " + MyID + "/Ship/Gun1");
        guns[1] = GameObject.Find("/Player " + MyID + "/Ship/Gun2");
        guns[2] = GameObject.Find("/Player " + MyID + "/Ship/Gun3");
    }

	// Use this for initialization
	void Start () {
        Netman = NetworkManager.GetInstance();

        BM = BulletManager.GetInstance();

        VelocityX = 0f;
        VelocityY = 0f;
        IsShooting = false;
        RealPosition = gameObject.transform.position;
	}

    public void PushUpdate(float x, float y, float xvel, float yvel, bool isShooting, float triptime)
    {
        //Player is updating their position.
        float newx = x + PlayerSpeed * xvel * triptime;
        float newy = y + PlayerSpeed * yvel * triptime;

        //This is where we predict they are right now.
        RealPosition = new Vector3(newx, 15f, newy);

        VelocityX = xvel;
        VelocityY = yvel;
        IsShooting = isShooting;
    }

    public void UseSpecial(int specialType)
    {
        //Networked action request for a player.

        //Should probably be a case.
        if(specialType == (int)SpecialType.BigProjectile)
            BM.PlayerBigShot(guns[0].transform.position, guns[0].transform.rotation, false);
        if (specialType == (int)SpecialType.ShieldOn)
        {
            ShieldOn = true;
            Shield = GameObject.Instantiate(Resources.Load("Shield(Ally)"), gameObject.transform.position, Quaternion.identity) as GameObject;
            Shield.transform.parent = gameObject.transform;
            GameObject.Instantiate(Resources.Load("Sounds/ShieldUp"), Vector3.zero, Quaternion.identity); //Sound
        }
        if (specialType == (int)SpecialType.ShieldOff)
        {
            Destroy(Shield);
            ShieldOn = false;
        }
    }

    public void TakeHit()
    {
        //Local only, player takes damage.

        if (Netman.EnemyManager.CurrentHealth <= 0)
        {
            //Don't deal damage after the enemy is dead.  We're nice that way.  Still do a small explosion though.
            GameObject.Instantiate(Resources.Load("Detonator-CrazySparks"), gameObject.transform.position, Quaternion.identity);
            return;
        }

        if (TimeSinceHit > 2f && !ShieldOn)
        {
            TimeSinceHit = 0f;
            GameObject.Instantiate(Resources.Load("Detonator-Insanity"), gameObject.transform.position, Quaternion.identity);

            //Give 'em power if they don't have enough for the shield.
            if (ShipPower < 10f)
                ShipPower = 10f;

            Netman.TakeHit();

            ShieldsUp();
        }
    }

    void ShieldsUp()
    {
        //Local only, player activates sheild.
        ShieldOn = true;
        Shield = GameObject.Instantiate(Resources.Load("Shield(Player)"), gameObject.transform.position, Quaternion.identity) as GameObject;
        Shield.transform.parent = gameObject.transform;
        GameObject.Instantiate(Resources.Load("Sounds/ShieldUp"), Vector3.zero, Quaternion.identity); //Sound
        Netman.SendPlayerSpecial((int)SpecialType.ShieldOn);
    }

    void UpdatePowerBar()
    {
        float width = ShipPower / 20 * 152;
        if (width > 152)
            width = 152;

        barlerp = Mathf.Lerp(barlerp, width, Time.deltaTime*3);

        pb.pixelInset = new Rect(6, 6, barlerp, 44);
    }

	// Update is called once per frame
	void Update () {

        TimeSinceHit += Time.deltaTime;

        //Playercontroller has no use outside of the playable game phase.
        if (Netman.gamephase >= 2 && MyID >= 0)
        {
            if (!IsLocal)
            {
                //Stuff to show player names over their heads.
                if (!initialized)
                {
                    playername = GameObject.Instantiate(Resources.Load("PlayerText")) as GameObject;
                    GUIText name = playername.GetComponent<GUIText>();

                    PlayerObject player = Netman.Players.Find(p => p.Id == MyID);
                    name.text = player.Name;
					//playername.transform.parent=transform;
                    initialized = true;
                }

                playername.transform.position = Camera.mainCamera.WorldToViewportPoint(transform.position) + new Vector3(0f, 0.05f, 0f);
            }

            if (IsLocal)
            {
                if (!HasRigidBody)
                {
                    //For some reason itween animation in the intro breaks if it has a rigid body.
                    //The fix?  Add the rigid body after the intro.
                    Rigidbody rb = gameObject.AddComponent<Rigidbody>();
                    rb.useGravity = false;
                    rb.isKinematic = true;
                    HasRigidBody = true;
                }

                if (!initialized)
                {
                    initialized = true;
                    powerbar = GameObject.Instantiate(Resources.Load("PowerBar")) as GameObject;
                    powermeter = GameObject.Instantiate(Resources.Load("EnergyBox")) as GameObject;
                    pb = powerbar.GetComponent<GUITexture>();

                }
                
                if (Netman.EnemyManager.CurrentHealth <= 0)
                {
                    if (powerbar)
                    {
                        Destroy(powerbar);
                        Destroy(powermeter);
                    }
                }
                else
                    UpdatePowerBar();



                //Input!!!
                float xMove = Input.GetAxisRaw("Horizontal");
                float zMove = Input.GetAxisRaw("Vertical");
                bool shoot = Input.GetButton("Fire1");
                bool bigshot = Input.GetButtonDown("Fire2");
                bool shieldon = Input.GetButtonDown("Fire3");

                ShipPower += Time.deltaTime;
                
                //Shield drains power at a rate dependant on game difficulty.
                if (ShieldOn)
                    ShipPower -= Time.deltaTime * (3 + Netman.difficulty);

                //Max power.
                if (ShipPower > 30f)
                    ShipPower = 30f;

                //Check if shield should end
                if (ShipPower <= 0 && Shield != null)
                {
                    Destroy(Shield);
                    ShieldOn = false;
                    Netman.SendPlayerSpecial((int)SpecialType.ShieldOff);
                }

                //Fire the big shot (if there's enough power)
                if (bigshot && ShipPower >= 5)
                {
                    BM.PlayerBigShot(guns[0].transform.position, guns[0].transform.rotation, true);
                    ShipPower -= 5;
                    Netman.SendPlayerSpecial((int)SpecialType.BigProjectile);
                }

                //Shield power on (if there's enough power)
                if (shieldon && ShipPower >= 10 && !ShieldOn)
                {
                    ShieldsUp();
                }

                //Move the player!
                gameObject.transform.position += new Vector3(xMove * PlayerSpeed * Time.deltaTime, 0f, zMove * PlayerSpeed * Time.deltaTime);

                //Send network message if the player's velocity has changed since last frame.
                if (xMove != VelocityX || zMove != VelocityY || shoot != IsShooting)
                    Netman.SendPlayerUpdate(MyID, xMove, zMove, shoot);

                //We use this just to check if they moved since last frame.
                VelocityX = xMove;
                VelocityY = zMove;
                IsShooting = shoot;
            }
            else
            {
                //Handle network player movement.  Presume they move in the same direction and lerp them to their predicted location.

                float xMove = VelocityX * PlayerSpeed * Time.deltaTime;
                float zMove = VelocityY * PlayerSpeed * Time.deltaTime;

                Vector3 movedistance = new Vector3(xMove, 0f, zMove);

                gameObject.transform.position += movedistance;
                RealPosition += movedistance;

                gameObject.transform.position = Vector3.Lerp(gameObject.transform.position, RealPosition, Time.deltaTime * 3);
            }

            //Make sure it doesn't get too far away from the crab.

            Vector3 crabspot = new Vector3(Netman.Enemy.gameObject.transform.position.x, gameObject.transform.position.y, Netman.Enemy.gameObject.transform.position.z);
            //Debug.DrawRay(crabspot, -(crabspot - gameObject.transform.position).normalized * 120); 

            if (Vector3.Distance(crabspot, gameObject.transform.position) > 120)
            {
                //If they're too far out, reel them in.
                Ray ray = new Ray(crabspot, -(crabspot - gameObject.transform.position));
                gameObject.transform.position = ray.GetPoint(120);
            }

            //Rotate player to target.
            Vector3 enemyLoc = new Vector3(Netman.Enemy.transform.position.x, 15f, Netman.Enemy.transform.position.z);
            Quaternion lookAt = Quaternion.LookRotation(enemyLoc - gameObject.transform.position);
            gameObject.transform.rotation = lookAt; // Quaternion.Lerp(player.transform.rotation, lookAt, 5f * Time.deltaTime);

            //Handle regular firing.
            LastShotTime = Mathf.Clamp(LastShotTime - Time.deltaTime, 0, ShotDelay);

            //Handle delay for shooting.
            if (IsShooting && LastShotTime <= 0)
            {
                LastShotTime = ShotDelay;

                //Cycle between available guns to fire.
                BM.PlayerShot(guns[lastGunFired].transform.position, guns[lastGunFired].transform.rotation, 180f, 3f, IsLocal);
                
                lastGunFired++;
                if (lastGunFired > 2)
                    lastGunFired = 0;
            }
        }
        else
            RealPosition = gameObject.transform.position; //We presume it's in the right spot before the game begins.
	}

    //Handle collisions of the player with the enemy.
    void OnTriggerEnter(Collider other)
    {
        if ((other.tag == "Enemy" || other.tag == "WeakSpot") && Netman.EnemyManager.CurrentHealth > 0)
            TakeHit();
    }

    void OnTriggerStay(Collider other)
    {
        if ((other.tag == "Enemy" || other.tag == "WeakSpot") && Netman.EnemyManager.CurrentHealth > 0)
            TakeHit();
    }
}
