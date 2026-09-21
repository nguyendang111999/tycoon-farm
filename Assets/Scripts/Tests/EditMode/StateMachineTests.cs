using Farm.Core;
using NUnit.Framework;

namespace Farm.Tests
{
    public class StateMachineTests
    {
        private sealed class TestContext
        {
            public int Value;
        }

        private sealed class TrackingState : IState<TestContext>
        {
            public int EnterCount;
            public int TickCount;
            public int ExitCount;

            public void Enter(TestContext context)
            {
                EnterCount++;
                context.Value += 10;
            }

            public void Tick(TestContext context)
            {
                TickCount++;
                context.Value += 1;
            }

            public void Exit(TestContext context)
            {
                ExitCount++;
                context.Value -= 5;
            }
        }

        [Test]
        public void StateMachine_TransitionsCallExitAndEnterInOrder()
        {
            var context = new TestContext();
            var stateA = new TrackingState();
            var stateB = new TrackingState();

            var fsm = new StateMachine<TestContext>(context, stateA);

            Assert.AreSame(stateA, fsm.CurrentState);
            Assert.AreEqual(1, stateA.EnterCount);
            Assert.AreEqual(10, context.Value);

            fsm.ChangeState(stateB);

            Assert.AreSame(stateB, fsm.CurrentState);
            Assert.AreEqual(1, stateA.ExitCount);
            Assert.AreEqual(1, stateB.EnterCount);
            // 10 - 5 (Exit A) + 10 (Enter B) = 15
            Assert.AreEqual(15, context.Value);
        }

        [Test]
        public void StateMachine_Tick_InvokesCurrentStateTick()
        {
            var context = new TestContext();
            var state = new TrackingState();
            var fsm = new StateMachine<TestContext>(context, state);

            fsm.Tick();
            fsm.Tick();

            Assert.AreEqual(2, state.TickCount);
            Assert.AreEqual(12, context.Value); // 10 from Enter + 2 from Ticks
        }

        [Test]
        public void StateMachine_ChangeStateToSameState_DoesNotReEnter()
        {
            var context = new TestContext();
            var state = new TrackingState();
            var fsm = new StateMachine<TestContext>(context, state);

            fsm.ChangeState(state);

            Assert.AreEqual(1, state.EnterCount);
            Assert.AreEqual(0, state.ExitCount);
        }

        [Test]
        public void StateMachine_ChangeStateToNull_DoesNothing()
        {
            var context = new TestContext();
            var state = new TrackingState();
            var fsm = new StateMachine<TestContext>(context, state);

            fsm.ChangeState(null);

            Assert.AreSame(state, fsm.CurrentState);
            Assert.AreEqual(0, state.ExitCount);
        }

        [Test]
        public void StateMachine_StateChangedEvent_FiresWithPreviousAndNextStates()
        {
            var context = new TestContext();
            var stateA = new TrackingState();
            var stateB = new TrackingState();

            IState<TestContext> reportedPrev = null;
            IState<TestContext> reportedNext = null;

            var fsm = new StateMachine<TestContext>(context);
            fsm.StateChanged += (prev, next) =>
            {
                reportedPrev = prev;
                reportedNext = next;
            };

            fsm.ChangeState(stateA);
            Assert.IsNull(reportedPrev);
            Assert.AreSame(stateA, reportedNext);

            fsm.ChangeState(stateB);
            Assert.AreSame(stateA, reportedPrev);
            Assert.AreSame(stateB, reportedNext);
        }
    }
}
