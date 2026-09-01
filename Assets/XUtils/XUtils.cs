using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShowX.Utils
{
    public class XUtils
    {
        public static T GetEnumByCode<T>(string code, T defaultValue)
        {
            try
            {
                return (T)Enum.Parse(typeof(T), code, true);
            }
            catch (ArgumentException)
            {
                return defaultValue;
            }
        }

        public static T Max<T>(Dictionary<T, int> keyValues)
        {
            T returnValue = default(T);

            int max = int.MinValue;
            foreach (T key in keyValues.Keys.Where(key => keyValues[key] > max))
            {
                max = keyValues[key];
                returnValue = key;
            }

            return returnValue;
        }


        public static T Min<T>(Dictionary<T, int> keyValues)
        {
            T returnValue = default(T);

            int min = int.MaxValue;
            foreach (T key in keyValues.Keys.Where(key => keyValues[key] < min))
            {
                min = keyValues[key];
                returnValue = key;
            }

            return returnValue;
        }

        public static void Append<T>(HashSet<T> srcValues, HashSet<T> appendValues)
        {
            foreach (T appendValue in appendValues)
            {
                srcValues.Add(appendValue);
            }
        }

        public static void Append<T>(HashSet<T> srcValues, List<T> appendValues)
        {
            foreach (T appendValue in appendValues)
            {
                srcValues.Add(appendValue);
            }
        }

        public static HashSet<K> FilterDicKeySet<K, V>(Dictionary<K, V> keyValues)
        {
            HashSet<K> returnValue = new HashSet<K>();

            foreach (K key in keyValues.Keys)
            {
                returnValue.Add(key);
            }

            return returnValue;
        }

        //
        public static StringBuilder ToString<K, T>(Dictionary<K, T> dictionary)
        {
            StringBuilder returnValue = null;

            if (dictionary != null)
            {
                returnValue = new StringBuilder();

                returnValue.Append("[");
                foreach (KeyValuePair<K, T> keyValuePair in dictionary)
                {
                    returnValue.Append("<");
                    returnValue.Append(keyValuePair.Key).Append(":").Append(keyValuePair.Value);
                    returnValue.Append(">, ");
                }
                returnValue.Append("]");
            }

            return returnValue;
        }

        public static StringBuilder ToString<T>(HashSet<T> values)
        {
            StringBuilder returnValue = null;

            if (values != null)
            {
                returnValue = new StringBuilder();

                returnValue.Append("[");
                int i = 0;
                foreach (T value in values)
                {
                    returnValue.Append(value);

                    if (i < values.Count - 1)
                    {
                        returnValue.Append(", ");
                    }

                    i++;
                }
                returnValue.Append("]");
            }

            return returnValue;
        }

        public static StringBuilder ToString<T>(T[] array)
        {
            StringBuilder returnValue = null;

            if (array != null)
            {
                returnValue = new StringBuilder();

                returnValue.Append("[");
                for (int i = 0; i < array.Length; i++)
                {
                    returnValue.Append(array[i]);

                    if (i < array.Length - 1)
                    {
                        returnValue.Append(", ");
                    }
                }
                returnValue.Append("]");
            }

            return returnValue;
        }

        public static StringBuilder ToString<T>(List<T> values)
        {
            StringBuilder returnValue = null;

            if (values != null)
            {
                returnValue = new StringBuilder();

                returnValue.Append("[");
                for (int i = 0; i < values.Count; i++)
                {
                    returnValue.Append(values[i]);

                    if (i < values.Count - 1)
                    {
                        returnValue.Append(", ");
                    }
                }
                returnValue.Append("]");
            }

            return returnValue;
        }

        public static StringBuilder ToString<T>(List<List<T>> valuesList)
        {
            StringBuilder returnValue = null;

            if (valuesList != null)
            {
                returnValue = new StringBuilder();

                returnValue.Append("[");
                for (int i = 0; i < valuesList.Count; i++)
                {
                    returnValue.Append(ToString(valuesList[i]));

                    if (i < valuesList.Count - 1)
                    {
                        returnValue.Append(", ");
                    }
                }

                returnValue.Append("]");
            }

            return returnValue;
        }

        public static StringBuilder ToString<T>(List<List<List<T>>> valuesLists)
        {
            StringBuilder returnValue = null;

            if (valuesLists != null)
            {
                returnValue = new StringBuilder();

                returnValue.Append("[");
                for (int i = 0; i < valuesLists.Count; i++)
                {
                    returnValue.Append(ToString(valuesLists[i]));

                    if (i < valuesLists.Count - 1)
                    {
                        returnValue.Append(", ");
                    }
                }
                returnValue.Append("]");
            }

            return returnValue;
        }

        public static List<T> JoinLists<T>(params List<T>[] lists)
        {
            return lists.SelectMany(list => list).ToList();
        }

        public static HashSet<T> ExceptHashSet<T>(HashSet<T> setA, HashSet<T> setB)
        {
            HashSet<T> returnValue = new HashSet<T>();

            foreach (T a in setA)
            {
                returnValue.Add(a);
            }

            foreach (T b in setB)
            {
                returnValue.Remove(b);
            }

            return returnValue;
        }

        public static HashSet<T> JoinHashSets<T>(params HashSet<T>[] sets)
        {
            HashSet<T> returnValue = new HashSet<T>();

            foreach (HashSet<T> set in sets)
            {
                foreach (T item in set)
                {
                    returnValue.Add(item);
                }

            }

            return returnValue;
        }
    }
}

