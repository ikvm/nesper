///////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2006-2015 Esper Team. All rights reserved.                           /
// http://esper.codehaus.org                                                          /
// ---------------------------------------------------------------------------------- /
// The software in this package is published under the terms of the GPL license       /
// a copy of which has been included with this distribution in the license.txt file.  /
///////////////////////////////////////////////////////////////////////////////////////

using System;

using com.espertech.esper.client;
using com.espertech.esper.client.scopetest;
using com.espertech.esper.client.soda;
using com.espertech.esper.compat.collections;
using com.espertech.esper.metrics.instrumentation;
using com.espertech.esper.support.client;
using com.espertech.esper.util;

using NUnit.Framework;

namespace com.espertech.esper.regression.rowrecog
{
    [TestFixture]
    public class TestRowPatternRecognitionAfter
    {
        private void RunAssertion(EPServiceProvider epService, SupportUpdateListener listener, EPStatement stmt)
        {
            String[] fields = "a,b0,b1".Split(',');

            epService.EPRuntime.SendEvent(new SupportRecogBean("A1", 1));
            EPAssertionUtil.AssertPropsPerRow(
                listener.GetAndResetLastNewData(),
                fields, new Object[][]
                {
                        new Object[]
                        {
                            "A1", null, null
                        }
                });

            EPAssertionUtil.AssertPropsPerRow(
                stmt.GetEnumerator(), fields,
                new Object[][]
                {
                        new Object[]
                        {
                            "A1", null, null
                        }
                });

            // since the first match skipped past A, we do not match again
            epService.EPRuntime.SendEvent(new SupportRecogBean("B1", 2));
            EPAssertionUtil.AssertPropsPerRow(
                listener.GetAndResetLastNewData(),
                fields, new Object[][]
                {
                        new Object[]
                        {
                            "A1", "B1", null
                        }
                });

            EPAssertionUtil.AssertPropsPerRow(
                stmt.GetEnumerator(), fields,
                new Object[][]
                {
                        new Object[]
                        {
                            "A1", "B1", null
                        }
                });
        }

        [Test]
        public void TestAfterCurrentRow()
        {
            Configuration config = SupportConfigFactory.GetConfiguration();

            config.AddEventType("MyEvent", typeof (SupportRecogBean));
            EPServiceProvider epService = EPServiceProviderManager.GetDefaultProvider(
                config);

            epService.Initialize();
            if (InstrumentationHelper.ENABLED) { InstrumentationHelper.StartTest(epService, GetType(), GetType().FullName); }

            String text = "select * from MyEvent.win:keepall() "
                          + "match_recognize ("
                          + " measures A.TheString as a, B[0].TheString as b0, B[1].TheString as b1"
                          + " after match skip to current row" + " pattern (A B*)"
                          + " define" + " A as A.TheString like \"A%\","
                          + " B as B.TheString like \"B%\"" + ")";

            EPStatement stmt = epService.EPAdministrator.CreateEPL(text);
            var listener = new SupportUpdateListener();

            stmt.Events += listener.Update;

            RunAssertion(epService, listener, stmt);

            stmt.Dispose();
            EPStatementObjectModel model = epService.EPAdministrator.CompileEPL(
                text);

            SerializableObjectCopier.Copy(model);
            Assert.AreEqual(text, model.ToEPL());
            stmt = epService.EPAdministrator.Create(model);
            stmt.Events += listener.Update;
            Assert.AreEqual(text, stmt.Text);

            RunAssertion(epService, listener, stmt);
        
            if (InstrumentationHelper.ENABLED) { InstrumentationHelper.EndTest(); }
        }

