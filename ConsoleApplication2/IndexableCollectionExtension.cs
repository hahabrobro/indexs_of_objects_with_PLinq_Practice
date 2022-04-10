using System;
using System.Linq;
using Ex = System.Linq.Expressions.Expression;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;

namespace i4o.old
{
    public static class IndexableCollectionExtension
    {
        public static IndexableCollection<T> ToIndexableCollection<T>(this IEnumerable<T> enumerable)
        {
            return new IndexableCollection<T>(enumerable);
        }

        public static IndexableCollection<T> ToIndexableCollection<T>(this IEnumerable<T> enumerable, IndexSpecification<T> indexSpecification)
            where T : class
        {
            return new IndexableCollection<T>(enumerable)
                .UseIndexSpecification(indexSpecification);
        }


        public static IEnumerable<TResult> Join<T, TInner, TKey, TResult>(
            this IndexableCollection<T> outer,
            IndexableCollection<TInner> inner,
            Expression<Func<T, TKey>> outerKeySelector,
            Expression<Func<TInner, TKey>> innerKeySelector,
            Func<T, TInner, TResult> resultSelector,
            IEqualityComparer<TKey> comparer)
        {
            if (outer == null || inner == null || outerKeySelector == null || innerKeySelector == null || resultSelector == null)
                throw new ArgumentNullException();

            bool haveIndex = false;
            if (innerKeySelector.NodeType == ExpressionType.Lambda
              && innerKeySelector.Body.NodeType == ExpressionType.MemberAccess
              && outerKeySelector.NodeType == ExpressionType.Lambda
              && outerKeySelector.Body.NodeType == ExpressionType.MemberAccess)
            {
                MemberExpression membExpInner = (MemberExpression)innerKeySelector.Body;
                MemberExpression membExpOuter = (MemberExpression)outerKeySelector.Body;
                Dictionary<int, List<TInner>> innerIndex = new Dictionary<int, List<TInner>>();
                Dictionary<int, List<T>> outerIndex = new Dictionary<int, List<T>>();


                if (inner.ContainsIndex(membExpInner.Member.Name)
                  && outer.ContainsIndex(membExpOuter.Member.Name))
                {
                    innerIndex = inner.GetIndexByPropertyName(membExpInner.Member.Name);
                    outerIndex = outer.GetIndexByPropertyName(membExpOuter.Member.Name);
                    haveIndex = true;
                }

                if (haveIndex)
                    foreach (int outerKey in outerIndex.Keys)
                    {
                        List<T> outerGroup = outerIndex[outerKey];
                        List<TInner> innerGroup;
                        if (innerIndex.TryGetValue(outerKey, out innerGroup))
                        {
                            //do a join on the GROUPS based on key result
                            IEnumerable<TInner> innerEnum = innerGroup.AsEnumerable<TInner>();
                            IEnumerable<T> outerEnum = outerGroup.AsEnumerable<T>();
                            IEnumerable<TResult> result = outerEnum.Join<T, TInner, TKey, TResult>(innerEnum, outerKeySelector.Compile(), innerKeySelector.Compile(), resultSelector, comparer);
                            foreach (TResult resultItem in result)
                                yield return resultItem;
                        }
                    }

            }
            if (!haveIndex)
            {
                //this will happen if we don't have keys in the right places
                IEnumerable<TInner> innerEnum = inner.AsEnumerable<TInner>();
                IEnumerable<T> outerEnum = outer.AsEnumerable<T>();
                IEnumerable<TResult> result = outerEnum.Join<T, TInner, TKey, TResult>(innerEnum, outerKeySelector.Compile(), innerKeySelector.Compile(), resultSelector, comparer);
                foreach (TResult resultItem in result)
                    yield return resultItem;
            }
        }


        public static IEnumerable<TResult> Join<T, TInner, TKey, TResult>(
            this IndexableCollection<T> outer,
            IndexableCollection<TInner> inner,
            Expression<Func<T, TKey>> outerKeySelector,
            Expression<Func<TInner, TKey>> innerKeySelector,
            Func<T, TInner, TResult> resultSelector)
        {
            return outer.Join<T, TInner, TKey, TResult>(inner, outerKeySelector, innerKeySelector, resultSelector, EqualityComparer<TKey>.Default);
        }

