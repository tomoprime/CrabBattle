using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Lidgren.Network;

public class PlayerObject
{
    public int Id;
    public float xVelocity = 0;
    public float yVelocity = 0;
    public GameObject Obj;
    public string Name = "";
    public PlayerController Controller;

    public int dmgnormal = 0;
    public int dmgweakpoint = 0;
    public int hittaken = 0;

    public PlayerObject(int id, GameObject obj, string name)
    {
        Id = id;
        Obj = obj;
        Name = name;
        Controller = Obj.GetComponent<PlayerController>();
    }
}

public class NetworkManager : MonoBehaviour {

    public static NetworkManager Instance;
    public static GameObject Container;

    private static NetClient client;
    private NetIncomingMessage inc;

    float roundtriptime = 0f;

    public int ClientId = -1;
    public PlayerObject Player;
    public List<PlayerObject> Players;
    public GameObject Enemy;
    public CrabManager EnemyManager;

    public int readystatus = 0;
    public int gamephase = 0;
    public int difficulty = 1;
    public int healthmod = 1;

    CrabBattleServer.CrabBehavior cb;

    Rect windowrect;
    Rect lobbyrect;
	Rect button;

    GameManager gm;

    string hostIp;
    string username = "";
    string newname = "";

    string curtyping = "";

    List<string> console;
    List<string> lobby;

    bool isConnected = false;
    bool isReady = false;
    float lastBeat = 0f;
    int numPlayers = 0;

    public float starttime = 0;
    public float endtime = 0;

    bool isSoloMode = false;
	internal bool isPlayIntro = true;

    enum PacketTypes
    {
        Beat,
        AssignId,
        Ready,
        UpdateName,
        PlayerAttemptedStart,
        PlayIntro,
        StartGame,
        AddPlayer,
        RemovePlayer,
        PlayerAction,
        PlayerSpecial,
        HurtTarget,
        EnemyHealth,
        SelfHit,
        PlayerHit,
        EnemyPhaseChange,
        EnemyAction,
        EnemyTargetPosition,
        EnemyStartTargeting,
        EnemyEndTargeting,
        Message,
        SettingsChange,
        PlayerCount,
        Disconnect,
        LobbyMessage,
        EnemySync
    }

    public static NetworkManager GetInstance()
    {
        if (!Instance)
        {
            Container = new GameObject();
            Container.name = "NetworkManager";
            Instance = Container.AddComponent(typeof(NetworkManager)) as NetworkManager;
        }
        return Instance;
    }
	
	// Use this for initialization
	void Start () 
    {
        Debug.Log("Starting Network Manager");
		button = new Rect(Screen.width - 130, Screen.height -50, 120, 40);
        //Debug.Log(CryptoHelper.GetMd5String("Hello World!"));

        gm = GameManager.GetInstance();

        hostIp = gm.ipAddress;
        isSoloMode = gm.isSoloPlay;

        console = new List<string>();
        lobby = new List<string>();

        Players = new List<PlayerObject>();
		
        if (!isSoloMode)
        {
            NetPeerConfiguration config = new NetPeerConfiguration("crab_battle");

            client = new NetClient(config);

            NetOutgoingMessage outmsg = new NetOutgoingMessage();

            client.Start();

            outmsg.Write("A Client");

            client.Connect(hostIp, gm.gamePort, outmsg);

            AddConsoleMessage("Waiting for connection to server...");
        }
        else
        {
            isConnected = true; // In solo mode, we're always connected!
            username = PlayerPrefs.GetString("Username", "Player");
            newname = username;
            isReady = true;
            ClientId = 1;
        }

        windowrect = new Rect(Screen.width / 2f - 100f, Screen.height / 2f - 150f, 200f, 310f);
        lobbyrect = new Rect(Screen.width / 2f - 200f, Screen.height - 100f, 400f, 100f);
        
        Enemy = GameObject.Instantiate(Resources.Load("battlecrab"), Vector3.zero, Quaternion.identity) as GameObject;
        Enemy.animation.Play("laying");

        EnemyManager = Enemy.GetComponent<CrabManager>();
	}

