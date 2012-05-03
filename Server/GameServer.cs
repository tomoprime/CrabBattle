using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;
using Lidgren.Network;

enum PacketTypes
{
    Beat,
    AssignId,
    Ready,
    UpdateName,
	KeepAlive,
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
    EnemySync,
	MessageDebug
}

enum GameState
{
    Lobby,
    Intro,
    InGame
}

class PlayerObject
{
    public NetConnection Connection;
    public int Id;
    public float X;
    public float Y;
    public float VelocityX;
    public float VelocityY;
    public bool Firing;
    public bool Ready;
	public bool Started;
    public string Name;
    public int LastBeat;

    public int dmgnormal = 0;
    public int dmgweakpoint = 0;
    public int hitstaken = 0;
	public double keepAlive = 0, lastKeepAlive = 0;

    public PlayerObject(int id, NetConnection connection, string name, int curBeat)
    {
        Connection = connection;
        Id = id;
        X = 0;
        Y = 0;
        VelocityX = 0;
        VelocityY = 0;
        Ready = false;
        Name = name;
        LastBeat = curBeat;
    }
}

/*
 * Created by TomoPrime
 * 
 * Added update for Sequence Channels
 * 
 * PacketTypes of:
 * 0 - Message, MessageDebug are set to 0
 * 1 - Mostly Server connect types are like: AssignID, AddPlayer, RemovePlayer, StartGame, Disconnect
 * 2 - Player related types are like: PlayerHit, PlayerSpecial, PlayerAction
 * 3 - Enemy types are like: EnemySync, EnemyHealth, EnemyAction
 * 4 - Beat aka heartbeat probably will go away later on replaced by KeepAlive
 * 5 - Mostly Game settings like: SettingsChange, PlayerIntro, Ready
 * 6 - PlayerCount used to keep track of new joiners and leavers
 */

namespace CrabBattleServer
{ 
	class GameServer
	{
		private static NetServer server;
		private NetPeerConfiguration config;
		//private AsyncOperation operation;
		public static List<PlayerObject> players;
		private int idCount;
		public static int healthMod = 1;
		public int ticksPerSecond = 66;
		private CrabBehavior crab;
		private DateTime lastBeat, introtime;
		private TimeSpan beatrate, introlength;
		private double last15sec;
		private bool playintro = true;
		private volatile bool isRunning;
		private int gamePhase = 0;
		private int beatnum = 0;
		private int gameDifficulty = 1;
		private Thread t1;//, t2;
		private NetIncomingMessage inmsg;
		
		public GameServer (int gameport, int maxplayers)
		{
			// needed for RegisterReceivedCallback, TODO replace with AsyncOperationM...
			if(SynchronizationContext.Current == null) 
				SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
			
			//operation = AsyncOperationManager.CreateOperation(null);
			
			config = new NetPeerConfiguration("crab_battle");
	        config.Port = gameport;
	        config.MaximumConnections = maxplayers;
			config.ConnectionTimeout = 10; // ping timeout default 25 secs. Cannot be less than PingInterval
			config.PingInterval=3; // default is 4 secs
			//config.MaxConnectionRetries
			config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
		}
		
		public void StartServer()
		{
			players = new List<PlayerObject>();
			server = new NetServer(config);
			server.RegisterReceivedCallback(new SendOrPostCallback(HandleMessages));
			server.Start();
			Console.WriteLine(" Crab Battle server open for business on Port: "+config.Port);
			Console.WriteLine(" ConnectionTimeout "+config.ConnectionTimeout+" PingInterval "+config.PingInterval);
			Console.WriteLine(" Max number of players is: "+config.MaximumConnections);
			
			t1 = new Thread(new ThreadStart(RunGameLoop));
			t1.Name = "Game Loop Thread";
			t1.Start();
			Thread.Sleep(500);
			// Replaced by RegisterReceivedCallback a.k.a HandleMessages
			/*
			t2 = new Thread(new ThreadStart(MessageProcessor));
			t2.Name = "Message Loop Thread";
			t2.Start();
			*/	
		}
		
