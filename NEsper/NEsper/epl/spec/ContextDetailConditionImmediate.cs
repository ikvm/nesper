///////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2006-2015 Esper Team. All rights reserved.                           /
// http://esper.codehaus.org                                                          /
// ---------------------------------------------------------------------------------- /
// The software in this package is published under the terms of the GPL license       /
// a copy of which has been included with this distribution in the license.txt file.  /
///////////////////////////////////////////////////////////////////////////////////////

using System.Collections.Generic;

using com.espertech.esper.compat.collections;
using com.espertech.esper.filter;

namespace com.espertech.esper.epl.spec
{
    public class ContextDetailConditionImmediate : ContextDetailCondition
    {
        public IList<FilterSpecCompiled> FilterSpecIfAny
        {
            get { return Collections.GetEmptyList<FilterSpecCompiled>(); }
        }
    }
}
