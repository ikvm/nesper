///////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2006-2015 Esper Team. All rights reserved.                           /
// http://esper.codehaus.org                                                          /
// ---------------------------------------------------------------------------------- /
// The software in this package is published under the terms of the GPL license       /
// a copy of which has been included with this distribution in the license.txt file.  /
///////////////////////////////////////////////////////////////////////////////////////

using System.Collections.Generic;

using com.espertech.esper.client;
using com.espertech.esper.collection;
using com.espertech.esper.compat.logging;
using com.espertech.esper.core.context.util;
using com.espertech.esper.epl.core;
using com.espertech.esper.epl.expression.core;
using com.espertech.esper.epl.spec;
using com.espertech.esper.events;
using com.espertech.esper.metrics.instrumentation;
using com.espertech.esper.util;

namespace com.espertech.esper.epl.view
{
	/// <summary>
	/// Handles output rate limiting for LAST and without order-by.
	/// </summary>
	public class OutputProcessViewConditionLastAllUnord : OutputProcessViewBaseWAfter
	{
	    private readonly OutputProcessViewConditionFactory _parent;
	    private readonly OutputCondition _outputCondition;
	    private readonly bool _isAll;

        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public OutputProcessViewConditionLastAllUnord(ResultSetProcessorHelperFactory resultSetProcessorHelperFactory, ResultSetProcessor resultSetProcessor, long? afterConditionTime, int? afterConditionNumberOfEvents, bool afterConditionSatisfied, OutputProcessViewConditionFactory parent, AgentInstanceContext agentInstanceContext)
            : base(resultSetProcessorHelperFactory, agentInstanceContext, resultSetProcessor, afterConditionTime, afterConditionNumberOfEvents, afterConditionSatisfied)
        {
	        _parent = parent;
	        _isAll = parent.OutputLimitLimitType == OutputLimitLimitType.ALL;

	        var outputCallback = GetCallbackToLocal(parent.StreamCount);
	        _outputCondition = parent.OutputConditionFactory.Make(agentInstanceContext, outputCallback);
	    }

	    public override int NumChangesetRows
	    {
	        get { return 0; }
	    }

	    public override OutputCondition OptionalOutputCondition
	    {
	        get { return _outputCondition; }
	    }

	    public override void Update(EventBean[] newData, EventBean[] oldData)
	    {
	        if ((ExecutionPathDebugLog.IsEnabled) && (Log.IsDebugEnabled))
	        {
	            Log.Debug(".update Received update, " +
	                    "  newData.length==" + ((newData == null) ? 0 : newData.Length) +
	                    "  oldData.length==" + ((oldData == null) ? 0 : oldData.Length));
	        }

	        bool isGenerateSynthetic = _parent.StatementResultService.IsMakeSynthetic;
	        base.ResultSetProcessor.ProcessOutputLimitedLastAllNonBufferedView(newData, oldData, isGenerateSynthetic, _isAll);

	        if (!base.CheckAfterCondition(newData, _parent.StatementContext))
	        {
	            if (InstrumentationHelper.ENABLED) { InstrumentationHelper.Get().AOutputProcessWCondition(false);}
	            return;
	        }

	        int newDataLength = 0;
	        int oldDataLength = 0;
	        if (newData != null) { newDataLength = newData.Length; }
	        if (oldData != null) { oldDataLength = oldData.Length; }

	        _outputCondition.UpdateOutputCondition(newDataLength, oldDataLength);
	    }

	    /// <summary>
	    /// This process (update) method is for participation in a join.
	    /// </summary>
	    /// <param name="newEvents">new events</param>
	    /// <param name="oldEvents">old events</param>
	    public override void Process(ISet<MultiKey<EventBean>> newEvents, ISet<MultiKey<EventBean>> oldEvents, ExprEvaluatorContext exprEvaluatorContext)
	    {
	        if ((ExecutionPathDebugLog.IsEnabled) && (Log.IsDebugEnabled))
	        {
	            Log.Debug(
                    ".process Received update, " +
	                "  newData.length==" + ((newEvents == null) ? 0 : newEvents.Count) +
	                "  oldData.length==" + ((oldEvents == null) ? 0 : oldEvents.Count));
	        }

	        bool isGenerateSynthetic = _parent.StatementResultService.IsMakeSynthetic;
	        base.ResultSetProcessor.ProcessOutputLimitedLastAllNonBufferedJoin(newEvents, oldEvents, isGenerateSynthetic, _isAll);

	        if (!base.CheckAfterCondition(newEvents, _parent.StatementContext))
	        {
	            if (InstrumentationHelper.ENABLED) { InstrumentationHelper.Get().AOutputProcessWCondition(false);}
	            return;
	        }

	        int newEventsSize = 0;
	        if (newEvents != null) {
	            newEventsSize = newEvents.Count;
	        }

	        int oldEventsSize = 0;
	        if (oldEvents != null) {
	            oldEventsSize = oldEvents.Count;
	        }
	        _outputCondition.UpdateOutputCondition(newEventsSize, oldEventsSize);
	    }