		public void StopServer()
		{
			isRunning = false;
			if (server!=null)
				server.Shutdown(" Stopping Server");
			if (t1 != null)
				t1.Join();
			Console.WriteLine(" Crab Battle server was shutdown.");
		}
		
		public void SendStartGame(NetConnection conn, string playername)
		{
			Console.WriteLine("Sending ok to StartGame for "+playername);
			NetOutgoingMessage outmsg;
			outmsg = server.CreateMessage();
			outmsg.Write((byte)PacketTypes.StartGame);
			server.SendMessage(outmsg, conn, NetDeliveryMethod.ReliableOrdered, 1);
		}
		
		public void StartGame(PlayerObject player)
		{
			//Someone clicked start game.  Lets make sure everyone is ready and we're in a state where we can start.
			
			// Is Hailing player ready?
			//if (!player.Ready) return;
			
			// prevent players from hitting start more than once
			if (player.Started) return;
			
			NetOutgoingMessage outmsg;
			
			// Is All Players in the Lobby Ready?
			if (gamePhase == (int)GameState.Lobby)
				if (players.TrueForAll(p => p.Ready == true))
					{
						Console.WriteLine("Everone appears ready...");
					}
				else
				{
					outmsg = server.CreateMessage();
					outmsg.Write((byte)PacketTypes.Message);
					outmsg.Write(player.Name+" attempted to start the game, but not all players are ready.");
					server.SendToAll(outmsg, NetDeliveryMethod.ReliableOrdered);
					SendLobbyMessage("Server", "Game cannot start until all users are ready.");
					return;
				}
			
			// Ok we made it this far let's start adding our players
			float numplayers = players.Count();
			float curplayer = ((numplayers-1) * 20f) / 2f * -1f;
			Console.WriteLine("Total: "+players.Count);
			
			foreach (PlayerObject p in players)
			{
		        outmsg = server.CreateMessage();
		        outmsg.Write((byte)PacketTypes.AddPlayer);
		        outmsg.Write((Int16)p.Id);
			
				// If new joiner or if waiting in Lobby send this player's data to everyone.
				// Let's create all the player objects for all players, 20 paces apart.
				if (player.Id == p.Id || gamePhase == (int) GameState.Lobby)
				{
					Console.WriteLine("A - Creating player "+p.Id+" gamephase "+gamePhase);
					p.X = curplayer;
					p.Y = -400; // -500 Hardcoded ftw!
					p.Started = true;
					curplayer += 20f;
				
			        outmsg.Write(p.X);
			        outmsg.Write(p.Y);
			        outmsg.Write(p.Name);
					server.SendMessage(outmsg,inmsg.SenderConnection, NetDeliveryMethod.ReliableOrdered, 1);
					
					// Above sent data to current player. Below sends data to everyone else
					outmsg = server.CreateMessage();
			        outmsg.Write((byte)PacketTypes.AddPlayer);
			        outmsg.Write((Int16)p.Id);
					outmsg.Write(p.X);
			        outmsg.Write(p.Y);
			        outmsg.Write(p.Name);
					server.SendToAll(outmsg,inmsg.SenderConnection, NetDeliveryMethod.ReliableOrdered, 1);
					
					// Send current Crab position, health, and target sync for new joiner...
					if (gamePhase == (int)GameState.InGame || gamePhase == (int)GameState.Intro)  
					{
						PlayerObject target = players.Find(t => t.Id == crab.CurrentTarget);
                        if (target == null) target = player; // most likely the game is already over.
	                    outmsg = server.CreateMessage();
	                    outmsg.Write((byte)PacketTypes.EnemySync);
	                    outmsg.Write((Int16)target.Id); //Id of the current crab controller.
	                    outmsg.Write(crab.RealPosition.X); //EnemyX
	                    outmsg.Write(crab.RealPosition.Z); //EnemyZ
	                    outmsg.Write(target.X);
	                    outmsg.Write(target.Y);
	                    outmsg.Write(crab.Direction);
	                    outmsg.Write(target.Connection.AverageRoundtripTime/2f); //Divide by 2 to get trip time.
	                    server.SendMessage(outmsg,inmsg.SenderConnection, NetDeliveryMethod.ReliableOrdered, 3);
						
						outmsg = server.CreateMessage();
                        outmsg.Write((byte)PacketTypes.EnemyHealth);
                        outmsg.Write((Int16)crab.CurrentHealth);
                        server.SendMessage(outmsg,inmsg.SenderConnection, NetDeliveryMethod.ReliableOrdered, 3);
					}
				}
				// Otherwise only send each person's data to the new joiner as the game is already running
				else if (gamePhase == (int)GameState.InGame || gamePhase == (int)GameState.Intro)
				{
					Console.WriteLine("B - Creating player "+p.Id+" gamephase "+gamePhase);
					outmsg.Write(p.X);
			        outmsg.Write(p.Y);
			        outmsg.Write(p.Name);
					server.SendMessage(outmsg,inmsg.SenderConnection, NetDeliveryMethod.ReliableOrdered, 1);
				}
				else Console.WriteLine ("Something bad happened!");
			}
			
			// If the game's running don't reset it, just notify the new player to join in.
			if (gamePhase == (int)GameState.InGame || gamePhase == (int)GameState.Intro) 
			{
				// Since the game is already in session only let the hailing player know it's ok to start.
				SendStartGame(inmsg.SenderConnection, player.Name);
				return;
			}
			
			// Otherwise let everyone know it's ok to start the game.
			players.ForEach(p=>SendStartGame(p.Connection, p.Name));
			
			crab = new CrabBehavior(); //Prepare the CRAB!
			introtime = DateTime.Now;
			gamePhase = (int)GameState.Intro;
			return;
		}
		
