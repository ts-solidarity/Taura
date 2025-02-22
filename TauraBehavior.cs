using System.Collections.Generic;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.ObjectSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using TaleWorlds.CampaignSystem.Conversation.Tags;




namespace Taura
{
    public partial class SubModule
    {
        [DefaultView]
        public class TauraBehavior : CampaignBehaviorBase
        {
            ItemObject? _tauraItem;
            CharacterObject? _tauraCharacter;

            public override void RegisterEvents()
            {
                CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
                CampaignEvents.DailyTickTownEvent.AddNonSerializedListener(this, DailyTick);
                CampaignEvents.LocationCharactersAreReadyToSpawnEvent.AddNonSerializedListener(this, SpawnTauraCharacterInBreweries);
                CampaignEvents.LocationCharactersAreReadyToSpawnEvent.AddNonSerializedListener(this, new Action<Dictionary<string, int>>(SpawnTauraCharacterForTaverns));
                CampaignEvents.WorkshopOwnerChangedEvent.AddNonSerializedListener(this, OnWorkshopOwnerChanged);

            }

            private LocationCharacter CreateTauraCharacterTavern(CultureObject culture, LocationCharacter.CharacterRelations relation)
            {
                return CreateTauraCharacterTavern(culture, relation, _tauraCharacter);
            }

            private LocationCharacter CreateTauraCharacterTavern(CultureObject culture, LocationCharacter.CharacterRelations relation, CharacterObject? tauraCharacter)
            {
                int minAge = 20;
                int maxAge = 25;

                Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(tauraCharacter.Race, "_settlement");
                AgentData agentData = new AgentData(new SimpleAgentOrigin(tauraCharacter, -1, null, default(UniqueTroopDescriptor))).Monster(monsterWithSuffix).Age(MBRandom.RandomInt(minAge, maxAge));

                LocationCharacter tauraLocationCharacter = new LocationCharacter(agentData, new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors), "sp_tavern_wench", true, relation, ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, agentData.AgentIsFemale, "_barmaid"), true, false, null, false, false, true)
                {
                    PrefabNamesForBones =
                {
                    {
                        agentData.AgentMonster.OffHandItemBoneIndex,
                        "kitchen_pitcher_b_tavern"
                    }
                }
                };
                return tauraLocationCharacter;
            }


            private void SpawnTauraCharacterForTaverns(Dictionary<string, int> unusedUsablePointCount)
            {
                Settlement settlement = PlayerEncounter.LocationEncounter.Settlement;
                if (settlement.IsTown && CampaignMission.Current != null)
                {
                    Location location = CampaignMission.Current.Location;
                    if (location != null && location.StringId == "tavern")
                    {
                        // HOW MANY CHARACTERS TO CREATE
                        int characterCount = 2;

                        for (int i = 0; i < characterCount; i++)
                        {
                            location.AddLocationCharacters(new CreateLocationCharacterDelegate(CreateTauraCharacterTavern), settlement.Culture, LocationCharacter.CharacterRelations.Neutral, 1);
                        }
                        return;
                    }

                }
            }

            private void OnSessionLaunched(CampaignGameStarter starter)
            {
                _tauraItem = MBObjectManager.Instance.GetObject<ItemObject>("taura_beer");
                _tauraCharacter = MBObjectManager.Instance.GetObject<CharacterObject>("taura_brewer");
                AddDialogs(starter);
            }


