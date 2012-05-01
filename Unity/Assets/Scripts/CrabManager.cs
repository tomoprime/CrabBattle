//#define Use_Vectrosity

using UnityEngine;
using System.Collections;

public class CrabManager : MonoBehaviour {

    NetworkManager Netman;
	GameManager gm;

    enum WalkStates
    {
        Stopped,
        WalkLeft,
        WalkRight
    }

    enum CrabActions
    {
        Walk,
        WalkLeft,
        WalkRight,
        WalkStop,
        SweepShot,
        RandomSpray,
        RapidCannon,
        MegaBeam,
        CrazyBarrage,
        CannonSpawn
    }

    int WalkState = 0;
    float WalkSpeed = 0.75f;

    Object BulletSmallShot;
    Object BulletSpawnShot;
    Object SoundRegularShot;

    GameObject healthbarobject;
    GameObject white;
    GUITexture healthbar;

    BulletManager BMan;
    
    public Material BoundaryMat;

    Vector3 RealPosition;
    Quaternion RealRotation;
    Vector3 CurrentTarget;

    float SyncLerp;
    float RealLerp;

    float lastWeakPointHit = 0f;

    bool Direction;
    bool BackGunActive = false;

    bool CrabDying = false;

    bool crabActive = false;

    public int CurrentHealth = 2000;
    public int MaxHealth = 2000;

    float healthLerp = 0;

    bool breaklaser = false;
    int diffmod = 0;
	
	float lastUpdate = 0;
	
	public bool isAlive
	{
		get {return !CrabDying;}
	}
        
#if Use_Vectrosity

    VectorLine Boundary;
    bool UpdatedBoundaries = false;

#endif
    
	// Use this for initialization
	void Start () 
    {
        Netman = NetworkManager.Instance;//.GetInstance();
		gm = GameManager.GetInstance();
        Netman.Enemy.animation.Play("idle");

        Animation a = Netman.Enemy.animation;

        //a.animation["laying"].blendMode = AnimationBlendMode.Additive;
        a.animation["laying"].layer = 1;
        a.Play("laying");

        a.animation["walkleft"].layer = 1;
        a.animation["walkright"].layer = 1;
        a.animation["crabdeath"].layer = 1;


        BulletSmallShot = Resources.Load("Bullets/EnemySmallShot");
        BulletSpawnShot = Resources.Load("Bullets/EnemySpawnShot");
        SoundRegularShot = Resources.Load("Sounds/SpawnShotFire");

        BMan = BulletManager.GetInstance();

        RealPosition = transform.position;
        RealRotation = transform.rotation;

        CurrentTarget = new Vector3(0f, transform.position.y, -50f);

        CurrentHealth = 2000;

        SyncLerp = 0f;
        RealLerp = 0f;

        white = GameObject.Find("White");
        if (white!=null)
			white.active = false; //Stupid being unable to find an inactive game object...

#if Use_Vectrosity
        Boundary = new VectorLine("Boundary", new Vector3[100], BoundaryMat, 3f);
        Vector.MakeCircleInLine(Boundary, new Vector3(0f, 1.5f, 0f), Vector3.up, 12f); //It should be 150 radius but the object we're taking transform of is scale 10
#endif
	}

