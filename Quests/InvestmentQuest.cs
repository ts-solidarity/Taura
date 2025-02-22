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


namespace Taura.Quests.InvestmentQuest
{
    public class GlobalClass
    {


        // If you set investmentAmount manually to something, you should also change overrideInvestmentAmount to false
        public static int investmentAmount = CalculateInvestmentAmount(null);

        // Change this to true, if you want to set investmentAmount manually
        public static bool overrideInvestmentAmount = true;

        // How much profit you're expected to make
        public static int profitPercentage = 10;

        // After accepting issue as a quest, you have this amount of time
        public static float questTime = 7f;

        // Issue will be available for this period of time
        public static float issueTime = 10f;

        // How frequent the issue should emerge
        public static IssueBase.IssueFrequency issueFrequency = IssueBase.IssueFrequency.VeryCommon;

        // How much you need to return after making a profit
        public static int paymentAmount = CalculatePaymentAmount();

        public static int tradeSkill = Hero.MainHero.GetSkillValue(new SkillObject("Trade"));

        // All task strings in one place
        public class Texts
        {
            // Issue Texts
            public static string issueTitle = "Investment Opportunity";
            public static string issueDescription = "{QUEST_GIVER} in {QUEST_SETTLEMENT} invested {INVESTMENT_AMOUNT}{GOLD_ICON} in you. Trusting in your abilities, he expects {PAYMENT_AMOUNT}{GOLD_ICON} in return.";
            public static string issueBriefByIssueGiver = "Well... Let's just say I have earned a bit money from here and there. And I thought, why not use it to earn more, instead of spending it to bri-- I mean, doing business?";
            public static string issueAcceptByPlayer = "I don't really care where you acquired it. Just tell me, how much is that 'a bit'?";
            public static string issueQuestSolutionExplanationByIssueGiver = "I am willing to invest {INVESTMENT_AMOUNT}{GOLD_ICON} in you, to be precise. Since you have some experience trading, making a %{PROFIT_PERCENTAGE} of profit must be child's play for ya, right? You can keep any extra. What do you say?";
            public static string issueQuestSolutionAcceptByPlayer = "Deal. Just don't ask me where I earned it.";
            public static string issueAsRumorInSettlement = "People say {QUEST_GIVER} has earned quite a bit of money and looking for investment opportunities. Ah, wish I had that much money! I'd spend it all in tavern, drinking TauraBeers.";

            // Quest Texts
            public static string questTitle = "Investor in {QUEST_SETTLEMENT}";
            public static string stageOnePlayerAcceptsQuestLogText = "{QUEST_GIVER.LINK}, an investor in the town of {QUEST_SETTLEMENT} asked you to make at least %{PROFIT_PERCENTAGE} of profit, using {INVESTMENT_AMOUNT}{GOLD_ICON} of investment, with your trade abilities. You are expected to return {PAYMENT_AMOUNT}{GOLD_ICON} to the investor. According to the agreement, any extra profit is yours to keep.";
            public static string stageTwoPlayerHasIssueItemLogText = "You have enough {CURRENT_GOLD} to complete the quest. Return to {QUEST_SETTLEMENT} to hand it over.";
            public static string stageTimeoutLogText = "You have failed to return {PAYMENT_AMOUNT}{GOLD_ICON} to {QUEST_GIVER.LINK}. People will doubt your abilities in future.";
            public static string stageSuccessLogText = "You have kept your promise and returned {PAYMENT_AMOUNT}{GOLD_ICON} to {QUEST_GIVER.LINK}.";
            public static string stageCancelDueToWarLogText = "Your clan is now at war with the {QUEST_GIVER.LINK}'s lord. Your agreement with {ISSUE_GIVER.LINK} was canceled.";

            public static string initialThankYou = "I trust in your abilities to profit even more than that. Just try not to screw this up.";
            public static string finalThankYou = "T'was a pleasure doing business with ya.";
            public static string waitingText = "Yeah sure. Try to remember our terms when time's up as well.";

            public static string questDiscuss = "Have you profited enough to return my {PAYMENT_AMOUNT}{GOLD_ICON}?";
            public static string playerOption1 = "Yes, here is your gold.";
            public static string playerOption2 = "Nah, time is the essence of profit after all. Wait a bit more.";

            public static string explanation = "You don't have enough {GOLD_ICON}";
            public static string directive = "Acquire {PAYMENT_AMOUNT}{GOLD_ICON}";
            public static string positiveNotification = "You have enough {GOLD_ICON} to complete the quest. Return to {QUEST_SETTLEMENT} to hand it over.";
            public static string successConsequence = "You have successfuly returned {PAYMENT_AMOUNT}{GOLD_ICON}.";
        }

        // Dynamically calculates needed item count the player needs to bring to the issue giver
        public static int CalculateInvestmentAmount(Hero issueGiver)
        {
            int investmentAmount = 0;
            
            int baseInvestment = 10000;
            int renown = (int)Clan.PlayerClan.Renown;
            int tradeSkill = Hero.MainHero.GetSkillValue(new SkillObject("Trade"));
            int prosperity = 2000;

            if (issueGiver != null)
            {
                prosperity = (int)issueGiver.CurrentSettlement.Town.Prosperity;
            }

            GlobalClass.tradeSkill = tradeSkill;

            int investmentBasedOnRenown = renown * 25;
            int investmentBasedOnTradeSkill = tradeSkill * 250;
            int investmentBasedOnProspertiy = prosperity * 2;

            investmentAmount = baseInvestment + investmentBasedOnRenown + investmentBasedOnTradeSkill + investmentBasedOnProspertiy;
            return investmentAmount;
        }

