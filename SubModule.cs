using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.ObjectSystem;


namespace Taura
{
    public partial class SubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
        }

        protected override void OnSubModuleUnloaded()
        {
            base.OnSubModuleUnloaded();
        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();
            InformationManager.DisplayMessage(new InformationMessage("Taceddin Aura!"));
        }

        public override void OnMissionBehaviorInitialize(Mission mission)
        {
            base.OnMissionBehaviorInitialize(mission);
            mission.AddMissionBehavior(new TauraMissionView());
        }


        protected override void InitializeGameStarter(Game game, IGameStarter starterObject)
        {
            base.InitializeGameStarter(game, starterObject);

            if (starterObject is CampaignGameStarter starter)
            {
                starter.AddBehavior(new TauraBehavior());

                // Quests

                // Workshop Quests
                starter.AddBehavior(new Quests.BrewerQuest.SubModule.TaskBehavior());
                starter.AddBehavior(new Quests.WoodWorkshopQuest.SubModule.TaskBehavior());
                starter.AddBehavior(new Quests.SmithyQuest.SubModule.TaskBehavior());
                starter.AddBehavior(new Quests.TanneryQuest.SubModule.TaskBehavior());
                starter.AddBehavior(new Quests.WinePressQuest.SubModule.TaskBehavior());
                starter.AddBehavior(new Quests.LinenWeaveryQuest.SubModule.TaskBehavior());
                starter.AddBehavior(new Quests.OlivePressQuest.SubModule.TaskBehavior());
                //starter.AddBehavior(new Quests.PotteryShopQuest.SubModule.TaskBehavior());  // CRASH BEFORE INIT
                //starter.AddBehavior(new Quests.SilverSmithyQuest.SubModule.TaskBehavior()); // CRASH BEFORE INIT
                //starter.AddBehavior(new Quests.VelvetWeaveryQuest.SubModule.TaskBehavior()); // CRASH AFTER INIT
                //starter.AddBehavior(new Quests.WoolWeaveryQuest.SubModule.TaskBehavior());   // PROBLEMATIC TASK BEHAVIOR


                // Investment Quest
                starter.AddBehavior(new Quests.InvestmentQuest.SubModule.TaskBehavior());

                // Town Is Hungry Quest
                starter.AddBehavior(new Quests.TownIsHungryQuest.SubModule.TaskBehavior());


            }

        }
    }
}