        /// <summary>
        /// Called once the output condition has been met.
        /// Invokes the result set processor.
        /// Used for non-join event data.
        /// </summary>
        /// <param name="doOutput">true if the batched events should actually be output as well as processed, false if they should just be processed</param>
        /// <param name="forceUpdate">true if output should be made even when no updating events have arrived</param>
        internal virtual void ContinueOutputProcessingView(bool doOutput, bool forceUpdate)
        {
            if ((ExecutionPathDebugLog.IsEnabled) && (Log.IsDebugEnabled))
            {
                Log.Debug(".continueOutputProcessingView");
            }

            var isGenerateSynthetic = _parent.StatementResultService.IsMakeSynthetic;
            var newOldEvents = ResultSetProcessor.ContinueOutputLimitedLastAllNonBufferedView(isGenerateSynthetic, _isAll);

            ContinueOutputProcessingViewAndJoin(doOutput, forceUpdate, newOldEvents);
        }

        internal virtual void Output(bool forceUpdate, UniformPair<EventBean[]> results)
        {
            // Child view can be null in replay from named window
            if (ChildView != null)
            {
                OutputStrategyUtil.Output(forceUpdate, results, ChildView);
            }
        }

		/// <summary>
		/// Called once the output condition has been met.
		/// Invokes the result set processor.
		/// Used for non-join event data.
		/// </summary>
		/// <param name="doOutput">true if the batched events should actually be output as well as processed, false if they should just be processed</param>
		/// <param name="forceUpdate">true if output should be made even when no updating events have arrived</param>
        internal virtual void ContinueOutputProcessingJoin(bool doOutput, bool forceUpdate)
		{
			if ((ExecutionPathDebugLog.IsEnabled) && (Log.IsDebugEnabled))
	        {
	            Log.Debug(".continueOutputProcessingJoin");
	        }

	        bool isGenerateSynthetic = _parent.StatementResultService.IsMakeSynthetic;
	        UniformPair<EventBean[]> newOldEvents = base.ResultSetProcessor.ContinueOutputLimitedLastAllNonBufferedJoin(isGenerateSynthetic, _isAll);

	        ContinueOutputProcessingViewAndJoin(doOutput, forceUpdate, newOldEvents);
		}

	    private OutputCallback GetCallbackToLocal(int streamCount)
	    {
	        // single stream means no join
	        // multiple streams means a join
	        if(streamCount == 1)
	        {
	            return (doOutput, forceUpdate) => 
	            {
                    ContinueOutputProcessingView(doOutput, forceUpdate);
	            };
	        }
	        else
	        {
	            return ContinueOutputProcessingJoin;
	        }
	    }

	    public override IEnumerator<EventBean> GetEnumerator()
        {
	        return OutputStrategyUtil.GetEnumerator(base.JoinExecutionStrategy, base.ResultSetProcessor, base.ParentView, _parent.IsDistinct);
	    }

	    public override void Terminated()
        {
	        if (_parent.IsTerminable) {
	            _outputCondition.Terminated();
	        }
	    }

	    private void ContinueOutputProcessingViewAndJoin(bool doOutput, bool forceUpdate, UniformPair<EventBean[]> newOldEvents)
        {

	        if (_parent.IsDistinct && newOldEvents != null)
	        {
	            newOldEvents.First = EventBeanUtility.GetDistinctByProp(newOldEvents.First, _parent.EventBeanReader);
	            newOldEvents.Second = EventBeanUtility.GetDistinctByProp(newOldEvents.Second, _parent.EventBeanReader);
	        }

	        if (doOutput) {
	            Output(forceUpdate, newOldEvents);
	        }

	        if (InstrumentationHelper.ENABLED) { InstrumentationHelper.Get().AOutputRateConditionOutputNow(true);}
	    }
	}
} // end of namespace