		public void RunGameLoop()
		{
			Console.WriteLine(" Starting RunGameLoop Thread.");
			//isRunning = true;
			
			if(playintro == false)
			   introlength = new TimeSpan(0, 0, 7);
			else
			    introlength = new TimeSpan(0, 0, 21);
			    
			beatrate = new TimeSpan(0, 0, 1);
			
			introtime = DateTime.Now;
			lastBeat = DateTime.Now;
			
			NetOutgoingMessage outmsg;
			
			crab = new CrabBehavior();
			
			//while(isRunning)
			int sendRate = 1000 / ticksPerSecond; // 1 sec = 1000ms as Sleep uses ms.
			for ( isRunning=true; isRunning; Thread.Sleep(sendRate) )
			{
                if (gamePhase != (int)GameState.Lobby && players.Count == 0)
                {
                    Console.WriteLine("All players disconnected, returning to lobby gamestate.");
					
                    gamePhase = (int)GameState.Lobby;
                }
				
                if (gamePhase == (int)GameState.Intro && ((introtime + introlength) < DateTime.Now))
                {
                    //Intro has ran for it's length, lets start the game proper.
					Console.WriteLine("PlayIntro was set to '"+playintro+"' sending StartGame packet to clients...");
                    //outmsg = server.CreateMessage();
                    //outmsg.Write((byte)PacketTypes.StartGame);
                    //server.SendMessage(outmsg, server.Connections, NetDeliveryMethod.ReliableOrdered, 1);

                    crab.Direction = false;
                    gamePhase = (int)GameState.InGame;
                }
                
                if(gamePhase == (int)GameState.InGame && crab.CurrentHealth>0)
                {
                    crab.GoGoBattleCrab();
                } 
				
			// Handle dropping players and long idle connections. Note Client must send a KeepAlive packet within 15s
			if (NetTime.Now > last15sec+15)
				{
					foreach (PlayerObject player in players)
					{
						//Console.WriteLine("KeepAlive Check "+player.Name+" (Id"+player.Id+") "+ player.Connection.Status+" RTT "+player.Connection.AverageRoundtripTime +" keepAlive "+player.keepAlive+" vs. lastKeepAlive "+player.lastKeepAlive);
						if (player.keepAlive > player.lastKeepAlive)
							player.lastKeepAlive = NetTime.Now;
						else if (player.keepAlive != player.lastKeepAlive) 
						{
							SendLobbyMessage("Server","You may need the latest github update v1.1 for multiplayer support.",player.Connection);
							SendConsoleMessage("You may need the lastest github update v1.1 for multiplayer support.",player.Connection);
							SendMessageDebug("You may need the lastest github update v1.1 for multiplayer support.",player.Connection);
							player.Connection.Deny(player.Name + " (Id"+player.Id+") idle connection's keepAlive "+player.keepAlive+" is < lastKeepAlive "+player.lastKeepAlive);
						}
						else if (player.keepAlive == player.lastKeepAlive)
							player.lastKeepAlive = NetTime.Now;
					}
					last15sec = NetTime.Now;
				}

                //Send update beats.
                if ((lastBeat + beatrate) < DateTime.Now && gamePhase >=1)
                {
                    //Send a beat to all users. Because the server doesn't really know if they disconnected unless we are sending packets to them.
                    //Beats go out every 2 seconds.
                    foreach (PlayerObject p in players)
                    {
                        outmsg = server.CreateMessage();
                        outmsg.Write((byte)PacketTypes.Beat);
                        outmsg.Write((Int16)beatnum);
                        outmsg.Write(p.Connection.AverageRoundtripTime/2f);
                        server.SendMessage(outmsg, p.Connection, NetDeliveryMethod.ReliableOrdered, 4);
                    }
                    beatnum++;
                    lastBeat = DateTime.Now;
                }
			}
			Console.WriteLine(" Stopped RunGameLoop Thread.");
		}
		
