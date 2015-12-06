using System;

namespace RethinkDb.Driver.Net.Clustering
{
    // Structs implementing this interface are used to convert the average response time for a host
    // into a score that can be used to weight hosts in the epsilon greedy hostpool. Lower response
    // times should yield higher scores (we want to select the faster hosts more often) The default
    // LinearEpsilonValueCalculator just uses the reciprocal of the response time. In practice, any
    // decreasing function from the positive reals to the positive reals should work.
    public abstract class EpsilonValueCalculator
    {
        public abstract double CalcValueFromAvgResponseTime(double v);

        public static double LinearEpsilonValueCalculator(double v)
        {
            return 1.0 / v;
        }
        public static double LogEpsilonValueCalculator(double v)
        {
            return LinearEpsilonValueCalculator(Math.Log(v + 1.0));
        }

        public static double PolynomialEpsilonValueCalculator(double v, double exp)
        {
            return LinearEpsilonValueCalculator(Math.Pow(v, exp));
        }
    }


    public class LinearEpsilonValueCalculator : EpsilonValueCalculator
    {
        public override double CalcValueFromAvgResponseTime(double v)
        {
            return LinearEpsilonValueCalculator(v);
        }
    }

    public class LogEpsilonValueCalculator : EpsilonValueCalculator
    {
        public override double CalcValueFromAvgResponseTime(double v)
        {
            return LogEpsilonValueCalculator(v);
        }
    }

    public class PolynomialEpsilonValueCalculator : EpsilonValueCalculator
    {
        private readonly double exponent;

        public PolynomialEpsilonValueCalculator(double exponent)
        {
            this.exponent = exponent;
        }

        public override double CalcValueFromAvgResponseTime(double v)
        {
            return PolynomialEpsilonValueCalculator(v, exponent);
        }
    }
}