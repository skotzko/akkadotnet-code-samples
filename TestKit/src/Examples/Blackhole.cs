using System;
using Akka.Actor;
using Akka.TestKit;
using Akka.TestKit.NUnit;
using Akka.TestKit.TestActors;
using NUnit.Framework;

namespace TestKitSample.Examples
{
    #region Messages
    public class CreateUser { }

    public class UserResult
    {
        public bool Successful { get; }

        public UserResult() : this(false) {}

        public UserResult(bool successful)
        {
            Successful = successful;
        }
    }
    #endregion


    /// <summary>
    /// This demo actor collaborates with the <see cref="AuthenticationActor"/>
    /// to authenticate <see cref="CreateUser"/> requests before creating those users.
    /// </summary>
    public class IdentityManagerActor : ReceiveActor
    {
        private readonly IActorRef _authenticator;

        public IdentityManagerActor(IActorRef authenticationActor)
        {
            _authenticator = authenticationActor;

            Receive<CreateUser>(create =>
            {
                // since we're using the PipeTo pattern, we need to close over state
                // that can change between messages, such as Sender
                // for more, see the PipeTo sample: https://github.com/petabridge/akkadotnet-code-samples/tree/master/PipeTo
                var senderClosure = Sender;

                // this actor needs it create user request to be authenticated
                // within 2 seconds or this operation times out & cancels 
                // the Task returned by Ask<>
                _authenticator.Ask<UserResult>(create, TimeSpan.FromSeconds(2))
                    .ContinueWith(tr =>
                    {
                        // if the task got messed up / failed, return failure result
                        if (tr.IsCanceled || tr.IsFaulted)
                            return new UserResult(false);

                        // otherwise return whatever the actual result was
                        return tr.Result;
                    }).PipeTo(senderClosure);
            });
        }
    }

    /// <summary>
    /// Authentication counterpart that works with the <see cref="IdentityManagerActor"/>
    /// to authenticate <see cref="CreateUser"/> requests.
    /// </summary>
    public class AuthenticationActor : ReceiveActor
    {
        public AuthenticationActor()
        {
            Receive<CreateUser>(create =>
            {
                // arbitrary because why not
                var successful = new Random().NextDouble() > 0.5;
                Sender.Tell(new UserResult(successful));
            });
        }
    }

    [TestFixture]
    public class IdentityManagerActorSpecs : TestKit
    {
        [Test]
        public void IdentityManagerActor_should_fail_create_user_on_timeout()
        {
            // the BlackHoleActor will NEVER respond to any message sent to it
            // which will force the CreateUser request to time out
            var blackhole = Sys.ActorOf(BlackHoleActor.Props);
            var identity = Sys.ActorOf(Props.Create(() => new IdentityManagerActor(blackhole)));

            identity.Tell(new CreateUser());
            var result = ExpectMsg<UserResult>().Successful;            
            Assert.False(result);
        }
    }
}
