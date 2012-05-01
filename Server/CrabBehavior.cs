using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrabBattleServer;

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

namespace CrabBattleServer
{
    class Action
    {
        public int Id;
        public int Probability;
        public int StartHealth;
        public float BaseLength;
        public float DelayTime;
        public bool CanAdjustSpeed;
        public bool CanAdjustDelay;
        public Stopwatch TimeSinceUsage;
        public bool HasBeenUsed;

        public Action(int id, int probability, int startHealth, float baseLength, float delayTime, bool canAdjustSpeed, bool canAdjustDelay)
        {
            Id = id;
            Probability = probability;
            StartHealth = startHealth;
            BaseLength = baseLength;
            DelayTime = delayTime;
            CanAdjustDelay = canAdjustDelay;
            CanAdjustSpeed = canAdjustSpeed;
            HasBeenUsed = false;
            TimeSinceUsage = new Stopwatch();
        }
    }
    
    class CrabBehavior
    {
        public List<Action> MajorActions;
        public List<Action> MovementActions;

        public int CurrentHealth;
        public int MaxHealth;

        public Random random;
        Stopwatch CurrentAction;
        Stopwatch TargetChange;
        float ActionLength;
        bool IsPerformingAction;

        public bool Direction;

        public int CurrentTarget;
		public Vector3 RealPosition;
		
        Stopwatch TotalElapsed;

        float changetick;

        public CrabBehavior()
        {
            random = new Random();
			RealPosition = new Vector3();
            MajorActions = new List<Action>();
            MovementActions = new List<Action>();

            //Id, Probability, StartHealth, BaseLength, DelayTime, CanAdjustSpeed, CanAdjustDelay
            MajorActions.Add(new Action((int)CrabActions.CrazyBarrage, 100, 35, 42, 40, false, false));
            MajorActions.Add(new Action((int)CrabActions.CannonSpawn, 100, 60, 4, 9000, false, false));
            MajorActions.Add(new Action((int)CrabActions.MegaBeam, 100, 60, 12, 25, false, false));
            MajorActions.Add(new Action((int)CrabActions.SweepShot, 100, 100, 5, 15, true, true));
            //MajorActions.Add(new Action((int)CrabActions.Walk, 100, 100, 5, 30, true, true));
            //MajorActions.Add(new Action((int)CrabActions.MegaBeam, 50, 75, 10, 40, false, false));
            MajorActions.Add(new Action((int)CrabActions.RapidCannon, 50, 90, 7, 11, true, true));
            MajorActions.Add(new Action((int)CrabActions.SweepShot, 100, 100, 5, 40, true, true));
            MajorActions.Add(new Action((int)CrabActions.RandomSpray, 75, 75, 9, 15, false, false));
            MajorActions.Add(new Action((int)CrabActions.RapidCannon, 50, 90, 7, 30, true, true));
            MajorActions.Add(new Action((int)CrabActions.SweepShot, 100, 100, 5, 0, true, true));
            
            MovementActions.Add(new Action((int)CrabActions.WalkLeft, 100, 100, 5, 30, true, true));
            MovementActions.Add(new Action((int)CrabActions.WalkRight, 100, 100, 5, 30, true, true));
            MovementActions.Add(new Action((int)CrabActions.WalkStop, 100, 100, 5, 30, true, true));

            CalculateHealth();
            
            TotalElapsed = new Stopwatch();
            TotalElapsed.Start();

            CurrentAction = new Stopwatch();

            TargetChange = new Stopwatch();
            TargetChange.Start();
            changetick = 10000;

            CurrentTarget = -1;
        }
		
		public void CalculateHealth()
		{
			MaxHealth = 800 + 700 * GameServer.players.Count * GameServer.healthMod;
            CurrentHealth = MaxHealth;
		}
		
        public void UseCrabAction(Action crabAction)
        {
            ActionLength = crabAction.BaseLength;

            float actionMulti = 1 - 0.5f * (1f - (float)CurrentHealth / (float)MaxHealth);
            if (actionMulti < 0.65f)
                actionMulti = 0.65f;

            if (crabAction.CanAdjustSpeed)
                ActionLength *= actionMulti;

            if (crabAction.Id == (int)CrabActions.Walk)
            {
                int randomWalk = random.Next(0, 2);
                GameServer.CrabAction(MovementActions[randomWalk].Id, ActionLength);
            }
            else
                GameServer.CrabAction(crabAction.Id, 1/actionMulti);

            crabAction.HasBeenUsed = true;
            crabAction.TimeSinceUsage.Restart();
            CurrentAction.Restart();

            IsPerformingAction = true;
        }

        public void GoGoBattleCrab()
        {
            if (CurrentTarget == -1)
               CurrentTarget = GameServer.players[0].Id;
			
			// new code simply makes crab target only players in game basicly
			List<PlayerObject> readyPlayers = GameServer.players.FindAll(p => p.Ready);
            if (readyPlayers.Count == 0) return;
			
			if (TargetChange.ElapsedMilliseconds > changetick)
            {
                int rnd = random.Next(0, 4);
                if (rnd == 0)
                {
                    changetick = 10000;
                    TargetChange.Restart();
					int prnd = random.Next(0, readyPlayers.Count);
                    CurrentTarget = readyPlayers[prnd].Id;
                    Console.WriteLine("(id "+CurrentTarget+") Crab is changing targets to "+readyPlayers[prnd].Name);
                }
                else
                    changetick += 1000;
            }

            if (IsPerformingAction)
            {
                if (CurrentAction.ElapsedMilliseconds > ActionLength * 1000)
                {
                    IsPerformingAction = false;
                }
            }

            //CurrentHealth = (int)(1500f - TotalElapsed.ElapsedMilliseconds/100f);

            if (!IsPerformingAction)
            {
                float healthPercent = ((float)CurrentHealth / (float)MaxHealth) * 100f;

                for (int i = 0; i < MajorActions.Count; i++)
                {
                    //Console.WriteLine(MajorActions[i].StartHealth + " " + healthPercent);
                    if (MajorActions[i].StartHealth >= healthPercent)
                    {
                        if (MajorActions[i].HasBeenUsed == false || MajorActions[i].TimeSinceUsage.ElapsedMilliseconds > MajorActions[i].DelayTime * 1000)
                        {
                            Console.WriteLine("Crab health is at " + healthPercent + "% Health (" + CurrentHealth + "/" + MaxHealth + ").");

                            UseCrabAction(MajorActions[i]);
                            break;
                            
                        }
                    }
                }   
            }

            if (!IsPerformingAction)
                Console.WriteLine("The crab is not currently performing any actions!!");
        }
    }
}
