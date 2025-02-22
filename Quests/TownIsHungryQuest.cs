using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Helpers;
using SandBox.ViewModelCollection.Nameplate.NameplateNotifications.SettlementNotificationTypes;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;


namespace Taura.Quests.TownIsHungryQuest
{
    public class GlobalClass
    {
        // If you set foodAmount manually to something, you should also change overrideFoodAmount to false
        public static int foodAmount = CalculateFoodAmount(null);

        // Change this to true, if you want to set foodAmount manually
        public static bool overrideFoodAmount = true;

        // After accepting issue as a quest, you have this amount of time
        public static float questTime = 7f;

        // Issue will be available for this period of time
        public static float issueTime = 10f;

        // How frequent the issue should emerge
        public static IssueBase.IssueFrequency issueFrequency = IssueBase.IssueFrequency.VeryCommon;

        // All task strings in one place
        public class Texts
        {
            // Issue Texts
            public static string issueTitle = "Town is Hungry";
            public static string issueDescription = "{QUEST_GIVER} in {QUEST_SETTLEMENT} asked you to bring {FOOD_AMOUNT} piles of food to the town.";
            public static string issueBriefByIssueGiver = "We are in a dire situation. People consumed their last pieces of food. Some even tried to eat fucking grasshopers. If we can't bring food anytime soon, I'm afraid people might even eat each other.";
            public static string issueAcceptByPlayer = "Hell. It sounds harsh.";
            public static string issueQuestSolutionExplanationByIssueGiver = "It is. Unless someone helps us. If you could bring us {FOOD_AMOUNT} piles of food here, we would be beyond grateful. You'd save our children and elderly. We can't give you much, but you'd earn our deepest gratitude.";
            public static string issueQuestSolutionAcceptByPlayer = "Alright, I can handle it. Hold on tight, until I save you.";
            public static string issueAsRumorInSettlement = "I AM SO... FUCKING... HUNGRY... I can eat a horse. Huh? Sorry 'bout that.";

            // Quest Texts
            public static string questTitle = "{QUEST_SETTLEMENT} is Starving";
            public static string stageOnePlayerAcceptsQuestLogText = "{QUEST_GIVER.LINK}, a notable in the town of {QUEST_SETTLEMENT} asked you to bring {FOOD_AMOUNT} piles of food to save the city from starving. You won't get anything in return.";
            public static string stageTwoPlayerHasIssueItemLogText = "You have enough food to complete the quest. Return to {QUEST_SETTLEMENT} to hand them over.";
            public static string stageTimeoutLogText = "You have failed to return {FOOD_AMOUNT} piles of food to {QUEST_GIVER.LINK}. Rumor says the city is doomed and people have resorted to cannibalism.";
            public static string stageSuccessLogText = "You have kept your promise and brought {FOOD_AMOUNT} piles of food to {QUEST_GIVER.LINK}. The city and the notables are grateful to you.";
            public static string stageCancelDueToWarLogText = "Your clan is now at war with the {QUEST_GIVER.LINK}'s lord. Your agreement with {ISSUE_GIVER.LINK} was canceled.";

            public static string initialThankYou = "Thank you. You are our last hope. God bless you.";
            public static string finalThankYou = "You have saved us, the people, and the children. You truly are a hero. God bless you on your way in life.";
            public static string waitingText = "Okay, but I don't know how much we can hold on. Please hurry.";

            public static string questDiscuss = "Have you brought us {FOOD_AMOUNT} piles of food?";
            public static string playerOption1 = "Yes, here they are. I hope it helps a bit.";
            public static string playerOption2 = "I am still stocking food.";

            public static string explanation = "You don't have enough food.";
            public static string directive = "Get {FOOD_AMOUNT} piles of food";
            public static string positiveNotification = "You have enough food to complete the quest. Return to {QUEST_SETTLEMENT} to hand them over.";
            public static string successConsequence = "You have successfuly brought {FOOD_AMOUNT} piles of food to the city.";
        }

        // Dynamically calculates needed food amount the player needs to bring to the issue giver
        public static int CalculateFoodAmount(Hero issueGiver)
        {
            int foodAmount = 0;
            
            int baseAmount = 300;
            int prosperity = 2000;

            if (issueGiver != null)
            {
                prosperity = (int)issueGiver.CurrentSettlement.Town.Prosperity;
            }


            int investmentBasedOnProspertiy = prosperity / 10;

            foodAmount = baseAmount + investmentBasedOnProspertiy;


            return foodAmount;
        }

