using NUnit.Framework;
using NUnit.Framework.Internal.Execution;
using SS3D.Logging;
using SS3D.Systems.Health;
using System.Collections.Generic;
using UnityEditor;

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
        List<BodyPart> BodyParts;

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
            BodyParts = new List<BodyPart>() { brain, head, torso, leftArm, rightArm, leftLeg, rightLeg, 
            leftHand, rightHand, leftFoot, rightFoot};
        }

        /// <summary>
        /// Test to confirm that interactions can only be commenced when stamina is greater than zero, and will otherwise fail.
        /// </summary>
        [Test]
        public void PainisZeroInSimpleHealthyBody()
        {
            Assert.AreEqual(brain.ComputeAveragePain(), 0);
        }

        [Test]
        [TestCase(120f)]
        [TestCase(100f)]
        [TestCase(98f)]
        [TestCase(40f)]
        [TestCase(0f)]
        [TestCase(-5f)]
        public void SimpleBodyAveragePainWhenAllHurtButNerveIsCorrect(float damage)
        {
            var LayersCounts = new List<int>();
            foreach (BodyPart bodyPart in BodyParts)
            {
                bodyPart.InflictDamageToAllLayerButOne<NerveLayer>(new DamageTypeQuantity(DamageType.Crush, damage));
                LayersCounts.Add(bodyPart.BodyLayers.Count);
            }
            float resultingPain = brain.ComputeAveragePain();
            float expectedPain = 0;

            foreach(BodyPart bodyPart in BodyParts)
            {
                expectedPain += (bodyPart.TotalDamage / bodyPart.MaxDamage);
            }
            expectedPain = expectedPain / BodyParts.Count ;
            Assert.AreEqual( expectedPain, resultingPain, 0.001f);
        }

        [Test]
        [TestCase(120f)]
        [TestCase(100f)]
        [TestCase(98f)]
        [TestCase(40f)]
        [TestCase(0f)]
        [TestCase(-5f)]
        public void PainOnSingleOrganIsCorrect(float damage)
        {
            leftHand.TryInflictDamage<MuscleLayer>(new DamageTypeQuantity(DamageType.Crush, damage));
            leftHand.TryInflictDamage<CirculatoryLayer>(new DamageTypeQuantity(DamageType.Crush, damage));
            float resultingPain = leftHand.ProducePain();
            float muscleMaxDamage = leftHand.GetBodyLayer<MuscleLayer>().MaxDamage;
            float circulatoryMaxDamage = leftHand.GetBodyLayer<CirculatoryLayer>().MaxDamage;

            if (damage < 0)
            {
                Assert.AreEqual(0, resultingPain, 0.001f);
            }
            else if(damage > muscleMaxDamage && damage > circulatoryMaxDamage)
            {
                Assert.AreEqual((muscleMaxDamage + circulatoryMaxDamage) / leftHand.MaxDamage, resultingPain, 0.001f);
            }
            else if (damage > muscleMaxDamage && damage < circulatoryMaxDamage)
            {
                Assert.AreEqual((muscleMaxDamage + damage) / leftHand.MaxDamage, resultingPain, 0.001f);
            }
            else if (damage > circulatoryMaxDamage && damage < muscleMaxDamage)
            {
                Assert.AreEqual((circulatoryMaxDamage + damage) / leftHand.MaxDamage, resultingPain, 0.001f);
            }
            else
            {
                Assert.AreEqual(damage*2 /leftHand.MaxDamage, resultingPain, 0.001f);
            }
        }

        [Test]
        [TestCase(120f)]
        [TestCase(99.7f)]
        public void AveragePainIsWeakWhenCNSNerveIsStronglyHurt(float damage)
        {
            var LayersCounts = new List<int>();
            foreach (BodyPart bodyPart in BodyParts)
            {
                bodyPart.InflictDamageToAllLayerButOne<NerveLayer>(new DamageTypeQuantity(DamageType.Crush, bodyPart.MaxDamage/2));
                LayersCounts.Add(bodyPart.BodyLayers.Count);
            }
            brain.TryInflictDamage<NerveLayer>(new DamageTypeQuantity(DamageType.Crush, damage));

            float resultingPain = brain.ComputeAveragePain();
  
            Assert.Less(resultingPain, 0.1f);
        }

        /// </summary>
        [Test]
        [TestCase(120f,154f)]
        [TestCase(100f,100f)]
        [TestCase(100f, 80f)]
        [TestCase(100f, 70f)]
        [TestCase(98f,10f)]
        [TestCase(40f,96f)]
        [TestCase(0f,0f)]
        [TestCase(-5f,-57f)]
        public void AveragePainIsBetweenZeroAndOne(float damage, float nerveDamage)
        {
            foreach (BodyPart bodyPart in BodyParts)
            {
                bodyPart.InflictDamageToAllLayerButOne<NerveLayer>(new DamageTypeQuantity(DamageType.Crush, damage));
            }
            float resultingPain = brain.ComputeAveragePain();
            Assert.GreaterOrEqual(resultingPain, 0);
            Assert.LessOrEqual(resultingPain, 1);

            foreach (BodyPart bodyPart in BodyParts)
            {
                bodyPart.TryInflictDamage<NerveLayer>(new DamageTypeQuantity(DamageType.Crush, nerveDamage));
            }
            resultingPain = brain.ComputeAveragePain();
            Assert.GreaterOrEqual(resultingPain, 0);
            Assert.LessOrEqual(resultingPain, 1);
        }

        /// <summary>
        /// Test to confirm that interactions can only be commenced when stamina is greater than zero, and will otherwise fail.
        /// </summary>
        [Test]
        [TestCase(25f)]
        [TestCase(5f)]
        [TestCase(152f)]
        public void PainIsCorrectOnSingleBodyPartHurt(float damage)
        {
            leftFoot.TryInflictDamage<BoneLayer>(new DamageTypeQuantity(DamageType.Crush, damage));
            float painInBrain = brain.ComputeAveragePain();
            float painInFoot = ((INerveSignalTransmitter)leftFoot.GetBodyLayer<NerveLayer>()).ProducePain();
            Assert.AreEqual(painInBrain, painInFoot / BodyParts.Count, 0.00001f);
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
            float painInBrain = brain.ComputeAveragePain();
            float painInFoot = ((INerveSignalTransmitter)leftFoot.GetBodyLayer<NerveLayer>()).ProducePain();
            float painInTorso = ((INerveSignalTransmitter)torso.GetBodyLayer<NerveLayer>()).ProducePain();
            // that looks weird, but that's actually what is expected.
            // ProducePain takes into account the damages of nerve layers higher in the hierarchy.
            Assert.AreEqual(painInBrain, (painInFoot + painInTorso)/BodyParts.Count, 0.00001f);
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