    public void OnGUI()
    {
		//if (Application.loadedLevel!=0)
		
        if (gamephase == 0 && isConnected)
        {
            windowrect.x = Mathf.Clamp(windowrect.x, 0, Screen.width - windowrect.width);
            windowrect.y = Mathf.Clamp(windowrect.y, 0, Screen.height - windowrect.height);

            windowrect = GUI.Window(1, windowrect, ConnectionWindow, "Connection");

            lobbyrect.x = Mathf.Clamp(lobbyrect.x, 0, Screen.width - lobbyrect.width);
            lobbyrect.y = Mathf.Clamp(lobbyrect.y, 0, Screen.height - lobbyrect.height);

            lobbyrect = GUI.Window(2, lobbyrect, LobbyWindow, "Lobby");
        }
		else {
			if (gamephase >= 1)
			if (GUI.Button(button, "Return to Lobby"))
            Application.LoadLevel("mainscene");
		}

        if (gamephase == 0)
        {
			if (GUI.Button(button, "Return to Main"))
            Application.LoadLevel("opening");
			
            int top = 5;
            foreach (string msg in console)
            {
                GUI.Label(new Rect(10, top, Screen.width, 25), msg);
                top += 17;
            }
        }
    }
    
    void LobbyWindow(int windowID)
    {
        int top = 15;
        foreach (string msg in lobby)
        {
            GUI.Label(new Rect(10, top, 380, 25), msg);
            top += 13;
        }

        GUI.SetNextControlName("LobbyText");
        curtyping = GUI.TextField(new Rect(10, 75, 380, 20), curtyping);

        if (Event.current.keyCode == KeyCode.Return && GUI.GetNameOfFocusedControl() == "LobbyText" && curtyping != "")
        {
            SendLobbyText(curtyping);
            curtyping = "";
        }

        GUI.DragWindow();
    }

    void ConnectionWindow(int windowID)
    {
        GUI.Label(new Rect(10, 25, 100, 25), "User Name");
        GUI.SetNextControlName("Namebox");
        newname = GUI.TextField(new Rect(10, 45, 110, 20), newname, 20);
        if (GUI.Button(new Rect(125, 45, 65, 20), "Set"))
            ChangeName(newname);

        GUI.Label(new Rect(10, 70, 90, 25), "Difficulty");
        int newdiff = GUI.SelectionGrid(new Rect(10, 90, 180, 80), difficulty, new string[] { "Easy", "Normal", "Hard", "Lunatic" }, 1);

        GUI.Label(new Rect(10, 175, 90, 25), "Battle Length");
        int newhealth = GUI.SelectionGrid(new Rect(10, 195, 180, 40), healthmod, new string[] { "Short", "Normal", "Long", "Absurd" }, 2);
		
		bool intro = GUI.Toggle(new Rect(100, 240, 100, 20), isPlayIntro, "Play Intro?");
        bool ready = GUI.Toggle(new Rect(20, 240, 80, 20), isReady, "Ready?");

        if (!ready)
            GUI.enabled = false;

        if (GUI.Button(new Rect(10, 265, 180, 30), "START"))
            SendStart();

        GUI.enabled = true;

        if (newdiff != difficulty || newhealth != healthmod)
            ChangeDifficulty(newdiff, newhealth);

        if (ready != isReady)
            ToggleReady(ready);
		
		if (intro != isPlayIntro)
            TogglePlayIntro(intro);

        GUI.DragWindow();

        //If they hit enter while typing their name, send the changes to the server.
        if (Event.current.keyCode == KeyCode.Return && GUI.GetNameOfFocusedControl() == "Namebox")
            if (newname != username && newname != "")
            {
                ChangeName(newname);
                username = newname;
            }
    }

    public void DealEnemyDamage(int damage, bool weakpoint)
    {
        if (EnemyManager.CurrentHealth <= 0)
            return;

        if (!weakpoint)
            Player.dmgnormal += damage;
        else
            Player.dmgweakpoint += damage;

        if (isSoloMode)
        {
            EnemyManager.CurrentHealth -= damage;
            return;
        }

        NetOutgoingMessage outmsg = new NetOutgoingMessage();
        outmsg.Write((byte)PacketTypes.HurtTarget);
        outmsg.Write((Int16)damage);
        outmsg.Write(weakpoint);
        client.SendMessage(outmsg, NetDeliveryMethod.Unreliable, 0);
    }

    public void SendPlayerSpecial(int SpecialType)
    {
        if (isSoloMode)
            return;

        NetOutgoingMessage outmsg = new NetOutgoingMessage();
        outmsg.Write((byte)PacketTypes.PlayerSpecial);
        outmsg.Write((Int16)SpecialType);
        client.SendMessage(outmsg, NetDeliveryMethod.ReliableUnordered, 2);
    }