        public static void SetTextVariables(TextObject textObject, Hero issueGiver)
        {

            if (issueGiver == null) return;

            textObject.SetTextVariable("QUEST_GIVER", issueGiver.Name);
            textObject.SetTextVariable("QUEST_SETTLEMENT", issueGiver.CurrentSettlement.Name);
            textObject.SetTextVariable("FOOD_AMOUNT", foodAmount);


            StringHelpers.SetCharacterProperties("QUEST_GIVER", issueGiver.CharacterObject, textObject);
            StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject);
        }
    }

    public partial class SubModule
    {
        [DefaultView]
        public class TaskBehavior : CampaignBehaviorBase
        {

            // Register this event to check for issue event
            public override void RegisterEvents()
            {
                CampaignEvents.OnCheckForIssueEvent.AddNonSerializedListener(this, new Action<Hero>(OnCheckForIssue));
            }

            // Unused SyncData method
            public override void SyncData(IDataStore dataStore)
            {
            }

            // Necessary conditions
            private bool ConditionsHold(Hero issueGiver)
            {
                return issueGiver.IsNotable && issueGiver.CurrentSettlement != null && issueGiver.CurrentSettlement.IsTown && issueGiver.CurrentSettlement.IsStarving;
            }

            // If the conditions hold, start this issue. Otherwise? Just add it as a possible issue.
            private void OnCheckForIssue(Hero hero)
            {
                if (ConditionsHold(hero))
                {
                    // Start the issue
                    Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(
                        new PotentialIssueData.StartIssueDelegate(OnStartIssue),
                        typeof(TaskIssue),
                        GlobalClass.issueFrequency, null));

                }

                // Add it as a possible issue
                Campaign.Current.IssueManager.AddPotentialIssueData(
                    hero,
                    new PotentialIssueData(typeof(TaskIssue),
                    GlobalClass.issueFrequency));

                return;
            }

            private IssueBase OnStartIssue(in PotentialIssueData pid, Hero issueOwner)
            {
                PotentialIssueData potentialIssueData = pid;
                return new TaskIssue(issueOwner);
            }


            internal class TaskIssue : IssueBase
            {

                public TaskIssue(Hero issueOwner) : base(issueOwner, CampaignTime.DaysFromNow(GlobalClass.issueTime))
                {
                    if (GlobalClass.overrideFoodAmount)
                    {
                        GlobalClass.foodAmount = GlobalClass.CalculateFoodAmount(issueOwner);
                    }
                }

                public override TextObject Title
                {
                    get
                    {
                        TextObject textObject = new TextObject(GlobalClass.Texts.issueTitle, null);
                        GlobalClass.SetTextVariables(textObject, IssueOwner);
                        return textObject;
                    }
                }

                public override TextObject Description
                {
                    get
                    {
                        TextObject textObject = new TextObject(GlobalClass.Texts.issueDescription, null);
                        GlobalClass.SetTextVariables(textObject, IssueOwner);
                        return textObject;
                    }
                }

                public override TextObject IssueBriefByIssueGiver
                {
                    get
                    {
                        TextObject textObject = new TextObject(GlobalClass.Texts.issueBriefByIssueGiver, null);
                        GlobalClass.SetTextVariables(textObject, IssueOwner);
                        return textObject;
                    }
                }

                public override TextObject IssueAcceptByPlayer
                {
                    get
                    {
                        TextObject textObject = new TextObject(GlobalClass.Texts.issueAcceptByPlayer, null);
                        GlobalClass.SetTextVariables(textObject, IssueOwner);
                        return textObject;
                    }
                }

                public override TextObject IssueQuestSolutionExplanationByIssueGiver
                {
                    get
                    {
                        TextObject textObject = new TextObject(GlobalClass.Texts.issueQuestSolutionExplanationByIssueGiver, null);
                        GlobalClass.SetTextVariables(textObject, IssueOwner);
                        return textObject;
                    }
                }

                public override TextObject IssueQuestSolutionAcceptByPlayer
                {
                    get
                    {
                        TextObject textObject = new TextObject(GlobalClass.Texts.issueQuestSolutionAcceptByPlayer, null);
                        GlobalClass.SetTextVariables(textObject, IssueOwner);
                        return textObject;
                    }
                }

                public override TextObject IssueAsRumorInSettlement
                {
                    get
                    {
                        TextObject textObject = new TextObject(GlobalClass.Texts.issueAsRumorInSettlement, null);
                        GlobalClass.SetTextVariables(textObject, IssueOwner);
                        return textObject;
                    }
                }

                public override bool IsThereAlternativeSolution
                {
                    get
                    {
                        return false;
                    }
                }

                public override bool IsThereLordSolution
                {
                    get
                    {
                        return false;
                    }
                }

                public override IssueFrequency GetFrequency()
                {
                    return IssueFrequency.VeryCommon;
                }

                public override bool IssueStayAliveConditions()
                {
                    return IssueOwner.IsNotable && IssueOwner.CurrentSettlement != null;
                }

                protected override bool CanPlayerTakeQuestConditions(Hero issueGiver, out PreconditionFlags flag, out Hero? relationHero, out SkillObject? skill)
                {
                    relationHero = null;
                    flag = PreconditionFlags.None;

                    if (issueGiver.GetRelationWithPlayer() < -10f)
                    {
                        flag |= PreconditionFlags.Relation;
                        relationHero = issueGiver;
                    }
                    if (issueGiver.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
                    {
                        flag |= PreconditionFlags.AtWar;
                    }

                    skill = null;

                    return flag == PreconditionFlags.None;
                }


                protected override void CompleteIssueWithTimedOutConsequences()
                {
                }

                protected override QuestBase GenerateIssueQuest(string questId)
                {
                    return new TaskQuest(questId, IssueOwner, CampaignTime.DaysFromNow(GlobalClass.questTime), RewardGold, GlobalClass.foodAmount);
                }

                protected override void OnGameLoad()
                {
                }

                protected override void HourlyTick()
                {
                }

            }


            // QUEST
            public class TaskQuest : QuestBase
            {

                // Constructor with basic vars and any vars about the quest
                public TaskQuest(string questId, Hero questGiver, CampaignTime duration, int rewardGold, int foodAmount) : base(questId, questGiver, duration, rewardGold)
                {
                    SetDialogs();
                    InitializeQuestOnCreation();
                    FoodAmount = foodAmount;
                }

                public int GetFoodAmount()
                {
                    return FoodAmount;
                }


                // All of our text/logs
                public override TextObject Title
                {
                    get
                    {
                        TextObject textObject = new TextObject(GlobalClass.Texts.questTitle, null);
                        GlobalClass.SetTextVariables(textObject, QuestGiver);
                        textObject.SetTextVariable("QUEST_GIVER", QuestGiver.Name);
                        return textObject;
                    }
                }

                private TextObject StageOnePlayerAcceptsQuestLogText
                {
                    get
                    {
                        TextObject textObject = new TextObject(GlobalClass.Texts.stageOnePlayerAcceptsQuestLogText, null);
                        GlobalClass.SetTextVariables(textObject, QuestGiver);
                        return textObject;
                    }
                }

                private TextObject StageTwoPlayerHasEnoughFoodLogText
                {
                    get
                    {
                        TextObject textObject = new TextObject(GlobalClass.Texts.stageTwoPlayerHasIssueItemLogText, null);
                        GlobalClass.SetTextVariables(textObject, QuestGiver);
                        return textObject;
                    }
                }

                private TextObject StageTimeoutLogText
                {
                    get
                    {
                        TextObject textObject = new TextObject(GlobalClass.Texts.stageTimeoutLogText, null);
                        GlobalClass.SetTextVariables(textObject, QuestGiver);
                        return textObject;
                    }
                }

                private TextObject StageSuccessLogText
                {
                    get
                    {
                        TextObject textObject = new TextObject(GlobalClass.Texts.stageSuccessLogText, null);
                        GlobalClass.SetTextVariables(textObject, QuestGiver);
                        return textObject;
                    }
                }

                private TextObject StageCancelDueToWarLogText
                {
                    get
                    {
                        TextObject textObject = new TextObject(GlobalClass.Texts.stageCancelDueToWarLogText, null);
                        GlobalClass.SetTextVariables(textObject, QuestGiver);
                        return textObject;
                    }
                }



                // Register Events
                protected override void RegisterEvents()
                {
                    CampaignEvents.PlayerInventoryExchangeEvent.AddNonSerializedListener(this, new Action<List<ValueTuple<ItemRosterElement, int>>, List<ValueTuple<ItemRosterElement, int>>, bool>(OnPlayerInventoryExchange));
                    CampaignEvents.WarDeclared.AddNonSerializedListener(this, new Action<IFaction, IFaction, DeclareWarAction.DeclareWarDetail>(OnWarDeclared));
                    CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, new Action<Clan, Kingdom, Kingdom, ChangeKingdomAction.ChangeKingdomActionDetail, bool>(OnClanChangedKingdom));
                    CampaignEvents.MapEventStarted.AddNonSerializedListener(this, new Action<MapEvent, PartyBase, PartyBase>(OnMapEventStarted));
                }

                private void OnPlayerInventoryExchange(List<ValueTuple<ItemRosterElement, int>> purchasedItems, List<ValueTuple<ItemRosterElement, int>> soldItems, bool isTrading)
                {
                    PlayerAcceptedQuestLog.UpdateCurrentProgress(GetFoodAmountOnPlayer());
                    CheckIfPlayerReadyToReturnFood();
                }

                private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
                {
                    CheckWarDeclaration();
                }

                private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
                {
                    CheckWarDeclaration();
                }

                private void CheckWarDeclaration()
                {
                    if (QuestGiver.CurrentSettlement.OwnerClan.IsAtWarWith(Clan.PlayerClan))
                    {
                        CompleteQuestWithCancel(StageCancelDueToWarLogText);
                    }
                }

                private void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
                {
                    if (QuestHelper.CheckMinorMajorCoercion(this, mapEvent, attackerParty))
                    {
                        QuestHelper.ApplyGenericMinorMajorCoercionConsequences(this, mapEvent);
                    }
                }

                // Quest logic, the dialogs and conditions for it be to success or failure
                public override bool IsRemainingTimeHidden
                {
                    get
                    {
                        return false;
                    }
                }

                protected override void InitializeQuestOnGameLoad()
                {
                    SetDialogs();
                }

                protected override void SetDialogs()
                {
                    TextObject initialThankYouText = new TextObject(GlobalClass.Texts.initialThankYou, null);
                    TextObject finalThankYouText = new TextObject(GlobalClass.Texts.finalThankYou, null);
                    TextObject waitingText = new TextObject(GlobalClass.Texts.waitingText, null);
                    TextObject questDiscuss = new TextObject(GlobalClass.Texts.questDiscuss, null);

                    GlobalClass.SetTextVariables(initialThankYouText, QuestGiver);
                    GlobalClass.SetTextVariables(finalThankYouText, QuestGiver);
                    GlobalClass.SetTextVariables(waitingText, QuestGiver);
                    GlobalClass.SetTextVariables(questDiscuss, QuestGiver);

                    OfferDialogFlow = DialogFlow.CreateDialogFlow("issue_classic_quest_start", 100);
                    OfferDialogFlow.NpcLine(initialThankYouText, null, null)
                        .Condition(() => CharacterObject.OneToOneConversationCharacter == QuestGiver.CharacterObject)
                        .Consequence(new ConversationSentence.OnConsequenceDelegate(QuestAcceptedConsequences))
                        .CloseDialog();

                    DiscussDialogFlow = DialogFlow.CreateDialogFlow("quest_discuss", 100)
                        .NpcLine(questDiscuss, null, null)
                        .Condition(delegate
                    {
                        return CharacterObject.OneToOneConversationCharacter == QuestGiver.CharacterObject;
                    })
                        .BeginPlayerOptions()
                        .PlayerOption(new TextObject(GlobalClass.Texts.playerOption1, null), null)
                        .ClickableCondition(new ConversationSentence.OnClickableConditionDelegate(ReturnClickableConditions))
                        .NpcLine(finalThankYouText, null, null)
                        .Consequence(delegate
                    {
                        Campaign.Current.ConversationManager.ConversationEndOneShot += Success;
                    })
                        .CloseDialog()
                        .PlayerOption(new TextObject(GlobalClass.Texts.playerOption2, null), null)
                        .NpcLine(waitingText, null, null)
                        .CloseDialog()
                        .EndPlayerOptions()
                        .CloseDialog();
                }

                private bool ReturnClickableConditions(out TextObject explanation)
                {
                    if (PlayerAcceptedQuestLog.CurrentProgress >= FoodAmount)
                    {
                        explanation = TextObject.Empty;
                        return true;
                    }
                    explanation = new TextObject(GlobalClass.Texts.explanation, null);
                    GlobalClass.SetTextVariables(explanation, QuestGiver);
                    return false;
                }

                protected override void OnTimedOut()
                {
                    AddLog(StageTimeoutLogText, false);
                    Fail();
                }

                private void QuestAcceptedConsequences()
                {
                    StartQuest();
                    TextObject textObject = new TextObject(GlobalClass.Texts.directive, null);
                    GlobalClass.SetTextVariables(textObject, QuestGiver);
                    PlayerAcceptedQuestLog = AddDiscreteLog(StageOnePlayerAcceptsQuestLogText, textObject, GetFoodAmountOnPlayer(), FoodAmount, null, false);
                }

                private int GetFoodAmountOnPlayer()
                {
                    ItemRoster itemRoster = PartyBase.MainParty.ItemRoster;
                    return itemRoster.TotalFood;
                }

                private void CheckIfPlayerReadyToReturnFood()
                {
                    if (PlayerHasEnoughFood == null && PlayerAcceptedQuestLog.CurrentProgress >= FoodAmount)
                    {
                        PlayerHasEnoughFood = AddLog(StageTwoPlayerHasEnoughFoodLogText, false);
                        return;
                    }
                    if (PlayerHasEnoughFood != null && PlayerAcceptedQuestLog.CurrentProgress < FoodAmount)
                    {
                        RemoveLog(PlayerHasEnoughFood);
                        PlayerHasEnoughFood = null;
                    }
                }

                private void DecreaseFood()
                {

                    int neededFoodAmount = GlobalClass.foodAmount;
                    
                    ItemRoster itemRoster = PartyBase.MainParty.ItemRoster;

                    List<ItemObject> eatableItems = new();

                    foreach (ItemObject? item in Items.AllTradeGoods)
                    {
                        if (item.IsFood && itemRoster.GetItemNumber(item) != 0)
                        {
                            eatableItems.Add(item);
                        }
                    }

                    eatableItems.Sort((a, b) => a.Value.CompareTo(b.Value));

                    while (neededFoodAmount > 0)
                    {
                        foreach (ItemObject? food in eatableItems)
                        {

                            int itemCount = itemRoster.GetItemNumber(food);
                            if (itemCount > neededFoodAmount)
                            {
                                itemRoster.AddToCounts(food, -neededFoodAmount);
                                neededFoodAmount = 0;
                                break;
                            }

                            else
                            {
                                itemRoster.AddToCounts(food, -itemCount);
                                neededFoodAmount -= itemCount;
                                continue;
                            }
                        }
                    }
                }

                private void Success()
                {
                    CompleteQuestWithSuccess();
                    AddLog(StageSuccessLogText, false);

                    TextObject successText = new TextObject(GlobalClass.Texts.successConsequence, null);
                    GlobalClass.SetTextVariables(successText, QuestGiver);
                    AddLog(successText);

                    Clan.PlayerClan.AddRenown(50f, true);
                    QuestGiver.AddPower(25f);
                    RelationshipChangeWithQuestGiver = 35;
                    ChangeRelationAction.ApplyPlayerRelation(QuestGiver, RelationshipChangeWithQuestGiver, false, true);

                    foreach (var hero in QuestGiver.CurrentSettlement.Notables)
                    {
                        if (hero == QuestGiver)
                        {
                            continue;
                        }

                        ChangeRelationAction.ApplyPlayerRelation(hero, RelationshipChangeWithQuestGiver / 2, false, true);
                    }

                    Settlement.CurrentSettlement.Town.Prosperity += 50f;
                    Settlement.CurrentSettlement.Town.FoodStocks += GlobalClass.foodAmount;
                    DecreaseFood();
                }

                private void Fail()
                {
                    QuestGiver.AddPower(-25f);
                    QuestGiver.CurrentSettlement.Town.Prosperity += -50f;
                    RelationshipChangeWithQuestGiver = -20;
                    ChangeRelationAction.ApplyPlayerRelation(QuestGiver, RelationshipChangeWithQuestGiver, false, true);

                    foreach (var hero in QuestGiver.CurrentSettlement.Notables)
                    {
                        if (hero == QuestGiver)
                        {
                            continue;
                        }

                        ChangeRelationAction.ApplyPlayerRelation(hero, -RelationshipChangeWithQuestGiver / 2, false, true);
                    }
                }

                protected override void HourlyTick()
                {
                }

                // Saved vars/logs
                [SaveableField(1)]
                private readonly int FoodAmount;

                [SaveableField(2)]
                private JournalLog PlayerAcceptedQuestLog;

                [SaveableField(3)]
                private JournalLog? PlayerHasEnoughFood;


                public class IssueTypeDefiner : SaveableTypeDefiner
                {
                    public IssueTypeDefiner() : base(001480) // Starting from 001432 (+3 per quest; 001444, 1447 for ex.)
                    {
                    }

                    protected override void DefineClassTypes()
                    {
                        AddClassDefinition(typeof(TaskIssue), 1);
                        AddClassDefinition(typeof(TaskQuest), 2);
                    }

                }
            }
        }
    }
}