        [Test]
        public void TestAfterNextRow()
        {
            Configuration config = SupportConfigFactory.GetConfiguration();

            config.AddEventType("MyEvent", typeof (SupportRecogBean));
            EPServiceProvider epService = EPServiceProviderManager.GetDefaultProvider(
                config);

            epService.Initialize();
            if (InstrumentationHelper.ENABLED) { InstrumentationHelper.StartTest(epService, GetType(), GetType().FullName); }

            String[] fields = "a,b0,b1".Split(',');
            String text = "select * from MyEvent.win:keepall() "
                          + "match_recognize ("
                          + "  measures A.TheString as a, B[0].TheString as b0, B[1].TheString as b1"
                          + "  AFTER MATCH SKIP TO NEXT ROW " + "  pattern (A B*) "
                          + "  define " + "    A as A.TheString like 'A%',"
                          + "    B as B.TheString like 'B%'" + ")";

            EPStatement stmt = epService.EPAdministrator.CreateEPL(text);
            var listener = new SupportUpdateListener();

            stmt.Events += listener.Update;

            epService.EPRuntime.SendEvent(new SupportRecogBean("A1", 1));
            EPAssertionUtil.AssertPropsPerRow(
                listener.GetAndResetLastNewData(),
                fields, new Object[][]
                {
                        new Object[]
                        {
                            "A1", null, null
                        }
                });

            EPAssertionUtil.AssertPropsPerRow(
                stmt.GetEnumerator(), fields,
                new Object[][]
                {
                        new Object[]
                        {
                            "A1", null, null
                        }
                });

            // since the first match skipped past A, we do not match again
            epService.EPRuntime.SendEvent(new SupportRecogBean("B1", 2));
            Assert.IsFalse(listener.IsInvoked); // incremental skips to next 
            EPAssertionUtil.AssertPropsPerRow(
                stmt.GetEnumerator(), fields,
                new Object[][]
                {
                        new Object[]
                        {
                            "A1", "B1", null
                        }
                });

            if (InstrumentationHelper.ENABLED) { InstrumentationHelper.EndTest(); }
        }

        [Test]
        public void TestAfterSkipPastLast()
        {
            Configuration config = SupportConfigFactory.GetConfiguration();

            config.AddEventType("MyEvent", typeof (SupportRecogBean));
            EPServiceProvider epService = EPServiceProviderManager.GetDefaultProvider(config);

            epService.Initialize();
            if (InstrumentationHelper.ENABLED) { InstrumentationHelper.StartTest(epService, GetType(), GetType().FullName); }

            String[] fields = "a_string,b_string".Split(',');
            String text = "select * from MyEvent.win:keepall() "
                          + "match_recognize ("
                          + "  measures A.TheString as a_string, B.TheString as b_string "
                          + "  all matches " + "  after match skip past last row"
                          + "  pattern (A B) " + "  define B as B.value > A.value" + ") "
                          + "order by a_string, b_string";

            EPStatement stmt = epService.EPAdministrator.CreateEPL(text);
            var listener = new SupportUpdateListener();

            stmt.Events += listener.Update;

            epService.EPRuntime.SendEvent(new SupportRecogBean("E1", 5));
            epService.EPRuntime.SendEvent(new SupportRecogBean("E2", 3));
            Assert.IsFalse(listener.IsInvoked);
            Assert.IsFalse(stmt.HasFirst());

            epService.EPRuntime.SendEvent(new SupportRecogBean("E3", 6));
            EPAssertionUtil.AssertPropsPerRow(
                listener.GetAndResetLastNewData(),
                fields, new Object[][]
                {
                        new Object[]
                        {
                            "E2", "E3"
                        }
                });
            EPAssertionUtil.AssertPropsPerRow(
                stmt.GetEnumerator(), fields,
                new Object[][]
                {
                        new Object[]
                        {
                            "E2", "E3"
                        }
                });

            epService.EPRuntime.SendEvent(new SupportRecogBean("E4", 4));
            Assert.IsFalse(listener.IsInvoked);
            EPAssertionUtil.AssertPropsPerRow(
                stmt.GetEnumerator(), fields,
                new Object[][]
                {
                        new Object[]
                        {
                            "E2", "E3"
                        }
                });

            epService.EPRuntime.SendEvent(new SupportRecogBean("E5", 6));
            EPAssertionUtil.AssertPropsPerRow(
                listener.GetAndResetLastNewData(),
                fields, new Object[][]
                {
                        new Object[]
                        {
                            "E4", "E5"
                        }
                });
            EPAssertionUtil.AssertPropsPerRow(
                stmt.GetEnumerator(), fields,
                new Object[][]
                {
                        new Object[]
                        {
                            "E2", "E3"
                        }
                        ,
                        new Object[]
                        {
                            "E4", "E5"
                        }
                });

            epService.EPRuntime.SendEvent(new SupportRecogBean("E6", 10));
            Assert.IsFalse(listener.IsInvoked); // E5-E6 not a match since "skip past last row"
            EPAssertionUtil.AssertPropsPerRow(
                stmt.GetEnumerator(), fields,
                new Object[][]
                {
                        new Object[]
                        {
                            "E2", "E3"
                        },
                        new Object[]
                        {
                            "E4", "E5"
                        }
                });

            epService.EPRuntime.SendEvent(new SupportRecogBean("E7", 9));
            epService.EPRuntime.SendEvent(new SupportRecogBean("E8", 4));
            Assert.IsFalse(listener.IsInvoked);
            EPAssertionUtil.AssertPropsPerRow(
                stmt.GetEnumerator(), fields,
                new Object[][]
                {
                        new Object[]
                        {
                            "E2", "E3"
                        },
                        new Object[]
                        {
                            "E4", "E5"
                        }
                });

            stmt.Stop();
        
            if (InstrumentationHelper.ENABLED) { InstrumentationHelper.EndTest(); }
        }

