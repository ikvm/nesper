///////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2006-2015 Esper Team. All rights reserved.                           /
// http://esper.codehaus.org                                                          /
// ---------------------------------------------------------------------------------- /
// The software in this package is published under the terms of the GPL license       /
// a copy of which has been included with this distribution in the license.txt file.  /
///////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Xml.XPath;

using com.espertech.esper.client;
using com.espertech.esper.compat.collections;
using com.espertech.esper.compat.logging;
using com.espertech.esper.epl.generated;
using com.espertech.esper.events.property;

namespace com.espertech.esper.events.xml
{
    public class SimpleXElementType : BaseXMLEventType
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IDictionary<String, EventPropertyGetter> _propertyGetterCache;
        private readonly string _defaultNamespacePrefix;
        private readonly bool _isResolvePropertiesAbsolute;

        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="eventTypeMetadata">event type metadata</param>
        /// <param name="eventTypeId">The event type id.</param>
        /// <param name="configurationEventTypeXMLDOM">configures the event type</param>
        /// <param name="eventAdapterService">for type looking and registration</param>
        public SimpleXElementType(EventTypeMetadata eventTypeMetadata,
                                  int eventTypeId,
                                  ConfigurationEventTypeXMLDOM configurationEventTypeXMLDOM,
                                  EventAdapterService eventAdapterService)
            : base(eventTypeMetadata, eventTypeId, configurationEventTypeXMLDOM, eventAdapterService)
        {
            _isResolvePropertiesAbsolute = configurationEventTypeXMLDOM.IsXPathResolvePropertiesAbsolute;

            // Set of namespace context for XPath expressions
            var namespaceContext = new XPathNamespaceContext();
            foreach (var entry in configurationEventTypeXMLDOM.NamespacePrefixes)
            {
                namespaceContext.AddNamespace(entry.Key, entry.Value);
            }

            if (configurationEventTypeXMLDOM.DefaultNamespace != null)
            {
                var defaultNamespace = configurationEventTypeXMLDOM.DefaultNamespace;
                namespaceContext.AddNamespace(String.Empty, defaultNamespace);

                // determine a default namespace prefix to use to construct XPath expressions from pure property names
                _defaultNamespacePrefix = null;
                foreach (var entry in configurationEventTypeXMLDOM.NamespacePrefixes)
                {
                    if (Equals(entry.Value, defaultNamespace))
                    {
                        _defaultNamespacePrefix = entry.Key;
                        break;
                    }
                }
            }

            NamespaceContext = namespaceContext;
            Initialize(
                configurationEventTypeXMLDOM.XPathProperties.Values,
                Collections.GetEmptyList<ExplicitPropertyDescriptor>());

            _propertyGetterCache = new Dictionary<String, EventPropertyGetter>();
        }

        protected override Type DoResolvePropertyType(String propertyExpression)
        {
            EsperEPL2GrammarParser.StartEventPropertyRuleContext ast = PropertyParser.Parse(propertyExpression);
            return PropertyParser.IsPropertyDynamic(ast)
                ? typeof(XNode)
                : typeof(string);
        }

        protected override EventPropertyGetter DoResolvePropertyGetter(String propertyExpression)
        {
            var getter = _propertyGetterCache.Get(propertyExpression);
            if (getter != null)
            {
                return getter;
            }

            if (!ConfigurationEventTypeXMLDOM.IsXPathPropertyExpr)
            {
                Property prop = PropertyParser.ParseAndWalk(propertyExpression);
                getter = prop.GetGetterDOM();
                if (!prop.IsDynamic)
                {
                    getter = new DOMConvertingGetter(propertyExpression, (DOMPropertyGetter)getter, typeof(string));
                }
            }
            else
            {
                try
                {
                    var ast = PropertyParser.Parse(propertyExpression);
                    var isDynamic = PropertyParser.IsPropertyDynamic(ast);
                    var xPathExpr = SimpleXMLPropertyParser.Walk(
                        ast,
                        propertyExpression,
                        RootElementName,
                        _defaultNamespacePrefix,
                        _isResolvePropertiesAbsolute);

                    if (Log.IsInfoEnabled)
                    {
                        Log.Info("Compiling XPath expression for property '" + propertyExpression + "' as '" + xPathExpr +
                                 "'");
                    }

                    var xPathExpression = XPathExpression.Compile(xPathExpr, NamespaceContext);
                    var xPathReturnType = isDynamic ? XPathResultType.Any : XPathResultType.String;
                    getter = new XPathPropertyGetter(
                        propertyExpression,
                        xPathExpr,
                        xPathExpression,
                        xPathReturnType,
                        null,
                        null);
                }
                catch (XPathException e)
                {
                    throw new EPException(
                        "Error constructing XPath expression from property name '" + propertyExpression + '\'', e);
                }
            }

            // no fragment factory, fragments not allowed
            _propertyGetterCache.Put(propertyExpression, getter);
            return getter;
        }

        protected override FragmentEventType DoResolveFragmentType(String property)
        {
            return null;
        }
    }
}
