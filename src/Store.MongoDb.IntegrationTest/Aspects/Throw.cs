//----------------------------------------------------------------------- 
// PDS WITSMLstudio Store, 2018.3
//
// Copyright 2018 PDS Americas LLC
// 
// Licensed under the PDS Open Source WITSML Product License Agreement (the
// "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.pds.group/WITSMLstudio/OpenSource/ProductLicenseAgreement
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using log4net;
using NConcern;
using PDS.WITSMLstudio.Framework;

namespace PDS.WITSMLstudio.Store.Aspects
{
    public static class Throws
    {
        public static IContainer Container;
        public static Func<ThrowContext, bool> Predicate;

        public static IThrow<T> Using<T>(this IThrow<T> throws, IContainer container)
        {
            Container = container;
            return throws;
        }

        public static IThrow<T> When<T>(this IThrow<T> throws, Func<ThrowContext, bool> predicate)
        {
            Predicate = predicate;

            var type = Container?.Resolve<T>().GetType() ?? typeof(T);
            Aspect.Weave<Throw>(type);

            return throws;
        }

        public static IThrow<T> Before<T>(this IThrow<T> throws, string methodName, Func<ThrowContext, bool> predicate)
        {
            return throws.When(context => context.Method.Name == methodName && !context.IsAfter && predicate(context));
        }

        public static IThrow<T> Before<T>(this IThrow<T> throws, string methodName)
        {
            return throws.Before(methodName, contex => true);
        }

        public static IThrow<T> After<T>(this IThrow<T> throws, string methodName, Func<ThrowContext, bool> predicate)
        {
            return throws.When(context => context.Method.Name == methodName && context.IsAfter && predicate(context));
        }

        public static IThrow<T> After<T>(this IThrow<T> throws, string methodName)
        {
            return throws.After(methodName, contex => true);
        }

        public static IThrow<T> Reset<T>(this IThrow<T> throws)
        {
            Container = null;
            Predicate = null;
            return throws;
        }
    }

    public interface IThrow<T>
    {
    }

    public class Throw : IAspect
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(Throw));
        private readonly Func<ThrowContext, bool> _filter;

        public static IThrow<T> For<T>()
        {
            return default(IThrow<T>).Reset();
        }

        public Throw()
        {
            _filter = Throws.Predicate; // ?? (context => true);
        }

        public IEnumerable<IAdvice> Advise(MethodInfo method)
        {
            yield return Advice.Basic.Before((instance, args) =>
            {
                _log.Debug($"\nIntercepted before: {method.DeclaringType?.Name}.{method.Name}\n");
                if (!_filter(new ThrowContext(method, instance, args))) return;

                var message = $"Error raised before {method.DeclaringType?.Name}.{method.Name}";
                _log.Error(message + Environment.NewLine + new StackTrace());
                throw new Exception(message);
            });

            yield return Advice.Basic.After((instance, args) =>
            {
                _log.Debug($"\nIntercepted after: {method.DeclaringType?.Name}.{method.Name}\n");
                if (!_filter(new ThrowContext(method, instance, args, true))) return;

                var message = $"Error raised after {method.DeclaringType?.Name}.{method.Name}";
                _log.Error(message + Environment.NewLine + new StackTrace());
                throw new Exception(message);
            });
        }
    }

    public sealed class ThrowContext
    {
        public ThrowContext(MethodInfo method, object instance, object[] args, bool isAfter = false)
        {
            Method = method;
            Instance = instance;
            Args = args;
            IsAfter = isAfter;
        }

        public MethodInfo Method { get; }

        public object Instance { get; }

        public object[] Args { get; }

        public bool IsAfter { get; }
    }
}