    IEnumerator RandomSpray(float speed, int seed)
    {
        //Random spray speed doesn't change.  We use the speed value to adjust intensity.

        System.Random rng = new System.Random(seed);

        float actionMultiplier = 1; //(1 / speed);

        Debug.Log("Rapid Cannon start At: " + Time.time);

        Netman.Enemy.animation["randomspray"].layer = 2;
        Netman.Enemy.animation["randomspray"].speed = 0.85f; //hmm!
        Netman.Enemy.animation["randomspray"].blendMode = AnimationBlendMode.Additive;
        Netman.Enemy.animation.CrossFade("randomspray");

        GameObject gun1 = GameObject.Find("ArmGunL");
        GameObject gun2 = GameObject.Find("ArmGunR");
        GameObject gun3 = null;
        if (BackGunActive)
            gun3 = GameObject.Find("GunMid/Gun");
        GameObject projectile;
        EnemySmallShotProjectile bullet;
        Vector3 aim;
        
        int intensity = (int) ((speed - 1f) * 10);

        float wait = 0.5f;
        int shotstofire = 1;
        int maxshots = 1 + (intensity * Netman.difficulty)/3;
        
        float maxangle = 85f;
        int minspeed = 20;
        int maxspeed = 60;

        yield return new WaitForSeconds(1.1f * actionMultiplier);

        int firecount = 40;
        if (Netman.difficulty == 3)
            firecount = 45;
        if (Netman.difficulty == 0)
            firecount = 25;

        for (int i = 0; i < firecount; i++)
        {
            Vector3 gun1point = BMan.GetEnemyBulletSpawnPoint(gun1.transform.position);
            Vector3 gun2point = BMan.GetEnemyBulletSpawnPoint(gun2.transform.position);
            Vector3 gun3point = Vector3.zero;
            if (gun3 != null)
                gun3point = BMan.GetEnemyBulletSpawnPoint(gun3.transform.position + (Quaternion.Euler(gun3.transform.rotation.eulerAngles) * new Vector3(-25.2f, 0f, 0f)));


            for (int j = 0; j < shotstofire; j++)
            {
                float shotspeed = (float)rng.Next(minspeed * 10, maxspeed * 10) / 10f;
                float angle = (float)rng.Next(0, (int)maxangle * 2) - maxangle;

                aim = -gun1.transform.right;
                projectile = GameObject.Instantiate(BulletSmallShot, gun1point, Quaternion.identity) as GameObject;
                bullet = projectile.GetComponent<EnemySmallShotProjectile>();
                bullet.direction = Quaternion.Euler(new Vector3(0f, angle, 0f)) * aim;
                bullet.direction = new Vector3(bullet.direction.x, 0f, bullet.direction.z);
                bullet.speed = shotspeed;
            }

            for (int j = 0; j < shotstofire; j++)
            {
                float shotspeed = (float)rng.Next(minspeed * 10, maxspeed * 10) / 10f;
                float angle = (float)rng.Next(0, (int)maxangle * 2) - maxangle;

                aim = -gun2.transform.right;
                projectile = GameObject.Instantiate(BulletSmallShot, gun2point, Quaternion.identity) as GameObject;
                bullet = projectile.GetComponent<EnemySmallShotProjectile>();
                bullet.direction = Quaternion.Euler(new Vector3(0f, angle, 0f)) * aim;
                bullet.direction = new Vector3(bullet.direction.x, 0f, bullet.direction.z);
                bullet.speed = shotspeed;
            }

            if (gun3 != null)
            {
                for (int j = 0; j < Mathf.Clamp(shotstofire/2,1,3); j++)
                {
                    float shotspeed = (float)rng.Next(minspeed * 10, maxspeed * 10) / 10f;
                    float angle = (float)rng.Next(0, (int)maxangle * 2) - maxangle;

                    aim = -gun3.transform.right;
                    projectile = GameObject.Instantiate(BulletSmallShot, gun3point, Quaternion.identity) as GameObject;
                    bullet = projectile.GetComponent<EnemySmallShotProjectile>();
                    bullet.direction = Quaternion.Euler(new Vector3(0f, angle, 0f)) * aim;
                    bullet.direction = new Vector3(bullet.direction.x, 0f, bullet.direction.z);
                    bullet.speed = shotspeed;
                }
            }


            GameObject.Instantiate(SoundRegularShot, Vector3.zero, Quaternion.identity);

            yield return new WaitForSeconds(wait);

            wait -= 0.05f;
            if (wait < 0.1f && Netman.difficulty < 3)
                wait = 0.1f;
            if (wait < 0.07f)
                wait = 0.07f;
            if (wait < 0.2f && Netman.difficulty == 0)
                wait = 0.2f;

            if (Netman.difficulty < 3)
                shotstofire = i / 2;
            else
                shotstofire = i+1;
            
            if (shotstofire <= 0)
                shotstofire = 1;

            if (shotstofire > maxshots)
                shotstofire = maxshots;

            if (shotstofire < 2 && Netman.difficulty == 3)
                shotstofire = 2;
        }
        
        yield return null;
    }

    IEnumerator RapidCannon(float speed, int difficulty)
    {
        float actionMultiplier = (1 / speed);

        Debug.Log("Rapid Cannon start At: " + Time.time);

        Netman.Enemy.animation["rapidcannon"].layer = 2;
        Netman.Enemy.animation["rapidcannon"].speed = 1f * speed;
        Netman.Enemy.animation["rapidcannon"].blendMode = AnimationBlendMode.Additive;
        Netman.Enemy.animation.CrossFade("rapidcannon");

        GameObject gun1 = GameObject.Find("ArmGunL");
        GameObject gun2 = GameObject.Find("ArmGunR");
        GameObject gun3 = null;
        if (BackGunActive)
            gun3 = GameObject.Find("GunMid/Gun");
        GameObject projectile;
        EnemySmallShotProjectile bullet;
        Vector3 aim;

        float baseangle = 3f;
        if (difficulty > 0)
            baseangle /= difficulty * 1.5f;

        if (baseangle < 1)
            baseangle = 1;

        yield return new WaitForSeconds(0.5f * actionMultiplier);

        int k;
        int maxwidth = 1 + difficulty;
        if (maxwidth > 5)
            maxwidth = 5;
        if (maxwidth < 1)
            maxwidth = 1;
        float startangle = (maxwidth / 2f * baseangle * -1f);
        
        for (int j = 0; j < 5; j++)
        {
            float bspeed = 40f;

            for (int i = 0; i < 5; i++)
            {
                Vector3 gun1point = BMan.GetEnemyBulletSpawnPoint(gun1.transform.position);
                Vector3 gun2point = BMan.GetEnemyBulletSpawnPoint(gun2.transform.position);
                Vector3 gun3point = Vector3.zero;
                if(gun3 != null)
                     gun3point = BMan.GetEnemyBulletSpawnPoint(gun3.transform.position + (Quaternion.Euler(gun3.transform.rotation.eulerAngles) * new Vector3(-25.2f,0f,0f)));

                int cycles = (Netman.difficulty * 2) + 1;
                if (cycles > 5)
                    cycles = 5;

                for (int l = 0; l < cycles; l++)
                {
                    float haxangle = 0f;

                    switch (Netman.difficulty)
                    {
                        case 1: haxangle = -90f + (180f/2f) * l; break;
                        case 2: haxangle = -110f + (220f/4f) * l; break;
                        case 3: haxangle = -110f + (220f/4f) * l; break;
                    }
                        
                    for (k = 0; k < maxwidth; k++)
                    {
                        float angle = startangle + haxangle + baseangle * (float)k;

                        aim = -gun1.transform.right;
                        projectile = GameObject.Instantiate(BulletSmallShot, gun1point, Quaternion.identity) as GameObject;
                        bullet = projectile.GetComponent<EnemySmallShotProjectile>();
                        bullet.direction = Quaternion.Euler(new Vector3(0f, angle, 0f)) * aim;
                        bullet.direction.y = 0f;
                        bullet.speed = bspeed;
                    }

                    for (k = 0; k < maxwidth; k++)
                    {
                        float angle = startangle + haxangle + baseangle * (float)k;

                        aim = -gun2.transform.right;
                        projectile = GameObject.Instantiate(BulletSmallShot, gun2point, Quaternion.identity) as GameObject;
                        bullet = projectile.GetComponent<EnemySmallShotProjectile>();
                        bullet.direction = Quaternion.Euler(new Vector3(0f, angle, 0f)) * aim;
                        bullet.direction.y = 0f;
                        bullet.speed = bspeed;
                    }

                    if (gun3 != null && difficulty == Netman.difficulty) //second check means gun3 doesn't fire when megabeam is active
                    {
                        for (k = 0; k < Mathf.Clamp(maxwidth-2,1,999); k++)
                        {
                            float startangle2 = (Mathf.Clamp(maxwidth - 2, 1, 999) / 2f * baseangle * -1f);

                            float angle = startangle2 + haxangle + baseangle * (float)k;

                            aim = -gun3.transform.right;
                            projectile = GameObject.Instantiate(BulletSmallShot, gun3point, Quaternion.identity) as GameObject;
                            bullet = projectile.GetComponent<EnemySmallShotProjectile>();
                            bullet.direction = Quaternion.Euler(new Vector3(0f, angle, 0f)) * aim;
                            bullet.direction.y = 0f;
                            bullet.speed = bspeed;
                        }

                    }
                }

                GameObject.Instantiate(SoundRegularShot, Vector3.zero, Quaternion.identity);

                bspeed += 3f * Netman.difficulty;

                yield return new WaitForSeconds(0.10f * actionMultiplier);
            }
            yield return new WaitForSeconds(0.4f * actionMultiplier);
        }
    }