		/*		
		public void MessageProcessor()
		{
			Console.WriteLine("Starting MessageProcessor Thread");
            while(true)
			{
				server.MessageReceivedEvent.WaitOne(); 
				HandleMessages(server.ReadMessage());
			}
		}
		*/
		
		public static void CrabAction(int actionId, float speed)
        {
            Console.WriteLine("The crab is performing action " + Enum.GetName(typeof(CrabActions), actionId) + " with a modifier of "+speed+"x.");

            NetOutgoingMessage outmsg = server.CreateMessage();
            outmsg.Write((byte)PacketTypes.EnemyAction);
            outmsg.Write((Int16)actionId);
            outmsg.Write(speed);
            outmsg.Write((Int16)0f); //seed
            server.SendMessage(outmsg, server.Connections, NetDeliveryMethod.ReliableOrdered, 3);
        }
		
		public void SendMessageDebug(string message)
        {
			NetOutgoingMessage outmsg = server.CreateMessage();
			outmsg.Write((byte)PacketTypes.MessageDebug);
			outmsg.Write(message);
			server.SendMessage(outmsg, server.Connections, NetDeliveryMethod.ReliableOrdered, 0);
		}
		
		public void SendMessageDebug(string message, NetConnection singleConn)
        {
			NetOutgoingMessage outmsg = server.CreateMessage();
			outmsg.Write((byte)PacketTypes.MessageDebug);
			outmsg.Write(message);
			server.SendMessage(outmsg, singleConn, NetDeliveryMethod.ReliableOrdered, 0);
		}
		
		public void SendConsoleMessage(string message)
        {
			NetOutgoingMessage outmsg = server.CreateMessage();
			outmsg.Write((byte)PacketTypes.Message);
			outmsg.Write(message);
			server.SendMessage(outmsg, server.Connections, NetDeliveryMethod.ReliableOrdered, 0);
		}
		
		public void SendConsoleMessage(string message, NetConnection singleConn)
        {
			NetOutgoingMessage outmsg = server.CreateMessage();
			outmsg.Write((byte)PacketTypes.Message);
			outmsg.Write(message);
			server.SendMessage(outmsg, singleConn, NetDeliveryMethod.ReliableOrdered, 0);
		}
		
