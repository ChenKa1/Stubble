﻿using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Stubble.Core.Classes;

namespace Stubble.Core
{
    public sealed class Context
    {
        private IDictionary<string, object> Cache { get; set; }
        private readonly Registry _registry;
        private readonly object _view;

        public Context ParentContext { get; set; }
        public dynamic View { get; set; }

        public Context(object view, Registry registry)
            : this(view, registry, null)
        {
        }

        public Context(object view, Registry registry, Context parentContext)
        {
            _view = view;
            View = _view;
            Cache = new Dictionary<string, object>()
            {
                {".", _view}
            };
            ParentContext = parentContext;
            _registry = registry;
        }

        public object Lookup(string name)
        {
            object value = null;
            if (Cache.ContainsKey(name))
            {
                value = Cache[name];
            }
            else
            {
                var context = this;
                bool lookupHit = false;
                while (context != null)
                {
                    if (name.IndexOf('.') > 0)
                    {
                        var names = name.Split('.');
                        value = context._view;
                        for (var i = 0; i < names.Length; i++)
                        {
                            var tempValue = GetValueFromRegistry(value, names[i]);
                            if (tempValue != null)
                            {
                                if (i == names.Length - 1)
                                    lookupHit = true;

                                value = tempValue;
                            }
                            else if (i > 0)
                            {
                                return null;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                    else if (context._view != null)
                    {
                        value = GetValueFromRegistry(context._view, name);
                        if (value != null)
                        {
                            lookupHit = true;
                        }
                    }

                    if (lookupHit) break;

                    context = context.ParentContext;
                }

                Cache[name] = value;
            }

            return value;
        }

        public Context Push(object view)
        {
            return new Context(view, _registry, this);
        }

        public object GetValueFromRegistry(object value, string key)
        {
            foreach (var entry in _registry.ValueGetters.Where(x => x.Key.IsInstanceOfType(value)))
            {
                var outputVal = entry.Value(value, key);
                if (outputVal != null) return outputVal;
            }
            return null;
        }

        public bool IsTruthyValue(object value)
        {
            if (value == null)
            {
                return false;
            }

            bool boolValue;
            var parseResult = bool.TryParse(value.ToString(), out boolValue) ? (bool?)boolValue : null;
            if (parseResult.HasValue || value is bool)
            {
                return parseResult ?? (bool)value;
            }

            var strValue = value as string;
            if (strValue != null)
            {
                return !string.IsNullOrEmpty(strValue);
            }

            var enumerableValue = value as IEnumerable;
            if (enumerableValue != null)
            {
                return enumerableValue.GetEnumerator().MoveNext();
            }

            return true;
        }
    }
}
