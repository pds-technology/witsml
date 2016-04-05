//----------------------------------------------------------------------- 
// PDS.Witsml, 2016.1
//
// Copyright 2016 Petrotechnical Data Systems
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Energistics.DataAccess;
using Newtonsoft.Json;
using Ast = LinqExtender.Ast;

namespace PDS.Witsml.Client.Linq
{
    /// <summary>
    /// Default context to be queried.
    /// </summary>
    /// <typeparam name="T">Target type</typeparam>
    /// <typeparam name="V">List type</typeparam>
    public class WitsmlQuery<T, V> : ExpressionVisitor, IWitsmlQuery<T>, LinqExtender.IQueryContext<T> where V : IEnergisticsCollection
    {
        /// <summary>
        /// WitsmlQuery
        /// </summary>
        /// <param name="context"></param>
        public WitsmlQuery(WitsmlContext context)
        {
            Context = context;
            Query = WITSMLWebServiceConnection.BuildEmptyQuery<V>();
            Queryable = LinqExtender.Queryable.Select(this, x => x);
            Options = new Dictionary<string, string>();

            // update Version property modifed by BuildEmptyQuery
            Query.SetVersion(context.DataSchemaVersion);
        }

        /// <summary>
        /// WitsmlContext
        /// </summary>
        public WitsmlContext Context { get; private set; }

        /// <summary>
        /// Query
        /// </summary>
        public V Query { get; private set; }

        /// <summary>
        /// Options
        /// </summary>
        public Dictionary<string, string> Options { get; private set; }

        /// <summary>
        /// Invoked during execution of the query , with the
        /// pre populated expression tree.
        /// </summary>
        /// <param name="expression">Target expression block</param>
        /// <returns>Expected result</returns>
        public IEnumerable<T> Execute(Ast.Expression expression)
        {
            this.Visit(expression);
#if DEBUG
            Console.WriteLine();
            Console.WriteLine("Executing query...  OptionsIn: {0}{1}", JsonConvert.SerializeObject(Options), Environment.NewLine);
            Console.WriteLine(EnergisticsConverter.ObjectToXml(Query));
            Console.WriteLine();
#endif
            var result = Context.Connection.Read<V>(Query, Options);

            return (IEnumerable<T>)result.Items;
        }

        /// <summary>
        /// Provides a callback that can be used to include specific elements in the query response.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public IWitsmlQuery<T> Include(Action<T> action)
        {
            action((T)Query.Items[0]);
            return this;
        }

        /// <summary>
        /// Sets the options that will be passed in to the GetFromStore query.
        /// </summary>
        /// <param name="optionsIn"></param>
        /// <returns></returns>
        public IWitsmlQuery<T> With(OptionsIn optionsIn)
        {
            Options[optionsIn.Key] = optionsIn.Value;
            return this;
        }

        protected override void SetPropertyValue(PropertyInfo property, object value)
        {
            property.SetValue(Query.Items[0], value);
        }

        #region IQueryable<T> Members

        private System.Linq.IQueryable<T> Queryable;

        public Expression Expression
        {
            get { return Queryable.Expression; }
        }

        public Type ElementType
        {
            get { return Queryable.ElementType; }
        }

        public System.Linq.IQueryProvider Provider
        {
            get { return Queryable.Provider; }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return Queryable.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Queryable.GetEnumerator();
        }

        #endregion
    }
}