        [Test]
        public void TestSkipToNextRow()
        {
            Configuration config = SupportConfigFactory.GetConfiguration();

            config.AddEventType("MyEvent", typeof (SupportRecogBean));
            EPServiceProvider epService = EPServiceProviderManager.GetDefaultProvider(
                config);

            epService.Initialize();
            if (InstrumentationHelper.ENABLED) { InstrumentationHelper.StartTest(epService, GetType(), GetType().FullName); }

            String[] fields = "a_string,b_string".Split(',');
            String text = "select * from MyEvent.win:keepall() "
                          + "match_recognize ("
                          + "  measures A.TheString as a_string, B.TheString as b_string "
                          + "  all matches " + "  after match skip to next row "
                          + "  pattern (A B) " + "  define B as B.value > A.value" + ") "
                          + "order by a_string, b_string";

            EPStatement stmt = epService.EPAdministrator.CreateEPL(text);
            var listener = new SupportUpdateListener();

            stmt.Events += listener.Update;

            epService.EPRuntime.SendEvent(new SupportRecogBean("E1", 5));
            epService.EPRuntime.SendEvent(new SupportRecogBean("E2", 3));
            Assert.IsFalse(listener.IsInvoked);
            Assert.IsFalse(stmt.HasFirst());

            epService.EPRuntime.SendEvent(new SupportRecogBean("E3", 6));
            EPAssertionUtil.AssertPropsPerRow(
                listener.GetAndResetLastNewData(),
                fields, new Object[][]
                {
                        new Object[]
                        {
                            "E2", "E3"
                        }
                });
            EPAssertionUtil.AssertPropsPerRow(
                stmt.GetEnumerator(), fields,
                new Object[][]
                {
                        new Object[]
                        {
                            "E2", "E3"
                        }
                });

            epService.EPRuntime.SendEvent(new SupportRecogBean("E4", 4));
            Assert.IsFalse(listener.IsInvoked);
            EPAssertionUtil.AssertPropsPerRow(
                stmt.GetEnumerator(), fields,
                new Object[][]
                {
                        new Object[]
                        {
                            "E2", "E3"
                        }
                });

            epService.EPRuntime.SendEvent(new SupportRecogBean("E5", 6));
            EPAssertionUtil.AssertPropsPerRow(
                listener.GetAndResetLastNewData(),
                fields, new Object[][]
                {
                        new Object[]
                        {
                            "E4", "E5"
                        }
                });
            EPAssertionUtil.AssertPropsPerRow(
                stmt.GetEnumerator(), fields,
                new Object[][]
                {
                        new Object[]
                        {
                            "E2", "E3"
                        },
                        new Object[]
                        {
                            "E4", "E5"
                        }
                });

            epService.EPRuntime.SendEvent(new SupportRecogBean("E6", 10));
            EPAssertionUtil.AssertPropsPerRow(
                listener.GetAndResetLastNewData(),
                fields, new Object[][]
                {
                        new Object[]
                        {
                            "E5", "E6"
                        }
                });
            EPAssertionUtil.AssertPropsPerRow(
                stmt.GetEnumerator(), fields,
                new Object[][]
                {
                        new Object[]
                        {
                            "E2", "E3"
                        },
                        new Object[]
                        {
                            "E4", "E5"
                        },
                        new Object[]
                        {
                            "E5", "E6"
                        }
                });

            epService.EPRuntime.SendEvent(new SupportRecogBean("E7", 9));
            epService.EPRuntime.SendEvent(new SupportRecogBean("E8", 4));
            Assert.IsFalse(listener.IsInvoked);
            EPAssertionUtil.AssertPropsPerRow(
                stmt.GetEnumerator(), fields,
                new Object[][]
                {
                        new Object[]
                        {
                            "E2", "E3"
                        },
                        new Object[]
                        {
                            "E4", "E5"
                        },
                        new Object[]
                        {
                            "E5", "E6"
                        }
                });

            stmt.Stop();

            if (InstrumentationHelper.ENABLED) { InstrumentationHelper.EndTest(); }
        }

