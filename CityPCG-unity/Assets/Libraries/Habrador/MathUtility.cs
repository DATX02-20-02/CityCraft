using UnityEngine;

namespace Habrador {
    public class MathUtility {
        // Clamp list indices
        // Will even work if index is larger/smaller than listSize, so can loop multiple times
        public static int ClampListIndex(int index, int listSize) {
            index = ((index % listSize) + listSize) % listSize;

            return index;
        }
    }
}
