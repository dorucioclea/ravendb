using System;
using System.Linq;
using Newtonsoft.Json;
using Raven.Client.Documents.Session.TimeSeries;

namespace Raven.Client.Documents.Queries.TimeSeries
{
    public class TimeSeriesQueryResult
    {
        public long Count { get; set; }
    }

    public class TimeSeriesRawResult : TimeSeriesQueryResult
    {
        public TimeSeriesEntry[] Results { get; set; }
    }

    public class TimeSeriesRawResult<TValues> : TimeSeriesRawResult where TValues : TimeSeriesEntry
    {
        public new TValues[] Results { get; set; }
    }

    public class TimeSeriesAggregationResult : TimeSeriesQueryResult
    {
        public TimeSeriesRangeAggregation[] Results { get; set; }
    }

    public class TimeSeriesRangeAggregation
    {
        public long[] Count;
        public double?[] Max, Min, Last, First, Average;
        public DateTime To, From;
    }

    public class TimeSeriesAggregationResult<T> : TimeSeriesAggregationResult where T : ITimeSeriesValues, new()
    {
        public new TimeSeriesRangeAggregation<T>[] Results { get; set; }
    }

    public class TimeSeriesRangeAggregation<T> : TimeSeriesRangeAggregation where T : ITimeSeriesValues, new()
    {
        private T _max;
        private T _min;
        private T _last;
        private T _first;
        private T _average;
        private T _count;

        [JsonIgnore]
        public new T Max
        {
            get
            {
                if (base.Max == null)
                    return default;

                _max ??= new T {Values = Array.ConvertAll(base.Max, x => x ?? 0)};
                return _max;
            }
        }

        [JsonIgnore]
        public new T Min
        {
            get
            {
                if (base.Min == null)
                    return default;

                _min ??= new T {Values = Array.ConvertAll(base.Min, x => x ?? 0)};
                return _min;
            }
        }

        [JsonIgnore]
        public new T Last
        {
            get
            {
                if (base.Last == null)
                    return default;

                _last ??= new T {Values = Array.ConvertAll(base.Last, x => x ?? 0)};
                return _last;
            }
        }

        [JsonIgnore]
        public new T First 
        {
            get
            {
                if (base.First == null)
                    return default;

                _first ??= new T {Values = Array.ConvertAll(base.First, x => x ?? 0)};
                return _first;
            }
        }

        [JsonIgnore]
        public new T Average 
        {
            get
            {
                if (base.Average == null)
                    return default;

                _average ??= new T {Values = Array.ConvertAll(base.Average, x => x ?? 0)};
                return _average;
            }
        }

        [JsonIgnore]
        public new T Count
        {
            get
            {
                if (base.Count == null)
                    return default;

                _count ??= new T {Values = Array.ConvertAll<long, double>(base.Count, x => x)};
                return _count;
            }
        }
    }

    public enum AggregationType
    {
        // The order here matters.
        // When executing an aggregation query over rolled-up series,
        // we take just the appropriate aggregated value from each entry, 
        // according to the aggregation's position in this enum (e.g. AggregationType.Min => take entry.Values[2])

        First = 0,
        Last = 1,
        Min = 2,
        Max = 3,
        Sum = 4,
        Count = 5,
        Average = 6
    }
}
