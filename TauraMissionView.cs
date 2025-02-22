using System;
using System.Collections.Generic;
using SandBox.Missions.AgentBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.ObjectSystem;


namespace Taura
{
    public partial class SubModule
    {
        [DefaultView]
        public class TauraMissionView : MissionView
        {
            public override void OnMissionScreenTick(float dt)
            {
                base.OnMissionScreenTick(dt);



                // Drink Taura Beer
                if (Input.IsKeyPressed(TaleWorlds.InputSystem.InputKey.Q))
                {
                    DrinkBeer();
                }



                // Respect Action
                {
                    RespectAction();
                }


                // Run when player press V
                {
                 
                    int energy = 100000;
                    float maxSpeedLimit = -1;

                    if (maxSpeedLimit == -1)
                    {
                        maxSpeedLimit = Agent.Main.GetMaximumSpeedLimit();
                    }

                    if (Input.IsKeyPressed(TaleWorlds.InputSystem.InputKey.V) && energy > 10)
                    {
                        energy -= 3;
                        Agent.Main.SetMaximumSpeedLimit(maxSpeedLimit * 100, false);

                        Agent.Main.SetCurrentActionSpeed(1, maxSpeedLimit * 500);
                    }

                    if (!Input.IsKeyPressed(TaleWorlds.InputSystem.InputKey.V))
                    {
                        Agent.Main.SetMaximumSpeedLimit(maxSpeedLimit, false);
                        energy++;
                    }

                }

            }



            private void RespectAction()
            {
                bool flag1 = Hero.MainHero.CurrentSettlement.OwnerClan == Hero.MainHero.Clan;
                bool flag2 = false;

                if (Hero.MainHero.CurrentSettlement.OwnerClan.Kingdom == null)
                {
                    flag2 = false;
                }

                else
                {
                    flag2 = Hero.MainHero.CurrentSettlement.OwnerClan.Kingdom.RulingClan == Hero.MainHero.Clan;
                }


                if (flag1 || flag2)
                {
                    ShowRespect();
                }
            }

            private void ShowRespect()
            {

                MBList<Agent> nearbyAgents = Mission.Current.GetNearbyAgents(Agent.Main.Position.AsVec2, 10f, new MBList<Agent>());

                if (nearbyAgents.IsEmpty())
                {
                    return;
                }

                foreach (Agent agent in nearbyAgents)
                {

                    if (agent.IsMainAgent || AgentKneeledDown(agent))
                    {
                        continue;
                    }

                    KneelDown(agent);
                }
            }

            List<Agent> kneeledDownAgents = new();

            private void KneelDown(Agent agent)
            {
                agent.SetActionChannel(
                1,          // channelNo
                ActionIndexCache.Create("act_main_story_conspirator_kneel_down_3"),
                false,       // ignorePriority
                0UL,        // additionalFlags
                0f,         // blendWithNextActionFactor
                1f,         // actionSpeed
                2f,         // blendInPeriod
                0.4f,       // blendOutPeriodToNoAnim
                0.5f,       // startProgress
                true,       // useLinearSmoothing
                -0.2f,      // blendOutPeriod
                0,          // actionShift
                true        // forceFaceMorphRestart
                );

                LookAtPlayer(agent);
                kneeledDownAgents.Add(agent);
            }

            private void LookAtPlayer(Agent agent)
            {
                agent.SetLookAgent(Agent.Main);
            }

            private bool AgentKneeledDown(Agent agent)
            {

                if (kneeledDownAgents == null)
                {
                    return false;
                }

                foreach (var kneeledDownAgent in kneeledDownAgents)
                {
                    if (agent == kneeledDownAgent)
                    {
                        return true;
                    }
                }

                return false;
            }

            private void DrinkBeer()
            {

                if (!(Mission.Mode is MissionMode.Battle or MissionMode.Duel or MissionMode.Tournament or MissionMode.Stealth))
                {
                    InformationManager.DisplayMessage(new InformationMessage("You are not in danger, you can't heal yourself!"));
                    return;
                }

                var ma = Mission.MainAgent;                                                         // Main character
                var itemRoster = MobileParty.MainParty.ItemRoster;                                  // You can think of this like item inventory
                var tauraBeerObject = MBObjectManager.Instance.GetObject<ItemObject>("taura_beer"); // Taura beer object


                // Check whether you have taura beer
                var dontHave = itemRoster.GetItemNumber(tauraBeerObject) <= 0;

                // If you don't have any, don't do anything
                if (dontHave)
                {
                    InformationManager.DisplayMessage(new InformationMessage("You don't have any taura beers!"));
                    return;
                }

                // Check whether you are at max health
                var atMaxHealth = ma.Health >= ma.HealthLimit;

                // If you are, don't do anything
                if (atMaxHealth)
                {
                    InformationManager.DisplayMessage(new InformationMessage("You are already at maximum health!"));
                    return;
                }

                // If you have taura beers and you are not at maximum health;

                // Remove one taura beer
                itemRoster.AddToCounts(tauraBeerObject, -1);

                // Increase the main character's hp by 20 or to the max health if adding 20 is too much

                if (ma.Health < ma.HealthLimit)
                {
                    if (ma.Health + 20 >= ma.HealthLimit)
                    {
                        ma.Health = ma.HealthLimit;
                    }

                    ma.Health += 20;
                    InformationManager.DisplayMessage(new InformationMessage(String.Format("Health increased! Current health: {0}", Mission.MainAgent.Health)));
                    return;
                }
            }
        }
    }
}