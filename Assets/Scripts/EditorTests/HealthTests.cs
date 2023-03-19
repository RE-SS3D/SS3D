using NUnit.Framework;
using SS3D.Systems.Health;

namespace EditorTests
{
    public class HealthTests
    {
        #region Test set up
        [SetUp]
        public void SetUp()
        {

        }

        [TearDown]
        public void TearDown()
        {
        }
        #endregion

        #region StaminaTests

        /// <summary>
        /// Test to confirm that interactions can only be commenced when stamina is greater than zero, and will otherwise fail.
        /// </summary>
        [Test]
        [TestCase(9.99f, true)]
        [TestCase(10.01f, false)]
        public void CanCommenceInteractionOnlyWhenStaminaIsGreaterThatZero(float staminaToDeplete, bool expectedResult)
        {
            IStamina sut = StaminaFactory.Create(10f);

            sut.ConsumeStamina(staminaToDeplete);

            Assert.IsTrue(sut.CanCommenceInteraction == expectedResult);
        }

        /// <summary>
        /// Test to confirm that interactions can be continued when negative. However, they will not continue beyond 10% of the max
        /// stamina in deficit.
        /// </summary>
        [Test]
        [TestCase(10.99f, true)]
        [TestCase(11.01f, false)]
        public void CanContinueInteractionWithNegativeStaminaUntilNegativeTenPercent(float staminaToDeplete, bool expectedResult)
        {
            IStamina sut = StaminaFactory.Create(10f);

            sut.ConsumeStamina(staminaToDeplete);

            Assert.IsTrue(sut.CanContinueInteraction == expectedResult);
        }

        /// <summary>
        /// Test to confirm that reducing stamina will result in the correct value for Current property being returned.
        /// Note that Current should always be in the range of 0 to 1 (inclusive).
        /// </summary>
        [Test]
        [TestCase(0f, 1f)]
        [TestCase(7f, 0.3f)]
        [TestCase(100f, 0f)]
        public void ConsumeStaminaCorrectlyReducesTheStaminaValue(float staminaToDeplete, float expectedResult)
        {
            IStamina sut = StaminaFactory.Create(10f);

            sut.ConsumeStamina(staminaToDeplete);

            Assert.IsTrue(sut.Current == expectedResult);
        }

        /// <summary>
        /// Test to confirm that recharging stamina will result in the correct value for Current property being returned.
        /// Note that Current should always be in the range of 0 to 1 (inclusive).
        /// </summary>
        [Test]
        [TestCase(0f, 0f)]
        [TestCase(0.7f, 0.7f)]
        [TestCase(100f, 1f)]
        public void RechargingStaminaCorrectlyReducesTheStaminaValue(float secondsToRecharge, float expectedResult)
        {
            IStamina sut = StaminaFactory.Create(10f, 1f);   // Set up stamina to fully recharge after 1 second.
            sut.ConsumeStamina(10f);                        // Deplete all of the stamina

            sut.RechargeStamina(secondsToRecharge);

            Assert.IsTrue(sut.Current == expectedResult);
        }
        #endregion

        #region PainTests

        Brain brain;
        HumanBodypart head;
        HumanBodypart torso;
        HumanBodypart leftArm;
        HumanBodypart rightArm;
        HumanBodypart leftLeg;
        HumanBodypart rightLeg;
        HumanBodypart leftHand;
        HumanBodypart rightHand;
        HumanBodypart leftFoot;
        HumanBodypart rightFoot;

        [SetUp]
        public void SetUpSimpleBody()
        {
            brain = new Brain("brain");
            head = new HumanBodypart(brain, "head");
            torso = new HumanBodypart(head, "torso");
            leftArm = new HumanBodypart(torso, "leftArm");
            rightArm = new HumanBodypart(torso, "rightArm");
            leftLeg = new HumanBodypart(torso, "leftLeg");
            rightLeg = new HumanBodypart(torso, "rightLeg");
            leftHand = new HumanBodypart(leftArm, "leftHand");
            rightHand = new HumanBodypart(rightArm, "rightHand");
            leftFoot = new HumanBodypart(leftLeg, "leftFoot");
            rightFoot = new HumanBodypart(rightLeg, "rightFoot");
        }

