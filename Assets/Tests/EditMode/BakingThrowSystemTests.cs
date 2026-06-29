using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GWBGameJam
{
    public class BakingThrowSystemTests
    {
        private readonly List<Object> _createdObjects = new();

        [SetUp]
        public void SetUp()
        {
            LogAssert.ignoreFailingMessages = true;
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = _createdObjects.Count - 1; i >= 0; i--)
            {
                if (_createdObjects[i] != null)
                    Object.DestroyImmediate(_createdObjects[i]);
            }

            _createdObjects.Clear();
            LogAssert.ignoreFailingMessages = false;
        }

        [Test]
        public void ThrowRequestCarriesBakingState()
        {
            var request = new OnThrowRequested(3, BakingState.Cooked);

            Assert.AreEqual(3, request.LaneIndex);
            Assert.AreEqual(BakingState.Cooked, request.BakingState);
        }

        [Test]
        public void BakingSystemThrowsToCurrentHoveredLaneAtThrowTime()
        {
            var laneManager = CreateComponent<LaneManager>("LaneManager");
            var bakingSystem = CreateComponent<BakingSystem>("BakingSystem");

            SetPrivateField(laneManager, "_hoveredLaneIndex", 2);
            SetPrivateField(bakingSystem, "_laneManager", laneManager);

            bool received = false;
            OnThrowRequested capturedRequest = default;
            void Capture(OnThrowRequested e)
            {
                received = true;
                capturedRequest = e;
            }

            EventBus<OnThrowRequested>.Subscribe(Capture);
            try
            {
                InvokePrivate(bakingSystem, "StartBaking");
                SetPrivateField(laneManager, "_hoveredLaneIndex", 4);
                InvokePrivate(bakingSystem, "TransitionTo", BakingState.Cooked);
                InvokePrivate(bakingSystem, "TriggerThrow");
            }
            finally
            {
                EventBus<OnThrowRequested>.Unsubscribe(Capture);
            }

            Assert.IsTrue(received);
            Assert.AreEqual(4, capturedRequest.LaneIndex);
            Assert.AreEqual(BakingState.Cooked, capturedRequest.BakingState);
        }

        [Test]
        public void CookedBreadWithMatchingRatioHitsMonster()
        {
            var boundaryConfig = CreateAsset<DoughStateBoundaryConfig>();
            var throwSystem = CreateConfiguredThrowSystem(boundaryConfig, BakingState.Cooked, boundaryConfig.GetCenterRatio(DoughState.Medium));
            var monster = CreateMonster(DoughState.Medium);

            var result = (ThrowResult)InvokePrivate(throwSystem, "DetermineResult", monster);

            Assert.AreEqual(ThrowResult.Hit, result);
        }

        [TestCase(BakingState.Undercooked)]
        [TestCase(BakingState.Burnt)]
        public void NonCookedBreadCannotHitEvenWithMatchingRatio(BakingState bakingState)
        {
            var boundaryConfig = CreateAsset<DoughStateBoundaryConfig>();
            var throwSystem = CreateConfiguredThrowSystem(boundaryConfig, bakingState, boundaryConfig.GetCenterRatio(DoughState.Medium));
            var monster = CreateMonster(DoughState.Medium);

            var result = (ThrowResult)InvokePrivate(throwSystem, "DetermineResult", monster);

            Assert.AreEqual(ThrowResult.WrongRatio, result);
        }

        [Test]
        public void CookedBreadWithWrongRatioDoesNotHitMonster()
        {
            var boundaryConfig = CreateAsset<DoughStateBoundaryConfig>();
            float wrongRatio = boundaryConfig.GetCenterRatio(DoughState.Medium) + boundaryConfig.ToleranceHalfWidth + 0.01f;
            var throwSystem = CreateConfiguredThrowSystem(boundaryConfig, BakingState.Cooked, wrongRatio);
            var monster = CreateMonster(DoughState.Medium);

            var result = (ThrowResult)InvokePrivate(throwSystem, "DetermineResult", monster);

            Assert.AreEqual(ThrowResult.WrongRatio, result);
        }

        [Test]
        public void EmptyLaneStaysEmptyRegardlessOfBakingState()
        {
            var boundaryConfig = CreateAsset<DoughStateBoundaryConfig>();
            var throwSystem = CreateConfiguredThrowSystem(boundaryConfig, BakingState.Burnt, boundaryConfig.GetCenterRatio(DoughState.Medium));

            var result = (ThrowResult)InvokePrivate(throwSystem, "DetermineResult", (object)null);

            Assert.AreEqual(ThrowResult.EmptyLane, result);
        }

        private ThrowSystem CreateConfiguredThrowSystem(DoughStateBoundaryConfig boundaryConfig, BakingState bakingState, float ratio)
        {
            var throwSystem = CreateComponent<ThrowSystem>("ThrowSystem");
            SetPrivateField(throwSystem, "_boundaryConfig", boundaryConfig);
            SetPrivateField(throwSystem, "_capturedBakingState", bakingState);
            SetPrivateField(throwSystem, "_capturedRatio", ratio);
            return throwSystem;
        }

        private MonsterController CreateMonster(DoughState targetState)
        {
            var data = CreateAsset<MonsterData>();
            SetPrivateField(data, "_targetDoughState", targetState);

            var monster = CreateComponent<MonsterController>("Monster");
            SetPrivateField(monster, "<Data>k__BackingField", data);
            return monster;
        }

        private T CreateComponent<T>(string name) where T : Component
        {
            var gameObject = new GameObject(name);
            _createdObjects.Add(gameObject);
            return gameObject.AddComponent<T>();
        }

        private T CreateAsset<T>() where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            _createdObjects.Add(asset);
            return asset;
        }

        private static object InvokePrivate(object target, string methodName, params object[] args)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            MethodInfo method = target.GetType().GetMethod(methodName, flags);
            Assert.IsNotNull(method, $"Missing private method {methodName}");
            args ??= new object[] { null };
            return method.Invoke(target, args);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            FieldInfo field = target.GetType().GetField(fieldName, flags);
            Assert.IsNotNull(field, $"Missing private field {fieldName}");
            field.SetValue(target, value);
        }
    }
}
