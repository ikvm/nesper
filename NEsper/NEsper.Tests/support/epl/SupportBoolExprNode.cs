///////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2006-2015 Esper Team. All rights reserved.                           /
// http://esper.codehaus.org                                                          /
// ---------------------------------------------------------------------------------- /
// The software in this package is published under the terms of the GPL license       /
// a copy of which has been included with this distribution in the license.txt file.  /
///////////////////////////////////////////////////////////////////////////////////////

using System;
using System.IO;

using com.espertech.esper.compat;
using com.espertech.esper.epl.expression;
using com.espertech.esper.epl.expression.core;

namespace com.espertech.esper.support.epl
{
    [Serializable]
    public class SupportBoolExprNode : ExprNodeBase, ExprEvaluator
    {
        private readonly bool _evaluateResult;
    
        public SupportBoolExprNode(bool evaluateResult)
        {
            _evaluateResult = evaluateResult;
        }

        public override ExprEvaluator ExprEvaluator
        {
            get { return this; }
        }

        public override ExprNode Validate(ExprValidationContext validationContext)
        {
            return null;
        }

        public Type ReturnType
        {
            get { return typeof (Boolean); }
        }

        public override bool IsConstantResult
        {
            get { return false; }
        }

        public object Evaluate(EvaluateParams evaluateParams)
        {
            return _evaluateResult;
        }

        public override void ToPrecedenceFreeEPL(TextWriter writer)
        {
        }

        public override ExprPrecedenceEnum Precedence
        {
            get { return ExprPrecedenceEnum.UNARY; }
        }

        public override bool EqualsNode(ExprNode node)
        {
            throw new UnsupportedOperationException("not implemented");
        }
    }
}