    public void SendPlayerUpdate(int id, float xvel, float yvel, bool firing)
    {
        if (isSoloMode)
            return;

        PlayerObject player = Players.Find(p => p.Id == id);

        NetOutgoingMessage outmsg = new NetOutgoingMessage();
        outmsg.Write((byte)PacketTypes.PlayerAction);
        outmsg.Write(player.Obj.transform.position.x);
        outmsg.Write(player.Obj.transform.position.z);
        outmsg.Write(xvel);
        outmsg.Write(yvel);
        outmsg.Write(firing);
        client.SendMessage(outmsg, NetDeliveryMethod.ReliableOrdered, 1);
    }

    public void SendLobbyText(string msg)
    {
        NetOutgoingMessage outmsg = new NetOutgoingMessage();
        outmsg.Write((byte)PacketTypes.LobbyMessage);
        outmsg.Write(msg);
        client.SendMessage(outmsg, NetDeliveryMethod.ReliableOrdered, 1);
    }

    public void AddLobbyMessage(string message)
    {
        lobby.Add(message);
        if (lobby.Count > 4)
            lobby.Remove(lobby[0]);
    }

    public void AddConsoleMessage(string message)
    {
        console.Add(message);
        if (console.Count > 5)
            console.Remove(console[0]);
    }

    public void ShowScores()
    {
        GameObject scores = new GameObject("ScoresManager");
        scores.AddComponent<Scores>();
    }

    void StartSoloGame()
    {
        Vector3 position = new Vector3(0f, 15f, -500f);

        username = newname; //You only need to hit Set when playing multiplayer.

        PlayerPrefs.SetString("Username", username);

        PlayerPrefs.Save();

        GameObject p = GameObject.Instantiate(Resources.Load("player"), position, Quaternion.identity) as GameObject;

        Players.Add(new PlayerObject(1, p, username));

        Player = Players[0];

        cb = new CrabBattleServer.CrabBehavior();

        Debug.Log(username);

        gamephase = 1; //Start game.
    }

    public void SendStart()
    {
        if (isSoloMode)
        {
            StartSoloGame();
            return;
        }
        NetOutgoingMessage outmsg = new NetOutgoingMessage();
        outmsg.Write((byte)PacketTypes.StartGame);
        client.SendMessage(outmsg, NetDeliveryMethod.ReliableOrdered, 1);
    }

    public void TakeHit()
    {
        Player.hittaken += 1;

        if (isSoloMode)
            return;
        NetOutgoingMessage outmsg = new NetOutgoingMessage();
        outmsg.Write((byte)PacketTypes.PlayerHit);
        client.SendMessage(outmsg, NetDeliveryMethod.ReliableUnordered, 0);
    }

    public void ToggleReady(bool ready)
    {
		// no point in changing ready flag as there is only one player
        if (isSoloMode)
            return;

        isReady = ready;

        NetOutgoingMessage outmsg = new NetOutgoingMessage();
        outmsg.Write((byte)PacketTypes.Ready);
        outmsg.Write(isReady);
        client.SendMessage(outmsg, NetDeliveryMethod.ReliableUnordered, 2);
    }
	
	public void TogglePlayIntro(bool intro)
    {
        isPlayIntro = intro;
		
		if (isSoloMode)
            return;

        NetOutgoingMessage outmsg = new NetOutgoingMessage();
        outmsg.Write((byte)PacketTypes.PlayIntro);
        outmsg.Write(isPlayIntro);
        client.SendMessage(outmsg, NetDeliveryMethod.ReliableUnordered, 2);
    }

    public void ChangeDifficulty(int newdiff, int newhealth)
    {
        if (isSoloMode)
        {
            difficulty = newdiff;
            healthmod = newhealth;
            return;
        }

        difficulty = newdiff;
        healthmod = newhealth;

        Debug.Log("Setting difficulty to " + difficulty + " and health to " + healthmod);

        NetOutgoingMessage outmsg = new NetOutgoingMessage();
        outmsg.Write((byte)PacketTypes.SettingsChange);
        outmsg.Write((Int16)difficulty);
        outmsg.Write((Int16)healthmod);
        client.SendMessage(outmsg, NetDeliveryMethod.ReliableUnordered, 2);
    }

