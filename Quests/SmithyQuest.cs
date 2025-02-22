using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Helpers;
using SandBox.ViewModelCollection.Nameplate.NameplateNotifications.SettlementNotificationTypes;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
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
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;


namespace Taura.Quests.SmithyQuest
{
    public class GlobalClass
    {
        // Quest-related item's id
        public static string itemId = "iron";
        public static ItemObject questItem = MBObjectManager.Instance.GetObject<ItemObject>(itemId);

        // If you set neededItemCount manually to something, you should also change overrideItemCount to false
        public static int neededItemCount = CalculateNeededItemCount(null);

        // Change this if you manually set neededItemCount
        public static bool overrideItemCount = true;

        // This is the multiplier that's being applied to gold reward
        public static int priceMultiplier = 3;

        // After accepting issue as a quest, you have this amount of time
        public static float questTime = 14f;

        // Issue will be available for this period of time
        public static float issueTime = 20f;

        // How frequent the issue should emerge
        public static IssueBase.IssueFrequency issueFrequency = IssueBase.IssueFrequency.VeryCommon;

        // Quest-related workshop string id
        public static string workshopStringId = "smithy";
        public static int earnedGold = neededItemCount * questItem.Value * priceMultiplier;


        // All task strings in one place
        public class Texts
        {
            // Issue Texts
            public static string issueTitle = "{WORKSHOP_NAME} Owner Needs {ITEM_NAME}s";
            public static string issueDescription = "{QUEST_GIVER} in {QUEST_SETTLEMENT} needs you to bring him {ITEM_AMOUNT} piles of {ITEM_NAME}s.";
            public static string issueBriefByIssueGiver = "Our {ITEM_NAME}s supply is damaged. I need to fix it.";
            public static string issueAcceptByPlayer = "I might help, if the payment is good.";
            public static string issueQuestSolutionExplanationByIssueGiver = "It is not that easy. I need a large stock. {ITEM_AMOUNT} {ITEM_NAME}s, to be precise. I will pay triple for their worth.";
            public static string issueQuestSolutionAcceptByPlayer = "Don't worry, I've seen worse. I got this.";
            public static string issueAsRumorInSettlement = "I heard there is a problem going on with the {WORKSHOP_NAME} owner {QUEST_GIVER}. Rumor says there is no {ITEM_NAME}s left in this city.";

            // Quest Texts
            public static string questTitle = "{QUEST_GIVER} in {QUEST_SETTLEMENT} needs {ITEM_NAME}s";
            public static string stageOnePlayerAcceptsQuestLogText = "{QUEST_GIVER.LINK}, a {WORKSHOP_NAME} owner in the town of {QUEST_SETTLEMENT} asked you to bring {ITEM_AMOUNT} {ITEM_NAME}s, to help fulfil the current hole in supply chain. You will be paid triple the average price of each good on delivery.";
            public static string stageTwoPlayerHasIssueItemLogText = "You have enough {ITEM_NAME}s to complete the quest. Return to {QUEST_SETTLEMENT} to hand them over.";
            public static string stageTimeoutLogText = "You have failed to deliver {ITEM_AMOUNT} {ITEM_NAME}s to {QUEST_SETTLEMENT}. {QUEST_GIVER.LINK} is disappointed.";
            public static string stageSuccessLogText = "You have delivered {ITEM_AMOUNT} {ITEM_NAME}s to {QUEST_SETTLEMENT}. The people rejoice at the delivery of {ITEM_NAME}s.";
            public static string stageCancelDueToWarLogText = "Your clan is now at war with the {QUEST_GIVER.LINK}'s lord. Your agreement with {ISSUE_GIVER.LINK} was canceled.";

            public static string initialThankYou = "Thank you {?PLAYER.GENDER}milady{?}sir{\\?}! It's a great help.";
            public static string finalThankYou = "Thank you {?PLAYER.GENDER}milady{?}sir{\\?}! You'd been a huge help to the {WORKSHOP_NAME}. Here is your payment!";
            public static string waitingText = "Okay, but please hurry.";

            public static string questDiscuss = "Have you brought {ITEM_AMOUNT} {ITEM_NAME}s?";
            public static string playerOption1 = "Yes, here they are.";
            public static string playerOption2 = "I'm working on it.";

            public static string explanation = "You don't have enough {ITEM_NAME}s.";
            public static string directive = "Collect {ITEM_NAME}s.";
            public static string positiveNotification = "You have enough {ITEM_NAME}s to complete the quest. Return to {QUEST_SETTLEMENT} to hand them over.";
            public static string successConsequence = "You gave {ITEM_COUNT} {ITEM_NAME}s. In return you got {GOLD_AMOUNT}{GOLD_ICON}.";
        }