            private void SpawnTauraCharacterInBreweries(Dictionary<string, int> unusedUsablePointCount)
            {
                Location locationWithId = Settlement.CurrentSettlement.LocationComplex.GetLocationWithId("center");
                if (!(CampaignMission.Current.Location == locationWithId && CampaignTime.Now.IsDayTime)) return;

                Settlement settlement = PlayerEncounter.LocationEncounter.Settlement;

                CharacterObject tauraCharacter = _tauraCharacter;
                Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(tauraCharacter.Race, "_settlement");

                // HOW MANY CHARACTERS TO CREATE?
                int characterCount = 1;

                // Adding TauraCharacter to every Brewery Workshop
                foreach (Workshop workshop in settlement.Town.Workshops)
                {
                    unusedUsablePointCount.TryGetValue(workshop.Tag, out int num);
                    float num2 = (float)num * 0.33f;

                    bool isBrewery = workshop.WorkshopType.StringId == "brewery";

                    if (num2 > 0f && (isBrewery))
                    {
                        for (int num3 = 0; num3 < characterCount; num3++)
                        {
                            AgentData agentData = new AgentData(new SimpleAgentOrigin(tauraCharacter)).Monster(monsterWithSuffix).Age(MBRandom.RandomInt(20, 30));

                            LocationCharacter locationCharacter = new LocationCharacter
                                (
                                agentData,
                                new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors),
                                workshop.Tag, true, LocationCharacter.CharacterRelations.Friendly,
                                null, // Action Set Code                                
                                true, false, null, false, false, true
                                );

                            locationWithId.AddCharacter(locationCharacter);
                        }
                    }
                }
            }


            //private void AddCharacterToPlace(CharacterObject characterObj, string locationId, int count)
            //{
            //    Location locationWithId = Settlement.CurrentSettlement.LocationComplex.GetLocationWithId(locationId);
            //    CultureObject culture = Hero.MainHero.CurrentSettlement.Culture;
            //    LocationCharacter.CharacterRelations relation = LocationCharacter.CharacterRelations.Friendly;
            //    Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(characterObj.Race, "_settlement");
            //    AgentData agentData = new AgentData(new SimpleAgentOrigin(characterObj)).Monster(monsterWithSuffix).Age(MBRandom.RandomInt(20, 30));
            //    LocationCharacter locationCharacter = new LocationCharacter
            //        (
            //        agentData,
            //        new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddFixedCharacterBehaviors),

            //        null, // workshop.Tag

            //        true,
            //        relation,
            //        null, // Action Set Code                                
            //        true, false, null, false, false, true
            //        );


            //    for (int i = 0; i < count; i++)
            //    {
            //        locationWithId.AddCharacter(locationCharacter);
            //    }

            //}



            private void AddDialogs(CampaignGameStarter starter)
            {

                // Exit dialogs
                {
                    starter.AddDialogLine("tavernkeeper_enddialog_", "tavernkeeper_enddialog", "tavernkeeper_talk", "Allright then!", null, null);

                }

                // Taverner Ask About Brewery
                {
                    starter.AddPlayerLine("tavernkeeper_talk_ask_taura_beer", "tavernkeeper_talk", "tavernkeeper_taura_beer", "Does this town has any breweries?", null, null);
                    starter.AddDialogLine("tavernkeeper_talk_taura_beer_true", "tavernkeeper_taura_beer", "tavernkeeper_talk", "Damn right we have. It wouldn't be bearable to live in this mess otherwise.", () =>
                    {

                        foreach (var workshop in Settlement.CurrentSettlement.Town.Workshops)
                        {
                            if (workshop.WorkshopType.StringId == "brewery") return true;
                        }

                        return false;

                    }, null);
                    starter.AddDialogLine("tavernkeeper_talk_taura_beer_false", "tavernkeeper_taura_beer", "tavernkeeper_taurabeer_quest", "Unfortunately we don't. It is a shame for business.", null, null);
                }

                // Taverner Heads or Tails
                {

                    int betAmount = 0;
                    bool choiceIsHeads = false;
                    bool resultIsHeads = false;

                    bool hasWon = false;

                    starter.AddPlayerLine("tavernkeeper_headsortails_start", "tavernkeeper_talk", "tavernkeeper_headsortails", "I want to play heads or tails.", null, null);
                    starter.AddDialogLine("tavernkeeper_headsortails_howmuch", "tavernkeeper_headsortails", "tavernkeeper_headsortails_amount", "Sure. How much you want to bet?", null, null);
                    starter.AddPlayerLine("tavernkeeper_headsortails_bet", "tavernkeeper_headsortails_amount", "tavernkeeper_headsortails_gamble", "{GOLD_ICON}200", null, () =>
                    {
                        GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, 200);
                        betAmount = 200;
                    }, 100, (out TextObject explanation) =>
                    {
                        bool hasEnough = Hero.MainHero.Gold >= 200;
                        explanation = new TextObject("You don't have enough {GOLD_ICON}", null);
                        if (hasEnough) explanation = new TextObject("{GOLD_ICON}200", null);
                        return hasEnough;
                    }, 
                    null);
                    starter.AddPlayerLine("tavernkeeper_headsortails_bet", "tavernkeeper_headsortails_amount", "tavernkeeper_headsortails_gamble", "{GOLD_ICON}400", null, () =>
                    {
                        GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, 400);
                        betAmount = 400;
                    }, 100, (out TextObject explanation) =>
                    {
                        bool hasEnough = Hero.MainHero.Gold >= 400;
                        explanation = new TextObject("You don't have enough {GOLD_ICON}", null);
                        if (hasEnough) explanation = new TextObject("{GOLD_ICON}400", null);
                        return hasEnough;
                    },null);
                    starter.AddPlayerLine("tavernkeeper_headsortails_bet", "tavernkeeper_headsortails_amount", "tavernkeeper_headsortails_gamble", "{GOLD_ICON}600", null, () =>
                    {
                        GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, 600);
                        betAmount = 600;
                    }, 100, (out TextObject explanation) =>
                    {
                        bool hasEnough = Hero.MainHero.Gold >= 600;
                        explanation = new TextObject("You don't have enough {GOLD_ICON}", null);
                        if (hasEnough) explanation = new TextObject("{GOLD_ICON}600", null);
                        return hasEnough;
                    }, null);
                    starter.AddPlayerLine("tavernkeeper_headsortails_bet", "tavernkeeper_headsortails_amount", "tavernkeeper_enddialog", "Fuck, I forgot my wallet at home.", null, null);

                    starter.AddDialogLine("tavernkeeper_headsortails_askchoice", "tavernkeeper_headsortails_gamble", "tavernkeeper_headsortails_gamblechoice", "Okay! Heads or tails?", null, null);
                    starter.AddPlayerLine("tavernkeeper_headsortails_makechoice", "tavernkeeper_headsortails_gamblechoice", "tavernkeeper_headsortails_gamblewaiting", "Heads.", null,
                        
                        () =>
                        {
                            choiceIsHeads = true;
                        }
                        
                        );

                    starter.AddPlayerLine("tavernkeeper_headsortails_makechoice", "tavernkeeper_headsortails_gamblechoice", "tavernkeeper_headsortails_gamblewaiting", "Tails.", null,

                        () =>
                        {
                            choiceIsHeads = false;
                        }

                        );




                    starter.AddDialogLine("tavernkeeper_headsortails_beforegamble", "tavernkeeper_headsortails_gamblewaiting", "tavernkeeper_headsortails_gambleresult", "Okay, let's see! Get ready... *Flips a coin*", null, () =>
                    {
                        int randomValue = MBRandom.RandomInt(1, 100);
                        if (randomValue <= 50)
                        {
                            resultIsHeads = true;
                        }
                        else
                        {
                            resultIsHeads = false;
                        }

                        hasWon = choiceIsHeads == resultIsHeads;

                    });
                    
                    starter.AddDialogLine("tavernkeeper_headsortails_aftergamble", "tavernkeeper_headsortails_gambleresult", "tavernkeeper_headsortails_end", "Looks like it's heads.", 
                        
                        
                        () =>
                        {
                            return resultIsHeads;
                        }, null);

                    starter.AddDialogLine("tavernkeeper_headsortails_aftergamble", "tavernkeeper_headsortails_gambleresult", "tavernkeeper_headsortails_end", "Looks like it's tails.",


                        () =>
                        {
                            return !resultIsHeads;
                        }, null);

                    starter.AddDialogLine("tavernkeeper_headsortails_enddialog", "tavernkeeper_headsortails_end", "tavernkeeper_talk", "You won! Congrats!", () => { return hasWon; }, () =>
                    {
                        if (hasWon)
                        {
                            GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, betAmount * 2);
                        }
                    });
                    starter.AddDialogLine("tavernkeeper_headsortails_enddialog", "tavernkeeper_headsortails_end", "tavernkeeper_talk", "Ahh.. Not so lucky huh!", () => { return !hasWon; }, () =>
                    {
                        if (hasWon)
                        {
                            GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, betAmount * 2);
                        }
                    });

                }

                // TauraCharacter Dialogs
                {

                    starter.AddDialogLine("taurachar_taurabeer_starter", "start", "taurachar_taurabeer_answer", "Hey there. You want any Taura Beers?", () =>
                    {
                        return (CharacterObject.OneToOneConversationCharacter == _tauraCharacter);
                    }, null);
                    
                    
                    starter.AddPlayerLine("taurachar_taurabeer_player_accepted", "taurachar_taurabeer_answer", "taurachar_taurabeer_accepted", "Yeah, sure. (200 denars)", 
                        null,
                        // Consequence Delegate
                        () => 
                        {
                            GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, 200, false);
                            MobileParty.MainParty.ItemRoster.AddToCounts(_tauraItem, 1);

                            MBList<Agent> nearbyAgents = Mission.Current.GetNearbyAgents(Agent.Main.Position.AsVec2, 10f, new MBList<Agent>());

                            foreach (var agent in nearbyAgents)
                            {

                                if (agent.Character.Id == _tauraCharacter.Id)
                                {
                                    agent.SetActionChannel(
                                        1,          // channelNo
                                        ActionIndexCache.Create("act_lean_from_fence"),
                                        true,      // ignorePriority
                                        0UL,        // additionalFlags
                                        0f,         // blendWithNextActionFactor
                                        0.9f,         // actionSpeed
                                        -0.2f,         // blendInPeriod
                                        0.4f,       // blendOutPeriodToNoAnim
                                        0f,       // startProgress
                                        true,       // useLinearSmoothing
                                        -0.2f,      // blendOutPeriod
                                        0,          // actionShift
                                        true        // forceFaceMorphRestart
                                        );
                                }
                            }




                        }, 100,
                        // Clickable Delegate
                        (out TextObject? explanation) =>
                        {
                        explanation = new TextObject("{=!}{TAURA_BEER_COST}{GOLD_ICON}.", null);
                        MBTextManager.SetTextVariable("TAURA_BEER_COST", 200);
                        if (Hero.MainHero.Gold < 200)
                        {
                            explanation = new TextObject("{=xVZVYNan}You don't have enough{GOLD_ICON}.", null);
                            return false;
                        }
                        return true;
                    }, null);

                    starter.AddPlayerLine("taurachar_taurabeer_player_refused", "taurachar_taurabeer_answer", "taurachar_taurabeer_rejected", "Nah, I'm fine.", null, null);
                    starter.AddDialogLine("taurachar_taurabeer_negative_ender", "taurachar_taurabeer_rejected", "end", "Your loss baby!", null, null);

                    starter.AddDialogLine("taurachar_taurabeer_positive_ender", "taurachar_taurabeer_accepted", "end", "See you next time, *muah*!", null, null);

                }



                // Towns Woman as Companion
                {


                    starter.AddPlayerLine("taura_female_interaction", "town_or_village_player", "taura_female_interaction_answer", "I want you to come to my room tonight.", () =>
                    {
                        return (CharacterObject.OneToOneConversationCharacter.IsFemale && Hero.MainHero.CurrentSettlement.OwnerClan == Hero.MainHero.Clan);
                    }, null);

                    starter.AddDialogLine("taura_female_interaction", "taura_female_interaction_answer", "taura_female_interaction_married", "But... But I'm married, your lordship...",
                    
                        () =>
                        {
                            int randomNumber = MBRandom.RandomInt(0, 100);
                            if (randomNumber >= 50) return true;
                            return false;
                        }
                        
                        , null
                    );

                    starter.AddPlayerLine("taura_female_interaction", "taura_female_interaction_married", "taura_female_interaction_compelled", "I am sure your husband will be happy when he hears you are a maid at my court.", null, null);
                    starter.AddPlayerLine("taura_female_interaction", "taura_female_interaction_married", "close_window", "Then forget about it.", null, null);

                    starter.AddDialogLine("taura_female_interaction", "taura_female_interaction_compelled", "close_window", "If you say so, my lord.", null,
                        
                        
                        () =>
                        {
                            CharacterObject charObj = CharacterObject.OneToOneConversationCharacter;

                            //for (int i = 0; i < 100; i++)
                            //{
                            //    AddCharacterToPlace(charObj, "center", 100);
                            //}


                        }
                        
                        
                        );

                    starter.AddDialogLine("taura_female_interaction", "taura_female_interaction_answer", "close_window", "With pleasure, my lord.",

                            null, null
                    );
                }


            }



            private void DailyTick(Town town)
            {
                foreach (var workshop in town.Workshops)
                {
                    if (workshop.WorkshopType.StringId == "brewery")
                    {
                        workshop.ChangeGold(-TaleWorlds.Library.MathF.Round(workshop.Expense * 0.15f));
                    }
                }
            }

            private void OnWorkshopOwnerChanged(Workshop workshop, Hero hero)
            {
            }

            public override void SyncData(IDataStore dataStore)
            {
            }
        }


    }
}
