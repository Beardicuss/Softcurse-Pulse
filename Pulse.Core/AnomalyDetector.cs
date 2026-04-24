using System;
using System.Collections.Generic;
using System.Linq;

namespace Pulse.Core
{
    public class AnomalyDetector
    {
        private readonly Queue<double> _history = new Queue<double>();
        private readonly int _windowSize;
        private readonly double _multiplier;

        public AnomalyDetector(int windowSize = 30, double stdDevMultiplier = 2.0)
        {
            _windowSize = windowSize;
            _multiplier = stdDevMultiplier;
        }

        public bool IsAnomaly(double value)
        {
            if (_history.Count < _windowSize / 2)
            {
                _history.Enqueue(value);
                return false;
            }

            double avg = _history.Average();
            double sumOfSquaresOfDifferences = _history.Select(val => (val - avg) * (val - avg)).Sum();
            double stdDev = Math.Sqrt(sumOfSquaresOfDifferences / _history.Count);

            bool isAnomaly = value > avg + (_multiplier * stdDev);

            _history.Enqueue(value);
            if (_history.Count > _windowSize)
            {
                _history.Dequeue();
            }

            return isAnomaly;
        }
    }
}
