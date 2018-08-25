//----------------------------------------------------------------------- 
// PDS WITSMLstudio Core, 2018.3
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
using System.Reflection;
using LinqExtender;
using Ast = LinqExtender.Ast;


namespace PDS.WITSMLstudio.Linq
{
    /// <summary>
    /// Manages the evaluation of an <see cref="Ast.Expression"/>
    /// </summary>
    public class ExpressionVisitor
    {
        /// <summary>
        /// Visits the specified expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns>A type specific expression visitation.</returns>
        /// <exception cref="System.ArgumentException">Expression type is not supported</exception>
        internal Ast.Expression Visit(Ast.Expression expression)
        {
            switch (expression.CodeType)
            {
                case CodeType.BlockExpression:
                    return VisitBlockExpression((Ast.BlockExpression)expression);
                case CodeType.TypeExpression:
                    return VisitTypeExpression((Ast.TypeExpression)expression);
                case CodeType.LambdaExpresion:
                    return VisitLambdaExpression((Ast.LambdaExpression)expression);
                case CodeType.LogicalExpression:
                    return VisitLogicalExpression((Ast.LogicalExpression)expression);
                case CodeType.BinaryExpression:
                    return VisitBinaryExpression((Ast.BinaryExpression)expression);
                case CodeType.LiteralExpression:
                    return VisitLiteralExpression((Ast.LiteralExpression)expression);
                case CodeType.MemberExpression:
                    return VisitMemberExpression((Ast.MemberExpression)expression);
                case CodeType.OrderbyExpression:
                    return VisitOrderbyExpression((Ast.OrderbyExpression)expression);
            }

            throw new ArgumentException("Expression type is not supported");
        }

        /// <summary>
        /// Visits a <see cref="Ast.TypeExpression"/>
        /// </summary>
        /// <param name="typeExpression">The type expression.</param>
        /// <returns>An <see cref="Ast.Expression"/></returns>
        public virtual Ast.Expression VisitTypeExpression(Ast.TypeExpression typeExpression)
        {
            return typeExpression;
        }

        /// <summary>
        /// Visits a <see cref="Ast.BlockExpression"/>
        /// </summary>
        /// <param name="blockExpression">The block expression.</param>
        /// <returns>An <see cref="Ast.Expression"/></returns>
        public virtual Ast.Expression VisitBlockExpression(Ast.BlockExpression blockExpression)
        {
            foreach (var expression in blockExpression.Expressions)
                this.Visit(expression);

            return blockExpression;
        }

        /// <summary>
        /// Visits a <see cref="Ast.LogicalExpression"/>
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns>An <see cref="Ast.Expression"/></returns>
        public virtual Ast.Expression VisitLogicalExpression(Ast.LogicalExpression expression)
        {
            this.Visit(expression.Left);
            this.Visit(expression.Right);
            return expression;
        }

        /// <summary>
        /// Visits a <see cref="Ast.LambdaExpression"/>
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns>An <see cref="Ast.Expression"/></returns>
        public virtual Ast.Expression VisitLambdaExpression(Ast.LambdaExpression expression)
        {
            if (expression.Body != null)
                return this.Visit(expression.Body);
            return expression;
        }

        /// <summary>
        /// Visits a <see cref="Ast.BinaryExpression"/>
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns>An <see cref="Ast.Expression"/></returns>
        public virtual Ast.Expression VisitBinaryExpression(Ast.BinaryExpression expression)
        {
            var left = expression.Left as Ast.MemberExpression;
            var right = expression.Right as Ast.LiteralExpression;

            if (left != null && expression.Operator == BinaryOperator.Equal && right != null)
            {
                var property = left.Member.MemberInfo as PropertyInfo;

                if (property != null)
                {
                    SetPropertyValue(property, right.Value);
                }
            }

            this.Visit(expression.Left);
            this.Visit(expression.Right);

            return expression;
        }

        /// <summary>
        /// Visits a <see cref="Ast.MemberExpression"/>
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns>An <see cref="Ast.Expression"/></returns>
        public virtual Ast.Expression VisitMemberExpression(Ast.MemberExpression expression)
        {
            return expression;
        }

        /// <summary>
        /// Visits a <see cref="Ast.LiteralExpression"/>
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns>An <see cref="Ast.Expression"/></returns>
        public virtual Ast.Expression VisitLiteralExpression(Ast.LiteralExpression expression)
        {
            return expression;
        }

        /// <summary>
        /// Visits a <see cref="Ast.OrderbyExpression"/>
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns>An <see cref="Ast.Expression"/></returns>
        public virtual Ast.Expression VisitOrderbyExpression(Ast.OrderbyExpression expression)
        {
            return expression;
        }

        /// <summary>
        /// Sets the property value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        protected virtual void SetPropertyValue(PropertyInfo property, object value)
        {
        }
    }
}
