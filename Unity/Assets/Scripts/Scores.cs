using UnityEngine;
using System.Collections;

public class Scores : MonoBehaviour {
	
	GameManager gm;
    public Font f;
	NetworkManager netman;

    Rect button;

    void OnGUI()
    {
        if (GUI.Button(button, "Return to Menu"))
            Application.LoadLevel(0);
    }

    // Use this for initialization
    void Start () 
    {
        netman = NetworkManager.GetInstance();
		gm = GameManager.GetInstance();
		
		int count = netman.Players.Count;
		int height = count * 30 + 120;
        int width = 700;
		
		int offx = (Screen.width - width) / 2;
        int offy = (Screen.height - height) / 2;
		
		//button = new Rect(Screen.width / 2 - 100, Screen.height / 4 * 3 - 25, 200, 50);
        button = new Rect(offx+5, offy+5, 200, 50);

        GameObject scorebg = GameObject.Instantiate(Resources.Load("ScoreBackground")) as GameObject;

        float x = (1f - ((float)width / (float)Screen.width)) / 2f;
        float y = (1f - ((float)height / (float)Screen.height)) / 2f;

        scorebg.transform.position = new Vector3(x, y, 0);
        GUITexture gt = scorebg.GetComponent<GUITexture>();

        gt.pixelInset = new Rect(0, 0, width, height);

        float length = netman.endtime - netman.starttime;

        int mins = (int)Mathf.Floor(length / 60);
        int sec = (int)Mathf.Floor(length % 60);

        string time;

        if (sec < 10)
            time = mins + ":0" + sec;
        else
            time = mins + ":" + sec;

        string difficulty = "";
        switch (netman.difficulty)
        {
            case 0: difficulty = "Easy"; break;
            case 1: difficulty = "Normal"; break;
            case 2: difficulty = "Hard"; break;
            case 3: difficulty = "Lunatic"; break;
        }

        string health = "";
        switch (netman.healthmod)
        {
            case 0: health = "Short"; break;
            case 1: health = "Medium"; break;
            case 2: health = "Long"; break;
            case 3: health = "Absurd"; break;
        }

        CreateLabel(offx + 350, offy + 10, 20, "Scores");
        CreateLabel(offx + 350, offy + 30, 12, "Game Time: " + time);
        CreateLabel(offx + 350, offy + 45, 12, "Difficulty: " + difficulty + "   Length: " + health);

        CreateLabel(offx + 80, offy + 70, 14, "\nPlayer");
        CreateLabel(offx + 200, offy + 70, 14, "Hits\nTaken");
        CreateLabel(offx + 300, offy + 70, 14, "Normal\nDamage");
        CreateLabel(offx + 400, offy + 70, 14, "Weak Point\nDamage");
        CreateLabel(offx + 500, offy + 70, 14, "\nScore");
        CreateLabel(offx + 600, offy + 70, 14, "\nRank");

        for (int i = 0; i < count; i++)
        {
            int line = 110 + 30 * i;

            int score = netman.Players[i].dmgnormal * 5 + netman.Players[i].dmgweakpoint * 50;

            int timebonus = 200000;

            switch (netman.healthmod)
            {
                case 0: timebonus = (1 - (int)length / 80) * timebonus; break;
                case 1: timebonus = (1 - (int)length / 120) * timebonus; break;
                case 2: timebonus = (1 - (int)length / 160) * timebonus; break;
                case 3: timebonus = (1 - (int)length / 200) * timebonus; break;
            }

            timebonus = Mathf.Clamp(timebonus, 0, 200000);

            score = score + timebonus;

            for (int j = 0; j < netman.Players[i].hittaken; j++)
            {
                score = (int)((float)score * 0.8f);
            }

            string rank = "F";

            if (score > 70000)
                rank = "C";
            if (score > 140000)
                rank = "B";
            if (score > 180000)
                rank = "A";
            if (score > 220000)
                rank = "A+";
            if (score > 240000 && netman.Players[i].hittaken == 0)
                rank = "S"; //This is in fact doable on short!  Just... reaaaaly hard.

            score = score + (score * netman.difficulty / 2);

            if (netman.Players[i].Id == netman.ClientId)
            {
                StartCoroutine(SubmitScore(netman.Players[i].Name, netman.difficulty, netman.healthmod, netman.Players[i].hittaken, netman.Players[i].dmgnormal, netman.Players[i].dmgweakpoint, score, rank));
            }

            CreateLabel(offx + 80, offy + line, 18, netman.Players[i].Name);
            CreateLabel(offx + 200, offy + line, 18, "" + netman.Players[i].hittaken);
            CreateLabel(offx + 300, offy + line, 18, "" + netman.Players[i].dmgnormal);
            CreateLabel(offx + 400, offy + line, 18, "" + netman.Players[i].dmgweakpoint);
            CreateLabel(offx + 500, offy + line, 18, "" + score);
            CreateLabel(offx + 600, offy + line, 18, rank);
        }

    }

    IEnumerator SubmitScore(string name, int diff, int health, int hits, int dmg, int wpdmg, int score, string rank)
    {
        WWWForm form = new WWWForm();

        string key = "pleasedontcheatitsnotniceorfair";
        form.AddField("key", "pleasedontcheatitsnotniceorfair");
        form.AddField("name", name);
        form.AddField("difficulty", diff);
        form.AddField("healthmod", health);
        form.AddField("hits", hits);
        form.AddField("dmg", dmg);
        form.AddField("wpdmg", wpdmg);
        form.AddField("score", score);
        form.AddField("rank", rank);

        string hash = CryptoHelper.GetMd5String(name + hits + dmg + wpdmg + score + rank + key);
        Debug.Log(name + hits + dmg + wpdmg + score + rank + key);
        Debug.Log(hash);

        form.AddField("hash", hash);

        WWW www = new WWW(gm.scoreUrl, form);
        
        yield return www;
		// note Unity looks for crossdomain.xml at the server root on port 80. Can't change this otherwise. Also need php scripts.
        Debug.LogWarning("Post failed for url: "+gm.scoreUrl+" omitted form data "+www.error);
    }

    GameObject CreateLabel(int x, int y, int size, string text)
    {
        GameObject l = new GameObject("Text: " + text);
        GUIText t = l.AddComponent<GUIText>();

        t.text = text;
        t.fontSize = size;
        t.anchor = TextAnchor.UpperCenter;
        t.alignment = TextAlignment.Center;
        t.font = f;

        t.transform.position = PixelToVector(x, y, 1);

        return l;
    }

    Vector3 PixelToVector(int x, int y, int depth = 0)
    {
        float x2 = (((float)x / (float)Screen.width));
        float y2 = (1f - ((float)y / (float)Screen.height));

        return new Vector3(x2, y2, depth);
    }
    	
}