        public void SendLobbyMessage(string username, string message)
        {
            string msg = username + ": " + message;

            NetOutgoingMessage outmsg = server.CreateMessage();
            outmsg.Write((byte)PacketTypes.LobbyMessage);
            outmsg.Write(msg);
            server.SendMessage(outmsg, server.Connections, NetDeliveryMethod.ReliableOrdered, 0);
        }
		
		public void SendLobbyMessage(string username, string message, NetConnection singleConn)
        {
            string msg = username + ": " + message;

            NetOutgoingMessage outmsg = server.CreateMessage();
            outmsg.Write((byte)PacketTypes.LobbyMessage);
            outmsg.Write(msg);
            server.SendMessage(outmsg, singleConn, NetDeliveryMethod.ReliableOrdered, 0);
        }
		
		public void AddNewPlayer(NetIncomingMessage inc)
		{
			NetOutgoingMessage outmsg;

            Console.WriteLine("Assigning new player the name of Player " + (++idCount) + ".");
            inmsg.SenderConnection.Approve();  
				
            players.Add(new PlayerObject(idCount, inmsg.SenderConnection, "Player "+idCount, beatnum));
            SendLobbyMessage("Server", "Player "+idCount+" has connected. Connected players: "+players.Count);
            
			outmsg = server.CreateMessage();
            outmsg.Write((byte)PacketTypes.Message);
            outmsg.Write("You are now connected to CrabBattle Server.");
            server.SendMessage(outmsg, inmsg.SenderConnection, NetDeliveryMethod.ReliableOrdered, 0);
			
            // Assign Id number to client
            outmsg = server.CreateMessage();
            outmsg.Write((byte)PacketTypes.AssignId);
            outmsg.Write((Int32)(idCount));
            server.SendMessage(outmsg, inmsg.SenderConnection, NetDeliveryMethod.ReliableOrdered, 1);
	
            //Send the current status of Play Intro to client
            outmsg = server.CreateMessage();
            outmsg.Write((byte)PacketTypes.PlayIntro);
			if(gamePhase == (int)GameState.InGame || gamePhase == (int)GameState.Intro)
				outmsg.Write(false);
			else outmsg.Write(playintro);
            server.SendMessage(outmsg, inmsg.SenderConnection, NetDeliveryMethod.ReliableOrdered, 5);
	
            //Send difficulty settings to the new player
            Console.WriteLine("Game diffculty :"+gameDifficulty + " Battle Length:" + healthMod);
            outmsg = server.CreateMessage();
            outmsg.Write((byte)PacketTypes.SettingsChange);
            outmsg.Write((Int16)gameDifficulty);
            outmsg.Write((Int16)healthMod);
            server.SendMessage(outmsg, inmsg.SenderConnection, NetDeliveryMethod.ReliableOrdered, 5);
			
			//Send the current playercount to current player (yes sending all isn't enough for new player)
            outmsg = server.CreateMessage();
            outmsg.Write((byte)PacketTypes.PlayerCount);
            outmsg.Write((Int16)players.Count);
            server.SendMessage(outmsg, inmsg.SenderConnection, NetDeliveryMethod.ReliableOrdered, 6);
			
			//Send the current playercount to all but current player
            outmsg = server.CreateMessage();
            outmsg.Write((byte)PacketTypes.PlayerCount);
            outmsg.Write((Int16)players.Count); 
			server.SendToAll(outmsg,inmsg.SenderConnection, NetDeliveryMethod.ReliableOrdered, 6);
		}
		
