using UnityEngine;
using System.Collections.Generic;
public class LSystem {
    public static void ProductionRules(List<char> chars, int iterations) {
        while (iterations > 0) {
            List<char> result = new List<char>();
            foreach (char c in chars) {
                if (c == 'A') {
                    result.Add('A');
                    result.Add('B');
                }
                if (c == 'B') {
                    result.Add('B');
                    result.Add('A');
                }
            }
            var myString = new string(result.ToArray());
            Debug.Log(myString);
            iterations -= 1;
            ProductionRules(result, iterations);
        }
    }
}