    public void ChangeName(string name)
    {
        if (isSoloMode)
            return;

        NetOutgoingMessage outmsg = new NetOutgoingMessage();
        outmsg.Write((byte)PacketTypes.UpdateName);
        outmsg.Write(name);
        client.SendMessage(outmsg, NetDeliveryMethod.ReliableUnordered, 0);
        AddConsoleMessage("Name change request sent to the server.");
    }
	
	// Update is called once per frame
	void Update () 
    {
        if (isSoloMode)
        {
            if(gamephase == 2)
                cb.GoGoBattleCrab(Time.time, Time.deltaTime);
            return; //Don't do any of the net management stuff if we're solo.
        }

        if (client.Status == NetPeerStatus.Running && (inc = client.ReadMessage()) != null)
        {
            switch (inc.MessageType)
            {
                case NetIncomingMessageType.Data:
                    {
                        switch (inc.ReadByte())
                        {
							case (byte)PacketTypes.PlayIntro:
                                {
									isPlayIntro = inc.ReadBoolean();
                                    AddConsoleMessage("isPlayIntro "+isPlayIntro);
									Debug.Log("isPlayIntro "+isPlayIntro);
                                }
                                break;
                            case (byte)PacketTypes.Message:
                                {
                                    AddConsoleMessage(inc.ReadString());
                                }
                                break;
                            case (byte)PacketTypes.LobbyMessage:
                                {
                                    AddLobbyMessage(inc.ReadString());
                                }
                                break;

                            case (byte)PacketTypes.PlayerCount:
                                {
                                    numPlayers = inc.ReadInt16();
                                    AddConsoleMessage("The number of connected users has changed to " + numPlayers + ".");
                                }
                                break;
                            case (byte)PacketTypes.SettingsChange:
                                {
                                    difficulty = inc.ReadInt16();
                                    healthmod = inc.ReadInt16();
                                    Debug.Log(difficulty + " " + healthmod);
                                    AddConsoleMessage("Server sent changes to the difficulty settings.");
                                }
                                break;
                            case (byte)PacketTypes.AssignId:
                                {
                                    if (ClientId > 0 && username != "")
                                    {
                                        //We were previously connected before.  Re-submit our namechange request.
                                        ChangeName(username);
                                    }

                                    ClientId = inc.ReadInt32();
                                    
                                    if (username == "")
                                    {
                                        username = "Player " + ClientId;
                                        newname = username;
                                    }

                                    isConnected = true;
                                    lastBeat = Time.time;

                                    AddConsoleMessage("Server assigned you an id of " + ClientId + ".");
                                }
                                break;
                            case (byte)PacketTypes.AddPlayer:
                                {
                                    int playerid = inc.ReadInt16();
                                    float x = inc.ReadFloat();
                                    float y = inc.ReadFloat();
                                    string name = inc.ReadString();
                                    Vector3 position = new Vector3(x, 15, y);
                                    Debug.Log("Adding player " + playerid + " to scene.");
                                    GameObject p;
                                    if (playerid == ClientId)
                                    {
                                        //Debug.Log(position);
                                        p = GameObject.Instantiate(Resources.Load("player"), position, Quaternion.identity) as GameObject;
                                        Player = new PlayerObject(playerid, p, name);
                                    }
                                    else
                                        p = GameObject.Instantiate(Resources.Load("ally"), position, Quaternion.identity) as GameObject;

                                    Players.Add(new PlayerObject(playerid, p, name));

                                    if (Players.Count >= numPlayers)  //Lets hope it isn't ever larger.
                                        gamephase = 1;  //We have all our players, goooooo
                                }
                                break;
							case (byte)PacketTypes.RemovePlayer:
                                {
									if (gamephase==0) break;
                                    int playerid = inc.ReadInt16();
                                    Debug.Log("Player " + playerid + " had disconnected");
									PlayerObject player = Players.Find(p => p.Id == playerid);
									bool status = Players.Remove(player);
									Debug.Log("Removing player " + playerid +" "+status);
									PlayerController pc = player.Obj.GetComponent<PlayerController>();
									Destroy(pc.playername);
									Destroy(player.Obj);
                                }
                                break;
                            case (byte)PacketTypes.Beat:
                                {
                                    NetOutgoingMessage outmsg = new NetOutgoingMessage();
                                    outmsg.Write((byte)PacketTypes.Beat);
                                    outmsg.Write(inc.ReadInt16());
                                    if (Player != null)
                                    {
                                        outmsg.Write(Player.Obj.transform.position.x);
                                        outmsg.Write(Player.Obj.transform.position.z);
                                    }
                                    else
                                    {
                                        outmsg.Write(0f);
                                        outmsg.Write(0f);
                                    }
                                    outmsg.Write((float)Enemy.transform.position.x);
                                    outmsg.Write((float)Enemy.transform.position.z);
                                    client.SendMessage(outmsg, NetDeliveryMethod.ReliableOrdered, 0);
                                    //AddConsoleMessage("Client responded to a server sync message.");
                                    lastBeat = Time.time;

                                    roundtriptime = inc.ReadFloat();
                                }
                                break;
                            case (byte)PacketTypes.EnemySync:
                                {
                                    inc.ReadInt16();
                                    EnemyManager.CrabMoveSync(inc.ReadFloat(), inc.ReadFloat(), inc.ReadFloat(), inc.ReadFloat(), inc.ReadBoolean(), inc.ReadFloat() + roundtriptime);
                                }
                                break;
                            case (byte)PacketTypes.PlayerSpecial:
                                {
                                    int playerid = inc.ReadInt16();

                                    PlayerObject player = Players.Find(p => p.Id == playerid);
                                    if (player == null)
                                        break;

                                    int specialType = inc.ReadInt16();

                                    Debug.Log("Got special action request for player " + playerid);

                                    if(playerid != ClientId)
                                        player.Controller.UseSpecial(specialType);
                                }
                                break;
                            case (byte)PacketTypes.PlayerAction:
                                {
                                    int playerid = inc.ReadInt16();

                                    PlayerObject player = Players.Find(p => p.Id == playerid);
                                    if (player == null)
                                        break;

                                    float x = inc.ReadFloat();
                                    float y = inc.ReadFloat();

                                    player.xVelocity = inc.ReadFloat();
                                    player.yVelocity = inc.ReadFloat();

                                    bool isShooting = inc.ReadBoolean();

                                    float triptime = inc.ReadFloat() + roundtriptime;
                                    
                                    //Only push updates for remote players...
                                    if(playerid != ClientId)
                                        player.Controller.PushUpdate(x, y, player.xVelocity, player.yVelocity, isShooting, triptime);

                                }
                                break;
                            case (byte)PacketTypes.EnemyAction:
                                {
                                    int actionId = inc.ReadInt16();
                                    float speed = inc.ReadFloat();
                                    int seed = inc.ReadInt16();

                                    EnemyManager.CrabCommand(actionId, speed, seed);
                                }
                                break;
                            case (byte)PacketTypes.EnemyHealth:
                                {
                                    EnemyManager.CurrentHealth = inc.ReadInt16();
                                }
                                break;
                            case (byte)PacketTypes.PlayerHit:
                                {
                                    int playerid = inc.ReadInt16();

                                    if (Player.Id == playerid)
                                        break;

                                    PlayerObject player = Players.Find(p => p.Id == playerid);

                                    GameObject.Instantiate(Resources.Load("Detonator-Insanity"), player.Obj.transform.position, Quaternion.identity);
                                }
                                break;
                            default:
                                inc.Reset();
                                Debug.Log("Unhandled Packet Type Recieved.  Packettype is " + Enum.GetName(typeof(PacketTypes), (int)inc.ReadByte()) + ".");
                                break;
                        }
                    }
                    break;
                default:

                    break;
            }
        }

        if ((lastBeat + 10 < Time.time) || (lastBeat + 4 < Time.time && !isConnected))
        {
            if (isConnected)
            {
                AddConsoleMessage("Lost connection to server.  Attempting to reconnect...");
                isConnected = false;
                isReady = false;
            }
            else
                AddConsoleMessage("Attempting to connect to the server...");

            NetOutgoingMessage outmsg = new NetOutgoingMessage();
            outmsg.Write("A Client");

            client.Connect(hostIp, 14248, outmsg);

            lastBeat = Time.time;
        }
	}

    public void OnApplicationQuit()
    {
        if (!isSoloMode)
        {
            NetOutgoingMessage outmsg = client.CreateMessage();
            outmsg.Write((byte)PacketTypes.Disconnect);
            client.SendMessage(outmsg, NetDeliveryMethod.ReliableOrdered);
			client.Shutdown("Bye All");
			print("Closing client connection...");
        }
    }
}
