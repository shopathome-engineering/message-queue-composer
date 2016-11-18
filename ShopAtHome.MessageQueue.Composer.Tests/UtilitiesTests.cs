using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShopAtHome.MessageQueue.Configuration;
using ShopAtHome.MessageQueue.Statistics;
using ShopAtHome.UnitTestHelper;
using Telerik.JustMock;

namespace ShopAtHome.MessageQueue.Composer.Tests
{
    [TestClass]
    public class UtilitiesTests
    {
        private IProcessorStatisticsProvider _statisticsProvider;

        [TestInitialize]
        public void Init()
        {
            _statisticsProvider = Stubs.BareMock<IProcessorStatisticsProvider>();
        }

        [TestMethod]
        public void CreateOrderedWorkflow_GivenUnorderedData_CreatesProperlyOrderedList()
        {
            var data = new List<PocoWorkerConfiguration>
            {
                new PocoWorkerConfiguration
                {
                    SourceQueue = "110%PurchaseProcessing"
                },
                new PocoWorkerConfiguration
                {
                    SourceQueue = "ErrorLogging"
                },
                new PocoWorkerConfiguration
                {
                    SourceQueue = "AffiliateFileImport",
                    NextQueue = "TransactionHeaderProcessor"
                },
                new PocoWorkerConfiguration
                {
                    SourceQueue = "NoncriticalLogging"
                },
                new PocoWorkerConfiguration
                {
                    SourceQueue = "TransactionHeaderProcessor",
                    NextQueue = "PromotionProcessing"
                },
                new PocoWorkerConfiguration
                {
                    SourceQueue = "PromotionProcessing"
                }
            };

            var results = Utilities.CreateOrderedWorkflows(data);

            var firstWorkflow = results.First();
            firstWorkflow.Count.Should().Be(3); // AFI, THP, PP
            firstWorkflow.First().Should().Be("AffiliateFileImport");
            firstWorkflow.Last().Should().Be("PromotionProcessing");
        }

        [TestMethod]
        public void CreateOrderedWorkflow_GivenTwoWorkflows_CreatesProperlyOrderedList()
        {
            var data = new List<PocoWorkerConfiguration>
            {
                new PocoWorkerConfiguration
                {
                    SourceQueue = "A",
                    NextQueue = "B"
                },
                new PocoWorkerConfiguration
                {
                    SourceQueue = "B"
                },
                new PocoWorkerConfiguration
                {
                    SourceQueue = "C",
                    NextQueue = "D"
                },
                new PocoWorkerConfiguration
                {
                    SourceQueue = "E"
                },
                new PocoWorkerConfiguration
                {
                    SourceQueue = "D",
                    NextQueue = "F"
                },
                new PocoWorkerConfiguration
                {
                    SourceQueue = "F"
                }
            };

            var results = Utilities.CreateOrderedWorkflows(data);

            var firstWorkflow = results.First();
            firstWorkflow.Count.Should().Be(2); // A, B
            firstWorkflow.First().Should().Be("A");
            firstWorkflow.Last().Should().Be("B");

            var secondWorkflow = results.Last();
            secondWorkflow.Count.Should().Be(3); //  C, D, F
            secondWorkflow.First().Should().Be("C");
            secondWorkflow.Last().Should().Be("F");
        }

        [TestMethod]
        public void GetNumWorkersToScaleWith_GivenNoDependencyAndNoWorkflow_ScalesByATenth()
        {
            var numWorkersRequired = 10;
            var expectedScalingFactor = 0.1;
            var result = Utilities.GetNumWorkersToScaleWith(numWorkersRequired, "whatever", null, new List<string>());
            result.Should().Be((int)(numWorkersRequired * expectedScalingFactor));
        }

        [TestMethod]
        public void GetNumWorkersToScaleWith_GivenDependencyWithHalfCapacityAndNoWorkflow_ScalesByDependencyValuesAndATenth()
        {
            var numWorkersRequired = 100;
            var expectedScalingFactor = 0.1;
            var dependency = new DependencyInfo(true, 50);

            var result = Utilities.GetNumWorkersToScaleWith(numWorkersRequired, "whatever", dependency, new List<string>());

            result.Should().Be((int)(numWorkersRequired * expectedScalingFactor * (dependency.PercentAvailableResources * .01)));
        }

        [TestMethod]
        public void GetNumWorkersToScaleWith_GivenNoDependencyAndPlaceInAWorkflow_ScalesByWorkflowPosition()
        {
            var numWorkersRequired = 100;
            var workflow = new List<string> {"A", "B", "C"};

            var result = Utilities.GetNumWorkersToScaleWith(numWorkersRequired, "B", null, workflow);
            result.Should().Be(67); // 2/3rds of 100
        }

        [TestMethod]
        public void GetNumWorkersToScaleWith_GivenDependencyWithHalfCapacityAndPlaceInAWorkflow_ScalesByDependencyValuesAndWorkflowPosition()
        {
            var numWorkersRequired = 100;
            var workflow = new List<string> { "A", "B", "C" };
            var dependency = new DependencyInfo(true, 50);

            var result = Utilities.GetNumWorkersToScaleWith(numWorkersRequired, "B", dependency, workflow);
            result.Should().Be(34);
        }

        [TestMethod]
        public void GetNumWorkersRequiredToClearQueue_GivenMoreWorkersRequiredThanMessages_CapsToMessageCount()
        {
            var configuration = Stubs.BareMock<IQueueConfiguration>();
            Mock.Arrange(configuration, c => c.TargetTimeToClearQueue).Returns(1); // Require a super short clear time
            // Super long resolution time + super short clear time = need many workers
            Mock.Arrange(_statisticsProvider, s => s.GetQueueStatistics(Arg.AnyString)).Returns(new QueueStatistics {MessageResolutionTimes = new List<float> {1000}});
            var result = Utilities.GetNumWorkersRequiredToClearQueue(configuration, _statisticsProvider, 1);
            result.Should().Be(1);
        }
    }
}
