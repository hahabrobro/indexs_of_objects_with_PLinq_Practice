using System;
using System.Linq;
using Ex = System.Linq.Expressions.Expression;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;

[assembly: CLSCompliant(true)]
namespace i4o.old
{

    public class IndexableCollection<T> : Collection<T>
    {
        /// <summary>
        /// Local cach of reflected properties of T
        /// </summary>
        private Dictionary<string, PropertyInfo> _propertyInfos = new Dictionary<string, PropertyInfo>();

        //this defines a dictionary of dictionaries of lists of some type we are being a collection of :)
        //the index is always the hash of whatever we are indexing.
        private Dictionary<string, Dictionary<int, List<T>>> _indexes = new Dictionary<string, Dictionary<int, List<T>>>();

        public IndexableCollection() :
            this(new List<T>())
        { }

        public IndexableCollection(IndexSpecification<T> indexSpecification)
            : this(new List<T>(), indexSpecification)
        { }

        public IndexableCollection(IEnumerable<T> items)
            : this(items, new IndexSpecification<T>())
        {
        }

        public IndexableCollection(IEnumerable<T> items, IndexSpecification<T> indexSpecification)
        {
            if (indexSpecification == null)
                throw new ArgumentNullException("indexSpecification");

            //TODO: should we validate items argument for null?

            UseIndexSpecification(indexSpecification);

            //TODO: what properties get returned here? We only want public properties...
            foreach (var property in typeof(T).GetProperties())
                _propertyInfos.Add(property.Name, property);

            foreach (var item in items)
                this.Add(item);
        }

        public IndexableCollection<T> CreateIndexFor<TParameter>(Expression<Func<T, TParameter>> propertyExpression)
        {
            var propertyName = propertyExpression.GetMemberName();

            return CreateIndexFor(propertyName); ;
        }

        public bool RemoveIndexFor<TParameter>(Expression<Func<T, TParameter>> propertyExpression)
        {
            var propertyName = propertyExpression.GetMemberName();

            if (_indexes.ContainsKey(propertyName))
                return _indexes.Remove(propertyName);

            return false;
        }

        public bool ContainsIndex<TParameter>(Expression<Func<T, TParameter>> propertyExpression)
        {
            return ContainsIndex(propertyExpression.GetMemberName());
        }

        public bool ContainsIndex(string propertyName)
        {
            return _indexes.ContainsKey(propertyName);
        }

        public Dictionary<int, List<T>> GetIndexByPropertyName(string propName)
        {
            return _indexes[propName];
        }

        public new void Add(T item)
        {
            foreach (string key in _indexes.Keys)
            {
                AddToIndex(key, item, _indexes[key]);
            }

            base.Add(item);
        }

        public new bool Remove(T item)
        {
            foreach (string key in _indexes.Keys)
            {
                var theProp = _propertyInfos[key];
                if (theProp != null)
                {
                    var itemValue = theProp.GetValue(item, null);
                    RemoveItem(item, key, itemValue);
                }
            }
            return base.Remove(item);
        }

        public IndexableCollection<T> UseIndexSpecification(IndexSpecification<T> indexSpecification)
        {
            if (indexSpecification == null)
                throw new ArgumentNullException("indexSpecification");

            foreach (string propertyName in indexSpecification.IndexedProperties)
            {
                this.CreateIndexFor(propertyName);
            }

            return this;
        }


        //TODO:
        // what about instead of 
        //      foreach index
        //          foreach item
        //              build up index
        //
        // we flip it and try something like
        //      foreach item
        //          foreach index
        //              build up index
        //
        // TOOD: just prototype & speed test the scenerio.
        // I have a feeling it won't be faster, in fact may be slower due to the creation of all the extra iterators
        //public IndexableCollection<T> UseIndexSpecificationX(IndexSpecification<T> indexSpec)
        //{
        //    IndexableCollection<T> oldIndex = this;

        //    IndexableCollection<T> newIndex = new IndexableCollection<T>();
        //    newIndex.UseIndexSpecification(indexSpec);

        //    for (int i = 0; i < oldIndex.Count; i++)
        //    {
        //        newIndex.Add(oldIndex[i]);
        //    }

        //    return newIndex;
        //}

        private IndexableCollection<T> CreateIndexFor(string propertyName)
        {
            var newIndex = new Dictionary<int, List<T>>();

            for (int i = 0; i < this.Count; i++)
            {
                AddToIndex(propertyName, this[i], newIndex);
            }

            _indexes.Add(propertyName, newIndex);

            return this;
        }


        private void AddToIndex(string propertyName, T newItem, Dictionary<int, List<T>> index)
        {
            var theProp = _propertyInfos[propertyName];
            var propertyValue = theProp.GetValue(newItem, null);
            if (propertyValue != null)
            {
                AddValueToIndex(newItem, index, propertyValue);
            }
        }

        private static void AddValueToIndex(T newItem, Dictionary<int, List<T>> index, object propertyValue)
        {
            int hashCode = propertyValue.GetHashCode();
            List<T> list;

            if (index.TryGetValue(hashCode, out list))
                list.Add(newItem);
            else
                index.Add(hashCode, new List<T> { newItem });
        }


        private void RemoveItem(T item, string key, object itemValue)
        {
            int hashCode = itemValue.GetHashCode();
            Dictionary<int, List<T>> index = _indexes[key];
            if (index.ContainsKey(hashCode))
                index[hashCode].Remove(item);
        }

    }
}
