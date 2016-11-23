using System;
using System.Collections;
using HermesMiddleware.DataAcquisitionServiceServer;

namespace DataAcquisitionService
{
	/// <summary>
	/// Summary description for EventSubscription.
	/// </summary>
	[Serializable]
	public class EventSubscription
	{
		// name
		public string sName;

		// variable
		public string sVariable;

		// conditions
		public ArrayList conditions = new ArrayList();

		// GUID
		public string sGUID;

		public EventSubscription()
		{
		}

		// initialize
		public void Initialize(HermesMiddleware.DataAcquisitionServiceServer.Subscription subscription)
		{
			// copy simple data
			sVariable = subscription.variable;
			sName = subscription.name;
			sGUID = subscription.GUID;

			foreach (HermesMiddleware.DataAcquisitionServiceServer.Expression expression in subscription.Items)
			{
				// we use just range everywhere
				HermesMiddleware.DataAcquisitionServiceServer.Range rangeSource = (HermesMiddleware.DataAcquisitionServiceServer.Range)expression;

				// create new range
				Range rangeTarget = new Range(rangeSource);

				// add it to the elements
				conditions.Add(rangeTarget);
			}
		}
	}

	/// <summary>
	/// Summary description for Condition.
	/// </summary>
	[Serializable]
	public abstract class Condition
	{
		// variable
		public string sVariable; 

		// test value match
        public abstract bool Match(object oValue);
	}

	/// <summary>
	/// Range
	/// </summary>
	[Serializable]
	public class Range : Condition
	{
		// min/max range
		public object min, max;

		// strictness
		public bool bStrict;

		public Range()
		{
		}

		// copy constructor
		public Range(HermesMiddleware.DataAcquisitionServiceServer.Range sourceRange)
		{
			this.min = sourceRange.min;
			this.max = sourceRange.max;
			this.bStrict = sourceRange.strict;
			this.sVariable = sourceRange.variable;
		}

		// test value match
		public override bool Match(object oValue)
		{

			IComparable icValue = (IComparable)oValue;
			IComparable icMin   = (IComparable)min;
			IComparable icMax   = (IComparable)max;

			int maxComparison;
			int minComparison;

			if (max == null)
			{
				//No maximum limit
				maxComparison = -1;
			}
			else {

				maxComparison = icValue.CompareTo(icMax);
			}


			if (min == null)
			{
				//No minimum limit
				minComparison = 1;
			}
			else {

				minComparison = icValue.CompareTo(icMin);
			}


			if (bStrict)
			{
				return minComparison > 0 && maxComparison < 0;
			}
			else
			{
				return minComparison >= 0 && maxComparison <= 0;
			}
		}
	}

	/// <summary>
	/// Threshold
	/// </summary>
	[Serializable]
	public class Threshold : Condition
	{
		// threshold value
		public object oValue;

		// strictness
		public bool bStrict;

		// above/bellow
		public ThresholdType type;

		public Threshold()
		{
		}

		// copy constructor
		public Threshold(HermesMiddleware.DataAcquisitionServiceServer.Threshold sourceThreshold)
		{
			this.oValue = sourceThreshold.value;
			this.bStrict = sourceThreshold.strict;
			this.type = sourceThreshold.type;
			this.sVariable = sourceThreshold.variable;
		}

		// test value match
		public override bool Match(object oValue)
		{
			IComparable icValue = (IComparable)oValue;
			IComparable icThisValue = (IComparable)this.oValue;

			int nComparison = icValue.CompareTo(icThisValue);

			if (type == ThresholdType.above)
			{
				if (bStrict)
				{
					return nComparison > 0;
				}
				else
				{
					return nComparison >= 0;
				}
			}
			else
			{
				if (bStrict)
				{
					return nComparison < 0;
				}
				else
				{
					return nComparison <= 0;
				}
			}
		}
	}

	/// <summary>
	/// Equals
	/// </summary>
	[Serializable]
	public class Equals : Condition
	{
		// value
		public object oValue;

		public Equals()
		{
		}

		// copy constructor
		public Equals(HermesMiddleware.DataAcquisitionServiceServer.Equals sourceEquals)
		{
			this.oValue = sourceEquals.value;
			this.sVariable = sourceEquals.variable;
		}

		// test value match
		public override bool Match(object oValue)
		{

			IComparable icValue = (IComparable)oValue;
			IComparable icThisValue = (IComparable)this.oValue;

			return icValue.CompareTo(icThisValue) == 0;
		}
	}

}
