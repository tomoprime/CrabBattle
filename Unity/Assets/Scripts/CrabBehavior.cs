
//This file is totally a hacked up / modified version of the server file so that it runs on the client.

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
        public float UsageTime;
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
            UsageTime = 0f;
        }
    }

    class CrabBehavior
    {
        public List<Action> MajorActions;
        public List<Action> MovementActions;

        public int CurrentHealth;
        public int MaxHealth;

        public Random random;
        float CurrentAction = 0;
        Stopwatch TargetChange;
        float ActionLength;
        bool IsPerformingAction;

        NetworkManager netman;

        public bool Direction;

        public int CurrentTarget;

        Stopwatch TotalElapsed;

        float changetick;

        float lastBeat = 0f;

        public CrabBehavior()
        {
            random = new Random();

            MajorActions = new List<Action>();
            MovementActions = new List<Action>();

            netman = NetworkManager.GetInstance();

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


            MaxHealth = 800 + 700 * netman.Players.Count * netman.healthmod;
            CurrentHealth = MaxHealth;


            TotalElapsed = new Stopwatch();
            TotalElapsed.Start();
            
            TargetChange = new Stopwatch();
            TargetChange.Start();
            changetick = 10000;

            CurrentTarget = -1;
        }

        public void UseCrabAction(Action crabAction, float time)
        {
            ActionLength = crabAction.BaseLength;

            float actionMulti = 1 - 0.5f * (1f - (float)CurrentHealth / (float)MaxHealth);
            if (actionMulti < 0.65f)
                actionMulti = 0.65f;

            if (crabAction.CanAdjustSpeed)
                ActionLength *= actionMulti;

            int seed = random.Next(0,10000);

            netman.EnemyManager.CrabCommand((int)crabAction.Id, 1 / actionMulti, seed);

            crabAction.HasBeenUsed = true;
            crabAction.UsageTime = time;
            CurrentAction = time;

            IsPerformingAction = true;
        }

        public void GoGoBattleCrab(float time, float deltaTime)
        {
            if (CurrentTarget == -1)
                CurrentTarget = netman.Players[0].Id;

            lastBeat += deltaTime;

            if (lastBeat > 1f)
            {
                //Chance to change target
                if (random.Next(0, 10) > 8)
                {
                    Direction = !Direction;
                }

                //TargetId, x, y, playerx, playery, direction, triptime.
                netman.EnemyManager.CrabMoveSync(netman.Enemy.transform.position.x, netman.Enemy.transform.position.z, netman.Player.Obj.transform.position.x, netman.Player.Obj.transform.position.z, Direction, 0f);

                lastBeat = 0f;
            }

            if (TargetChange.ElapsedMilliseconds > changetick)
            {
                int rnd = random.Next(0, 4);
                if (rnd == 0)
                {
                    changetick = 10000;
                    TargetChange.Reset();
                    TargetChange.Start();
                    int prnd = random.Next(0, netman.Players.Count);
                    CurrentTarget = netman.Players[prnd].Id;
                    Console.WriteLine("Crab is changing targets to a new player.");
                }
                else
                    changetick += 1000;
            }

            if (IsPerformingAction)
            {
                if (time - CurrentAction > ActionLength)
                {
                    IsPerformingAction = false;
                }
            }

            //CurrentHealth = (int)(1500f - TotalElapsed.ElapsedMilliseconds/100f);

            if (!IsPerformingAction)
            {
                CurrentHealth = netman.EnemyManager.CurrentHealth;
                MaxHealth = netman.EnemyManager.MaxHealth;

                float healthPercent = ((float)CurrentHealth / (float)MaxHealth) * 100f;

                for (int i = 0; i < MajorActions.Count; i++)
                {
                    //Console.WriteLine(MajorActions[i].StartHealth + " " + healthPercent);
                    if (MajorActions[i].StartHealth >= healthPercent)
                    {
                        if (MajorActions[i].HasBeenUsed == false || time - MajorActions[i].UsageTime > MajorActions[i].DelayTime)
                        {
                            Console.WriteLine("Crab health is at " + healthPercent + "% Health (" + CurrentHealth + "/" + MaxHealth + ").");

                            UseCrabAction(MajorActions[i], time);
                            break;

                        }
                    }
                }
            }
        }
    }
}
