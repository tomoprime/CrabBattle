using System;
using System.Threading;

namespace CrabBattleServer
{
    class Program
    {    
		static void Main(string[] args)
        {
        int serverport = 14248;
        int policyport = 8843;
        int maxconnections = 50;
        
        if (args.Length>0)
        	serverport = int.Parse(args[0]);
        if (args.Length>1)
        	policyport = int.Parse(args[1]);
        if (args.Length>2)
        	maxconnections = int.Parse(args[2]);
        
			Console.WriteLine("Usage: CrabBattleServer.exe");
			Console.WriteLine("       CrabBattleServer.exe srvport");
			Console.WriteLine("       CrabBattleServer.exe srvport polyport");
			Console.WriteLine("       CrabBattleServer.exe srvport polyport maxplayers\n");
			Console.WriteLine("Example: CrabBattleServer.exe "+serverport+" "+policyport+" "+maxconnections+"\n");
			
			//Setup the policy server first.
            const string AllPolicy =
				@"<?xml version='1.0'?>"+
					"<cross-domain-policy>" +
					"	<allow-access-from domain='*' to-ports='*' />"+
					"</cross-domain-policy>";
			
			// start policy server on non root port > 1023
            SocketPolicyServer policyServer = new SocketPolicyServer(AllPolicy, policyport);
            policyServer.Start();

            // start game server on non root port > 1023 and max connections 20
            GameServer gs = new	GameServer(serverport, maxconnections);
			
			gs.StartServer();
			
			Console.WriteLine("\n Hit 'ESC' to stop service.");
			while (!Console.KeyAvailable || Console.ReadKey().Key != ConsoleKey.Escape)
				Thread.Sleep(50);
			
			policyServer.Stop();
			gs.StopServer();
    	}
    }
}
