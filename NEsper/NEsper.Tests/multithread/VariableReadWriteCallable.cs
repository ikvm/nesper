///////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2006-2015 Esper Team. All rights reserved.                           /
// http://esper.codehaus.org                                                          /
// ---------------------------------------------------------------------------------- /
// The software in this package is published under the terms of the GPL license       /
// a copy of which has been included with this distribution in the license.txt file.  /
///////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Threading;

using com.espertech.esper.client;
using com.espertech.esper.client.scopetest;
using com.espertech.esper.compat;
using com.espertech.esper.compat.logging;
using com.espertech.esper.support.bean;

using NUnit.Framework;

namespace com.espertech.esper.multithread
{
    public class VariableReadWriteCallable : ICallable<bool>
    {
        private readonly EPServiceProvider _engine;
        private readonly int _numRepeats;
        private readonly int _threadNum;
        private readonly SupportUpdateListener _selectListener;
    
        public VariableReadWriteCallable(int threadNum, EPServiceProvider engine, int numRepeats)
        {
            _engine = engine;
            _numRepeats = numRepeats;
            _threadNum = threadNum;
    
            _selectListener = new SupportUpdateListener();
            String stmtText = "select var1, var2, var3 from " + typeof(SupportBean_A).FullName + "(Id='" + threadNum + "')";
            engine.EPAdministrator.CreateEPL(stmtText).Events += _selectListener.Update;
        }

        public bool Call()
        {
            try
            {
                for (int loop = 0; loop < _numRepeats; loop++)
                {
                    long newValue = _threadNum * 1000000 + loop;
                    Object theEvent;
    
                    if (loop % 2 == 0)
                    {
                        theEvent = new SupportMarketDataBean("", 0, newValue, "");
                    }
                    else
                    {
                        SupportBean bean = new SupportBean();
                        bean.LongPrimitive = newValue;
                        theEvent = bean;
                    }
    
                    // Changes the variable values through either of the set-statements
                    _engine.EPRuntime.SendEvent(theEvent);
    
                    // Select the variable value back, another thread may have changed it, we are only
                    // determining if the set operation is atomic
                    _engine.EPRuntime.SendEvent(new SupportBean_A(Convert.ToString(_threadNum)));
                    EventBean received = _selectListener.AssertOneGetNewAndReset();
                    Assert.AreEqual(received.Get("var1"), received.Get("var2"));
                }
            }
            catch (AssertionException ex)
            {
                Log.Fatal("Assertion error in thread " + Thread.CurrentThread.ManagedThreadId, ex);
                return false;
            }
            catch (Exception ex)
            {
                Log.Fatal("Error in thread " + Thread.CurrentThread.ManagedThreadId, ex);
                return false;
            }
            return true;
        }
    
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    }
}