        [Test]
        public void TestSkipToNextRowPartitioned()
        {
            Configuration config = SupportConfigFactory.GetConfiguration();

            config.AddEventType("MyEvent", typeof (SupportRecogBean));
            EPServiceProvider epService = EPServiceProviderManager.GetDefaultProvider(
                config);

            epService.Initialize();
            if (InstrumentationHelper.ENABLED) { InstrumentationHelper.StartTest(epService, GetType(), GetType().FullName); }

            String[] fields = "a_string,a_value,b_value".Split(',');
            String text = "select * from MyEvent.win:keepall() "
                          + "match_recognize (" + "  partition by TheString"
                          + "  measures A.TheString as a_string, A.value as a_value, B.value as b_value "
                          + "  all matches " + "  after match skip to next row "
                          + "  pattern (A B) " + "  define B as (B.value > A.value)" + ")"
                          + " order by a_string";

            EPStatement stmt = epService.EPAdministrator.CreateEPL(text);
            var listener = new SupportUpdateListener();

            stmt.Events += listener.Update;

            epService.EPRuntime.SendEvent(new SupportRecogBean("S1", 5));
            epService.EPRuntime.SendEvent(new SupportRecogBean("S2", 6));
            epService.EPRuntime.SendEvent(new SupportRecogBean("S3", 3));
            epService.EPRuntime.SendEvent(new SupportRecogBean("S4", 4));
            epService.EPRuntime.SendEvent(new SupportRecogBean("S1", 5));
            epService.EPRuntime.SendEvent(new SupportRecogBean("S2", 5));
            epService.EPRuntime.SendEvent(new SupportRecogBean("S1", 4));
            epService.EPRuntime.SendEvent(new SupportRecogBean("S4", -1));
            Assert.IsFalse(listener.IsInvoked);
            Assert.IsFalse(stmt.HasFirst());

            epService.EPRuntime.SendEvent(new SupportRecogBean("S1", 6));
            EPAssertionUtil.AssertPropsPerRow(
                listener.GetAndResetLastNewData(),
                fields, new Object[][]
                {
                        new Object[]
                        {
                            "S1", 4, 6
                        }
                });
            EPAssertionUtil.AssertPropsPerRow(
                stmt.GetEnumerator(), fields,
                new Object[][]
                {
                        new Object[]
                        {
                            "S1", 4, 6
                        }
                });

            epService.EPRuntime.SendEvent(new SupportRecogBean("S4", 10));
            EPAssertionUtil.AssertPropsPerRow(
                listener.GetAndResetLastNewData(),
                fields, new Object[][]
                {
                        new Object[]
                        {
                            "S4", -1, 10
                        }
                });
            EPAssertionUtil.AssertPropsPerRow(
                stmt.GetEnumerator(), fields,
                new Object[][]
                {
                        new Object[]
                        {
                            "S1", 4, 6
                        },
                        new Object[]
                        {
                            "S4", -1, 10
                        }
                });

            epService.EPRuntime.SendEvent(new SupportRecogBean("S4", 11));
            EPAssertionUtil.AssertPropsPerRow(
                listener.GetAndResetLastNewData(),
                fields, new Object[][]
                {
                        new Object[]
                        {
                            "S4", 10, 11
                        }
                });
            EPAssertionUtil.AssertPropsPerRow(
                stmt.GetEnumerator(), fields,
                new Object[][]
                {
                        new Object[]
                        {
                            "S1", 4, 6
                        },
                        new Object[]
                        {
                            "S4", -1, 10
                        },
                        new Object[]
                        {
                            "S4", 10, 11
                        }
                });

            epService.EPRuntime.SendEvent(new SupportRecogBean("S3", 3));
            epService.EPRuntime.SendEvent(new SupportRecogBean("S4", -1));
            epService.EPRuntime.SendEvent(new SupportRecogBean("S3", 2));
            epService.EPRuntime.SendEvent(new SupportRecogBean("S1", 4));
            Assert.IsFalse(listener.IsInvoked);
            EPAssertionUtil.AssertPropsPerRow(
                stmt.GetEnumerator(), fields,
                new Object[][]
                {
                        new Object[]
                        {
                            "S1", 4, 6
                        },
                        new Object[]
                        {
                            "S4", -1, 10
                        },
                        new Object[]
                        {
                            "S4", 10, 11
                        }
                });

            epService.EPRuntime.SendEvent(new SupportRecogBean("S1", 7));
            EPAssertionUtil.AssertPropsPerRow(
                listener.GetAndResetLastNewData(),
                fields, new Object[][]
                {
                        new Object[]
                        {
                            "S1", 4, 7
                        }
                });
            EPAssertionUtil.AssertPropsPerRow(
                stmt.GetEnumerator(), fields,
                new Object[][]
                {
                        new Object[]
                        {
                            "S1", 4, 6
                        },
                        new Object[]
                        {
                            "S1", 4, 7
                        },
                        new Object[]
                        {
                            "S4", -1, 10
                        },
                        new Object[]
                        {
                            "S4", 10, 11
                        }
                });

            epService.EPRuntime.SendEvent(new SupportRecogBean("S4", 12));
            EPAssertionUtil.AssertPropsPerRow(
                listener.GetAndResetLastNewData(),
                fields, new Object[][]
                {
                        new Object[]
                        {
                            "S4", -1, 12
                        }
                });
            EPAssertionUtil.AssertPropsPerRow(
                stmt.GetEnumerator(), fields,
                new Object[][]
                {
                        new Object[]
                        {
                            "S1", 4, 6
                        },
                        new Object[]
                        {
                            "S1", 4, 7
                        },
                        new Object[]
                        {
                            "S4", -1, 10
                        },
                        new Object[]
                        {
                            "S4", 10, 11
                        },
                        new Object[]
                        {
                            "S4", -1, 12
                        }
                });

            epService.EPRuntime.SendEvent(new SupportRecogBean("S4", 12));
            epService.EPRuntime.SendEvent(new SupportRecogBean("S1", 7));
            epService.EPRuntime.SendEvent(new SupportRecogBean("S2", 4));
            epService.EPRuntime.SendEvent(new SupportRecogBean("S1", 5));
            Assert.IsFalse(listener.IsInvoked);

            epService.EPRuntime.SendEvent(new SupportRecogBean("S2", 5));
            EPAssertionUtil.AssertPropsPerRow(
                listener.GetAndResetLastNewData(),
                fields, new Object[][]
                {
                        new Object[]
                        {
                            "S2", 4, 5
                        }
                });
            EPAssertionUtil.AssertPropsPerRow(
                stmt.GetEnumerator(), fields,
                new Object[][]
                {
                        new Object[]
                        {
                            "S1", 4, 6
                        },
                        new Object[]
                        {
                            "S1", 4, 7
                        },
                        new Object[]
                        {
                            "S2", 4, 5
                        },
                        new Object[]
                        {
                            "S4", -1, 10
                        },
                        new Object[]
                        {
                            "S4", 10, 11
                        },
                        new Object[]
                        {
                            "S4", -1, 12
                        }
                });

            stmt.Dispose();
        
            if (InstrumentationHelper.ENABLED) { InstrumentationHelper.EndTest(); }
        }

