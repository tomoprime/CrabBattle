using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour {

    public static GameManager Instance;
    public static GameObject Container;
	
    public bool isSoloPlay = true;
    public string ipAddress = "www.tomogames.com"; //"localhost";
	public int gamePort = 14248;
	public int policyPort = 8843;
	public string downloadServerUrl = "https://github.com/tomoprime/CrabBattle";//"http://www.tomogames.com/CrabBattleServer.exe"; //"http://dodsrv.com/Unity/Crabbattle/CrabBattleServer.exe";
	public string scoreUrl = "http://www.dodsrv.com/crabbattle/scoresubmit.php";
	
	public string greeting = "You'll need to have at least one person running a multiplayer server in order to play. "+
			"That player will need to forward ports 8843 & 14248 to their servername. If you fail to connect by hostname "+
			"you may need to specify the direct ip address. Otherwise you can test against the tomogames.com server given below.";

	private CameraController cc;
	public int gamephase = 0;
	public bool isPlayIntro = true;
	internal bool isIntroStarted = false;
	internal bool isShowMenu = false;
	internal bool isReady = false;
	internal string username = "Player 1";
	
    public static GameManager GetInstance()
    {
        if (!Instance)
        {
            Container = new GameObject();
            Container.name = "GameManager";
            Instance = Container.AddComponent(typeof(GameManager)) as GameManager;
        }
        return Instance;
    }

	// Use this for initialization
	void Start () 
    {
    	DontDestroyOnLoad(this);
	}
	
	public void Reset ()
	{
		// note if a new scene gets loaded the GameManager doesn't die so you'll need to put all of 
		// your default values here to auto reset on opening screne load.
		isIntroStarted = false;
		isShowMenu = false;
		gamephase = 0;
		isReady = false;
	}
	
	// Update is called once per frame
	void Update () 
    {
		if (Application.loadedLevelName.Equals("mainscene"))
		{
			if (cc == null)
				cc = GameObject.Find("CameraContainer").GetComponent<CameraController>();
		    
		    if (gamephase == 1 && !isIntroStarted)
		        StartCoroutine(PlayIntro());
			
			if (gamephase == 0)
		    {
		        if (!isShowMenu) cc.CameraRotateAround();
				if (cc.playintro != isPlayIntro)
					cc.playintro = isPlayIntro;
		    }
		    else if (gamephase == 1)
		        cc.CameraTrackLocalPlayer();
		
		    else if (gamephase == 2)
		        cc.CameraMidpointAllActors();
			}
	}
	
	IEnumerator PlayIntro ()
	{
		isIntroStarted = true;
		yield return cc.StartCoroutine(cc.PlayIntro());
		gamephase = 2;
		isIntroStarted = false;
	}
}