        /// <summary>
        /// Test to confirm that interactions can only be commenced when stamina is greater than zero, and will otherwise fail.
        /// </summary>
        [Test]
        public void PainisZeroInSimpleHealthyBody()
        {
            Assert.AreEqual(brain.ComputePain(), 0);
        }

        /// <summary>
        /// Test to confirm that interactions can only be commenced when stamina is greater than zero, and will otherwise fail.
        /// </summary>
        [Test]
        public void PainIsCorrectOnSingleBodyPartHurt()
        {
            leftFoot.TryInflictDamage<BoneLayer>(new DamageTypeQuantity(DamageType.Crush, 25f));
            float painInBrain = brain.ComputePain();
            float painInFoot = ((INerveSignalTransmitter)leftFoot.GetBodyLayer<NerveLayer>()).ProducePain();
            Assert.AreEqual(painInBrain, painInFoot);
        }

        /// <summary>
        /// This check if multiple injuries give the right amount of pain.
        /// </summary>
        [Test]
        [TestCase(0f,0f)]
        [TestCase(20.47f, 50.12f)]
        [TestCase(100f, 100f)]
        [TestCase(47.3f, 68.5f)]
        [TestCase(47.3f, 125.5f)]
        public void PainIsCorrectOnLeftFootHurtAndTorsoNerveHalfDestroyed(float footDamage, float torsoDamage)
        {
            leftFoot.TryInflictDamage<BoneLayer>(new DamageTypeQuantity(DamageType.Crush, footDamage));
            torso.TryInflictDamage<NerveLayer>(new DamageTypeQuantity(DamageType.Crush, torsoDamage));
            float painInBrain = brain.ComputePain();
            float painInFoot = ((INerveSignalTransmitter)leftFoot.GetBodyLayer<NerveLayer>()).ProducePain();
            float painInTorso = ((INerveSignalTransmitter)torso.GetBodyLayer<NerveLayer>()).ProducePain();
            // that looks weird, but that's actually what is expected.
            // ProducePain takes into account the damages of nerve layers higher in the hierarchy.
            Assert.AreEqual(painInBrain, painInFoot + painInTorso);
        }

        /// <summary>
        /// Check if body hierarchy is constructed as expected.
        /// </summary>
        [Test]
        public void SimpleBodyHierarchyIsCorrect()
        {
            // child correctly setup.
            Assert.Contains(head, brain.ChildBodyParts);
            Assert.Contains(torso, head.ChildBodyParts);
            Assert.Contains(leftArm, torso.ChildBodyParts);
            Assert.Contains(rightArm, torso.ChildBodyParts);
            Assert.Contains(leftHand, leftArm.ChildBodyParts);
            Assert.Contains(rightHand, rightArm.ChildBodyParts);
            Assert.Contains(leftLeg, torso.ChildBodyParts);
            Assert.Contains(rightLeg, torso.ChildBodyParts);
            Assert.Contains(leftFoot, leftLeg.ChildBodyParts);
            Assert.Contains(rightFoot, rightLeg.ChildBodyParts);

            // parent correctly set up.
            Assert.AreEqual(head.ParentBodyPart, brain);
            Assert.AreEqual(torso.ParentBodyPart, head);
            Assert.AreEqual(leftArm.ParentBodyPart, torso);
            Assert.AreEqual(rightArm.ParentBodyPart, torso);
            Assert.AreEqual(leftLeg.ParentBodyPart, torso);
            Assert.AreEqual(rightLeg.ParentBodyPart, torso);
            Assert.AreEqual(leftHand.ParentBodyPart, leftArm);
            Assert.AreEqual(rightHand.ParentBodyPart, rightArm);
            Assert.AreEqual(leftFoot.ParentBodyPart, leftLeg);
            Assert.AreEqual(rightFoot.ParentBodyPart, rightLeg);
        }


        #endregion

    }
}
