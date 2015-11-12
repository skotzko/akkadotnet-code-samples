using Akka.Actor;
using Akka.Event;
using Akka.TestKit.NUnit;
using NUnit.Framework;

namespace TestKitSample.Examples
{
    public class HOCON
    {
        public class TestHoconActor : ReceiveActor
        {
            private ILoggingAdapter _log = Context.GetLogger();

            public TestHoconActor()
            {
                ReceiveAny(o => _log.Debug("TestHoconActor got a message"));
            }
        }

        [TestFixture]
        public class TestHoconActorSpecs : TestKit
        {
            public TestHoconActorSpecs() : base(@"
            akka {
                loglevel = INFO
            }") { }

            [Test]
            public void TestHoconActor_should_log_debug_messages_invisibly_when_loglevel_is_info()
            {
                EventFilter.Debug("TestHoconActor got a message").Expect(0, () =>
                {
                    var actor = Sys.ActorOf(Props.Create(() => new TestHoconActor()));
                    actor.Tell("foo");
                });
            }

        }

    }
}
