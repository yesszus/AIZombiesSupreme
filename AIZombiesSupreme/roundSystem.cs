﻿using System;
using InfinityScript;
using static InfinityScript.GSCFunctions;

namespace AIZombiesSupreme
{
    public class roundSystem : BaseScript
    {
        public static Action onRoundChange;

        public static uint Wave = 0;
        private static uint bossCount = 1;
        private static uint crawlerCount = 25;
        public static uint totalWaves = 30;

        public static bool isCrawlerWave = false;
        public static bool isBossWave = false;

        //private readonly static Entity level = Entity.GetEntity(2046);

        public static void startNextRound()
        {
            checkForEndGame();//Before we start, make sure there are players to start
            Wave++;

            //MakeDvarServerInfo("sv_privateClientsForClients", Wave);//Set round number to private client display

            //foreach (Entity players in Players)
            //if (AIZ.isPlayer(players) && players.HasField("isDown")) players.SetField("currentRound", Wave);
            //level.Notify("round_changed");
            botUtil.spawnedBots = 0;
            isCrawlerWave = Wave % 5 == 0 && !isBossWave;
            isBossWave = Wave % 10 == 0;
            if (isBossWave)
            {
                botUtil.botsForWave = bossCount;
                if (bossCount == 1) bossCount = 5;
                else bossCount += 5;
                foreach (Entity players in Players) if (AIZ.isPlayer(players) && players.HasField("isDown") && !players.GetField<bool>("isDown")) players.VisionSetNakedForPlayer(AIZ.bossVision);
            }
            else if (isCrawlerWave)
            {
                //e_hud.stringsCleared = false;
                botUtil.botsForWave = crawlerCount;
                crawlerCount += 25;
                if (Wave != 5) botUtil.crawlerHealth += 250;
            }
            else//isNormalWave
            {
                botUtil.botsForWave = 10 * Wave;
                int hellMapScalar = 1;
                if (AIZ.isHellMap)//Multiplicated scalar for hell maps
                    hellMapScalar = 5;

                if (Wave != 1) botUtil.health += botUtil.healthScalar * hellMapScalar;
            }

            if (!isBossWave) hud.stringsCleared = false;
            
            checkForHellMapVision();
            OnInterval(500, botUtil.startBotSpawn);
            onRoundChange();
            AIZ.zState = "ingame";
            foreach (Entity players in Players)
            {
                if (AIZ.isPlayer(players) && players.HasField("isDown"))
                {
                    players.PlayLocalSound("mp_bonus_end");
                    int randomStart = AIZ.rng.Next(8);
                    string sound = "";
                    switch (randomStart)
                    {
                        case 0:
                            sound = "US_1mc_fightback";
                            break;
                        case 1:
                            sound = "US_1mc_goodtogo";
                            break;
                        case 2:
                            sound = "US_1mc_holddown";
                            break;
                        case 3:
                            sound = "US_1mc_keepfighting";
                            break;
                        case 4:
                            sound = "US_1mc_pushforward";
                            break;
                        case 5:
                            sound = "US_1mc_readytomove";
                            break;
                        case 6:
                            sound = "US_1mc_positions_lock";
                            break;
                        case 7:
                            sound = "US_1mc_security_complete";
                            break;
                    }
                    players.PlayLocalSound(sound);
                }
            }
        }

        public static void checkForEndRound()
        {
            if (botUtil.botsInPlay.Count == 0 && botUtil.botsForWave == botUtil.spawnedBots)
            {
                if (Wave == totalWaves)
                {
                    AIZ.zState = "ended";
                    StartAsync(hud.endGame(true));
                    return;
                }
                //g_AIZ.zState = "intermission";
                AfterDelay(100, () => AIZ.startIntermission());
                foreach (Entity players in Players)
                {
                    if (AIZ.isPlayer(players) && players.HasField("isDown") && !players.GetField<bool>("isDown") && (!AIZ.isHellMap || (AIZ.isHellMap && killstreaks.visionRestored)))
                    {
                        if (((AIZ._mapname == "mp_bootleg" || AIZ._mapname == "mp_courtyard_ss") && !killstreaks.mapStreakOut))
                            players.VisionSetNakedForPlayer(AIZ.vision);
                        else if (AIZ._mapname != "mp_bootleg")
                            players.VisionSetNakedForPlayer(AIZ.vision);
                    }
                    if (AIZ.isPlayer(players))
                    {
                        players.PlayLocalSound("mp_bonus_start");
                        players.PlayLocalSound("US_1mc_encourage_win");
                    }
                    if (isCrawlerWave || isBossWave) AIZ.giveMaxAmmo(players);
                }
            }
            /*
            if (!hud.stringsCleared && !isBossWave && Wave > 4 && botUtil.botsInPlay.Count > 1
                  && hud.currentUnlocalizedConfigStrings > hud.maxUnlocalizedConfigStrings)
            {
                Utilities.PrintToConsole("Clearing strings due to comfortable limit: " + hud.currentUnlocalizedConfigStrings);
                hud.clearAllGameStrings();
            }
            */
            checkForCompass();
        }

        private static void checkForHellMapVision()
        {
            if (AIZ.isHellMap)
            {
                foreach (Entity player in Players)
                {
                    if (AIZ.isPlayer(player) && player.HasField("isDown") && !player.GetField<bool>("isDown") && !killstreaks.visionRestored)
                    {
                        player.VisionSetNakedForPlayer(AIZ.hellVision);
                    }
                }
            }
        }

        public static void checkForCompass()
        {
            int veh = GetNumVehicles();
            if (((botUtil.botsInPlay.Count < 11 && veh == 0) || (botUtil.botsInPlay.Count < 6 && veh > 0)) && botUtil.botsForWave - botUtil.spawnedBots == 0)
            {
                foreach (Entity bot in botUtil.botsInPlay)
                {
                    if (bot.GetField<bool>("isOnCompass") || !bot.GetField<bool>("isAlive")) continue;
                    int curObjID = 31 - mapEdit.getNextObjID();
                    bot.SetField("isOnCompass", true);
                    Objective_Add(curObjID, "active", bot.Origin, "compassping_enemy");
                    //Objective_Icon(curObjID, "compassping_enemy");
                    Objective_Team(curObjID, "allies");
                    Objective_OnEntity(curObjID, bot);
                    mapEdit.addObjID(bot, curObjID);
                }

                if (!hud.stringsCleared && !isBossWave && Wave > 4 && botUtil.botsInPlay.Count > 1
                    && hud.currentUnlocalizedConfigStrings > hud.maxUnlocalizedConfigStrings)
                {
                    //Utilities.PrintToConsole("Clearing strings due to comfortable limit: " + hud.currentUnlocalizedConfigStrings);
                    hud.clearAllGameStrings();
                }
            }
        }

        public static void checkForEndGame()
        {
            int playersAlive = GetTeamPlayersAlive("allies");
            if (playersAlive == 1 && AIZ.zState == "ingame")
            {
                foreach (Entity player in Players)
                {
                    if (player.IsAlive && player.HasField("isDown"))
                    {
                        AfterDelay(5000, () => player.PlayLocalSound("US_1mc_lastalive"));
                        break;
                    }
                }
            }
            else if (playersAlive == 0)
                StartAsync(hud.endGame(false));
        }
    }
}
