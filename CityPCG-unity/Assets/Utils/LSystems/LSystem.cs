using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;


namespace Utils.LSystems {
    public class LSystem<T, U> where U : class {
        private readonly Dictionary<T, Rules> rules;
        private Predicate<U> shouldContinue;

        public LSystem() {
            rules = new Dictionary<T, Rules>();
            shouldContinue = u => true;
        }

        public Rules CreateRules(T type) {
            if (rules.ContainsKey(type)) {
                throw new Exception("type " + type + " already exists");
            }

            var typeRules = new Rules();
            rules.Add(type, typeRules);

            return typeRules;
        }

        public void ShouldContinue(Predicate<U> shouldContinue) {
            this.shouldContinue = shouldContinue;
        }

        //TODO Validate that all values has a rule
        public List<T> Run(T axiom, U value) {
            return Run(new List<T>() {axiom}, value);
        }

        public List<T> Run(List<T> axioms, U value) {
            var result = axioms;

            //TODO: Break out result.Last()
            while (rules[result.Last()].HasRules()) {
                var next = rules[result.Last()].Next(value);
                if (next == null) {
                    break;
                }

                //TODO: Should not cast here. Problem is that .Next need to return null or something like that
                result.Add((T) next);
                value = rules[result.Last()].update(value);

                if (!shouldContinue(value)) {
                    break;
                }
            }

            return result;
        }

        public class Rules {
            internal Func<U, U> update;
            private Predicate<U> shouldAccept;

            private readonly List<Rule> rules = new List<Rule>();
            private float totalProbability = 0.0f;

            internal Rules() {
                update = u => u;
                shouldAccept = u => true;
            }

            public Rules Add(float probability, T next) {
                rules.Add(new Rule(probability, next));
                totalProbability += probability;
                return this;
            }

            public Rules ShouldAccept(Predicate<U> shouldAccept) {
                this.shouldAccept = shouldAccept;
                return this;
            }

            public Rules OnAccepted(Func<U, U> update) {
                this.update = update;
                return this;
            }

            internal object Next(U value) {
                if (totalProbability.CompareTo(1) != 0) {
                    throw new Exception("probabilities doesn't add up to 1.0");
                }

                //TODO: Optimize this. No need for a loop.
                Rule? nextRule = null;
                var d = Random.Range(0.0f, 1.0f);
                var step = 0.0f;
                foreach (var currentRule in rules) {
                    step += currentRule.probability;

                    //TODO: Don't run accepted every time.
                    //TODO: If an accepted is false, it will always take the next one in line.
                    if (d <= step && shouldAccept(value)) {
                        nextRule = currentRule;
                        break;
                    }
                }

                if (!nextRule.HasValue) {
                    return null;
                }

                return nextRule.Value.nextType;
            }

            public bool HasRules() {
                return rules.Count > 0;
            }

            private struct Rule {
                public readonly float probability;
                public readonly T nextType;

                public Rule(float probability, T nextType) {
                    this.probability = probability;
                    this.nextType = nextType;
                }
            }
        }
    }
}