    IEnumerator SweepShot(float speed)
    {
        float actionMultiplier = (1/speed);

        Netman.Enemy.animation["sweepshot"].layer = 2;
        Netman.Enemy.animation["sweepshot"].speed = 1f * speed;
        Netman.Enemy.animation["sweepshot"].blendMode = AnimationBlendMode.Additive;
        Netman.Enemy.animation.CrossFade("sweepshot");

        GameObject gun1 = GameObject.Find("ArmGunL");
        GameObject gun2 = GameObject.Find("ArmGunR");
        GameObject projectile;
        EnemySpawnShotProjectile bullet;
        Vector3 aim;
        
        yield return new WaitForSeconds(1.55f * actionMultiplier);

        for (int i = 0; i < 5; i++)
        {
            aim = -gun1.transform.right;
            projectile = GameObject.Instantiate(BulletSpawnShot, BMan.GetEnemyBulletSpawnPoint(gun1.transform.position), Quaternion.identity) as GameObject;
            bullet = projectile.GetComponent<EnemySpawnShotProjectile>();
            bullet.direction = aim;

            aim = -gun2.transform.right;
            projectile = GameObject.Instantiate(BulletSpawnShot, BMan.GetEnemyBulletSpawnPoint(gun2.transform.position), Quaternion.identity) as GameObject;
            bullet = projectile.GetComponent<EnemySpawnShotProjectile>();
            bullet.direction = aim;

            GameObject.Instantiate(SoundRegularShot, Vector3.zero, Quaternion.identity);

            yield return new WaitForSeconds(0.15f * actionMultiplier);
        }
    }

    IEnumerator MegaBeam(float length, bool useRapidCannon)
    {
        if(useRapidCannon)
            StartCoroutine(RapidCannon(0.5f, Mathf.Clamp(Netman.difficulty-2, 0, 5)));
        yield return null;

        if (BackGunActive)
        {
            int diffmod2 = diffmod;

            GameObject gun3 = GameObject.Find("GunMid/Gun");
            //Vector3 gun3point = BMan.GetEnemyBulletSpawnPoint(gun3.transform.position + (Quaternion.Euler(gun3.transform.rotation.eulerAngles) * new Vector3(-25.2f,0f,0f)));

            GameObject.Instantiate(Resources.Load("Sounds/ChargeUpLaser"), Vector3.zero, Quaternion.identity);
            GameObject laserflare = GameObject.Instantiate(Resources.Load("LaserFlare"), Vector3.zero, Quaternion.identity) as GameObject;
            laserflare.transform.parent = gun3.transform;
            laserflare.transform.localPosition = new Vector3(-7.4f,0f,0f);

            LensFlare f = laserflare.GetComponent<LensFlare>();
            float brightness = 0f;
            while(brightness < 1.5f)
            {
                if (CurrentHealth <= 0)
                    yield break;

                brightness += Time.deltaTime;
                f.brightness = brightness;
                yield return new WaitForEndOfFrame();
            }

            yield return new WaitForSeconds(0.5f);
            GameObject beam = GameObject.Instantiate(Resources.Load("Beam"), Vector3.zero, Quaternion.identity) as GameObject;
            GameObject beam2 = null;
            GameObject beam3 = null;

            GameObject sound = GameObject.Instantiate(Resources.Load("Sounds/LaserShot"), Vector3.zero, Quaternion.identity) as GameObject;
            GameObject.Instantiate(Resources.Load("Sounds/LaserFiring"), Vector3.zero, Quaternion.identity);
            beam.transform.parent = gun3.transform;
            beam.transform.localPosition = new Vector3(-7.4f, 0f, 0f);
            beam.transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, 180f));

