using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour {

    NetworkManager Netman;
    bool isIntroStarted = false;

    public GameObject Miniwarning1;
    public GameObject Miniwarning2;
    public GameObject Warning1;
    public GameObject Bossname;
    public GameObject Bossname2;

    public Material WarnMat1;
    public Material WarnMat2;

    public bool playintro = false;
    public bool PlayMusic = false;

    int introPhase = 0;

	// Use this for initialization
	void Start () 
    {
        Netman = NetworkManager.GetInstance();
	}

    IEnumerator ScrollWarningMaterial(float time)
    {
        for (float i = 0f; i < time; i += Time.deltaTime)
        {
            if (Miniwarning1 && Miniwarning2)
            {
                Miniwarning1.renderer.material.SetTextureOffset("_MainTex", new Vector2(Time.time / 2f, 0f));
                Miniwarning2.renderer.material.SetTextureOffset("_MainTex", new Vector2(-Time.time / 2f, 0f));
            }
            yield return null;
        }
    }

    IEnumerator FlashWarning(int times)
    {
        GameObject.Instantiate(Resources.Load("SoundWarning"));

        StartCoroutine(ScrollWarningMaterial(4f));

        Miniwarning1.active = true;
        Miniwarning2.active = true;
        Warning1.active = true;

        for (int i = 0; i < times; i++)
        {
            iTween.FadeTo(Miniwarning1, iTween.Hash("alpha", 1f, "time", 0.2f));
            iTween.FadeTo(Miniwarning2, iTween.Hash("alpha", 1f, "time", 0.2f));
            iTween.FadeTo(Warning1, iTween.Hash("alpha", 1f, "time", 0.2f));
           
            yield return new WaitForSeconds(0.2f);

            iTween.FadeTo(Miniwarning1, iTween.Hash("alpha", 0f, "time", 0.7f));
            iTween.FadeTo(Miniwarning2, iTween.Hash("alpha", 0f, "time", 0.7f));
            iTween.FadeTo(Warning1, iTween.Hash("alpha", 0f, "time", 0.7f));
           
            yield return new WaitForSeconds(0.7f);
        }

        yield return null;

        Destroy(Miniwarning1);
        Destroy(Miniwarning2);
        Destroy(Warning1);
    }

	IEnumerator PlayIntro()
    {
        //Why is the intro in the camera controller?  I dunno, it just happened to end up here!!
		
        foreach (PlayerObject p in Netman.Players)
        {		
            if(!playintro)
                p.Obj.transform.position = new Vector3(p.Obj.transform.position.x, 15f, -50f);
            else
                iTween.MoveTo(p.Obj, iTween.Hash("Position", new Vector3(p.Obj.transform.position.x, 15f, -50f), "easetype", "easeOutSine", "time", 20));
            
            p.Controller.SetupPlayer(p.Id, p.Id == Netman.ClientId ? true : false);
        }

        if (!playintro)
        {
            if (PlayMusic == true)
                GameObject.Instantiate(Resources.Load("Music"));

            yield return new WaitForSeconds(2f);

            //20 second mark
            Debug.Log("Starting Game!");
            Netman.gamephase = 2; //Start the game

        }
        else
        {
            yield return new WaitForSeconds(2f);
            StartCoroutine(FlashWarning(4));
            yield return new WaitForSeconds(4f);
            introPhase = 1;
            iTween.RotateTo(gameObject, iTween.Hash("x", -20, "easetype", "easeOutQuint", "time", 6));
            iTween.MoveTo(gameObject, iTween.Hash("position", new Vector3(0f, 5f, -50f), "easetype", "easeOutQuint", "time", 4));
            if(PlayMusic == true)
                GameObject.Instantiate(Resources.Load("Music"));
            yield return new WaitForSeconds(1.5f);
            Netman.Enemy.animation["intro"].layer = 1;
            Netman.Enemy.animation.Play("intro");

            GameObject eye, light;

            yield return new WaitForSeconds(1.5f);

            GameObject.Instantiate(Resources.Load("Sounds/RobotSoundMid"));

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

            yield return new WaitForSeconds(0.2f);

            GameObject.Instantiate(Resources.Load("Sounds/RobotSoundShort"));

            yield return new WaitForSeconds(1.2f);

            GameObject.Instantiate(Resources.Load("Sounds/RobotSoundShort"));

            yield return new WaitForSeconds(1.2f);

            GameObject.Instantiate(Resources.Load("Sounds/RobotSoundShort"));

            Bossname.active = true;
            iTween.FadeTo(Bossname, iTween.Hash("alpha", 1f, "time", 3f));

            yield return new WaitForSeconds(1f);

            Bossname2.active = true;
            iTween.FadeTo(Bossname2, iTween.Hash("alpha", 1f, "time", 3f));

            yield return new WaitForSeconds(0.2f);

            GameObject.Instantiate(Resources.Load("Sounds/RobotSoundShort"));

            yield return new WaitForSeconds(2.3f);

            iTween.FadeTo(Bossname, iTween.Hash("alpha", 0f, "time", 1f));
            iTween.FadeTo(Bossname2, iTween.Hash("alpha", 0f, "time", 1f));

            yield return new WaitForSeconds(0.5f);

            Vector3 midpoint = GetCameraMidpointForAllObjects();
            midpoint.y = 100f;

            iTween.RotateTo(gameObject, iTween.Hash("x", 90, "easetype", "easeOutQuint", "time", 4f));
            iTween.MoveTo(gameObject, iTween.Hash("position", midpoint, "easetype", "easeOutQuint", "time", 4f, "name", "cammove"));

            yield return new WaitForSeconds(2.2f);
            Destroy(Bossname);
            Destroy(Bossname2);

            yield return new WaitForSeconds(2f);

            //20 second mark
            Debug.Log("Starting Game!");
            Netman.gamephase = 2; //Start the game
        }
    }

    public Vector3 GetCameraMidpointForAllObjects()
    {
        Vector3 sum = Vector3.zero;
        int numobjects = 1;

        foreach (PlayerObject p in Netman.Players)
        {
            numobjects++;
            sum += new Vector3(p.Obj.transform.position.x, 0f, p.Obj.transform.position.z);
        }

        sum += new Vector3(Netman.Enemy.transform.position.x, 0f, Netman.Enemy.transform.position.z);

        return sum / numobjects;
    }

    void CameraTrackLocalPlayer()
    {
        if (introPhase == 1)
            return;

        float optimalheight = 100f; //This is how high the camera should be when tracking 1 player.

        Vector3 curpos = transform.position;
        Vector3 target = Netman.Player.Obj.transform.position;
        target.y = optimalheight;

        this.transform.position = Vector3.Lerp(curpos, target, Time.deltaTime * 3f);

        //There's got to be a better way to do this...
        Vector3 directdown = new Vector3(transform.position.x, 0f, transform.position.z);
        Quaternion currot = transform.rotation;
        transform.LookAt(directdown);
        Quaternion tarrot = transform.rotation;

        transform.rotation = Quaternion.Lerp(currot, tarrot, Time.deltaTime * 3f);
    }

    void CameraMidpointAllActors()
    {
        float optimalheight = 100f; //This is how high the camera should be when tracking 1 player.

        float maxdistance = 0f;

        foreach (PlayerObject p in Netman.Players)
        {
            foreach (PlayerObject p2 in Netman.Players)
            {
                float d = Vector3.Distance(p.Obj.transform.position, p2.Obj.transform.position);
                if (d > maxdistance)
                    maxdistance = d;
            }
            float d2 = Vector3.Distance(p.Obj.transform.position, Netman.Enemy.transform.position);
            if (d2 > maxdistance)
                maxdistance = d2;
        }

        if (maxdistance > 50f)
            optimalheight += (maxdistance - 50f) * 0.8f;

        Vector3 curpos = transform.position;
        Vector3 target = GetCameraMidpointForAllObjects();
        target.y = optimalheight;

        this.transform.position = Vector3.Lerp(curpos, target, Time.deltaTime * 3f);

        //There's got to be a better way to do this...
        Vector3 directdown = new Vector3(transform.position.x, 0f, transform.position.z);
        Quaternion currot = transform.rotation;
        transform.LookAt(directdown);
        Quaternion tarrot = transform.rotation;

        transform.rotation = Quaternion.Lerp(currot, tarrot, Time.deltaTime * 3f);
    }

	// Update is called once per frame
	void Update () 
    {
        if (Netman.gamephase == 0)
        {
            transform.RotateAround(Vector3.zero, Vector3.up, 10 * Time.deltaTime);
			if (playintro!=Netman.isPlayIntro)
				playintro=Netman.isPlayIntro;
        }
        if (Netman.gamephase == 1 && isIntroStarted == false)
        {
            StartCoroutine(PlayIntro());
            isIntroStarted = true;
        }
        if(Netman.gamephase == 1)
            CameraTrackLocalPlayer();

        if (Netman.gamephase == 2)
            CameraMidpointAllActors();
	}

    void LateUpdate()
    {
        
    }
}