        [Test]
        public void TestVariableMoreThenOnce()
        {
            Configuration config = SupportConfigFactory.GetConfiguration();

            config.AddEventType("MyEvent", typeof (SupportRecogBean));
            EPServiceProvider epService = EPServiceProviderManager.GetDefaultProvider(
                config);

            epService.Initialize();
            if (InstrumentationHelper.ENABLED) { InstrumentationHelper.StartTest(epService, GetType(), GetType().FullName); }

            String[] fields = "a0,b,a1".Split(',');
            String text = "select * from MyEvent.win:keepall() "
                          + "match_recognize ("
                          + "  measures A[0].TheString as a0, B.TheString as b, A[1].TheString as a1 "
                          + "  all matches " + "  after match skip to next row "
                          + "  pattern ( A B A ) " + "  define "
                          + "    A as (A.value = 1)," + "    B as (B.value = 2)" + ")";

            EPStatement stmt = epService.EPAdministrator.CreateEPL(text);
            var listener = new SupportUpdateListener();

            stmt.Events += listener.Update;

            epService.EPRuntime.SendEvent(new SupportRecogBean("E1", 3));
            epService.EPRuntime.SendEvent(new SupportRecogBean("E2", 1));
            epService.EPRuntime.SendEvent(new SupportRecogBean("E3", 2));
            epService.EPRuntime.SendEvent(new SupportRecogBean("E4", 5));
            epService.EPRuntime.SendEvent(new SupportRecogBean("E5", 1));
            epService.EPRuntime.SendEvent(new SupportRecogBean("E6", 2));
            Assert.IsFalse(listener.IsInvoked);
            Assert.IsFalse(stmt.HasFirst());

            epService.EPRuntime.SendEvent(new SupportRecogBean("E7", 1));
            EPAssertionUtil.AssertPropsPerRow(
                listener.GetAndResetLastNewData(),
                fields, new Object[][]
                {
                        new Object[]
                        {
                            "E5", "E6", "E7"
                        }
                });
            EPAssertionUtil.AssertPropsPerRow(
                stmt.GetEnumerator(), fields,
                new Object[][]
                {
                        new Object[]
                        {
                            "E5", "E6", "E7"
                        }
                });

            epService.EPRuntime.SendEvent(new SupportRecogBean("E8", 2));
            epService.EPRuntime.SendEvent(new SupportRecogBean("E9", 1));
            EPAssertionUtil.AssertPropsPerRow(
                listener.GetAndResetLastNewData(),
                fields, new Object[][]
                {
                        new Object[]
                        {
                            "E7", "E8", "E9"
                        }
                });
            EPAssertionUtil.AssertPropsPerRow(
                stmt.GetEnumerator(), fields,
                new Object[][]
                {
                        new Object[]
                        {
                            "E5", "E6", "E7"
                        },
                        new Object[]
                        {
                            "E7", "E8", "E9"
                        }
                });

            if (InstrumentationHelper.ENABLED) { InstrumentationHelper.EndTest(); }
        }
    }
}
