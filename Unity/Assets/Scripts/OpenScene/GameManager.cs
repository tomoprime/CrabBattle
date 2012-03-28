using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour {

    public static GameManager Instance;
    public static GameObject Container;
	
    public bool isSoloPlay = true;
    public string ipAddress = "www.tomogames.com";
	public int gamePort = 14248;
	public int policyPort = 8843;
	public string downloadServerUrl = "https://github.com/tomoprime/CrabBattle";//"http://www.tomogames.com/CrabBattleServer.exe"; //"http://dodsrv.com/Unity/Crabbattle/CrabBattleServer.exe";
	public string scoreUrl = "http://www.dodsrv.com/crabbattle/scoresubmit.php";
	
	public string greeting = "You'll need to have at least one person running a multiplayer server in order to play. "+
			"That player will need to forward ports 8843 & 14248 to their server. If you fail to connect by hostname "+
			"you may need to specify the direct ipaddress. Otherwise use dodsrv.com or tomogames.com as given below.";

	
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
}
