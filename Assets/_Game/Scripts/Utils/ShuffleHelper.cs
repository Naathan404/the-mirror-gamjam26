using System.Collections.Generic;
using UnityEngine;

namespace Game.Utils
{
    public static class ShuffleHelper
    {
        public static List<T> Shuffle<T>(IEnumerable<T> source)
        {
            var list = new List<T>(source);
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
            return list;
        }

    }
}