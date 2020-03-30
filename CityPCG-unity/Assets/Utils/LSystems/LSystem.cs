using System;
using System.Collections.Generic;
using System.Linq;
using Random = UnityEngine.Random;


namespace Utils.LSystems {
    public class LSystem<T> {
        private readonly Dictionary<T, Rules> rules = new Dictionary<T, Rules>();

        public Rules CreateRules(T type) {
            if (rules.ContainsKey(type)) {
                throw new Exception("type " + type + " already exists");
            }

            var typeRules = new Rules();
            rules.Add(type, typeRules);

            return typeRules;
        }

        public List<T> Run(T axiom) {
            var result = new List<T> {axiom};
            while (rules[result.Last()].HasRules()) {
                result.Add(rules[result.Last()].Next());
            }

            return result;
        }

        public class Rules {
            private readonly List<Rule> rules = new List<Rule>();
            private float totalProbability = 0.0f;

            internal Rules() {

            }

            public Rules Add(float probability, T next) {
                rules.Add(new Rule(probability, next));
                totalProbability += probability;
                return this;
            }

            public T Next() {
                if (totalProbability.CompareTo(1) != 0) {
                    throw new Exception("probabilities doesn't add up to 1.0");
                }

                Rule? nextRule = null;
                var d = Random.Range(0.0f, 1.0f);
                var step = 0.0f;
                foreach (var currentRule in rules) {
                    step += currentRule.probability;

                    if (d <= step) {
                        nextRule = currentRule;
                        break;
                    }
                }

                if (!nextRule.HasValue) {
                    throw new Exception("next rule wasn't found");
                }

                return nextRule.Value.nextFloorType;
            }

            public bool HasRules() {
                return rules.Count > 0;
            }

            private struct Rule {
                public readonly float probability;
                public readonly T nextFloorType;

                public Rule(float probability, T nextFloorType) {
                    this.probability = probability;
                    this.nextFloorType = nextFloorType;
                }
            }
        }
    }
}