            TemporaryObject t = sound.GetComponent<TemporaryObject>();
            t.life = length + 2f;

            if (Netman.difficulty + diffmod2 >= 3)
            {
                beam2 = GameObject.Instantiate(Resources.Load("Beam"), Vector3.zero, Quaternion.identity) as GameObject;
                beam2.transform.parent = gun3.transform;
                beam2.transform.localPosition = new Vector3(-7.4f, 0f, 0f);
                beam2.transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, 135f));

                beam3 = GameObject.Instantiate(Resources.Load("Beam"), Vector3.zero, Quaternion.identity) as GameObject;
                beam3.transform.parent = gun3.transform;
                beam3.transform.localPosition = new Vector3(-7.4f, 0f, 0f);
                beam3.transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, 225f));
            }
            
            brightness = 4f;
            while (brightness > 1f)
            {
                if (CurrentHealth <= 0) //If he dies mid beam
                {
                    if (beam) GameObject.Destroy(beam);
                    if (beam2) GameObject.Destroy(beam2);
                    if (beam3) GameObject.Destroy(beam3);
                    if (sound) GameObject.Destroy(sound);
                    yield break;
                }

                brightness -= Time.deltaTime * 3;
                f.brightness = brightness;
                yield return new WaitForEndOfFrame();
            }
            brightness = 1f;
            f.brightness = brightness;

            GameObject beamcollider = GameObject.Instantiate(Resources.Load("LaserCollider"), Vector3.zero, Quaternion.identity) as GameObject;
            GameObject beamcollider2 = null;
            GameObject beamcollider3 = null;

            if (Netman.difficulty + diffmod2 >= 3)
            {
                beamcollider2 = GameObject.Instantiate(Resources.Load("LaserCollider"), Vector3.zero, Quaternion.identity) as GameObject;
                beamcollider3 = GameObject.Instantiate(Resources.Load("LaserCollider"), Vector3.zero, Quaternion.identity) as GameObject;
            }

            float time = 0f;

            breaklaser = false;

            while (time < length)
            {
                time += Time.deltaTime;

                if (CurrentHealth <= 0) //If he dies mid beam
                {
                    if (beam) GameObject.Destroy(beam);
                    if (beam2) GameObject.Destroy(beam2);
                    if (beam3) GameObject.Destroy(beam3);
                    if (sound) GameObject.Destroy(sound);
                    yield break;
                }

                Vector3 beamspawn = BMan.GetEnemyBulletSpawnPoint(beam.transform.position);
                beamcollider.transform.position = beamspawn;
                beamcollider.transform.rotation = beam.transform.rotation;

                if (Netman.difficulty + diffmod2 >= 3)
                {
                    beamspawn = BMan.GetEnemyBulletSpawnPoint(beam2.transform.position);
                    beamcollider2.transform.position = beamspawn;
                    beamcollider2.transform.rotation = beam2.transform.rotation;

                    beamspawn = BMan.GetEnemyBulletSpawnPoint(beam3.transform.position);
                    beamcollider3.transform.position = beamspawn;
                    beamcollider3.transform.rotation = beam3.transform.rotation;
                }

                if (breaklaser)
                {
                    time = length;
                    if (sound != null)
                        GameObject.Destroy(sound);
                }

                yield return new WaitForEndOfFrame();
            }

            GameObject.Destroy(beamcollider);
            if (Netman.difficulty + diffmod2 >= 3)
            {
                GameObject.Destroy(beamcollider2);
                GameObject.Destroy(beamcollider3);
            }

            LineRenderer l = beam.GetComponent<LineRenderer>();
            LineRenderer l2 = null;
            LineRenderer l3 = null;

            if (Netman.difficulty + diffmod2 >= 3)
            {
                l2 = beam2.GetComponent<LineRenderer>();
                l3 = beam3.GetComponent<LineRenderer>();
            }

            //AudioSource a = beamsound.GetComponent<AudioSource>();
            
            float width = 20f;
            while (width > 0f && brightness > 0f)
            {
                if (CurrentHealth <= 0)
                    yield break;

                width = Mathf.Clamp(width - Time.deltaTime * 20,0,20);
                brightness = Mathf.Clamp(brightness - Time.deltaTime/2f, 0, 1);
                f.brightness = brightness;
                l.SetWidth(width, width);
                if (Netman.difficulty + diffmod2 >= 3)
                {
                    l2.SetWidth(width, width);
                    l3.SetWidth(width, width);
                }
                //a.volume = brightness;
                yield return new WaitForEndOfFrame();
            }

            f.brightness = 0;
            yield return new WaitForSeconds(0.2f);

            GameObject.Destroy(beam);
            GameObject.Destroy(laserflare);

            if (Netman.difficulty + diffmod2 >= 3)
            {
                GameObject.Destroy(beam2);
                GameObject.Destroy(beam3);
            }
        }
    }

    IEnumerator CrazyBarrage()
    {
        Netman.Enemy.animation["shittonofbullets"].layer = 2;
        Netman.Enemy.animation["shittonofbullets"].speed = 1f;
        Netman.Enemy.animation["shittonofbullets"].blendMode = AnimationBlendMode.Additive;
        Netman.Enemy.animation.CrossFade("shittonofbullets");

        if (!BackGunActive)
            SpawnGun();

        yield return new WaitForSeconds(3.5f);

        GameObject gun1 = GameObject.Find("ArmGunL");
        GameObject gun2 = GameObject.Find("ArmGunR");
        GameObject gun3 = GameObject.Find("GunMid/Gun");
        GameObject projectile;
        EnemySmallShotProjectile bullet;
        Vector3 aim = Vector3.forward;

        float bspeed = 25f;

        float angle = 0f;

        bool laseron = false;

        float shotdelay = 0.04f;
        float waittime = 0.5f;

        int shotcount = (Netman.difficulty + diffmod) * 2 + 1;
        if(Netman.difficulty + diffmod >= 3)
            shotcount = (Netman.difficulty + diffmod) * 3 + 1;

        int cyclecount = (int) (33.6f / (0.5f + 0.04 * shotcount));

        for (int k = 0; k < cyclecount; k++)
        {
            if (!laseron && ((float)CurrentHealth / (float)MaxHealth < 0.1f))
            {
                StartCoroutine(MegaBeam(999, false));
                laseron = true;
            }

            for (int i = 0; i < shotcount; i++)
            {
                Vector3 gun1point = BMan.GetEnemyBulletSpawnPoint(gun1.transform.position);
                Vector3 gun2point = BMan.GetEnemyBulletSpawnPoint(gun2.transform.position);
                Vector3 gun3point = BMan.GetEnemyBulletSpawnPoint(gun3.transform.position + (Quaternion.Euler(gun3.transform.rotation.eulerAngles) * new Vector3(-25.2f, 0f, 0f)));

                for (int j = 0; j < 6; j++)
                {
                    float angle2 = angle + 60 * j;

                    projectile = GameObject.Instantiate(BulletSmallShot, gun1point, Quaternion.identity) as GameObject;
                    bullet = projectile.GetComponent<EnemySmallShotProjectile>();
                    bullet.direction = Quaternion.Euler(new Vector3(0f, angle2, 0f)) * aim;
                    bullet.direction.y = 0f;
                    bullet.speed = bspeed;
                }

                for (int j = 0; j < 6; j++)
                {
                    float angle2 = -angle - 60 * j;

                    projectile = GameObject.Instantiate(BulletSmallShot, gun2point, Quaternion.identity) as GameObject;
                    bullet = projectile.GetComponent<EnemySmallShotProjectile>();
                    bullet.direction = Quaternion.Euler(new Vector3(0f, angle2, 0f)) * aim;
                    bullet.direction.y = 0f;
                    bullet.speed = bspeed;
                }

                GameObject.Instantiate(SoundRegularShot, Vector3.zero, Quaternion.identity);

                yield return new WaitForSeconds(shotdelay);

                angle += 2.5f;
            }

            yield return new WaitForSeconds(waittime);
            angle += 25f;
        }

        breaklaser = true;
        diffmod += 2;

        yield return null;
    }
	/*
    void OnGUI()
    {
        if (gm.gamephase == 2 && CurrentHealth > 0)
        {
            GUI.Label(new Rect(10, 40, 200, 25), "Enemy Health: " + CurrentHealth);
        }
    }
	*/
    void WalkLeft(float speed)
    {
        Netman.Enemy.animation["walkleft"].speed = speed;
        gameObject.animation.CrossFade("walkleft");
    }

    public void CrabCommand(int crabAction, float actionSpeed, int seed)
    {
        if (CrabDying)
            return;

        Debug.Log("Command "+ System.Enum.GetName(typeof(CrabActions),crabAction) + " fired with speed of " + actionSpeed);

        switch ((CrabActions)crabAction)
        {
            case CrabActions.SweepShot:
                StartCoroutine(SweepShot(actionSpeed));
                break;
            case CrabActions.RapidCannon:
                StartCoroutine(RapidCannon(actionSpeed, Netman.difficulty));
                break;
            case CrabActions.RandomSpray:
                StartCoroutine(RandomSpray(actionSpeed, seed));
                break;
            case CrabActions.CannonSpawn:
                SpawnGun();
                break;
            case CrabActions.MegaBeam:
                StartCoroutine(MegaBeam(8f, true));
                break;
            case CrabActions.CrazyBarrage:
                StartCoroutine("CrazyBarrage");
                break;
            case CrabActions.WalkLeft:
                WalkLeft(actionSpeed);
                break;
                
        }
    }

    IEnumerator ChangeTargetSync(Vector3 Target, float wait)
    {
        yield return new WaitForSeconds(wait);

        CurrentTarget = Target;
    }

    IEnumerator BlinkWeakPointMessage(int blinkcount)
    {
        Vector3 pos = new Vector3(0f, 15f, -10f);
        GameObject marker = GameObject.Instantiate(Resources.Load("WeakPointMarker"), Vector3.zero, Quaternion.identity) as GameObject;
        //GameObject sphere = GameObject.Instantiate(Resources.Load("Sphere"), Vector3.zero, Quaternion.identity) as GameObject;

        GameObject torso = GameObject.Find("Torso");

        for(int i = 0; i < blinkcount; i++)
        {
            float time = 0f;
            marker.guiTexture.enabled = true;
            while (time < 0.2f)
            {
                time += Time.deltaTime;
                pos = gameObject.transform.rotation * new Vector3(0f, 15f, -10f);
                marker.transform.position = Camera.mainCamera.WorldToViewportPoint(torso.transform.position + pos);
                //sphere.transform.position = torso.transform.position + pos;
                yield return new WaitForEndOfFrame();
            }
            time = 0f;
            marker.guiTexture.enabled = false;
            while (time < 0.2f)
            {
                time += Time.deltaTime;
                //pos = gameObject.transform.rotation * new Vector3(0f, 15f, -10f);
                //sphere.transform.position = torso.transform.position + pos;
                yield return new WaitForEndOfFrame();
            }
        }

        GameObject.Destroy(marker);

        yield return null;
    }

    public void SpawnGun()
    {
        Vector3 pos = new Vector3(0f, 1.65f, 0.3f);

        GameObject torso = GameObject.Find("Torso");

        GameObject panel = GameObject.Instantiate(Resources.Load("backpanel"), transform.position + (pos * 10), transform.rotation) as GameObject;
        panel.rigidbody.AddForce(new Vector3(0.5f, 1.5f, 0f), ForceMode.Impulse);
        panel.rigidbody.AddTorque(new Vector3(0f, 0f, 10f));
        GameObject panel2 = GameObject.Instantiate(Resources.Load("backpanel"), transform.position + (pos * 10), transform.rotation) as GameObject;
        panel2.rigidbody.AddForce(new Vector3(-0.5f, 1.5f, -1f), ForceMode.Impulse);

        GameObject.Instantiate(Resources.Load("Detonator-Insanity"), transform.position + (pos * 10), transform.rotation);

        GameObject gun = GameObject.Instantiate(Resources.Load("crabgun"), transform.position, transform.rotation) as GameObject;
        gun.transform.parent = torso.transform;
        gun.transform.localPosition = new Vector3(-3.07f, 0f, 0f);
        gun.transform.localRotation = Quaternion.Euler(new Vector3(0f, 180f, -90f));

        BackGunActive = true;
    }

    void UpdateHealthBar()
    {
        healthLerp = Mathf.Lerp(healthLerp, CurrentHealth, Time.deltaTime);

        float width = healthLerp / MaxHealth * 0.9f; //0.9f because the bar only goes across 90% of the screen.
        width = Mathf.Clamp(width, 0, 0.9f);

        width = Screen.width * width;

        healthbar.pixelInset = new Rect(0,0,width, 15);

        float color = healthbar.color.r;
        color = Mathf.Lerp(color, 0.5f, Time.deltaTime);
        float alpha = Mathf.Lerp(color/2, 0.2f, Time.deltaTime);
        healthbar.color = new Color(color, color, color, alpha);

    }

    public void WeakPointFeedback(float c)
    {
        if (healthbar !=null && ( healthbar.color.Equals(null) || c > healthbar.color.r )) 
            healthbar.color = new Color(c, c, c, 1);
    }

    public void HitWeakpoint()
    {
        //Reset the weak point hit counter.
        lastWeakPointHit = 0f;
    }

    public void CrabMoveSync(float x, float z, float aimx, float aimz, bool direction, float time)
    {
		lastUpdate = Time.time;
		//print ("Current "+CurrentTarget+" "+gm.isReady);
        if (CrabDying)
            return;
		
        RealPosition = new Vector3(x, transform.position.y, z);
        CurrentTarget = new Vector3(aimx, transform.position.y, aimz);

        RealRotation = Quaternion.LookRotation(transform.position - CurrentTarget, Vector3.up);

        SyncLerp = 0f;
        RealLerp = time;
		
		// Assuming the player is a late joiner jump to InGame position
		if (gm.gamephase == 1)
		{
			gameObject.transform.position = RealPosition;
			gameObject.transform.rotation = RealRotation;
			
			if (CurrentHealth <= 0 && CurrentHealth != -999 && !CrabDying)
			{
				CrabDying = true;
				StartCoroutine(CrabFastDeath());
			}
		}
		
        //Changed from left to right.  Or right to left.  I dunno really.  It works though.
        if (!direction && Direction != direction)
            gameObject.animation.CrossFade("walkright");
        
        if(direction && Direction != direction)
            gameObject.animation.CrossFade("walkleft");

        Direction = direction;
    }

    IEnumerator MusicFade()
    {
        GameObject music = GameObject.Find("Music(Clone)");

        AudioSource audio = music.GetComponent<AudioSource>();

        while (audio.volume > 0)
        {
            audio.volume -= Time.deltaTime / 10f;
            yield return new WaitForEndOfFrame();
        }
    }

    IEnumerator EyeDeath()
    {
        //Remove eye flares
        GameObject[] eyes = GameObject.FindGameObjectsWithTag("EyeFlare");

        yield return new WaitForSeconds(1.5f);

        for (int i = 0; i < 10; i++)
        {
            foreach (GameObject e in eyes)
            {
                e.active = !e.active;
            }
            yield return new WaitForSeconds(0.1f);
        }

        foreach (GameObject e in eyes)
        {
            GameObject.Destroy(e);
        }
    }
	
	IEnumerator EyesAlive()
    {
		GameObject eye, light;

        eye = GameObject.Find("EyeL1");
        light = GameObject.Instantiate(Resources.Load("CrabFlare"), eye.transform.position, eye.transform.rotation) as GameObject;
        light.transform.parent = eye.transform;

        eye = GameObject.Find("EyeR1");
        light = GameObject.Instantiate(Resources.Load("CrabFlare"), eye.transform.position, eye.transform.rotation) as GameObject;
        light.transform.parent = eye.transform;

        yield return new WaitForSeconds(0.3f);

        eye = GameObject.Find("EyeL2");
        light = GameObject.Instantiate(Resources.Load("CrabFlare"), eye.transform.position, eye.transform.rotation) as GameObject;
        light.transform.parent = eye.transform;

        eye = GameObject.Find("EyeR2");
        light = GameObject.Instantiate(Resources.Load("CrabFlare"), eye.transform.position, eye.transform.rotation) as GameObject;
        light.transform.parent = eye.transform;

        yield return new WaitForSeconds(0.3f);

        eye = GameObject.Find("EyeL3");
        light = GameObject.Instantiate(Resources.Load("CrabFlare"), eye.transform.position, eye.transform.rotation) as GameObject;
        light.transform.parent = eye.transform;

        eye = GameObject.Find("EyeR3");
        light = GameObject.Instantiate(Resources.Load("CrabFlare"), eye.transform.position, eye.transform.rotation) as GameObject;
        light.transform.parent = eye.transform;

        yield return new WaitForSeconds(0.3f);

        eye = GameObject.Find("EyeL4");
        light = GameObject.Instantiate(Resources.Load("CrabFlare"), eye.transform.position, eye.transform.rotation) as GameObject;
        light.transform.parent = eye.transform;

        eye = GameObject.Find("EyeR4");
        light = GameObject.Instantiate(Resources.Load("CrabFlare"), eye.transform.position, eye.transform.rotation) as GameObject;
        light.transform.parent = eye.transform;
	}

	public void CalculateHealth ()
	{
		MaxHealth = 800 + 700 * Netman.Players.Count * Netman.healthmod;
		CurrentHealth = MaxHealth;
	}
	
    IEnumerator CrabDeathExplosions()
    {
        Vector3 pos;

        GameObject torso = GameObject.Find("Torso");

        for (int i = 0; i < 80; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                float x = Random.Range(-18f, 18f);
                float y = Random.Range(-12f, 12f); //Yeah I know this is really z
                pos = Netman.Enemy.transform.rotation * new Vector3(x, 10f, y) + torso.transform.position;
                GameObject.Instantiate(Resources.Load("Detonator-Simple"), pos, Quaternion.identity);
            }

            GameObject.Instantiate(Resources.Load("Sounds/ExplosionSmall"), Vector3.zero, Quaternion.identity);

            yield return new WaitForSeconds(0.09f);
        }
    }
	
	IEnumerator CrabFastDeath()
	{
		gameObject.animation.CrossFade("laying");
		yield return new WaitForSeconds(1f);
		Netman.Enemy.animation["crabdeath"].speed = 5f;
        Netman.Enemy.animation["crabdeath"].wrapMode = WrapMode.ClampForever;
        Netman.Enemy.animation.CrossFade("crabdeath");
		yield return new WaitForSeconds(1f);
		GameObject gun = GameObject.Find("GunMid/Gun");

    	if (gun) for (int i = 0; i < gun.transform.childCount; i++)
            	Destroy(gun.transform.GetChild(i).gameObject);
		
		StartCoroutine(EyeDeath());
		yield return new WaitForSeconds(6f);
		StartCoroutine(MusicFade());
		yield return new WaitForSeconds(2f);
        gm.gamephase = 3; //Post-game
        Netman.ShowScores();
	}
	
    IEnumerator CrabDie()
    {
        Netman.Enemy.animation.Stop("shittonofbullets");
        Netman.endtime = Time.time;

        StopCoroutine("CrazyBarrage");
        GameObject torso = GameObject.Find("Torso");
        GameObject gun = GameObject.Find("GunMid/Gun");

        if (gun)
        {
            for (int i = 0; i < gun.transform.childCount; i++)
            {
                Destroy(gun.transform.GetChild(i).gameObject);
            }
        }

        GameObject lasersound = GameObject.Find("LaserShot(Clone)");
        if (lasersound)
            Destroy(lasersound);

        Time.timeScale = 0.5f;

        StartCoroutine(MusicFade());

        Netman.Enemy.animation["crabdeath"].speed = 0.5f;
        Netman.Enemy.animation["crabdeath"].wrapMode = WrapMode.ClampForever;
        Netman.Enemy.animation.CrossFade("crabdeath");

        yield return new WaitForSeconds(0.1f);

        StartCoroutine(CrabDeathExplosions());
        
        Vector3 pos = Netman.Enemy.transform.rotation * new Vector3(-10f, 10f, 0f) + torso.transform.position;
        GameObject.Instantiate(Resources.Load("Detonator-Insanity"), pos, Quaternion.identity);
        GameObject.Instantiate(Resources.Load("Sounds/ExplosionLarge"), pos, Quaternion.identity);

        yield return new WaitForSeconds(3.2f);

        pos = Netman.Enemy.transform.rotation * new Vector3(10f, 10f, -10f) + torso.transform.position;
        GameObject.Instantiate(Resources.Load("Detonator-Insanity"), pos, Quaternion.identity);
        GameObject.Instantiate(Resources.Load("Sounds/ExplosionLarge"), pos, Quaternion.identity);

        yield return new WaitForSeconds(1f);

        pos = Netman.Enemy.transform.rotation * new Vector3(-5f, 10f, 2f) + torso.transform.position;
        GameObject.Instantiate(Resources.Load("Detonator-Insanity"), pos, Quaternion.identity);
        GameObject.Instantiate(Resources.Load("Sounds/ExplosionLarge"), pos, Quaternion.identity);

        yield return new WaitForSeconds(0.6f);

        pos = Netman.Enemy.transform.rotation * new Vector3(5f, 10f, 6f) + torso.transform.position;
        GameObject.Instantiate(Resources.Load("Detonator-Insanity"), pos, Quaternion.identity);
        GameObject.Instantiate(Resources.Load("Sounds/ExplosionLarge"), pos, Quaternion.identity);

        yield return new WaitForSeconds(0.6f);

        Time.timeScale = 0.3f;

        pos = Netman.Enemy.transform.rotation * new Vector3(10f, 10f, 8f) + torso.transform.position;
        GameObject.Instantiate(Resources.Load("Detonator-MushroomCloud"), pos, Quaternion.identity);
        GameObject.Instantiate(Resources.Load("Sounds/ExplosionLarge"), pos, Quaternion.identity);

        pos = Netman.Enemy.transform.rotation * new Vector3(1f, 10f, 6f) + torso.transform.position;
        GameObject.Instantiate(Resources.Load("Detonator-MushroomCloud"), pos, Quaternion.identity);
        GameObject.Instantiate(Resources.Load("Sounds/ExplosionLarge"), pos, Quaternion.identity);

        pos = Netman.Enemy.transform.rotation * new Vector3(-11f, 10f, 7f) + torso.transform.position;
        GameObject.Instantiate(Resources.Load("Detonator-MushroomCloud"), pos, Quaternion.identity);
        GameObject.Instantiate(Resources.Load("Sounds/ExplosionLarge"), pos, Quaternion.identity);

        yield return new WaitForSeconds(0.9f);

        white.active = true;
        float alpha = 0f;
        while(alpha < 0.5f)
        {
            alpha += Time.deltaTime * 1;
            white.renderer.material.color = new Color(1,1,1, alpha);

            yield return new WaitForEndOfFrame();
        }

        alpha = 0.5f;
        
        GameObject.Instantiate(Resources.Load("Sounds/ExplosionHuge"), pos, Quaternion.identity);
        GameObject.Instantiate(Resources.Load("Sounds/ExplosionHuge"), pos, Quaternion.identity);
        GameObject.Instantiate(Resources.Load("Sounds/ExplosionHuge"), pos, Quaternion.identity);

        yield return new WaitForSeconds(0.1f);

        Time.timeScale = 0.5f;

        while (alpha > 0f)
        {
            alpha -= Time.deltaTime * 0.2f;
            white.renderer.material.color = new Color(1, 1, 1, alpha);
            
            yield return new WaitForEndOfFrame();
        }

        white.active = false;

        yield return new WaitForSeconds(0.8f);

        StartCoroutine(EyeDeath());

        Time.timeScale = 1f;

        yield return new WaitForSeconds(2f);

        gm.gamephase = 3; //Post-game

        Netman.ShowScores();
    }
    	
	// Update is called once per frame
	void Update () 
    {
		// Note see CrabMoveSync() for changes outside of Update and gamephase == 1
        if (gm.gamephase == 2 ) 
        {
			if (!gm.isSoloPlay && (Time.time > (lastUpdate + 2.5f))) 
			{
				//print("time "+Time.time + " lastU "+lastUpdate+3);
				gameObject.animation.CrossFade("laying");
				return;
			}
	
            if (crabActive == false)
            {
                crabActive = true;
				
				if (isAlive) {
					if (!gm.isPlayIntro) StartCoroutine(EyesAlive());
                	StartCoroutine(BlinkWeakPointMessage(10));
				}
				
                healthbarobject = GameObject.Instantiate(Resources.Load("Healthbar")) as GameObject;
                healthbar = healthbarobject.GetComponent<GUITexture>();
                lastWeakPointHit = 0f;

                MaxHealth = 800 + 700 * Netman.Players.Count * Netman.healthmod;
				//CurrentHealth = MaxHealth; // if late joiner don't reset CurrentHealth or out of sync

				healthLerp = CurrentHealth;
                Netman.starttime = Time.time;
            }
			
            if (CurrentHealth <= 0 && CurrentHealth != -999 && !CrabDying)
            {
                StartCoroutine(CrabDie());
                CrabDying = true;
            }

            UpdateHealthBar();

            if (CrabDying)
                return;

            lastWeakPointHit += Time.deltaTime;

            if (lastWeakPointHit > 10f)
            {
                //Shit takes forever if they aren't aiming for the weak point.  We remind them if they haven't hit it for 10s.
                StartCoroutine(BlinkWeakPointMessage(5));
                lastWeakPointHit = 0f;
            }

            if (WalkState == (int)WalkStates.Stopped)
            {
                WalkLeft(WalkSpeed);
            }
            
            //Quaternion lookRot = Quaternion.LookRotation(transform.position - CurrentTarget, Vector3.up);

            float RotLerp = Mathf.Lerp(SyncLerp, RealLerp, SyncLerp);  //Some dodgy bullshit right here.
            
            gameObject.transform.rotation = Quaternion.Lerp(gameObject.transform.rotation, RealRotation, RotLerp/10);

            if (!Direction)
            {
                gameObject.transform.position -= gameObject.transform.right * WalkSpeed * 25f * Time.deltaTime; //lolol hardcoded walking
                RealPosition -= gameObject.transform.right * WalkSpeed * 25f * Time.deltaTime;
            }
            else
            {
                gameObject.transform.position += gameObject.transform.right * WalkSpeed * 25f * Time.deltaTime; //lolol hardcoded walking
                RealPosition += gameObject.transform.right * WalkSpeed * 25f * Time.deltaTime;
            }

            gameObject.transform.position = Vector3.Lerp(gameObject.transform.position, RealPosition, SyncLerp);

            SyncLerp += Time.deltaTime;
            RealLerp += Time.deltaTime;

#if Use_Vectrosity
            
            if (!UpdatedBoundaries)
            {
                //Since vectrosity conveniently angles all lines towards the camera, we want to call this function only 
                //once after the game starts so the camera is in the right spot. Doing it more often produces a noticable drop in FPS.
                Vector.DrawLine3D(Boundary, gameObject.transform);
                UpdatedBoundaries = true;
            }
            Boundary.vectorObject.transform.position = new Vector3(transform.position.x, 1.5f, transform.position.z);
#endif
        }
	}
}
