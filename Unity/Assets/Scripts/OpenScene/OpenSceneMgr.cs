using UnityEngine;
using System.Collections;
using System.Net;

public class OpenSceneMgr : MonoBehaviour {

    GameManager gm;

    Rect windowrect;

    // Use this for initialization
    void Start () 
    {
        gm = GameManager.GetInstance();

        windowrect = new Rect(Screen.width / 2f - 150f, Screen.height / 2f - 150f, 300f, 375f);
    }

    public void OnGUI()
    {
        windowrect.x = Mathf.Clamp(windowrect.x, 0, Screen.width - windowrect.width);
        windowrect.y = Mathf.Clamp(windowrect.y, 0, Screen.height - windowrect.height);

        windowrect = GUI.Window(1, windowrect, OptionWindow, "Game Type");
    }

    public void OptionWindow(int windowid)
    {
        GUI.Label(new Rect(20, 30, 200, 20), "Single Player");

        if (GUI.Button(new Rect(20, 60, 260, 30), "Play Single Player"))
        {
            gm.isSoloPlay = true;
            Application.LoadLevel("mainscene");            
        }

        GUI.Label(new Rect(20, 100, 250, 20), "MultiPlayer instructions for your own host:");

        GUI.Label(new Rect(30, 125, 260, 125), gm.greeting);
        if (GUI.Button(new Rect(190, 240, 90, 20), "Get Source"))
        {
			Application.OpenURL(gm.downloadServerUrl);
        }

        GUI.Label(new Rect(30, 255, 230, 40), "Server\nIP Address: ");

        gm.ipAddress = GUI.TextField(new Rect(110, 270, 170, 22), gm.ipAddress);

        if (GUI.Button(new Rect(20, 300, 260, 30), "Play Multi Player"))
        {
            gm.isSoloPlay = false;
			bool isPolicyConnected = false;
			string hostname = gm.ipAddress;
			IPAddress[] ips = Dns.GetHostAddresses(gm.ipAddress);
			foreach (IPAddress ip in ips)
			{
				gm.ipAddress = ip.ToString();
				if (!(isPolicyConnected=Security.PrefetchSocketPolicy(gm.ipAddress, gm.policyPort, 5000))) 
					print("policy socket address failed to connect to "+gm.ipAddress+":"+gm.policyPort);
				else break;
			}
            if(isPolicyConnected)
				Application.LoadLevel("mainscene");
			else gm.ipAddress=hostname;
        }

        GUI.DragWindow();
    }
}