        private static bool HasIndexablePropertyOnLeft<T>(Expression leftSide, IndexableCollection<T> sourceCollection, out MemberExpression theMember)
        {
            theMember = null;
            MemberExpression mex = leftSide as MemberExpression;
            if (leftSide.NodeType == ExpressionType.Call)
            {
                var call = leftSide as System.Linq.Expressions.MethodCallExpression;
                if (call.Method.Name == "CompareString")
                {
                    mex = call.Arguments[0] as MemberExpression;
                }
            }

            if (mex == null) return false;

            theMember = mex;
            return sourceCollection.ContainsIndex(((MemberExpression)mex).Member.Name);

        }


        private static int? GetHashRight(Expression leftSide, Expression rightSide)
        {
            if (leftSide.NodeType == ExpressionType.Call)
            {
                var call = leftSide as System.Linq.Expressions.MethodCallExpression;
                if (call.Method.Name == "CompareString")
                {
                    LambdaExpression evalRight = Ex.Lambda(call.Arguments[1], null);
                    //Compile it, invoke it, and get the resulting hash
                    return (evalRight.Compile().DynamicInvoke(null).GetHashCode());
                }
            }
            //rightside is where we get our hash...
            switch (rightSide.NodeType)
            {
                //shortcut constants, dont eval, will be faster
                case ExpressionType.Constant:
                    ConstantExpression constExp
                      = (ConstantExpression)rightSide;
                    return (constExp.Value.GetHashCode());

                //if not constant (which is provably terminal in a tree), convert back to Lambda and eval to get the hash.
                default:
                    //Lambdas can be created from expressions... yay
                    LambdaExpression evalRight = Ex.Lambda(rightSide, null);
                    //Compile that mutherf-ker, invoke it, and get the resulting hash
                    return (evalRight.Compile().DynamicInvoke(null).GetHashCode());
            }
        }

        //extend the where when we are working with indexable collections! 
        public static IEnumerable<T> Where<T>
        (
          this IndexableCollection<T> sourceCollection,
          Expression<Func<T, bool>> expr
        )
        {
            //our indexes work from the hash values of that which is indexed, regardless of type
            int? hashRight = null;
            bool noIndex = true;
            //indexes only work on equality expressions here
            if (expr.Body.NodeType == ExpressionType.Equal)
            {
                //Equality is a binary expression
                BinaryExpression binExp = (BinaryExpression)expr.Body;
                //Get some aliases for either side
                Expression leftSide = binExp.Left;
                Expression rightSide = binExp.Right;

                hashRight = GetHashRight(leftSide, rightSide);

                //if we were able to create a hash from the right side (likely)
                MemberExpression returnedEx = null;
                if (hashRight.HasValue && HasIndexablePropertyOnLeft<T>(leftSide, sourceCollection, out returnedEx))
                {
                    //cast to MemberExpression - it allows us to get the property
                    MemberExpression propExp = (MemberExpression)returnedEx;
                    string property = propExp.Member.Name;
                    Dictionary<int, List<T>> myIndex =
                      sourceCollection.GetIndexByPropertyName(property);
                    if (myIndex.ContainsKey(hashRight.Value))
                    {
                        IEnumerable<T> sourceEnum = myIndex[hashRight.Value].AsEnumerable<T>();
                        IEnumerable<T> result = sourceEnum.Where<T>(expr.Compile());
                        foreach (T item in result)
                            yield return item;
                    }
                    noIndex = false; //we found an index, whether it had values or not is another matter
                }

            }
            if (noIndex) //no index?  just do it the normal slow way then...
            {
                IEnumerable<T> sourceEnum = sourceCollection.AsEnumerable<T>();
                IEnumerable<T> result = sourceEnum.Where<T>(expr.Compile());
                foreach (T resultItem in result)
                    yield return resultItem;
            }

        }
    }
}