        // Checks whether or not the notable has the desired workshop
        public static bool HasDesiredWorkshop(Hero issueGiver)
        {
            foreach (var workshop in issueGiver.OwnedWorkshops)
            {
                if (issueGiver.CurrentSettlement != null && workshop.WorkshopType.StringId == workshopStringId) return true;
            }

            return false;
        }

        // Returns the issue giver's quest-related workshop
        public static Workshop? GetDesiredWorkshop(Hero issueGiver)
        {
            foreach (var workshop in issueGiver.OwnedWorkshops)
            {
                if (workshop.WorkshopType.StringId == workshopStringId) return workshop;
            }

            return null;
        }

        // Dynamically calculates needed item count the player needs to bring to the issue giver
        public static int CalculateNeededItemCount(Hero issueGiver)
        {
            int baseCount = 10;

            int partySize = PartyBase.MainParty.NumberOfAllMembers;
            int renown = (int)Clan.PlayerClan.Renown;

            int amountBasedOnPartySize = partySize / 5;
            int amountBasedOnRenown = renown / 10;

            int defaultItemCount = baseCount + amountBasedOnPartySize + amountBasedOnRenown;

            if (issueGiver == null) return defaultItemCount;
            Workshop workshop = GetDesiredWorkshop(issueGiver);
            if (workshop == null) return defaultItemCount;

            int profit = workshop.ProfitMade;
            int amountBasedOnProfit = 0;

            if (profit >= 0) amountBasedOnProfit = 10;
            else amountBasedOnProfit = 20;

            int prosperity = (int)issueGiver.CurrentSettlement.Town.Prosperity;
            int amountBasedOnProsperity = prosperity / 200;

            int dynamicItemCount = defaultItemCount / 3 + amountBasedOnProfit + amountBasedOnProsperity;
            return dynamicItemCount;
        }