        public static int CalculatePaymentAmount()
        {
            return investmentAmount + (investmentAmount * profitPercentage / 100);
        }

        public static void SetTextVariables(TextObject textObject, Hero issueGiver)
        {

            if (issueGiver == null) return;

            textObject.SetTextVariable("QUEST_GIVER", issueGiver.Name);
            textObject.SetTextVariable("QUEST_SETTLEMENT", issueGiver.CurrentSettlement.Name);
            textObject.SetTextVariable("PAYMENT_AMOUNT", paymentAmount);
            textObject.SetTextVariable("PROFIT_PERCENTAGE", profitPercentage);
            textObject.SetTextVariable("INVESTMENT_AMOUNT", investmentAmount);
            textObject.SetTextVariable("TRADE_SKILL", tradeSkill);

            StringHelpers.SetCharacterProperties("QUEST_GIVER", issueGiver.CharacterObject, textObject);
            StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject);
        }

        public static bool ReturnQuestStartRequirements(out TextObject explanation)
        {
            explanation = new TextObject("You are not experienced enough (Required: {TRADE_SKILL} Trade Points)", null);
            return tradeSkill >= 50;
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
                return issueGiver.IsNotable && issueGiver.CurrentSettlement != null && issueGiver.CurrentSettlement.IsTown && !issueGiver.CurrentSettlement.IsStarving;
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
                    if (GlobalClass.overrideInvestmentAmount)
                    {
                        GlobalClass.investmentAmount = GlobalClass.CalculateInvestmentAmount(issueOwner);
                        GlobalClass.paymentAmount = GlobalClass.CalculatePaymentAmount();
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
                    return new TaskQuest(questId, IssueOwner, CampaignTime.DaysFromNow(GlobalClass.questTime), RewardGold, GlobalClass.paymentAmount);
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
                public TaskQuest(string questId, Hero questGiver, CampaignTime duration, int rewardGold, int paymentAmount) : base(questId, questGiver, duration, rewardGold)
                {
                    SetDialogs();
                    InitializeQuestOnCreation();
                    PaymentAmount = paymentAmount;
                }

                public int GetPaymentAmount()
                {
                    return PaymentAmount;
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

                private TextObject StageTwoPlayerHasEnoughGoldLogText
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
                    PlayerAcceptedQuestLog.UpdateCurrentProgress(GetGoldAmountOnPlayer());
                    CheckIfPlayerReadyToReturnGold();
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
                        GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, GlobalClass.paymentAmount);
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
                    if (PlayerAcceptedQuestLog.CurrentProgress >= PaymentAmount)
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
                    GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, GlobalClass.investmentAmount);
                    PlayerAcceptedQuestLog = AddDiscreteLog(StageOnePlayerAcceptsQuestLogText, textObject, GetGoldAmountOnPlayer(), PaymentAmount, null, false);
                }

                private int GetGoldAmountOnPlayer()
                {
                    return Hero.MainHero.Gold;
                }

                private void CheckIfPlayerReadyToReturnGold()
                {
                    if (PlayerHasEnoughGold == null && PlayerAcceptedQuestLog.CurrentProgress >= PaymentAmount)
                    {
                        PlayerHasEnoughGold = AddLog(StageTwoPlayerHasEnoughGoldLogText, false);
                        return;
                    }
                    if (PlayerHasEnoughGold != null && PlayerAcceptedQuestLog.CurrentProgress < PaymentAmount)
                    {
                        RemoveLog(PlayerHasEnoughGold);
                        PlayerHasEnoughGold = null;
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
                    RelationshipChangeWithQuestGiver = 25;
                    ChangeRelationAction.ApplyPlayerRelation(QuestGiver, RelationshipChangeWithQuestGiver, false, true);

                    foreach (var hero in QuestGiver.CurrentSettlement.Notables)
                    {
                        if (hero == QuestGiver)
                        {
                            continue;
                        }

                        ChangeRelationAction.ApplyPlayerRelation(hero, RelationshipChangeWithQuestGiver / 3, false, true);
                    }

                    Settlement.CurrentSettlement.Town.Prosperity += 50f;
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

                        ChangeRelationAction.ApplyPlayerRelation(hero, -RelationshipChangeWithQuestGiver / 3, false, true);
                    }
                }

                protected override void HourlyTick()
                {
                }

                // Saved vars/logs
                [SaveableField(1)]
                private readonly int PaymentAmount;

                [SaveableField(2)]
                private JournalLog PlayerAcceptedQuestLog;

                [SaveableField(3)]
                private JournalLog? PlayerHasEnoughGold;


                public class IssueTypeDefiner : SaveableTypeDefiner
                {
                    public IssueTypeDefiner() : base(001485) // Starting from 001432 (+3 per quest; 001444, 1447 for ex.)
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
