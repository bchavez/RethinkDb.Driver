using System;

namespace RethinkDb.Driver.Net.Clustering
{
    /// <summary>
    /// Classes implementing this interface are used to convert the average response time for a host
    /// into a score that can be used to weight hosts in the epsilon greedy hostpool. Lower response
    /// times should yield higher scores (we want to select the faster hosts more often) The default
    /// LinearEpsilonValueCalculator just uses the reciprocal of the response time. In practice, any
    /// decreasing function from the positive reals to the positive reals should work.
    /// </summary>
    public abstract class EpsilonValueCalculator
    {
        /// <summary>
        /// The implementation of the value calculation.
        /// </summary>
        public abstract float CalcValueFromAvgResponseTime(float v);

        /// <summary>
        /// Linear value calculation
        /// </summary>
        public static float LinearEpsilonValueCalculator(float v)
        {
            return 1.0f / v;
        }

        /// <summary>
        /// Logarithmic value calculation
        /// </summary>
        public static float LogEpsilonValueCalculator(float v)
        {
            return LinearEpsilonValueCalculator(Convert.ToSingle(Math.Log(v + 1.0d)));
        }

        /// <summary>
        /// Polynomial value calculation
        /// </summary>
        public static float PolynomialEpsilonValueCalculator(float v, float exp)
        {
            return LinearEpsilonValueCalculator(Convert.ToSingle(Math.Pow(v, exp)));
        }
    }

    /// <summary>
    /// Linear epsilon value calculation
    /// </summary>
    public class LinearEpsilonValueCalculator : EpsilonValueCalculator
    {
        /// <summary>
        /// The implementation of the value calculation.
        /// </summary>
        public override float CalcValueFromAvgResponseTime(float v)
        {
            return LinearEpsilonValueCalculator(v);
        }
    }

    /// <summary>
    /// Logarithmic epsilon value calculation
    /// </summary>
    public class LogEpsilonValueCalculator : EpsilonValueCalculator
    {
        /// <summary>
        /// The implementation of the value calculation.
        /// </summary>
        public override float CalcValueFromAvgResponseTime(float v)
        {
            return LogEpsilonValueCalculator(v);
        }
    }

    /// <summary>
    /// Polynomial epsilon value calculation
    /// </summary>
    public class PolynomialEpsilonValueCalculator : EpsilonValueCalculator
    {
        private readonly float exponent;

        /// <summary>
        /// Constructor with exponent
        /// </summary>
        public PolynomialEpsilonValueCalculator(float exponent)
        {
            this.exponent = exponent;
        }

        /// <summary>
        /// The implementation of the value calculation.
        /// </summary>
        public override float CalcValueFromAvgResponseTime(float v)
        {
            return PolynomialEpsilonValueCalculator(v, exponent);
        }
    }


    /// <summary>
    /// Classes implementing this interface are used to convert the average response time for a host
    /// into a score that can be used to weight hosts in the epsilon greedy hostpool. Lower response
    /// times should yield higher scores (we want to select the faster hosts more often) The default
    /// LinearEpsilonValueCalculator just uses the reciprocal of the response time. In practice, any
    /// decreasing function from the positive reals to the positive reals should work.
    /// </summary>
    public static class EpsilonCalculator
    {
        /// <summary>
        /// Linear calculator to convert the average response time for a host
        /// into a score that can be used to weight hosts in the epsilon greedy hostpool. Lower response
        /// times should yield higher scores (we want to select the faster hosts more often) The default
        /// LinearEpsilonValueCalculator just uses the reciprocal of the response time. In practice, any
        /// decreasing function from the positive reals to the positive reals should work.
        /// </summary>
        public static EpsilonValueCalculator Linear()
        {
            return new LinearEpsilonValueCalculator();
        }


        /// <summary>
        /// Logarithmic calculator to convert the average response time for a host
        /// into a score that can be used to weight hosts in the epsilon greedy hostpool. Lower response
        /// times should yield higher scores (we want to select the faster hosts more often) The default
        /// LinearEpsilonValueCalculator just uses the reciprocal of the response time. In practice, any
        /// decreasing function from the positive reals to the positive reals should work.
        /// </summary>
        public static EpsilonValueCalculator Logarithmic()
        {
            return new LogEpsilonValueCalculator();
        }


        /// <summary>
        /// Polynomial calculator to convert the average response time for a host
        /// into a score that can be used to weight hosts in the epsilon greedy hostpool. Lower response
        /// times should yield higher scores (we want to select the faster hosts more often) The default
        /// LinearEpsilonValueCalculator just uses the reciprocal of the response time. In practice, any
        /// decreasing function from the positive reals to the positive reals should work.
        /// </summary>
        public static EpsilonValueCalculator Polynomial(float exponent)
        {
            return new PolynomialEpsilonValueCalculator(exponent);
        }
    }
}