        public static void SetTextVariables(TextObject textObject, Hero issueGiver)
        {

            if (issueGiver == null) return;

            textObject.SetTextVariable("QUEST_GIVER", issueGiver.Name);
            textObject.SetTextVariable("QUEST_SETTLEMENT", issueGiver.CurrentSettlement.Name);
            textObject.SetTextVariable("WORKSHOP_NAME", GetDesiredWorkshop(issueGiver).Name);
            textObject.SetTextVariable("LORD_NAME", issueGiver.CurrentSettlement.Owner.Name);
            textObject.SetTextVariable("ITEM_NAME", questItem.Name);
            textObject.SetTextVariable("ITEM_AMOUNT", neededItemCount);
            textObject.SetTextVariable("GOLD_AMOUNT", earnedGold);

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

            // Issue giver must have the desired workshop
            private bool ConditionsHold(Hero issueGiver)
            {
                return GlobalClass.HasDesiredWorkshop(issueGiver);
            }

            // If the conditions hold, start this issue. Otherwise? Just add it as a possible issue.
            private void OnCheckForIssue(Hero hero)
            {
                ItemObject issueItem = GlobalClass.questItem;

                if (ConditionsHold(hero))
                {
                    // Start the issue
                    Campaign.Current.IssueManager.AddPotentialIssueData(hero, new PotentialIssueData(
                        new PotentialIssueData.StartIssueDelegate(OnStartIssue),
                        typeof(TaskIssue),
                        GlobalClass.issueFrequency, issueItem));

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
                    if (GlobalClass.overrideItemCount)
                        GlobalClass.neededItemCount = GlobalClass.CalculateNeededItemCount(issueOwner);
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
                    return GlobalClass.HasDesiredWorkshop(IssueOwner);
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
                    return new TaskQuest(questId, IssueOwner, CampaignTime.DaysFromNow(GlobalClass.questTime), RewardGold, GlobalClass.neededItemCount);
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
                public TaskQuest(string questId, Hero questGiver, CampaignTime duration, int rewardGold, int neededItemCount) : base(questId, questGiver, duration, rewardGold)
                {
                    SetDialogs();
                    InitializeQuestOnCreation();
                    NeededItemCount = neededItemCount;
                }

                public int GetNeededItemCount()
                {
                    return NeededItemCount;
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

                private TextObject StageTwoPlayerHasIssueItemLogText
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
                    PlayerAcceptedQuestLog.UpdateCurrentProgress(GetIssueItemCountOnPlayer());
                    CheckIfPlayerReadyToReturnIssueItem();
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
                    OfferDialogFlow.NpcLine(initialThankYouText, null, null).Condition(() => CharacterObject.OneToOneConversationCharacter == QuestGiver.CharacterObject).Consequence(new ConversationSentence.OnConsequenceDelegate(QuestAcceptedConsequences)).CloseDialog();


                    DiscussDialogFlow = DialogFlow.CreateDialogFlow("quest_discuss", 100).NpcLine(questDiscuss, null, null).Condition(delegate
                    {
                        return CharacterObject.OneToOneConversationCharacter == QuestGiver.CharacterObject;
                    }).BeginPlayerOptions().PlayerOption(new TextObject(GlobalClass.Texts.playerOption1, null), null).ClickableCondition(new ConversationSentence.OnClickableConditionDelegate(ReturnClickableConditions)).NpcLine(finalThankYouText, null, null).Consequence(delegate
                    {
                        Campaign.Current.ConversationManager.ConversationEndOneShot += Success;
                    }).CloseDialog().PlayerOption(new TextObject(GlobalClass.Texts.playerOption2, null), null).NpcLine(waitingText, null, null).CloseDialog().EndPlayerOptions().CloseDialog();
                }

                private bool ReturnClickableConditions(out TextObject explanation)
                {
                    if (PlayerAcceptedQuestLog.CurrentProgress >= NeededItemCount)
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
                    PlayerAcceptedQuestLog = AddDiscreteLog(StageOnePlayerAcceptsQuestLogText, textObject, GetIssueItemCountOnPlayer(), NeededItemCount, null, false);
                }

                private int GetIssueItemCountOnPlayer()
                {
                    ItemObject issueItem = GlobalClass.questItem;
                    int itemNumber = PartyBase.MainParty.ItemRoster.GetItemNumber(issueItem);

                    if (itemNumber > NeededItemCount)
                    {
                        TextObject textObject = new TextObject(GlobalClass.Texts.positiveNotification, null);
                        GlobalClass.SetTextVariables(textObject, QuestGiver);
                        MBInformationManager.AddQuickInformation(textObject, 0, null, "");
                    }

                    return itemNumber;
                }

                private void CheckIfPlayerReadyToReturnIssueItem()
                {
                    if (PlayerHasNeededIssueItemLog == null && PlayerAcceptedQuestLog.CurrentProgress >= NeededItemCount)
                    {
                        PlayerHasNeededIssueItemLog = AddLog(StageTwoPlayerHasIssueItemLogText, false);
                        return;
                    }
                    if (PlayerHasNeededIssueItemLog != null && PlayerAcceptedQuestLog.CurrentProgress < NeededItemCount)
                    {
                        RemoveLog(PlayerHasNeededIssueItemLog);
                        PlayerHasNeededIssueItemLog = null;
                    }
                }

                private void Success()
                {
                    CompleteQuestWithSuccess();
                    AddLog(StageSuccessLogText, false);



                    TextObject gaveTauraBeersText = new TextObject(GlobalClass.Texts.successConsequence, null);
                    GlobalClass.SetTextVariables(gaveTauraBeersText, QuestGiver);
                    AddLog(gaveTauraBeersText);

                    QuestGiver.CurrentSettlement.Town.FoodStocks += NeededItemCount;
                    GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, GlobalClass.earnedGold, false);
                    Clan.PlayerClan.AddRenown(12f, true);
                    QuestGiver.AddPower(25f);
                    RelationshipChangeWithQuestGiver = 10;
                    ChangeRelationAction.ApplyPlayerRelation(QuestGiver, RelationshipChangeWithQuestGiver, false, true);

                    foreach (var hero in QuestGiver.CurrentSettlement.Notables)
                    {
                        if (hero == QuestGiver)
                        {
                            continue;
                        }

                        ChangeRelationAction.ApplyPlayerRelation(hero, RelationshipChangeWithQuestGiver / 4, false, true);
                    }

                    Settlement.CurrentSettlement.Town.Prosperity += 50f;
                }

                private void Fail()
                {
                    QuestGiver.AddPower(-25f);
                    QuestGiver.CurrentSettlement.Town.Prosperity += -50f;
                    RelationshipChangeWithQuestGiver = -10;
                    ChangeRelationAction.ApplyPlayerRelation(QuestGiver, RelationshipChangeWithQuestGiver, false, true);
                }

                protected override void HourlyTick()
                {
                }

                // Saved vars/logs
                [SaveableField(1)]
                private readonly int NeededItemCount;

                [SaveableField(2)]
                private JournalLog PlayerAcceptedQuestLog;

                [SaveableField(3)]
                private JournalLog? PlayerHasNeededIssueItemLog;


                public class IssueTypeDefiner : SaveableTypeDefiner
                {
                    public IssueTypeDefiner() : base(001447) // Starting from 001432 (+3 per quest; 001444, 1447 for ex.)
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
