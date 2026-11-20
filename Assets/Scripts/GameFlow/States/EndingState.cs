using YokiFrame;

namespace Anchor.GameFlow.States
{
    public sealed class EndingState : GameFlowStateBase
    {
        public EndingState(FSM<GameFlowState> fsm, GameFlowBlackboard blackboard, GameFlowController controller)
            : base(fsm, blackboard, controller)
        {
        }

        protected override void OnEnter()
        {
            NotifyEntered(GameFlowState.Ending);
            var result = Controller.ResolveService.ResolveEnding(mBlack);
            var entry = GameLeaderboardService.RecordGameEnd(mBlack, result);
            mBlack.SetEnding(result, entry.RunId);
            Controller.NotifyEndingSelected(result);
        }
    }
}