		public void HandleMessages(object fromPlayer)
		{
			if ((inmsg = ((NetServer)fromPlayer).ReadMessage()) == null) return;

			NetOutgoingMessage outmsg;
                switch(inmsg.MessageType)
                {
					/*
					case NetIncomingMessageType.DebugMessage:
					case NetIncomingMessageType.VerboseDebugMessage:
					case NetIncomingMessageType.WarningMessage:
					*/
					case NetIncomingMessageType.ConnectionApproval:
						{
							if (players.Count==0) idCount = 0;
                        	Console.WriteLine("Incoming login request. " + inmsg.SenderConnection.ToString());
							AddNewPlayer(inmsg);
							
							if (gamePhase == (int)GameState.InGame || gamePhase == (int)GameState.Intro) 
							{
								// Auto Join Game
								StartGame(players.Find(p => p.Connection == inmsg.SenderConnection));
							}
						}
                        break;
                    case NetIncomingMessageType.Data:

                        PlayerObject player = players.Find(p => p.Connection == inmsg.SenderConnection);
                        if (player == null || inmsg.LengthBytes < 1)
                            break; //Don't accept data from connections that don't have a player attached.

                        switch ((PacketTypes)inmsg.ReadByte())
                        {
                            case PacketTypes.LobbyMessage:
                                {
                                    string msg = inmsg.ReadString();
                                    SendLobbyMessage(player.Name, msg);
                                    Console.WriteLine("Chat "+player.Name + ": " + msg);
                                }
                                break;
                            case PacketTypes.SettingsChange:
                                {
									if(inmsg.LengthBytes<2) break;
                                    //Difficulty or health mod changed, broadcast changes to all clients.
                                    gameDifficulty = inmsg.ReadInt16();
                                    healthMod = inmsg.ReadInt16();
									crab.CalculateHealth();
                                    Console.WriteLine(player.Name+ " (Id"+player.Id+") changed difficulty/healthmod to " + gameDifficulty + "/" + healthMod + ".");

                                    outmsg = server.CreateMessage();
                                    outmsg.Write((byte)PacketTypes.SettingsChange);
                                    outmsg.Write((Int16)gameDifficulty);
                                    outmsg.Write((Int16)healthMod);
                                    server.SendMessage(outmsg, server.Connections, NetDeliveryMethod.ReliableOrdered, 5);
                                }
                                break;
							case PacketTypes.KeepAlive:
								{
									// one way heartbeat from client. Server sets a timestamp.
									// Unity can potentially keep unstable client threads open that waist server resources
									player.keepAlive = NetTime.Now;
								}
								break;
                            case PacketTypes.Beat:
                                {
									if (inmsg.LengthBytes<2) break;// .PeekInt16()) break;
									//if (!player.Ready) break;
                                    player.LastBeat = inmsg.ReadInt16();
                                    player.X = inmsg.ReadFloat();
                                    player.Y = inmsg.ReadFloat();

                                    if (crab.CurrentTarget == player.Id)
                                    {
                                        float CrabX = inmsg.ReadFloat();
                                        float CrabZ = inmsg.ReadFloat();
										
										crab.RealPosition.X = CrabX;
										crab.RealPosition.Z = CrabZ;

                                        if (crab.random.Next(0, 10) > 8)
                                            crab.Direction = !crab.Direction;
                                        
                                        //Crab position and target sync.
                                        outmsg = server.CreateMessage();
                                        outmsg.Write((byte)PacketTypes.EnemySync);
                                        outmsg.Write((Int16)player.Id); //Id of the current crab controller.
                                        outmsg.Write(CrabX); //EnemyX
                                        outmsg.Write(CrabZ); //EnemyZ
                                        outmsg.Write(player.X);
                                        outmsg.Write(player.Y);
                                        outmsg.Write(crab.Direction);
                                        outmsg.Write(player.Connection.AverageRoundtripTime/2f); //Divide by 2 to get trip time.
                                        server.SendMessage(outmsg, server.Connections, NetDeliveryMethod.ReliableOrdered, 3);
                                    }
                                }
                                break;
							case PacketTypes.Ready:
                                {
                                    //Player is ready to start the game.
                                    player.Ready = inmsg.ReadBoolean();

                                    Console.WriteLine(player.Name + " (Id"+player.Id+") changed their ready status to " + player.Ready);
                                    if (player.Ready)
                                        SendLobbyMessage("Server", player.Name + " is now Ready.");
                                    else
                                        SendLobbyMessage("Server", player.Name + " is no longer ready.");
                                }
                                break;
							case PacketTypes.PlayIntro:
                                {
									if(gamePhase == (int)GameState.InGame || gamePhase == (int)GameState.Intro)
									{
										SendConsoleMessage("cannot change intro while game is in play",inmsg.SenderConnection);
										outmsg = server.CreateMessage();
									    outmsg.Write((byte)PacketTypes.PlayIntro);
										outmsg.Write(false);
										server.SendMessage(outmsg, inmsg.SenderConnection, NetDeliveryMethod.ReliableOrdered, 5);
										break;
									}
                                    //Player changed the status of Play Intro
                                    playintro = inmsg.ReadBoolean();
									if(playintro) introlength = new TimeSpan(0, 0, 21);
	           						else introlength = new TimeSpan(0, 0, 7);

                                    Console.WriteLine(player.Name + " (Id"+player.Id+") changed PlayInto to " + playintro);
                                    if (playintro)
                                        SendLobbyMessage("Server", player.Name + " enabled play intro.");
                                    else
                                        SendLobbyMessage("Server", player.Name + " skipped play intro.");
									
									// Now sync everyone up.
									outmsg = server.CreateMessage();
                                    outmsg.Write((byte)PacketTypes.PlayIntro);
									outmsg.Write(playintro);
                                    server.SendMessage(outmsg, server.Connections, NetDeliveryMethod.ReliableOrdered, 5);
                                }
                                break;
                            case PacketTypes.UpdateName:
                                {
                                    //Player changed their name.  Since the clients aren't aware of each other until the game starts,
                                    //there's no need to broadcast this message to other users.
                                    string newname = inmsg.ReadString();
                                    Console.WriteLine(player.Name + " (Id"+player.Id+") changed their name to '" + newname + "'.");
                                    SendLobbyMessage("Server", player.Name + " (Id"+player.Id+") changed their name to '" + newname + "'.");
                                    player.Name = newname;
									
									// let all other clients know that player changed his/her name
									outmsg = server.CreateMessage();
                                    outmsg.Write((byte)PacketTypes.UpdateName);
									outmsg.Write(player.Name);
									outmsg.Write(player.Id);
									server.SendToAll(outmsg, inmsg.SenderConnection, NetDeliveryMethod.ReliableOrdered, 5);
                                }
                                break;
                            case PacketTypes.Disconnect:
                                {
                                    //Player requests to disconnect from the server.
                                    Console.WriteLine(player.Name+" (Id"+player.Id+") has disconnected, removing player object.");

                                    outmsg = server.CreateMessage();
                                    outmsg.Write((byte)PacketTypes.RemovePlayer);
                                    outmsg.Write((Int16)player.Id);
                                    server.SendMessage(outmsg, server.Connections, NetDeliveryMethod.ReliableOrdered, 1);
									
                                    players.Remove(player);
									
                                    outmsg = server.CreateMessage();
                                    outmsg.Write((byte)PacketTypes.PlayerCount);
                                    outmsg.Write((Int16)players.Count);
                                    server.SendMessage(outmsg, server.Connections, NetDeliveryMethod.ReliableOrdered, 6);
                                }
                                break;
                            case PacketTypes.StartGame:
                                {
									StartGame(player);
								}    
                                break;
                            case PacketTypes.PlayerSpecial:
                                {
                                    int shottype = inmsg.ReadInt16();

                                    outmsg = server.CreateMessage();
                                    outmsg.Write((byte)PacketTypes.PlayerSpecial);
                                    outmsg.Write((Int16)player.Id);
                                    outmsg.Write((Int16)shottype);
                                    server.SendMessage(outmsg, server.Connections, NetDeliveryMethod.ReliableUnordered, 2);
                                    //Console.WriteLine("Relaying Special Action Message for " + player.Name + ".");
                                }
                                break;
                            case PacketTypes.PlayerAction:
                                {
									if(inmsg.LengthBytes<2) break;
                                    //Player hit a key or something!  Change their status, broadcast to other users.

                                    //Set player values
                                    //inmsg.ReadInt16(); //Player id is submitted, but not used.
                                    player.X = inmsg.ReadFloat();
                                    player.Y = inmsg.ReadFloat();
                                    player.VelocityX = inmsg.ReadFloat();
                                    player.VelocityY = inmsg.ReadFloat();
                                    player.Firing = inmsg.ReadBoolean();

                                    //Broadcast them to everyone else
                                    outmsg = server.CreateMessage();
                                    outmsg.Write((byte)PacketTypes.PlayerAction);
                                    outmsg.Write((Int16)player.Id);
                                    outmsg.Write(player.X);
                                    outmsg.Write(player.Y);
                                    outmsg.Write(player.VelocityX);
                                    outmsg.Write(player.VelocityY);
                                    outmsg.Write(player.Firing);
                                    outmsg.Write(player.Connection.AverageRoundtripTime/2f); //Not an exact science, but we'll use this to predict their position.
                                    server.SendMessage(outmsg, server.Connections, NetDeliveryMethod.ReliableOrdered, 2);

                                    //Console.WriteLine("Relaying Action Message for " + player.Name + ". "+player.VelocityX + " " + player.VelocityY);
                                }
                                break;
                            case PacketTypes.HurtTarget:
                                {
                                    int damage = inmsg.ReadInt16();

                                    crab.CurrentHealth -= damage;
                                    bool hittype = inmsg.ReadBoolean();

                                    if (!hittype)
                                        player.dmgnormal += damage;
                                    else
                                        player.dmgweakpoint += damage;

                                    outmsg = server.CreateMessage();
                                    outmsg.Write((byte)PacketTypes.EnemyHealth);
                                    outmsg.Write((Int16)crab.CurrentHealth);
                                    server.SendMessage(outmsg, server.Connections, NetDeliveryMethod.ReliableUnordered, 3);
                                }
                                break;
                            case PacketTypes.PlayerHit:
                                {
                                    player.hitstaken += 1;

                                    outmsg = server.CreateMessage();
                                    outmsg.Write((byte)PacketTypes.PlayerHit);
                                    outmsg.Write((Int16)player.Id);
                                    server.SendMessage(outmsg, server.Connections, NetDeliveryMethod.ReliableUnordered, 2);
                                }
                                break;
                        }
                        break;
                    case NetIncomingMessageType.StatusChanged:
                        {
						 	player = players.Find(p => p.Connection == inmsg.SenderConnection);
                        	if (player == null)
                            	break; //Don't accept data from connections that don't have a player attached.
				
							NetConnectionStatus status = (NetConnectionStatus)inmsg.ReadByte();
                            string reason = inmsg.ReadString();
							
							if (player.Connection.Status == NetConnectionStatus.Disconnected || player.Connection.Status == NetConnectionStatus.Disconnecting)
	                        {
	                            SendLobbyMessage("Server", player.Name + " (Id"+player.Id+") has disconnected.");
	
	                            outmsg = server.CreateMessage();
	                            outmsg.Write((byte)PacketTypes.RemovePlayer);
	                            outmsg.Write((Int16)player.Id);
	                            server.SendMessage(outmsg, server.Connections, NetDeliveryMethod.ReliableOrdered, 1);
								
	                            players.Remove(player);
								
	                            outmsg = server.CreateMessage();
	                            outmsg.Write((byte)PacketTypes.PlayerCount);
	                            outmsg.Write((Int16)players.Count);
	                            server.SendMessage(outmsg, server.Connections, NetDeliveryMethod.ReliableOrdered, 6);
	                        }
                            Console.WriteLine(player.Name + " (Id"+player.Id+") status changed to "+status+" (" + reason + ") "+players.Count);
                        }
                        break;
                    default:
                        break;
            }
        }
	}
}

