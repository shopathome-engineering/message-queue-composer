using System;
using System.Collections.Generic;
using System.Linq;
using ShopAtHome.MessageQueue.Configuration;
using ShopAtHome.MessageQueue.Consumers.Configuration;
using ShopAtHome.MessageQueue.Statistics;

namespace ShopAtHome.MessageQueue.Composer
{
    internal static class Utilities
    {
        internal static List<List<string>> CreateOrderedWorkflows(IEnumerable<IWorkerConfiguration> configurations)
        {
            var workedQueues = configurations.ToList();
            // filter to ones that either specify a "next queue" or are themselves a target of a "next queue"
            var workFlowQueues = workedQueues.Where(x => !string.IsNullOrEmpty(x.NextQueue));
            var targetedQueues = workedQueues.Where(x => workFlowQueues.Any(y => y.NextQueue == x.SourceQueue)).ToList();
            var unorderedResults = workFlowQueues.Select(x => x.SourceQueue).Union(targetedQueues.Select(x => x.SourceQueue)).ToList();
            // now order them according to their place in the workflow
            var workflowHeads = unorderedResults.Where(x => !targetedQueues.Select(y => y.SourceQueue).Contains(x));
            var results = new List<List<string>>();
            foreach (var head in workflowHeads)
            {
                var thisWorkflow = new List<string>();
                var current = head;
                while (!string.IsNullOrEmpty(current))
                {
                    thisWorkflow.Add(current);
                    current = workedQueues.FirstOrDefault(y => y.SourceQueue == current)?.NextQueue;
                }
                results.Add(thisWorkflow);
            }
            return results;
        }

        internal static int GetNumWorkersToScaleWith(int numWorkersRequired, string scalingQueueIdentifier, DependencyInfo mostLimitedSystem, List<string> orderedWorkflowQueueIdentifiers)
        {
            // Scale according to the most limited system - if it only has 30% capacity, only create 30% of the requested workers
            var numNewWorkers = (int)Math.Ceiling(numWorkersRequired * (mostLimitedSystem?.PercentAvailableResources / 100d) ?? numWorkersRequired);

            // If a dependency has no resources we still want to create one worker
            if (numNewWorkers == 0) { numNewWorkers++; }
            
            // is this one of our critical workflow queues, and if so, what position is it in?
            var workflowPosition = orderedWorkflowQueueIdentifiers.IndexOf(scalingQueueIdentifier) + 1; // IndexOf is zero-based
            if (workflowPosition > 0)
            {
                // Reduce our scaling by the position of the queue - queues at the "back" get the most attention
                var scalingFactor = workflowPosition / (double)orderedWorkflowQueueIdentifiers.Count;
                numNewWorkers = (int)Math.Ceiling(numNewWorkers * scalingFactor);
            }
            else
            {
                // Harshly reduce our scaling as this is a less important part of the workflow
                numNewWorkers = (int)Math.Ceiling(numNewWorkers * .1);
            }

            return numNewWorkers;
        }

        internal static int GetNumWorkersRequiredToClearQueue(IQueueConfiguration configuration, IProcessorStatisticsProvider statisticsProvider, int messageCount)
        {
            // Since we're storing message resolution time in floating point, if we're faster than 100 ns, we get zero - not good
            var meanMessageResolutionTime = Math.Max(statisticsProvider.GetQueueStatistics(configuration.QueueIdentifier).MeanMessageResolutionTime, 0.0000001);
            // units: seconds / (worker * seconds / message)
            var numMessagesResolvedInTargetTimePerWorker = configuration.TargetTimeToClearQueue / meanMessageResolutionTime;
            // Take the ceiling here to err on the side of faster dequeuing and to ensure that we act on very slow queues with low message counts
            var numWorkersRequired = (int)Math.Ceiling(messageCount / numMessagesResolvedInTargetTimePerWorker);
            // Never create more workers than there are messages
            return numWorkersRequired > messageCount ? messageCount : numWorkersRequired;
        }
    }
}
